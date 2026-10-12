using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class SpawnWithRandomRadius : AIAction
	{
		public FP Radius;
		public FP Amount;
		public AssetRef<EntityPrototype> Prototype;
		public FPVector2 Origin;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			for (int i = 0; i < Amount; i++)
			{
				FPVector2 randomPosition = FPVector2Helpers.RandomInsideCircle(frame, Origin, Radius);
				EntityRef newEntity = frame.Create(Prototype);
				frame.Unsafe.GetPointer<Transform2D>(newEntity)->Position = randomPosition;
			}
		}
	}
}
