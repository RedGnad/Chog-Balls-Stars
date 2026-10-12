using Photon.Deterministic;

namespace Quantum
{
	public enum RunawayDirection { None, North, South, East, West }

	[System.Serializable]
	public unsafe class RunInBush : AIAction
	{
		public FP MinRunawayDistance = 6;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			// Discover runaway direction
			FPVector2 agentPosition = frame.Unsafe.GetPointer<Transform2D>(entity)->Position;

			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			EntityRef targetEntity = blackboard->GetEntityRef(frame, AIConstants.KEY_TARGET);
			if (targetEntity == default)
				return;
			FPVector2 enemyPosition = frame.Unsafe.GetPointer<Transform2D>(targetEntity)->Position;

			NavMeshPathfinder* pathfinder = frame.Unsafe.GetPointer<NavMeshPathfinder>(entity);
			NavMesh navMesh = frame.Map.GetNavMesh("NavMesh");
			AISteering* aiSteering = frame.Unsafe.GetPointer<AISteering>(entity);

			if (FPVector2.Distance(agentPosition, enemyPosition) > MinRunawayDistance)
			{
				aiSteering->MainSteeringData.SteeringEntryNavMesh->SetData(default);
				return;
			}
			pathfinder->IsActive = true;

			FPVector2 runawayDirection = enemyPosition - agentPosition;
			RunawayDirection runawayDefinition = RunawayDirection.None;
			if (FPMath.Abs(runawayDirection.Y) > FPMath.Abs(runawayDirection.X))
			{
				if(runawayDirection.Y > 0)
				{
					runawayDefinition = RunawayDirection.South;
				}
				else
				{
					runawayDefinition = RunawayDirection.North;
				}
			}
			else
			{
				if (runawayDirection.X > 0)
				{
					runawayDefinition = RunawayDirection.West;
				}
				else
				{
					runawayDefinition = RunawayDirection.East;
				}
			}
			// Get my bush so we can find a neighbor based on the direction
			int myBushId = frame.Unsafe.GetPointer<Invisibility>(entity)->StaticColliderId;
			EntityRef myBushEntity = frame.Global->GetInvisibilitySpotEntity(frame, myBushId);
			if(frame.Exists(myBushEntity) == false)
			{
				return;
			}
			InvisibilitySpot* myBush = frame.Unsafe.GetPointer<InvisibilitySpot>(myBushEntity);

			EntityRef targetBushEntity = default;
			FPVector2 targetPosition = default;
			switch (runawayDefinition)
			{
				case RunawayDirection.None:
					return;
				case RunawayDirection.North:
					targetBushEntity = frame.Global->GetInvisibilitySpotEntity(frame, myBush->NorthNeighborId);
					break;
				case RunawayDirection.South:
					targetBushEntity = frame.Global->GetInvisibilitySpotEntity(frame, myBush->SouthNeighborId);
					break;
				case RunawayDirection.East:
					targetBushEntity = frame.Global->GetInvisibilitySpotEntity(frame, myBush->EastNeighborId);
					break;
				case RunawayDirection.West:
					targetBushEntity = frame.Global->GetInvisibilitySpotEntity(frame, myBush->WestNeighborId);
					break;
				default:
					break;
			}

			if (frame.Exists(targetBushEntity) == true)
			{
				targetPosition = frame.Unsafe.GetPointer<Transform2D>(targetBushEntity)->Position;
			}
			else
			{
				targetPosition = frame.Unsafe.GetPointer<Transform2D>(myBushEntity)->Position;
			}

			pathfinder->SetTarget(frame, targetPosition.XOY, navMesh);

			aiSteering->SetNavMeshSteeringEntry(frame, entity);
		}
	}
}
