using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class RunToCoverSpot : AIAction
	{
		public AIBlackboardValueKey CoverSpot;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			EntityRef targetEntity = blackboard->GetEntityRef(frame, AIConstants.KEY_TARGET);
			if (targetEntity == default)
				return;
			FPVector2 agentPosition = frame.Unsafe.GetPointer<Transform2D>(entity)->Position;

			FPVector2 closestCoverPoint = blackboard->GetVector2(frame, CoverSpot.Key);
			FP distanceToAgent = FPVector2.Distance(closestCoverPoint, agentPosition);
			if (distanceToAgent > FP._0_50)
			{
				NavMeshPathfinder* pathfinder = frame.Unsafe.GetPointer<NavMeshPathfinder>(entity);
				NavMesh navMesh = frame.Map.GetNavMesh("NavMesh");
				pathfinder->SetTarget(frame, closestCoverPoint.XOY, navMesh);

				AISteering* aiSteering = frame.Unsafe.GetPointer<AISteering>(entity);
				aiSteering->SetNavMeshSteeringEntry(frame, entity);
			}

			return;
		}
	}
}
