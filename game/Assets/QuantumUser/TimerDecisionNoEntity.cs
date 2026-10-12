using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial class TimerDecisionNoEntity : HFSMDecision
	{
		public AIParamFP TimeToTrueState = FP._3;

		public override unsafe bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var blackboard = frame.Has<AIBlackboardComponent>(entity) ? frame.Get<AIBlackboardComponent>(entity) : default;

			FP requiredTime = TimeToTrueState.Resolve(frame, entity, &blackboard, null);

			var hfsmData = frame.Global->GameManagerHFSM;
			return hfsmData.Time >= requiredTime;
		}
	}
}
