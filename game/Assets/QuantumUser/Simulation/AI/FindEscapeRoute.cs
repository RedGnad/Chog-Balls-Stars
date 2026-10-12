using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class FindEscapeRoute : AIAction
	{
		public AIBlackboardValueKey EscapeRoute;
		public FP MinDistance = 2;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			FPVector2 myPosition = frame.Get<Transform2D>(entity).Position;
			int myTeam = frame.Get<TeamInfo>(entity).Index;

			var allEscapeRoutes = frame.Filter<EscapeRoute, Transform2D>();
			FP smallestDistance = FP.MaxValue;
			FPVector2 closestEscapePosition = default;
			while (allEscapeRoutes.NextUnsafe(out var escapeRouteEntity, out var escapeRoute, out var transform))
			{
				if (escapeRoute->TeamId != myTeam)
					continue;

				FP distance = FPVector2.Distance(myPosition, transform->Position);
				if (distance > MinDistance && distance < smallestDistance)
				{
					smallestDistance = distance;
					closestEscapePosition = transform->Position;
				}
			}

			blackboard->Set(frame, EscapeRoute.Key, closestEscapePosition);
		}
	}
}
