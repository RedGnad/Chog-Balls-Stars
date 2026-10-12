namespace TwinStickShooter
{
  using UnityEngine.InputSystem;
  using TMPro;
  using Quantum;

  public unsafe class TeamStrategyUI : QuantumSceneViewComponent<CustomViewContext>
  {
    public static bool IsEnabled = false;

    public TextMeshProUGUI LeftTeamStrategy;
    public TextMeshProUGUI RightTeamStrategy;

    private PlayerInput _playerInput;

    private void Start()
    {
      _playerInput = FindObjectOfType<PlayerInput>();

      LeftTeamStrategy.enabled = IsEnabled;
      RightTeamStrategy.enabled = IsEnabled;
    }

    private void Update()
    {
      if (_playerInput.actions["ActivateTacticsDebug"].WasPressedThisFrame() == true)
      {
        IsEnabled = !IsEnabled;
        LeftTeamStrategy.enabled = IsEnabled;
        RightTeamStrategy.enabled = IsEnabled;

        //TODO fix improve
        var tacticsUI = FindObjectsOfType<TacticsUI>();
        foreach (var ui in tacticsUI)
        {
          ui.SetActiveState();
        }
      }
    }

    public override unsafe void OnUpdateView()
    {
      if (IsEnabled == false)
      {
        return;
      }

      if (ViewContext.LocalView == null)
      {
        return;
      }

      var teamsData = VerifiedFrame.ResolveList(VerifiedFrame.Global->TeamsData);

      TeamInfo teamInfo = VerifiedFrame.Get<TeamInfo>(ViewContext.LocalView.EntityRef);

      TeamData leftTeamData = teamInfo.Index == 0 ? teamsData[0] : teamsData[1];
      TeamData rightTeamData = teamInfo.Index == 0 ? teamsData[1] : teamsData[0];

      string leftTeamStrategy = string.Format(
        "Fight: {0}\n" +
        "Score: {1}\n" +
        "Run: {2}"
        , leftTeamData.StrategyFightActivated == true ? "O" : "X"
        , leftTeamData.StrategyScoreActivated == true ? "O" : "X"
        , leftTeamData.StrategyRunActivated == true ? "O" : "X");

      string rightTeamStrategy = string.Format(
        "Fight: {0}\n" +
        "Score: {1}\n" +
        "Run: {2}"
        , rightTeamData.StrategyFightActivated == true ? "O" : "X"
        , rightTeamData.StrategyScoreActivated == true ? "O" : "X"
        , rightTeamData.StrategyRunActivated == true ? "O" : "X");

      LeftTeamStrategy.text = leftTeamStrategy;
      RightTeamStrategy.text = rightTeamStrategy;
    }
  }
}