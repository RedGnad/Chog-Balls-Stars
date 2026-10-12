using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class FindObjectivePoint : AIAction
	{
		public AIBlackboardValueKey ObjectivePoint;
		public FP MinDistance = 2;
		
		private const string PREVIOUS_OBJECTIVE_POINT = "ObjectivePointPrevious";

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			FPVector2 myPosition = frame.Get<Transform2D>(entity).Position;
			int myTeam = frame.Get<TeamInfo>(entity).Index;

			var previousChoice = blackboard->GetVector2(frame, PREVIOUS_OBJECTIVE_POINT);

			var allEscapeRoutes = frame.Filter<ObjectivePoint, Transform2D>();
			FP smallestDistance = FP.MaxValue;
			FPVector2 closestObjectivePoint = default;
			while (allEscapeRoutes.NextUnsafe(out var objectivePointEntity, out var objectivePoint, out var transform))
			{
				if(transform->Position == previousChoice)
				{
					continue;
				}

				FP distance = FPVector2.Distance(myPosition, transform->Position);
				if (distance > MinDistance && distance < smallestDistance)
				{
					smallestDistance = distance;
					closestObjectivePoint = transform->Position;
				}
			}

			// Update the previous choice so we avoid re-choosing it on the next attempt, just to add better variety
			var newPrevious = blackboard->GetVector2(frame, ObjectivePoint.Key);
			blackboard->Set(frame, PREVIOUS_OBJECTIVE_POINT, newPrevious);

			blackboard->Set(frame, ObjectivePoint.Key, closestObjectivePoint);
		}
	}
}
