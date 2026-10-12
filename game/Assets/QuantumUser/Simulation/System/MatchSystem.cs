using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
  [Preserve]
  public unsafe class MatchSystem : SystemMainThread, ISignalOnGameOver
  {
    // The game manager HFSM triggers this signal and we store the game state
    // so other systems know if the game is already over
    public void OnGameOver(Frame frame, QBoolean value)
    {
      if (value == true)
      {
        frame.Global->State = GameState.Over;
        frame.SystemDisable<GameplaySystemsGroup>();
      }
      else
      {
        frame.Global->State = GameState.Playing;
        frame.SystemEnable<GameplaySystemsGroup>();
      }
    }

    // Update the match duration, which is polled on the Unity side to set the timer on the UI
    public override void Update(Frame frame)
    {
      if (frame.Global->State == GameState.Playing)
      {
        frame.Global->MatchDuration += frame.DeltaTime;
      }
    }
  }
}