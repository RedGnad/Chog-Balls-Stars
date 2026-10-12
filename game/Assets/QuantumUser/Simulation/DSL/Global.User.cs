namespace Quantum
{
	public unsafe partial struct _globals_
	{
		public TeamData* GetTeamData(Frame frame, EntityRef entity)
		{
			if(frame.TryGet(entity, out TeamInfo teamInfo) == false)
			{
				return null;
			}

			return GetTeamData(frame, teamInfo.Index);
		}

		public TeamData* GetOpponentTeamData(Frame frame, EntityRef entity)
		{
			if (frame.TryGet(entity, out TeamInfo teamInfo) == false)
			{
				return null;
			}

			return GetTeamData(frame, teamInfo.Index == 0 ? 1 : 0);
		}

		public TeamData* GetTeamData(Frame frame, int teamId)
		{
			var teamDataList = frame.ResolveList(TeamsData);
			Assert.Check(teamDataList.Count > teamId);
			return teamDataList.GetPointer(teamId);
		}
	}
}
