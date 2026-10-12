namespace Quantum
{
	[System.Serializable]
	public unsafe class CollectibleExists : HFSMDecision
	{
		public AIBlackboardValueKey TargetCollectible;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			EntityRef collectibleEntity = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->GetEntityRef(frame, TargetCollectible.Key);
			return frame.Exists(collectibleEntity);
		}
	}
}
