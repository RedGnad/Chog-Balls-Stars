namespace TwinStickShooter
{
  using Chog.Match;
  using Photon.Deterministic;
  using Quantum;
  using System.Collections;
  using UnityEngine;

  /// <summary>
  /// The match camera follows the local hero from above. Its angle, field of view and offset come from
  /// Chog.Match.ChogCameraRig when a page asks for another angle than the sample's (the prefab's values
  /// otherwise). The place it follows is kept on its own (the lerped base); a light shake
  /// (Chog.Match.ChogCameraShake: the square of a trauma that decays) is added on top of it each frame, so
  /// the shake never drags the follow, and the camera keeps shaking while the local hero is down.
  /// </summary>
  public sealed class LocalPlayerCameraFollow : QuantumSceneViewComponent<CustomViewContext>
  {
    public float InterpolationSpeed = 1;
    public Vector3 BaseOffset;
    public float MaxZ, MinZ;
    public float InvertedMaxZ, InvertedMinZ;

    public Vector3 InitialPosition;
    public bool CanMove;
    public bool LockXCoordinate = true;
    public float InitialDelay = 2;
    public float GameStartDelay = 3;

    public GameObject GamePresentationUI;
    public GameObject TimerUI;

    private float _initialInterpolationSpeed;
    private bool _isCameraInverted;

    // Where the camera would be without the shake.
    private Vector3 _basePosition;
    private bool _rigApplied;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      ApplyRig();
      _initialInterpolationSpeed = InterpolationSpeed;
      _basePosition = transform.position;
      ChogCameraShake.Clear();
      QuantumEvent.Subscribe<EventArenaPresentation>(this, OnArenaPresentation);
      QuantumEvent.Subscribe<EventCountdownStarted>(this, OnCountdownStarted);
      QuantumEvent.Subscribe<EventCountdownStopped>(this, OnCountdownStopped);
    }

    public override void OnDeactivate()
    {
      base.OnDeactivate();
      QuantumEvent.UnsubscribeListener(this);
    }

    // The angle a page asked for (ChogCameraRig), once, before the camera of team 1 is turned around.
    private void ApplyRig()
    {
      if (_rigApplied || !ChogCameraRig.Overridden)
      {
        return;
      }

      _rigApplied = true;
      BaseOffset = ChogCameraRig.FollowOffset;
      MinZ = ChogCameraRig.ShiftBound(MinZ, false);
      MaxZ = ChogCameraRig.ShiftBound(MaxZ, false);
      InvertedMinZ = ChogCameraRig.ShiftBound(InvertedMinZ, true);
      InvertedMaxZ = ChogCameraRig.ShiftBound(InvertedMaxZ, true);
      InitialPosition = ChogCameraRig.PresentationPosition(InitialPosition);
      transform.rotation = Quaternion.Euler(ChogCameraRig.Pitch, transform.rotation.eulerAngles.y, 0);
      var view = GetComponentInChildren<Camera>();
      if (view != null)
      {
        view.fieldOfView = ChogCameraRig.FieldOfView;
      }
    }

    private void OnArenaPresentation(EventArenaPresentation ev)
    {
      StartCoroutine(PresentArenaCoroutine());
    }

    private void OnCountdownStarted(EventCountdownStarted ev)
    {
      if (TimerUI != null)
      {
        TimerUI.SetActive(true);
      }
    }

    private void OnCountdownStopped(EventCountdownStopped ev)
    {
      if (TimerUI != null)
      {
        TimerUI.SetActive(false);
      }
    }

    private IEnumerator PresentArenaCoroutine()
    {
      CanMove = false;
      yield return new WaitForSeconds(.5f);

      _basePosition = InitialPosition;
      if (_isCameraInverted == true)
      {
        _basePosition = new Vector3(_basePosition.x, _basePosition.y, -_basePosition.z);
      }
      transform.position = _basePosition;

      if (GamePresentationUI != null)
      {
        GamePresentationUI.SetActive(true);
      }

      bool showIntroduction = Game.Configurations.Runtime.ShowIntroduction;
      if (showIntroduction == true)
      {
        yield return new WaitForSeconds(InitialDelay);
      }

      CanMove = true;

      if (GamePresentationUI != null)
      {
        GamePresentationUI.SetActive(false);
      }

      InterpolationSpeed = 2;

      if (showIntroduction == true)
      {
        yield return new WaitForSeconds(GameStartDelay);
      }

      InterpolationSpeed = _initialInterpolationSpeed;
    }

    public void InvertCamera()
    {
      _isCameraInverted = true;
      transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, 180, 0);
      BaseOffset.z = -BaseOffset.z;
    }

    public override void OnUpdateView()
    {
      base.OnUpdateView();

      FollowLocalHero();

      // The shake on top of the place followed, in the camera's own axes.
      var shake = ChogCameraShake.Step(Time.deltaTime, Time.time);
      transform.position = _basePosition + transform.right * shake.x + transform.up * shake.y;
    }

    private void FollowLocalHero()
    {
      if (CanMove == false || ViewContext?.LocalView == null)
      {
        return;
      }

      if (VerifiedFrame.TryGet<Health>(ViewContext.LocalView.EntityRef, out var health) == false || health.IsDead)
      {
        return;
      }

      Vector3 targetPosition = ViewContext.LocalView.transform.position + BaseOffset;
      if (_isCameraInverted == true)
      {
        targetPosition.z = Mathf.Clamp(targetPosition.z, InvertedMinZ, InvertedMaxZ);
      }
      else
      {
        targetPosition.z = Mathf.Clamp(targetPosition.z, MinZ, MaxZ);
      }

      if (LockXCoordinate)
      {
        targetPosition.x = 0;
      }

      _basePosition = Vector3.Lerp(_basePosition, targetPosition, Time.deltaTime * InterpolationSpeed);
    }
  }
}
