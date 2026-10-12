namespace TwinStickShooter
{
  using Quantum;
  using Quantum.Menu;
  using UnityEngine;

  [RequireComponent(typeof(QuantumMenuUIScreen))]
  public class GameplayRoomManager : MonoBehaviour
  {
    private QuantumMenuUIScreen _menuUIScreen;
    
    void Start()
    {
      _menuUIScreen = GetComponent<QuantumMenuUIScreen>();
      QuantumEvent.Subscribe<EventFinishCharacterSelection>(this, OnFinishCharacterSelection);
    }

    private void OnFinishCharacterSelection(EventFinishCharacterSelection e)
    {
      _menuUIScreen.Connection.Client.CurrentRoom.IsOpen = false;
      Debug.Log("Closed the room after character selection.");
    }
  }
}