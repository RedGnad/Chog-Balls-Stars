using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class RunToEscapeRoute : AIAction
	{
		public AIBlackboardValueKey EscapeRoute;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			FPVector2 escapePosition = blackboard->GetVector2(frame, EscapeRoute.Key);

			NavMeshPathfinder* pathfinder = frame.Unsafe.GetPointer<NavMeshPathfinder>(entity);
			NavMesh navMesh = frame.Map.GetNavMesh("NavMesh");
			pathfinder->SetTarget(frame, escapePosition.XOY, navMesh);

			AISteering* aiSteering = frame.Unsafe.GetPointer<AISteering>(entity);
			aiSteering->SetNavMeshSteeringEntry(frame, entity);
		}
	}
}
