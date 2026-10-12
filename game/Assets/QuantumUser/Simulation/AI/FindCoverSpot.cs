using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class FindCoverSpot: AIAction
	{
		public AIBlackboardValueKey CoverSpot;

		public AIParamFP ChooseCoverSpotIntervalTicks;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			// The interval is authored in ticks at 60 Hz; keep the same real-time pace at any rate.
			FP authoredInterval = ChooseCoverSpotIntervalTicks.Resolve(frame, entity, blackboard, null);
			int interval = TickIntervals.FromAuthoringTicks(frame, FPMath.RoundToInt(authoredInterval));
			if (frame.Number % interval != 0)
			{
				return;
			}

			EntityRef targetEntity = blackboard->GetEntityRef(frame, AIConstants.KEY_TARGET);
			if (targetEntity == default)
				return;
			FPVector2 enemyPosition = frame.Unsafe.GetPointer<Transform2D>(targetEntity)->Position;

			FPVector2 agentPosition = frame.Unsafe.GetPointer<Transform2D>(entity)->Position;
			var hits = frame.Physics2D.OverlapShape(agentPosition, 0, Shape2D.CreateCircle(8), frame.Layers.GetLayerMask("CoverPoint"),
				QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo);

			FPVector2 closestCoverPoint = default;
			FP closestCoverDistance = FP.UseableMax;
			FP distanceToAgent = 0;
			for (int i = 0; i < hits.Count; i++)
			{
				var hit = hits[i];
				var staticData = hit.GetStaticData(frame);
				var position = frame.Map.StaticColliders2D[staticData.ColliderIndex].Position;

				var distToEnemy = FPVector2.Distance(position, enemyPosition);
				if (distToEnemy < 5)
				{
					continue;
				}

				var coverPointAsset = frame.FindAsset<CoverPoint>(hit.GetStaticData(frame).Asset.Id);
				var normal = coverPointAsset.Normal;

				var dirToEnemy = (enemyPosition - position).Normalized;
				var dot = FPVector3.Dot(normal, dirToEnemy.XOY);

				distanceToAgent = FPVector2.Distance(position, agentPosition);
				if (dot < -FP._0_25)
				{
					if (distanceToAgent < closestCoverDistance)
					{
						closestCoverPoint = position;
						closestCoverDistance = distanceToAgent;
					}
					//Draw.Circle(position, FP._0_50, ColorRGBA.White);
				}
			}

			blackboard->Set(frame, CoverSpot.Key, closestCoverPoint);
		}
	}
}
