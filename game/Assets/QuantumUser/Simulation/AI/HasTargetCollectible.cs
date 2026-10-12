namespace Quantum
{
	[System.Serializable]
	public unsafe class HasTargetCollectible : HFSMDecision
	{
		public AIBlackboardValueKey TargetCollectible;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->GetEntityRef(frame, TargetCollectible.Key) != default;
		}
	}
}
