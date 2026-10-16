using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class GetTeamScore : AIFunction<FP>
	{
		public int TeamId;

		public override FP Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			// Only the game manager reads this, to start its coin countdown. In a siege the score is
			// the damage done to structures (MatchModes), which must not start it.
			if (MatchModes.IsSiege(frame))
			{
				return 0;
			}

			return frame.ResolveList(frame.Global->TeamsData)[TeamId].Score;
		}
	}
}
