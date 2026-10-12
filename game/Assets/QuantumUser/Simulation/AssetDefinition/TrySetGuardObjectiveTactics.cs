using Quantum;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe class TrySetGuardObjectiveTactics : TacticalSensor
	{
		public override bool TrySetTactic(Frame frame, EntityRef entity, Bot* bot)
		{
			// We always let this pass, as this is the fallback case when nothing else seems interesting

			AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(entity);

			int teamId = frame.Unsafe.GetPointer<TeamInfo>(entity)->Index;
			TeamData* teamData = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId);
			AIDirector myDirector = teamData->AIDirector;

			if (myDirector.CanChangeTactics(ETactics.GuardObjective, teamData) == true)
			{
				HFSMManager.TriggerEvent(frame, entity, AIConstants.TACTIC_EVENT_GUARD_OBJECTIVE);
				bot->CurrentTactic = ETactics.GuardObjective;
				return true;
			}

			return false;
		}
	}
}
