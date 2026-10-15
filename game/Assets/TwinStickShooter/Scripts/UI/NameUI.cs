namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;
  using TMPro;

  public class NameUI : QuantumEntityViewComponent<CustomViewContext>
  {
    public TextMeshProUGUI NameText;

    private CharacterUIFollow _characterFollow;

    private void Awake()
    {
      _characterFollow = GetComponent<CharacterUIFollow>();
    }

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      Chog.Screens.ChogTheme.AsTitle(NameText);
      // A Genesis shows its own name and wears its own face, whether a player or a bot controls it.
      var genesisName = GenesisCast.DisplayName(PredictedFrame, _characterFollow.EntityView.EntityRef);
      if (genesisName != null)
      {
        NameText.text = Chog.Screens.GenesisHeroCard.CleanName(genesisName);
        DressAsGenesis();
        return;
      }

      if (PredictedFrame.TryGet<PlayerLink>(_characterFollow.EntityView.EntityRef, out var playerLink))
      {
        int playerId = playerLink.PlayerRef;
        RuntimePlayer playerData = PredictedFrame.GetPlayerData(playerId);
        if (playerData != null)
        {
          NameText.text = playerData.PlayerNickname;
        }
        else
        {
          NameText.text = GetNameFromFile(playerLink.PlayerRef - 1);
        }

        // A player without a Genesis (a recruit) wears the bare Chog, in grey.
        if (Chog.Match.GenesisEffects.Enabled)
        {
          Chog.Match.ChogHeroModel.Wear(_characterFollow.EntityView.transform, Chog.Match.ChogHeroModel.Recruit);
        }
      }
    }

    // The Genesis wears its 3D hero (its traits modelled), or, for a Genesis not modelled yet, its
    // real image as the head and the colour of its skin on the body.
    private void DressAsGenesis()
    {
      var roster = GenesisCast.Roster(PredictedFrame);
      var hero = PredictedFrame.Get<GenesisHero>(_characterFollow.EntityView.EntityRef);
      var index = roster == null ? -1 : roster.IndexOf(hero.TokenId);
      if (index >= 0)
      {
        Chog.Match.GenesisAppearance.Apply(_characterFollow.EntityView.transform, roster, roster.Entries[index]);
      }
    }

    private string GetNameFromFile(int id)
    {
      TextAsset asset = Resources.Load("BotsNames") as TextAsset;
      string[] names = asset.text.Split(';');
      return names[id];
    }
  }
}