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

      // In a siege the score is the damage done to the other team's tower and core, in hundreds of
      // health (MatchModes): the bar fills as they fall. The coins fill it at ten.
      bool siege = MatchModes.IsSiege(VerifiedFrame);
      float full = siege ? ((Structures.TowerHealth + Structures.CoreHealth) / MatchModes.HealthPerSiegePoint).AsFloat : 10f;
      LeftBar.fillAmount = leftTeamScore / full;
      RightBar.fillAmount = rightTeamScore / full;

      if (siege)
      {
        LeftBarAnimator.SetBool("IsShining", false);
        RightBarAnimator.SetBool("IsShining", false);
      }
      else if (leftTeamScore >= 10 && leftTeamScore > rightTeamScore)
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