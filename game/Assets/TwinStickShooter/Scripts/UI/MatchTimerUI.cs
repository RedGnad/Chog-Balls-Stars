namespace TwinStickShooter
{
  using TMPro;
  using Quantum;

  public class MatchTimerUI : QuantumCallbacks
  {
    public TextMeshProUGUI TimerText;

    public override unsafe void OnUpdateView(QuantumGame game)
    {
      Frame frame = game.Frames.Verified;

      float matchTime = frame.Global->MatchTimer.AsFloat;
      TimerText.text = ((int)matchTime).ToString();
    }
  }
}