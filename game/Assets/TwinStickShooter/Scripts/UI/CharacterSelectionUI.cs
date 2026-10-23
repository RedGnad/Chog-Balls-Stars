namespace TwinStickShooter
{
  using System.Collections.Generic;
  using Quantum;
  using Quantum.Menu;
  using UnityEngine;

  public class CharacterSelectionUI : QuantumSceneViewComponent<CustomViewContext>
  {
    public GameObject StartButton;
    
    public AssetRef<EntityPrototype> PlaceHolderPrototype;
    public List<CharacterSelectionTeamInfo> Teams;

    private QuantumMenuConnectionBehaviour _connection;
    private UnityEngine.UI.Button _selectedButton;
    private CharacterSelectionButtonDelegate[] _classButtons;
    // Token of the Genesis whose card is shown; 0 while there is none.
    private int _cardTokenId;
    // Once the match starts this screen is hidden: nothing here needs refreshing any more.
    private bool _hidden;
    // A player without a Genesis is told once, above the class buttons, what a recruit is.
    private bool _recruitNoteShown;
    // Metadata id of the Genesis shown in each team slot this frame; 0 for none.
    // Two teams of three slots, as laid out in the scene; extra slots would show no portrait.
    // The dark panel of a slot, measured on the TeamSelectionUI prefab in slot units: the
    // portrait sits just outside it, on the side away from the centre of the screen.
    private const float SlotPanelHalfWidth = 157f;
    private const float SlotPanelCenterY = -25f;
    private const float SlotPortraitSize = 120f;
    private readonly int[,] _slotGenesis = new int[2, 3];
    private UnityEngine.UI.Button _changeTeam;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      _hidden = false;
      _cardTokenId = 0;
      _recruitNoteShown = false;
      StartButton.SetActive(false);
      _connection = FindObjectOfType<QuantumMenuConnectionBehaviour>();
      QuantumEvent.Subscribe<EventArenaPresentation>(this, OnArenaPreparation);

      RuntimePlayer playerData = new RuntimePlayer();
      playerData.PlayerAvatar = PlaceHolderPrototype;

      if (_connection != null)
      {
        playerData.PlayerNickname = _connection.Client.LocalPlayer.NickName;
      }

      Chog.Match.LocalGenesis.ApplyTo(playerData);

      Game.AddPlayer(playerData);
    }

    public override void OnUpdateView()
    {
      base.OnUpdateView();

      // The sample kept rebuilding the slot texts every frame of the match, behind the hidden
      // screen; with Genesis names and portraits that is wasted work and memory on a phone.
      if (_hidden)
      {
        return;
      }

      for (int i = 0; i < Teams.Count; i++)
      {
        CleanTeamSlots(i);
        UpdatePlayerByTeam(i);
      }

      UpdateSlotPortraits();
      UpdateClassButtons();
      // The stakes of the match, where the game services stake on matches.
      Chog.Screens.WagerPanel.Show(transform, Teams[0].TeamCharacters[0], LocalTeam());
      // A way back to the menu before the match starts.
      Chog.Screens.PreparationBack.Show(transform, Teams[0].TeamCharacters[0]);
      // CHANGE TEAM fades once this player has staked on the team it is in.
      if (_changeTeam == null)
      {
        foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
          if (button.name == "ChangeTeamButton")
          {
            _changeTeam = button;
          }
        }
      }
      if (_changeTeam != null)
      {
        _changeTeam.interactable = !Chog.Match.GenesisWager.HasStaked;
      }
      // The code of a room made with FRIENDS, for the friend to join.
      Chog.Screens.PartyRoomCode.Show(transform, _connection != null ? _connection.SessionName : null);

      // The host's clock: the match starts by itself when nobody presses START.
      var hosts = _connection == null || _connection.Client.LocalPlayer.IsMasterClient;
      Chog.Screens.LobbyCountdown.Show(transform, hosts, PlayersInRoom(), OnStartClicked);

      if (_connection == null)
      {
        StartButton.SetActive(true);
        return;
      }

      StartButton.SetActive(_connection.Client.LocalPlayer.IsMasterClient);
    }

    private void CleanTeamSlots(int teamIndex)
    {
      for (int i = 0; i < Teams[teamIndex].TeamPlayers.Length; i++)
      {
        // A seat nobody holds yet: another player may take it, a bot does at the start.
        Teams[teamIndex].TeamPlayers[i].text = "<alpha=#80>OPEN";
        Teams[teamIndex].TeamCharacters[i].text = "";
      }

      for (int i = 0; i < _slotGenesis.GetLength(1); i++)
      {
        _slotGenesis[teamIndex, i] = 0;
      }
    }

    private unsafe void UpdatePlayerByTeam(int teamIndex)
    {
      var characterIndex = 0;
      var characterFilter = VerifiedFrame.Filter<PlayerLink, Character, TeamInfo>();
      while (characterFilter.Next(out var entityRef, out var playerLink, out var character, out var team))
      {
        if (team.Index != teamIndex)
        {
          continue;
        }

        var playerData = VerifiedFrame.GetPlayerData(playerLink.PlayerRef);
        var nickname = "none";
        if (playerData != null && playerLink.PlayerRef != PlayerRef.None)
        {
          nickname = playerData.PlayerNickname;
        }

        Teams[team.Index].TeamPlayers[characterIndex].text = nickname;
        var characterInfo = VerifiedFrame.FindAsset<Quantum.CharacterInfo>(character.Info.Id);
        var genesisName = GenesisCast.DisplayName(VerifiedFrame, entityRef);
        if (genesisName != null && team.Index < _slotGenesis.GetLength(0) && characterIndex < _slotGenesis.GetLength(1))
        {
          _slotGenesis[team.Index, characterIndex] = VerifiedFrame.Get<GenesisHero>(entityRef).MetadataId;
        }
        // A Genesis shows what its traits give it: class from the outfit, attack from the eyes,
        // effect from the aura.
        Teams[team.Index].TeamCharacters[characterIndex].text =
          genesisName == null
            ? Chog.Screens.ChogTheme.ClassName(characterInfo.Name)
            : Chog.Screens.GenesisHeroCard.CleanName(genesisName) + "\n" + GenesisCast.KitLabel(VerifiedFrame, entityRef);
        Teams[team.Index].TeamCharacters[characterIndex].color = characterInfo.NameColor;
        // The name and the kit of a Genesis take two lines: the text shrinks to stay in its slot (22 October 2026, the owner:
        // "the detail of the player does not fit in its window").
        var slotText = Teams[team.Index].TeamCharacters[characterIndex];
        if (slotText.enableAutoSizing == false)
        {
          slotText.fontSizeMax = slotText.fontSize;
          slotText.fontSizeMin = 10f;
          slotText.enableAutoSizing = true;
          slotText.overflowMode = TMPro.TextOverflowModes.Truncate;
        }

        characterIndex++;
      }
    }

    // Each slot held by a Genesis shows its image on the outer side of the slot.
    private void UpdateSlotPortraits()
    {
      for (int teamIndex = 0; teamIndex < Teams.Count && teamIndex < _slotGenesis.GetLength(0); teamIndex++)
      {
        var slots = Teams[teamIndex].TeamCharacters;
        for (int i = 0; i < slots.Length && i < _slotGenesis.GetLength(1); i++)
        {
          var slot = (RectTransform)slots[i].rectTransform.parent;
          var towardsOutside = teamIndex == 0 ? -1f : 1f;
          Chog.Match.GenesisPortrait.Show(slot, _slotGenesis[teamIndex, i], SlotPortraitSize,
            new Vector2(0.5f, 0.5f), new Vector2(teamIndex == 0 ? 1f : 0f, 0.5f),
            new Vector2(towardsOutside * SlotPanelHalfWidth, SlotPanelCenterY));
        }
      }
    }

    // A player who entered as a Genesis does not choose a class: where the class buttons were,
    // a card shows the Genesis and what each of its traits gives it.
    private void UpdateClassButtons()
    {
      if (TryGetLocalGenesis(out var roster, out var hero) == false)
      {
        ShowRecruitNote();
        return;
      }

      if (hero.TokenId == _cardTokenId)
      {
        return;
      }

      if (_classButtons == null)
      {
        _classButtons = GetComponentsInChildren<CharacterSelectionButtonDelegate>(true);
      }

      if (_classButtons.Length == 0)
      {
        return;
      }

      var center = Vector2.zero;
      foreach (var classButton in _classButtons)
      {
        center += ((RectTransform)classButton.transform).anchoredPosition / _classButtons.Length;
        classButton.gameObject.SetActive(false);
      }

      Chog.Screens.GenesisHeroCard.Show((RectTransform)_classButtons[0].transform, center,
        Teams[0].TeamCharacters[0], roster, hero);
      _cardTokenId = hero.TokenId;
    }

    // A recruit plays one character, the one the menu shows: the class buttons go, as for a
    // Genesis, and a note in their place says what a recruit is.
    private void ShowRecruitNote()
    {
      if (_recruitNoteShown)
      {
        return;
      }

      var characters = VerifiedFrame.Filter<PlayerLink, Character>();
      while (characters.Next(out var entityRef, out var playerLink, out _))
      {
        if (Game.PlayerIsLocal(playerLink.PlayerRef) == false || Recruit.Is(VerifiedFrame, entityRef) == false)
        {
          continue;
        }

        if (_classButtons == null)
        {
          _classButtons = GetComponentsInChildren<CharacterSelectionButtonDelegate>(true);
        }

        if (_classButtons.Length == 0)
        {
          return;
        }

        var center = Vector2.zero;
        foreach (var classButton in _classButtons)
        {
          center += ((RectTransform)classButton.transform).anchoredPosition / _classButtons.Length;
          classButton.gameObject.SetActive(false);
        }

        Chog.Screens.RecruitNote.Show((RectTransform)_classButtons[0].transform, center, Teams[0].TeamCharacters[0]);
        _recruitNoteShown = true;

        // A test option of a page served from this computer ("?recruitclass=2"): the recruit plays
        // that class, to look at its attacks without a Genesis of it.
        var url = Application.absoluteURL ?? "";
        var chosen = Chog.Screens.MatchModeChoice.QueryValue(url, "recruitclass");
        if (Chog.Match.LocalGenesis.TestEntriesAllowed(url, Application.isEditor) && int.TryParse(chosen, out var index)
            && index >= 0 && index < _classButtons.Length)
        {
          SelectCharacterClicked(_classButtons[index].CharacterPrototype, _classButtons[index].SelectButton);
        }
        return;
      }
    }

    private bool TryGetLocalGenesis(out GenesisRoster roster, out GenesisRosterEntry hero)
    {
      hero = default;
      roster = GenesisCast.Roster(VerifiedFrame);
      if (roster == null)
      {
        return false;
      }

      var heroes = VerifiedFrame.Filter<PlayerLink, GenesisHero>();
      while (heroes.Next(out _, out var playerLink, out var genesis))
      {
        if (Game.PlayerIsLocal(playerLink.PlayerRef))
        {
          var index = roster.IndexOf(genesis.TokenId);
          if (index < 0)
          {
            return false;
          }

          hero = roster.Entries[index];
          return true;
        }
      }

      return false;
    }

    public void SelectCharacterClicked(AssetRef<EntityPrototype> CharacterPrototype, UnityEngine.UI.Button button)
    {
      UpdateSelectedButtonState(button);
      
      var selectCommand = new SelectCharacterCommands.SelectCharacterCommand();
      selectCommand.CharacterSelected = CharacterPrototype;
      Game.SendCommand(selectCommand);
    }

    public void ChangeTeamClicked()
    {
      // A stake is for the team it was made on: changing team after it would only call the match off (the audit, 23 October 2026).
      if (Chog.Match.GenesisWager.HasStaked)
      {
        return;
      }

      var changeTeamCommand = new SelectCharacterCommands.ChangePlayerTeamCommand();
      Game.SendCommand(changeTeamCommand);
    }

    private void UpdateSelectedButtonState(UnityEngine.UI.Button button)
    {
      if (_selectedButton != null)
      {
        _selectedButton.interactable = true;
      }
      _selectedButton = button;
      _selectedButton.interactable = false;
    }

    public void OnStartClicked()
    {
      // With stakes on the match they are locked first: nobody stakes once play has begun.
      Chog.Screens.WagerPanel.BeforeStart(() =>
      {
        var startCommand = new SelectCharacterCommands.FinishCharacterSelectionCommand();
        Game.SendCommand(startCommand);
      });
    }

    // The players in the room with a character, bots left out.
    private int PlayersInRoom()
    {
      var count = 0;
      var characters = VerifiedFrame.Filter<PlayerLink, TeamInfo>();
      while (characters.Next(out _, out var playerLink, out _))
      {
        if (playerLink.PlayerRef != PlayerRef.None)
        {
          count++;
        }
      }

      return count;
    }

    // The team of the local player's character, or -1 while it has none.
    private int LocalTeam()
    {
      var characters = VerifiedFrame.Filter<PlayerLink, TeamInfo>();
      while (characters.Next(out _, out var playerLink, out var team))
      {
        if (playerLink.PlayerRef != PlayerRef.None && Game.PlayerIsLocal(playerLink.PlayerRef))
        {
          return team.Index;
        }
      }

      return -1;
    }

    private void OnArenaPreparation(EventArenaPresentation e)
    {
      _hidden = true;
      var objects = GetComponentsInChildren<Transform>();
      foreach (Transform obj in objects)
      {
        obj.gameObject.SetActive(false);
      }
    }
  }

  
}