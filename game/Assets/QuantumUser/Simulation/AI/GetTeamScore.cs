using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class GetTeamScore : AIFunction<FP>
	{
		public int TeamId;

		public override FP Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			return frame.ResolveList(frame.Global->TeamsData)[TeamId].Score;
		}
	}
}
