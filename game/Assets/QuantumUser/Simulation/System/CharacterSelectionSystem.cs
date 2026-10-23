namespace Quantum
{
  using Photon.Deterministic;
  using UnityEngine.Scripting;

  [Preserve]
  public unsafe class CharacterSelectionSystem : SystemSignalsOnly,
    ISignalOnCharacterSelectionStart,
    ISignalOnCommandFinishCharacterSelection,
    ISignalOnCreatePlayerCharacter,
    ISignalOnChangePlayerTeam,
    ISignalOnDestroyPlayerCharacter
  {
    public void OnCharacterSelectionStart(Frame frame)
    {
      frame.Events.StartCharacterSelection();
    }

    public void OnCommandFinishCharacterSelection(Frame frame)
    {
      HFSMManager.TriggerEvent(frame, &frame.Global->GameManagerHFSM, default, "OnFinishCharacterSelection");
      frame.Events.FinishCharacterSelection();
      frame.SystemDisable<CharacterSelectionSystem>();
    }

    public void OnDestroyPlayerCharacter(Frame frame, PlayerRef player)
    {
      var players = frame.GetComponentIterator<PlayerLink>();
      foreach (var playerLink in players)
      {
        if (playerLink.Component.PlayerRef == player)
        {
          frame.Destroy(playerLink.Entity);
        }
      }
    }

    public void OnChangePlayerTeam(Frame frame, PlayerRef player)
    {
      var characterFilter = frame.Filter<PlayerLink, TeamInfo>();
      while (characterFilter.NextUnsafe(out var entity, out var playerLink, out var team))
      {
        if (playerLink->PlayerRef == player)
        {
          var targetTeam = team->Index == 0 ? 1 : 0;
          var playersInTargetTeam = GetTeamPlayerCount(frame, targetTeam);

          if (playersInTargetTeam > Constants.MAX_TEAM_SIZE) return;

          team->Index = targetTeam;
          frame.Signals.OnRespawnCharacter(entity, true);
        }
      }
    }

    public void OnCreatePlayerCharacter(Frame frame, PlayerRef player, AssetRef<EntityPrototype> prototype)
    {
      var playerData = frame.GetPlayerData(player);
      GenesisCombatLoadout genesisLoadout = null;
      GenesisRosterEntry genesisHero = default;
      var isGenesis = false;
      if (playerData.GenesisLoadout.IsValid)
      {
        genesisLoadout = frame.FindAsset<GenesisCombatLoadout>(playerData.GenesisLoadout.Id);
        if (!GenesisLoadoutSelection.IsConsistent(frame.RuntimeConfig, playerData, genesisLoadout))
        {
          Log.Error("Genesis loadout is inconsistent with the match catalog.");
          return;
        }
        // Ignore prototype changes requested by sample selection/debug commands.
        prototype = genesisLoadout.CharacterPrototype;
      }
      else if (frame.RuntimeConfig.RequireGenesisLoadout)
      {
        Log.Error("This match requires a Genesis loadout.");
        return;
      }
      else if (GenesisCast.TryGetPlayerHero(frame, playerData, out genesisHero, out var genesisPrototype))
      {
        // A Genesis plays the class its traits give it; selection commands cannot change it.
        prototype = genesisPrototype;
        isGenesis = true;
      }

      var currentTeam = GetTeamInfo(frame, player);
      // Copy before destroying the old entity; its component pointer is then invalid.
      var currentTeamIndex = currentTeam == null ? FindValidTeam(frame) : currentTeam->Index;
      frame.Signals.OnDestroyPlayerCharacter(player);

      EntityRef playerCharacter = frame.Create(prototype);

      if (genesisLoadout != null)
      {
        GenesisLoadoutSelection.Apply(frame, playerCharacter, genesisLoadout);
      }

      if (isGenesis)
      {
        GenesisCast.Tag(frame, playerCharacter, genesisHero, playerData.GenesisForm);
      }
      else if (genesisLoadout == null)
      {
        // No Genesis: the player is a recruit, a little weaker than the class it plays.
        Recruit.Apply(frame, playerCharacter);
      }

      // Store it's PlayerRef so we can later use it for input polling
      PlayerLink* playerLink = frame.Unsafe.GetPointer<PlayerLink>(playerCharacter);
      playerLink->PlayerRef = player;

      TeamInfo* teamInfo = frame.Unsafe.GetPointer<TeamInfo>(playerCharacter);
      teamInfo->Index = currentTeamIndex;

      // Spawn the character
      frame.Signals.OnRespawnCharacter(playerCharacter, true);
      TestOptions.ApplyToNewCharacter(frame, playerCharacter);

      // If, from the player data, we want to force this character to be controlled by AI,
      // we already setup the components needed
      if (frame.GetPlayerData(player).ForceAI == true)
      {
        AISetupHelper.Botify(frame, playerCharacter);
      }
    }

    // A player who joins takes the team with fewer players, the left one on a tie, as matchmade games balance their
    // teams (23 October 2026, the owner: two humans who pressed PLAY landed on the same team, so they could not stake
    // against each other). CHANGE TEAM still moves a player.
    private int FindValidTeam(Frame frame)
    {
      var left = GetTeamPlayerCount(frame, 0);
      var right = GetTeamPlayerCount(frame, 1);
      return right < left ? 1 : 0;
    }

    private TeamInfo* GetTeamInfo(Frame frame, PlayerRef player)
    {
      var characterFilter = frame.Filter<PlayerLink, TeamInfo>();
      while (characterFilter.NextUnsafe(out var entity, out var playerLink, out var team))
      {
        if (playerLink->PlayerRef == player)
        {
          return team;
        }
      }

      return null;
    }

    private int GetTeamPlayerCount(Frame frame, int teamIndex)
    {
      var count = 0;
      var teams = frame.GetComponentIterator<TeamInfo>();
      foreach (var team in teams)
      {
        if (team.Component.Index == teamIndex)
        {
          count++;
        }
      }

      return count;
    }
  }
}
