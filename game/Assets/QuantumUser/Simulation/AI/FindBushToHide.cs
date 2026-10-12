using Photon.Deterministic;
using Quantum.Physics2D;

namespace Quantum
{
	[System.Serializable]
	public unsafe class FindBushToHide : AIAction
	{
		public AIBlackboardValueKey ChosenBush;

		public FP MinDistToEnemy;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			if (EnemyPositionsHelper.TryGetClosestCharacter(frame, entity, 25, false, true, out var closestEnemy) == false)
			{
				return;
			}

			Transform2D agentTransform = frame.Get<Transform2D>(entity);
			FPVector2 closestEnemyPosition = frame.Get<Transform2D>(closestEnemy).Position;
			HitCollection bushes = frame.Physics2D.OverlapShape(agentTransform, Shape2D.CreateCircle(10),
				frame.Layers.GetLayerMask("InvisibilityPoint"), QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo | QueryOptions.HitTriggers);

			for (int i = 0; i < bushes.Count; i++)
			{
				var bush = bushes[i];
				FP bushEnemyDistance = FPVector2.Distance(bush.Point, closestEnemyPosition);

				if (bushEnemyDistance > MinDistToEnemy)
				{
					EntityRef bushEntity = frame.Global->GetInvisibilitySpotEntity(frame, bush.StaticColliderIndex);
					frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->Set(frame, ChosenBush.Key, bushEntity);
					return;
				}
			}
		}
	}
}
