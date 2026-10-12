using Photon.Deterministic;

namespace Quantum
{
	// Spawns any prototype on the specified position

	public unsafe class GameMaster_SpawnPrototype : DeterministicCommand
	{
		public AssetRef<EntityPrototype> Prototype;
		public FPVector2 TargetPosition;

		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref Prototype);
			stream.Serialize(ref TargetPosition);
		}

		public void Execute(Frame frame)
		{
			EntityRef newEntity = frame.Create(Prototype);
			frame.Unsafe.GetPointer<Transform2D>(newEntity)->Position = TargetPosition;
		}
	}
}
