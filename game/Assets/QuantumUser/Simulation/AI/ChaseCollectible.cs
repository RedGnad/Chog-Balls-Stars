using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class ChaseCollectible : AIAction
	{
		public AIBlackboardValueKey TargetCollectible;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			EntityRef targetCollectible = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->GetEntityRef(frame, TargetCollectible.Key);

			if (frame.Exists(targetCollectible) == false)
			{
				return;
			}

			FPVector2 collectiblePosition = frame.Get<Transform2D>(targetCollectible).Position;

			NavMeshPathfinder* pathfinder = frame.Unsafe.GetPointer<NavMeshPathfinder>(entity);
			NavMesh navMesh = frame.Map.GetNavMesh("NavMesh");
			pathfinder->SetTarget(frame, collectiblePosition.XOY, navMesh);

			AISteering* aiSteering = frame.Unsafe.GetPointer<AISteering>(entity);
			aiSteering->SetNavMeshSteeringEntry(frame, entity);
		}
	}
}
