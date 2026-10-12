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