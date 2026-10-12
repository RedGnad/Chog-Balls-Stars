namespace TwinStickShooter
{
  using TMPro;
  using Quantum;
  using System;

  public class MatchDurationUI : QuantumCallbacks
  {
    public TextMeshProUGUI TimerText;

    public override unsafe void OnUpdateView(QuantumGame game)
    {
      Frame frame = game.Frames.Verified;

      float matchDuration = frame.Global->MatchDuration.AsFloat;
      var minutes = TimeSpan.FromSeconds(matchDuration).Minutes;
      var seconds = TimeSpan.FromSeconds(matchDuration).Seconds;
      TimerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }
  }
}