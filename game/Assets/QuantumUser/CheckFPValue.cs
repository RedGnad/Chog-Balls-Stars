using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class CheckFPValue : HFSMDecision
	{
		public AIParamFP ValueA;
		public AIParamFP ValueB;
		public EValueComparison Comparison;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			FP valueA = ValueA.Resolve(frame, entity, null, null);
			FP valueB = ValueB.Resolve(frame, entity, null, null);

			switch (Comparison)
			{
				case EValueComparison.LessThan:
					return valueA < valueB;
				case EValueComparison.MoreThan:
					return valueA > valueB;
				case EValueComparison.EqualTo:
					return valueA == valueB;
				default:
					return false;
			}
		}
	}
}
