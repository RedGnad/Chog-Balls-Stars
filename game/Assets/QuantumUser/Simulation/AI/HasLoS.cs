using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class HasLoS : HFSMDecision
	{
		public AIBlackboardValueKey TargetEntity;

		public AIParamBool IgnoreLoS;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			AIConfig aiConfig = frame.FindAsset<AIConfig>(frame.Get<HFSMAgent>(entity).Config.Id);
			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);

			if (IgnoreLoS.Resolve(frame, entity, blackboard, aiConfig) == true)
				return true;

			FPVector2 agentPosition = frame.Get<Transform2D>(entity).Position;

			EntityRef targetEntity = blackboard->GetEntityRef(frame, AIConstants.KEY_TARGET);
			if (targetEntity == default)
				return false;
			FPVector2 targetPosition = frame.Get<Transform2D>(targetEntity).Position;

			FPVector2 dirToTarget = (targetPosition - agentPosition).Normalized;

			var hit = frame.Physics2D.Raycast(agentPosition, dirToTarget, 8, frame.Layers.GetLayerMask("Character", "Static"));
			if (hit.HasValue == true && hit.Value.IsDynamic == true)
			{
				return true;
			}

			return false;
		}
	}
}
