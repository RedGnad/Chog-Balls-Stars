using Photon.Deterministic;

namespace Quantum
{
  public class SelectCharacterCommands
  {
    public class FinishCharacterSelectionCommand: DeterministicCommand
    {
      public override void Serialize(BitStream stream)
      {
      }
    }
    
    public class SelectCharacterCommand: DeterministicCommand
    {
      public AssetRef<EntityPrototype> CharacterSelected;
      public override void Serialize(BitStream stream)
      {
        stream.Serialize(ref CharacterSelected);
      }
    }
    
    public class ChangePlayerTeamCommand: DeterministicCommand
    {
      public override void Serialize(BitStream stream) {}
    }
  }
}