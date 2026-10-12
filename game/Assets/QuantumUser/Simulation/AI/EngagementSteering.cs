using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class EngagementSteering : AIAction
	{
		public AIParamFP ThreatDistance;
		public AIParamFP RunDistance;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			Bot* bot = frame.Unsafe.GetPointer<Bot>(entity);
			FPVector2 agentPosition = frame.Unsafe.GetPointer<Transform2D>(entity)->Position;

			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			EntityRef targetEntity = blackboard->GetEntityRef(frame, AIConstants.KEY_TARGET);
			if (targetEntity == default)
				return;

			AISteering* aiSteering = frame.Unsafe.GetPointer<AISteering>(entity);

			FPVector2 enemyPosition = frame.Unsafe.GetPointer<Transform2D>(targetEntity)->Position;
			FPVector2 dirToEnemy = enemyPosition - agentPosition;

			var hit = frame.Physics2D.Raycast(agentPosition, dirToEnemy.Normalized, dirToEnemy.Magnitude, -1, QueryOptions.HitStatics);

			if (hit.HasValue == true)
			{
				NavMeshPathfinder* pathfinder = frame.Unsafe.GetPointer<NavMeshPathfinder>(entity);
				NavMesh navMesh = frame.Map.GetNavMesh("NavMesh");
				pathfinder->SetTarget(frame, enemyPosition.XOY, navMesh);

				aiSteering->SetNavMeshSteeringEntry(frame, entity);
			}
			else
			{
				var aiConfig = frame.FindAsset<AIConfig>(bot->AIConfig.Id);
				FP threatDistance = ThreatDistance.Resolve(frame, entity, blackboard, aiConfig);
				FP runDistance = RunDistance.Resolve(frame, entity, blackboard, aiConfig);

				aiSteering->SetContextSteeringEntry(frame, entity, targetEntity, runDistance, threatDistance);
			}
		}
	}
}
