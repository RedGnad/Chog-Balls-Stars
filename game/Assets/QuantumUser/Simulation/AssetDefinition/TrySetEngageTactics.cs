using Quantum;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe class TrySetEngageTactics : TacticalSensor
	{
		public bool CheckLoF;

		public override bool TrySetTactic(Frame frame, EntityRef entity, Bot* bot)
		{
			AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(entity);

			if (EnemyPositionsHelper.TryGetClosestCharacter(frame, entity, 8, CheckLoF, true, out var closestCharacter) == true)
			{
				int teamId = frame.Unsafe.GetPointer<TeamInfo>(entity)->Index;
				TeamData* teamData = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId);
				AIDirector myDirector = teamData->AIDirector;

				if (myDirector.CanChangeTactics(ETactics.Engage, teamData) == true)
				{
					HFSMManager.TriggerEvent(frame, entity, AIConstants.TACTIC_EVENT_ENGAGE);
					bot->CurrentTactic = ETactics.Engage;
					return true;
				}
			}

			return false;
		}
	}
}
