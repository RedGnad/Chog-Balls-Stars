using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class HasEscapeRoute : HFSMDecision
	{
		public AIBlackboardValueKey EscapeRoute;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			return blackboard->GetVector2(frame, EscapeRoute.Key) != default;
		}
	}
}
