namespace TwinStickShooter
{
  using UnityEngine;
  using TMPro;
  using Quantum;

  public class CoinGrabUI : MonoBehaviour
  {
    public GameObject GameOverContainer;
    public TextMeshProUGUI WinnerText;

    private void OnEnable()
    {
      QuantumEvent.Subscribe<EventGameOver>(this, OnGameOver);
    }

    private void OnDisable()
    {
      QuantumEvent.UnsubscribeListener(this);
    }

    private void OnGameOver(EventGameOver ev)
    {
      GameOverContainer.SetActive(true);
      string winnerName = ev.WinnerTeam == 0 ? "Left Team" : "Right Team";
      WinnerText.text = ResultText(LocalTeam(ev), ev.WinnerTeam, winnerName);

      // The game's own end screen: the result and the hero played, in 3D (Chog.Screens.ChogResultScreen).
      if (Chog.Screens.ChogResultScreen.Show(LocalTeam(ev), ev.WinnerTeam, LocalHero(ev), LocalToken(ev)))
      {
        GameOverContainer.SetActive(false);
      }
    }

    // The hero of the local player: the number of its Genesis, the recruit's when it has none.
    private static int LocalHero(EventGameOver ev)
    {
      var frame = ev.Game.Frames.Verified;
      var characters = frame.Filter<PlayerLink, TeamInfo>();
      while (characters.Next(out var entity, out var playerLink, out _))
      {
        if (ev.Game.PlayerIsLocal(playerLink.PlayerRef))
        {
          return frame.TryGet<GenesisHero>(entity, out var genesis) ? genesis.MetadataId : Chog.Match.ChogHeroModel.Recruit;
        }
      }

      return Chog.Match.ChogHeroModel.Recruit;
    }

    // The Genesis the local player played, 0 for a recruit: its career is announced on the end screen.
    private static int LocalToken(EventGameOver ev)
    {
      var frame = ev.Game.Frames.Verified;
      var characters = frame.Filter<PlayerLink, GenesisHero>();
      while (characters.Next(out _, out var playerLink, out var genesis))
      {
        if (ev.Game.PlayerIsLocal(playerLink.PlayerRef))
        {
          return genesis.TokenId;
        }
      }

      return 0;
    }

    // A player reads the result of their own team; a spectator reads which team won.
    public static string ResultText(int localTeam, int winnerTeam, string winnerName)
    {
      if (localTeam < 0)
      {
        return winnerName + " wins!";
      }

      return localTeam == winnerTeam ? "Victory!" : "Defeat";
    }

    // Team of the local player's character, or -1 when this client plays no character.
    private static int LocalTeam(EventGameOver ev)
    {
      var characters = ev.Game.Frames.Verified.Filter<PlayerLink, TeamInfo>();
      while (characters.Next(out _, out var playerLink, out var team))
      {
        if (ev.Game.PlayerIsLocal(playerLink.PlayerRef))
        {
          return team.Index;
        }
      }

      return -1;
    }
  }
}