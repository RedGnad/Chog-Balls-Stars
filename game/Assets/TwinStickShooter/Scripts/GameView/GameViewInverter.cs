namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;

  public class GameViewInverter : QuantumSceneViewComponent<CustomViewContext>
  {
    [SerializeField] private GameObject TeamALight;
    [SerializeField] private GameObject TeamBLight;

    private TopDownInput _topDownInput;
    private LocalPlayerCameraFollow _cameraFollow;
    private bool _hasInvested = false;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      QuantumEvent.Subscribe<EventFinishCharacterSelection>(this, OnFinishCharacterSelection);
      _cameraFollow = FindObjectOfType<LocalPlayerCameraFollow>();
      _topDownInput = FindObjectOfType<TopDownInput>();
    }

    private void OnFinishCharacterSelection(EventFinishCharacterSelection e)
    {
      TeamInfo teamInfo = VerifiedFrame.Get<TeamInfo>(ViewContext.LocalView.EntityRef);
      UpdateLocalCameraAndHud(teamInfo);
    }

    private void UpdateLocalCameraAndHud(TeamInfo teamInfo)
    {
      if (_hasInvested)
      {
        return;
      }

      _hasInvested = true;

      if (teamInfo.Index != 0)
      {
        _topDownInput.IsInverseControl = true;
        _cameraFollow.InvertCamera();
        TeamALight.SetActive(false);
        TeamBLight.SetActive(true);
      }
      else
      {
        _topDownInput.IsInverseControl = false;
        TeamALight.SetActive(true);
        TeamBLight.SetActive(false);
      }
    }
  }
}