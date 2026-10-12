namespace TwinStickShooter
{
  using UnityEngine;

  public class GameSettings : MonoBehaviour
  {
    private static GameSettings _instance;

    public static GameSettings Instance
    {
      get => _instance;
    }

    [SerializeField] private GameColors _gameColors;

    public GameColors GameColors
    {
      get => _gameColors;
    }

    private void Awake()
    {
      if (_instance == null)
      {
        _instance = this;
      }
    }
  }
}