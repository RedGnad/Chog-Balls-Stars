namespace Quantum
{
	[System.Serializable]
	public unsafe class CheckBoolean : HFSMDecision
	{
		public AIParamBool Boolean;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			AIConfig aiConfig = frame.FindAsset<AIConfig>(frame.Unsafe.GetPointer<HFSMAgent>(entity)->Config.Id);
			return Boolean.Resolve(frame, entity, blackboard, aiConfig);
		}
	}
}
