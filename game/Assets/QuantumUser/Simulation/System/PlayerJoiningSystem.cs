using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
  [Preserve]
  public unsafe class PlayerJoiningSystem : SystemMainThread, ISignalOnPlayerAdded,
    ISignalOnPlayerDisconnected
  {
    public void OnPlayerAdded(Frame frame, PlayerRef player, bool firstTime)
    {
      // Create the player character based on a prototype defined on the Menu
      RuntimePlayer playerData = frame.GetPlayerData(player);
      if (playerData.PlayerAvatar == null)
        return;

      frame.Signals.OnCreatePlayerCharacter(player, playerData.PlayerAvatar);
    }

    public override void OnInit(Frame frame)
    {
      // Prepare the list of teams which is stored on the frame's Global variables
      var teamsData = frame.AllocateList<TeamData>();
      teamsData.Add(new TeamData());
      teamsData.Add(new TeamData());
      frame.Global->TeamsData = teamsData;
    }

    public override void Update(Frame frame)
    {
      if (frame.Global->State == GameState.Playing && frame.RuntimeConfig.FillWithBots)
      {
        CheckRoomFillInterval(frame);
      }
    }

    private void CheckRoomFillInterval(Frame frame)
    {
      if (frame.Global->TimeToFillWithBots < frame.RuntimeConfig.RoomFillInterval)
      {
        frame.Global->TimeToFillWithBots += frame.DeltaTime;

        if (frame.Global->TimeToFillWithBots > frame.RuntimeConfig.RoomFillInterval)
        {
          AISetupHelper.FillWithBots(frame);
        }
      }
    }

    public void OnPlayerDisconnected(Frame frame, PlayerRef player)
    {
      if (frame.Global->State == GameState.CharacterSelection)
      {
        frame.Signals.OnDestroyPlayerCharacter(player);
      }
    }
  }
}