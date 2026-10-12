using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SensorCollectibles : Sensor
	{
		public FP MaxDistance = 5;

		public override void Execute(Frame frame, EntityRef entity)
		{
			FPVector2 myPosition = frame.Get<Transform2D>(entity).Position;

			var allCollectibles = frame.Filter<Collectible, Transform2D>();
			FP smallestDistance = FP.MaxValue;
			EntityRef closestCoin = default;
			while (allCollectibles.NextUnsafe(out var coinEntity, out var collectible, out var transform))
			{
				FP distance = FPVector2.Distance(myPosition, transform->Position);
				if (distance < smallestDistance && distance <= MaxDistance)
				{
					smallestDistance = distance;
					closestCoin = coinEntity;
				}
			}

			if(closestCoin != default)
			{
				AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(entity);
				aiMemory->ClosestCoin = closestCoin;
			}
		}
	}
}
