using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class ShowIntroductionDecision : HFSMDecision
	{
		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return frame.RuntimeConfig.ShowIntroduction;
		}
	}
}
