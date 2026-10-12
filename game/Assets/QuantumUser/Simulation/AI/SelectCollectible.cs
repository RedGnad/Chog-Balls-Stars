using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class SelectCollectible : AIAction
	{
		public AIBlackboardValueKey TargetCollectible;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			FPVector2 myPosition = frame.Get<Transform2D>(entity).Position;

			var allCollectibles = frame.Filter<Collectible, Transform2D>();
			FP smallestDistance = FP.MaxValue;
			EntityRef closestCoin = default;
			while (allCollectibles.NextUnsafe(out var coinEntity, out var collectible, out var transform))
			{
				FP distance = FPVector2.Distance(myPosition, transform->Position);
				if (distance < smallestDistance)
				{
					smallestDistance = distance;
					closestCoin = coinEntity;
				}
			}

			frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->Set(frame, TargetCollectible.Key, closestCoin);
		}
	}
}
