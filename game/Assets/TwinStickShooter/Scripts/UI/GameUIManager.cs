namespace TwinStickShooter
{
  using UnityEngine;
  using Quantum;

  public class GameUIManager : QuantumCallbacks
  {
    public GameObject DisconnectButton;
    //TODO fix
    // protected override void OnEnable()
    // {
    //   base.OnEnable();
    //   QuantumEvent.Subscribe<EventGameOver>(this, OnGameOver);
    //   if (UIMain.Client != null)
    //   {
    //     DisconnectButton.SetActive(false);
    //   }
    // }
    //
    // private void OnGameOver(EventGameOver ev)
    // {
    //   if (UIMain.Client != null)
    //   {
    //     DisconnectButton.SetActive(true);
    //     UIGame.ShowScreen();
    //   }
    // }
  }
}