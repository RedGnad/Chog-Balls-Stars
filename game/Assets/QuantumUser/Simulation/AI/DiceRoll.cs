using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class DiceRoll : HFSMDecision
	{
		public AIParamFP Input;
		public AIParamFP SuccessThreshold;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			var config = frame.FindAsset<AIConfig>(frame.Get<Bot>(entity).AIConfig.Id);

			FP input = Input.Resolve(frame, entity, blackboard, config);
			FP expected = SuccessThreshold.Resolve(frame, entity, blackboard, config);

			if (input < expected)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
