using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class TargetIsInRange : HFSMDecision
	{
		public AIBlackboardValueKey TargetEntity;

		public AIParamFP ExpectedRange;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIMemory* sensorsMemory = frame.Unsafe.GetPointer<AIMemory>(entity);

			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			EntityRef targetEntity = blackboard->GetEntityRef(frame, AIConstants.KEY_TARGET);
			if (targetEntity == default)
				return false;

			Bot* bot = frame.Unsafe.GetPointer<Bot>(entity);

			AIConfig aiConfig = frame.FindAsset<AIConfig>(bot->AIConfig.Id);

			FPVector2 agentPosition = frame.Get<Transform2D>(entity).Position;


			FPVector2 targetPosition = frame.Get<Transform2D>(targetEntity).Position;

			FP range = ExpectedRange.Resolve(frame, entity, blackboard, aiConfig);

			return FPVector2.Distance(agentPosition, targetPosition) < range;
		}
	}
}
