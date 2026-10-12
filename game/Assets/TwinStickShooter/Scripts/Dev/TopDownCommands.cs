namespace TwinStickShooter
{
  using Photon.Deterministic;
  using UnityEngine;
  using UnityEngine.InputSystem;
  using Quantum;

  public class TopDownCommands : MonoBehaviour
  {
    public AssetRef<Quantum.EntityPrototype> _prototypeToSpawn;

    private int _selectedNumber = 0;
    private PlayerInput _playerInput;

    private void Start()
    {
      _playerInput = FindObjectOfType<PlayerInput>();
    }

    public void ButtonPressed(int number)
    {
      _selectedNumber = number - 1;
    }

    private void Update()
    {
      var numbersInput = _playerInput.actions["Numbers"];
      if (numbersInput.WasPressedThisFrame() == true)
      {
        _selectedNumber = (int)numbersInput.ReadValue<float>() - 1;
      }

      var specialWasPressed = _playerInput.actions["Special"].WasPressedThisFrame();

      var altIsPressed = _playerInput.actions["Alt"].IsPressed();
      if (specialWasPressed == true && altIsPressed == true)
      {
        var setPositionCommand = new Quantum.GameMaster_SetCharacterPosition()
        {
          CharacterNumber = _selectedNumber,
          TargetPosition = GetMousePosition()
        };

        QuantumRunner.Default.Game.SendCommand(setPositionCommand);
      }

      var ctrlIsPressed = _playerInput.actions["Ctrl"].IsPressed();
      if (specialWasPressed == true && ctrlIsPressed == true)
      {
        var spawnPrototypeCommand = new Quantum.GameMaster_SpawnPrototype()
        {
          Prototype = _prototypeToSpawn,
          TargetPosition = GetMousePosition()
        };

        QuantumRunner.Default.Game.SendCommand(spawnPrototypeCommand);
      }
    }

    private FPVector2 GetMousePosition()
    {
      Vector2 mousePosition = _playerInput.actions["Point"].ReadValue<Vector2>();
      Ray ray = Camera.main.ScreenPointToRay(mousePosition);
      UnityEngine.Plane plane = new UnityEngine.Plane(Vector3.up, Vector3.zero);

      if (plane.Raycast(ray, out var enter))
      {
        return ray.GetPoint(enter).ToFPVector2();
      }

      return default;
    }
  }
}