namespace Quantum
{
  [System.Serializable]
  public unsafe class TriggerQuantumEvent : AIAction
  {
    public enum QuantumEvent
    {
      None,
      ArenaPresentation,
      CountdownStarted,
      CountdownStopped,
      GameOver
    }

    public QuantumEvent Event;

    public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
    {
      switch (Event)
      {
        case QuantumEvent.ArenaPresentation:
          frame.Events.ArenaPresentation();
          break;
        case QuantumEvent.CountdownStarted:
          frame.Events.CountdownStarted();
          break;
        case QuantumEvent.CountdownStopped:
          frame.Events.CountdownStopped();
          break;
        case QuantumEvent.GameOver:
          int winnerTeam = 0;
          var teamsData = frame.ResolveList(frame.Global->TeamsData);
          winnerTeam = teamsData[0].Score > teamsData[1].Score ? 0 : 1;
          // A core that fell decides the match before any score.
          if (Structures.WinnerByCore(frame) >= 0)
          {
            winnerTeam = Structures.WinnerByCore(frame);
          }
          frame.Events.GameOver(winnerTeam);
          break;
        default:
          break;
      }
    }
  }
}