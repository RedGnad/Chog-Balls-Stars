using Photon.Deterministic;
using Quantum.Physics2D;

namespace Quantum
{
	[System.Serializable]
	public unsafe class HideInBush : AIAction
	{
		public AIBlackboardValueKey ChosenBush;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			EntityRef bushEntity = frame.Get<AIBlackboardComponent>(entity).GetEntityRef(frame, ChosenBush.Key);

			if(frame.Exists(bushEntity) == false)
			{
				return;
			}

			FPVector2 chosenBushPosition = frame.Get<Transform2D>(bushEntity).Position;

			NavMeshPathfinder* pathfinder = frame.Unsafe.GetPointer<NavMeshPathfinder>(entity);
			NavMesh navMesh = frame.Map.GetNavMesh("NavMesh");
			pathfinder->SetTarget(frame, chosenBushPosition.XOY, navMesh);

			AISteering* aiSteering = frame.Unsafe.GetPointer<AISteering>(entity);
			aiSteering->SetNavMeshSteeringEntry(frame, entity);
		}
	}
}
