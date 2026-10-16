namespace TwinStickShooter
{
  using Photon.Deterministic;
  using Quantum;
  using UnityEngine;
  using UnityEngine.InputSystem;
  using Chog.Controls;
  using Chog.Match;

  [RequireComponent(typeof(PlayerInput))]
  public class TopDownInput : MonoBehaviour
  {
    public FP AimSensitivity = 5;
    public CustomViewContext ViewContext;
    
    private AttackPreview _attackPreview;
    private PlayerInput _playerInput;
    private CombatInputReader _reader;
    private PressPulse _firePulse, _specialPulse;
    // What a held mouse button sent, written to the console once it is let go (CHOG_INPUT).
    private int _heldFrom, _heldTicks, _sentDown, _askedAgain, _lastTick = -1, _reports;
    private bool _focused = true;
    private bool _paused;

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
      // While a button is held, whatever the pulses send.
      bool showPreview = false, previewSpecial = false;
      if (_reader != null && _focused && !_paused)
      {
        var state = _reader.Read(MobileButtonsManager.TouchControlsVisible);
        var move = state.Move.ToFPVector2();
        input.MoveDirection = IsInverseControl ? -move : move;
        // The mouse attacks on the press (PressPulse); a stick or a pad on the release, as before.
        bool firePulse = _firePulse.Next(state.PointerFire, callback.Frame);
        bool specialPulse = _specialPulse.Next(state.PointerSpecial, callback.Frame);
        input.Fire = state.PointerFire ? firePulse : state.Fire;
        input.AltFire = state.PointerSpecial ? specialPulse : state.AltFire;
        NoteMouse(state.PointerFire || state.PointerSpecial, callback.Frame, firePulse || specialPulse);
        showPreview = state.Fire || state.AltFire;
        previewSpecial = state.AltFire;

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
        bool show = showPreview && input.AimDirection != FPVector2.Zero;
        _attackPreview.gameObject.SetActive(show);
        if (show) _attackPreview.UpdateAttackPreview(input.AimDirection, previewSpecial);
      }
      callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }

    private void NoteMouse(bool held, int tick, bool down)
    {
      if (held)
      {
        if (_heldTicks == 0) _heldFrom = tick;
        if (tick == _lastTick) _askedAgain++;
        else
        {
          _heldTicks++;
          if (down) _sentDown++;
        }
      }
      else if (_heldTicks > 0)
      {
        if (_reports++ < 20)
          Debug.Log("CHOG_INPUT mouse held from tick " + _heldFrom + " for " + _heldTicks + " ticks, sent down " +
                    _sentDown + ", ticks asked again " + _askedAgain);
        _heldTicks = _sentDown = _askedAgain = 0;
      }
      _lastTick = tick;
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
