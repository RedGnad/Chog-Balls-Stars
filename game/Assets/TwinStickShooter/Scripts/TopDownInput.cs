namespace TwinStickShooter
{
  using Photon.Deterministic;
  using Quantum;
  using UnityEngine;
  using System.Collections.Generic;
  using UnityEngine.EventSystems;
  using UnityEngine.InputSystem;
  using Chog.Controls;

  [RequireComponent(typeof(PlayerInput))]
  public class TopDownInput : MonoBehaviour
  {
    public FP AimSensitivity = 5;
    public CustomViewContext ViewContext;
    
    private AttackPreview _attackPreview;
    private PlayerInput _playerInput;
    private CombatInputReader _reader;
    private bool _focused = true;
    private bool _paused;
    private PointerEventData _pointerEvent;
    private EventSystem _pointerEventSystem;
    private readonly List<RaycastResult> _pointerHits = new List<RaycastResult>();

    public bool IsInverseControl { get; set; } = false;

    private void Awake()
    {
      _playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
      // PlayerInput has initialized its per-player action asset by this point.
      _reader = new CombatInputReader(_playerInput.actions);
    }

    private void OnEnable()
    {
      CharacterView.OnLocalPlayerInstantiated += OnLocalPlayerInstantiated;
      QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    private void OnDisable()
    {
      CharacterView.OnLocalPlayerInstantiated -= OnLocalPlayerInstantiated;
      QuantumCallback.UnsubscribeListener(this);
      ResetInput();
    }


    private void OnLocalPlayerInstantiated(CharacterView playerView)
    {
      if (_attackPreview != null)
      {
        Destroy(_attackPreview.gameObject);
      }
      _attackPreview = playerView.GetComponentInChildren<AttackPreview>(true);
      if (_attackPreview != null) _attackPreview.transform.parent = null;
    }

    private void OnApplicationFocus(bool focused)
    {
      _focused = focused;
      if (!focused) ResetInput();
    }

    private void OnApplicationPause(bool paused)
    {
      _paused = paused;
      if (paused) ResetInput();
    }

    private void ResetInput()
    {
      _reader?.Reset();
      if (_attackPreview != null) _attackPreview.gameObject.SetActive(false);
    }

    public void PollInput(CallbackPollInput callback)
    {
      QuantumDemoInputTopDown input = default;
      bool pointerAim = false;
      Vector2 pointer = default;
      if (_reader != null && _focused && !_paused)
      {
        var state = _reader.Read(MobileButtonsManager.TouchControlsVisible);
        var move = state.Move.ToFPVector2();
        input.MoveDirection = IsInverseControl ? -move : move;
        input.Fire = state.Fire;
        input.AltFire = state.AltFire;
        // Aimed with the mouse, an attack leaves on the press; with a stick or a pad, on the release.
        pointerAim = !state.UseStickAim;
        pointer = state.PointerPosition;
        input.AimsWithPointer = pointerAim;

        if (state.UseStickAim)
        {
          var aim = state.StickAim.ToFPVector2() * AimSensitivity;
          input.AimDirection = IsInverseControl ? -aim : aim;
        }
        else
        {
          input.AimDirection = GetDirectionToMouse(state.PointerPosition);
        }
      }

      if (_attackPreview != null)
      {
        // A stick shows its aim while it is held, as the attack leaves on the release. A mouse
        // attacks on the press, so its aim shows all the time, as a top-down shooter's reticle;
        // the owner found a lob otherwise "impossible to aim" (17 October).
        bool show = input.AimDirection != FPVector2.Zero
                    && (pointerAim ? PointerAimShown(callback.Game, pointer) : input.Fire || input.AltFire);
        _attackPreview.gameObject.SetActive(show);
        if (show) _attackPreview.UpdateAttackPreview(input.AimDirection, input.AltFire);
        else Chog.Match.ChogAimGuide.Hide();
      }
      callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }

    // The aim of a mouse shows while this player's hero is alive in a match under way, and not
    // while the pointer is on a button of the screen.
    private unsafe bool PointerAimShown(QuantumGame game, Vector2 pointer)
    {
      Frame frame = game != null ? game.Frames.Predicted : null;
      if (frame == null || frame.Global->State != GameState.Playing || ViewContext == null || ViewContext.LocalView == null)
        return false;
      if (frame.TryGet<Health>(ViewContext.LocalView.EntityRef, out var health) && health.IsDead)
        return false;
      return !OverButton(pointer);
    }

    // Only a button counts: a screen-wide layer that catches the pointer must not hide the aim.
    private bool OverButton(Vector2 pointer)
    {
      var events = EventSystem.current;
      if (events == null)
        return false;
      if (_pointerEvent == null || _pointerEventSystem != events)
      {
        _pointerEvent = new PointerEventData(events);
        _pointerEventSystem = events;
      }
      _pointerEvent.position = pointer;
      _pointerHits.Clear();
      events.RaycastAll(_pointerEvent, _pointerHits);
      foreach (var hit in _pointerHits)
        if (hit.gameObject != null && hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
          return true;
      return false;
    }

    private FPVector2 GetDirectionToMouse(Vector2 mousePosition)
    {
      if (QuantumRunner.Default == null || QuantumRunner.Default.Game == null)
        return default;

      Frame frame = QuantumRunner.Default.Game.Frames.Predicted;
      if (frame == null)
        return default;

      if (ViewContext == null || ViewContext.LocalView == null ||
          frame.Exists(ViewContext.LocalView.EntityRef) == false || Camera.main == null)
        return default;
      

      FPVector2 localCharacterPosition = frame.Get<Transform2D>(ViewContext.LocalView.EntityRef).Position;

      Ray ray = Camera.main.ScreenPointToRay(mousePosition);
      UnityEngine.Plane plane = new UnityEngine.Plane(Vector3.up, Vector3.zero);

      if (plane.Raycast(ray, out var enter))
      {
        var dirToMouse = ray.GetPoint(enter).ToFPVector2() - localCharacterPosition;
        return dirToMouse;
      }

      return default;
    }
  }
}
