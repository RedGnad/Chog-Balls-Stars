namespace TwinStickShooter
{
  using Photon.Deterministic;
  using Quantum;
  using System.Collections;
  using UnityEngine;

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

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      _initialInterpolationSpeed = InterpolationSpeed;
      QuantumEvent.Subscribe<EventArenaPresentation>(this, OnArenaPresentation);
      QuantumEvent.Subscribe<EventCountdownStarted>(this, OnCountdownStarted);
      QuantumEvent.Subscribe<EventCountdownStopped>(this, OnCountdownStopped);
    }

    public override void OnDeactivate()
    {
      base.OnDeactivate();
      QuantumEvent.UnsubscribeListener(this);
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

      transform.position = InitialPosition;
      if (_isCameraInverted == true)
      {
        transform.position = new Vector3(transform.position.x, transform.position.y, -transform.position.z);
      }

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

      transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * InterpolationSpeed);
    }
  }
}