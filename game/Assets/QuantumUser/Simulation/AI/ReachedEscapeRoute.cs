using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class ReachedEscapeRoute : HFSMDecision
	{
		public AIBlackboardValueKey EscapeRoute;
		public FP MinDistance;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			FPVector2 myPosition = frame.Get<Transform2D>(entity).Position;

			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			FPVector2 escapePosition = blackboard->GetVector2(frame, EscapeRoute.Key);

			return FPVector2.Distance(myPosition, escapePosition) < MinDistance;
		}
	}
}
