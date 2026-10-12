namespace TwinStickShooter
{
  using Quantum;
  using TMPro;

  public unsafe class TacticsUI : QuantumCallbacks
  {
    public TextMeshProUGUI TacticText;

    private void Start()
    {
      SetActiveState();
    }

    public void SetActiveState()
    {
      TacticText.enabled = TeamStrategyUI.IsEnabled;
    }
  }
}