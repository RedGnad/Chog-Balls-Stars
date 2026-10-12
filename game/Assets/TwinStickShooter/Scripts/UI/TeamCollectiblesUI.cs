namespace TwinStickShooter
{
  using UnityEngine;
  using TMPro;
  using UnityEngine.UI;
  using Quantum;

  public class TeamCollectiblesUI : QuantumSceneViewComponent<CustomViewContext>
  {
    public TextMeshProUGUI LeftScoreText;
    public TextMeshProUGUI RightScoreText;

    public Image LeftBar;
    public Image RightBar;

    public Animator LeftBarAnimator;
    public Animator RightBarAnimator;

    public override unsafe void OnUpdateView()
    {
      if (ViewContext.LocalView == null)
      {
        return;
      }

      var teamsData = VerifiedFrame.ResolveList(VerifiedFrame.Global->TeamsData);

      if (VerifiedFrame.TryGet<TeamInfo>(ViewContext.LocalView.EntityRef, out var teamInfo) == false)
      {
        return;
      }

      int leftTeamScore = teamInfo.Index == 0 ? teamsData[0].Score : teamsData[1].Score;
      int rightTeamScore = teamInfo.Index == 0 ? teamsData[1].Score : teamsData[0].Score;

      LeftScoreText.text = leftTeamScore.ToString();
      RightScoreText.text = rightTeamScore.ToString();

      LeftBar.fillAmount = leftTeamScore / 10f;
      RightBar.fillAmount = rightTeamScore / 10f;

      if (leftTeamScore >= 10 && leftTeamScore > rightTeamScore)
      {
        LeftBarAnimator.SetBool("IsShining", true);
      }
      else if (rightTeamScore >= 10 && rightTeamScore > leftTeamScore)
      {
        RightBarAnimator.SetBool("IsShining", true);
      }
      else
      {
        LeftBarAnimator.SetBool("IsShining", false);
        RightBarAnimator.SetBool("IsShining", false);
      }
    }
  }
}