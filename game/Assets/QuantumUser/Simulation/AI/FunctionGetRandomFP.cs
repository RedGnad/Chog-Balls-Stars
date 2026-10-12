using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class FunctionGetRandomFP : AIFunction<FP>
	{
		public AIParamFP Min;
		public AIParamFP Max;

		public bool IsInclusive;

		public override FP Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			var config = frame.FindAsset<AIConfig>(frame.Get<Bot>(entity).AIConfig.Id);

			FP min = Min.Resolve(frame, entity, blackboard, config);
			FP max = Max.Resolve(frame, entity, blackboard, config);

			if (IsInclusive == true)
			{
				return frame.RNG->NextInclusive(min, max);
			}
			else
			{
				return frame.RNG->Next(min, max);
			}
		}
	}
}
