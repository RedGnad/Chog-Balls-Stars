using Quantum;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe class TrySetCollectTactic : TacticalSensor
	{
		public override bool TrySetTactic(Frame frame, EntityRef entity, Bot* bot)
		{
			AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(entity);

			if (frame.Exists(aiMemory->ClosestCoin) == true)
			{
				int teamId = frame.Unsafe.GetPointer<TeamInfo>(entity)->Index;
				TeamData* teamData = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId);
				AIDirector myDirector = teamData->AIDirector;

				if (myDirector.CanChangeTactics(ETactics.Collect, teamData) == true)
				{
					HFSMManager.TriggerEvent(frame, entity, AIConstants.TACTIC_EVENT_COLLECT);
					bot->CurrentTactic = ETactics.Collect;
					return true;
				}
			}

			return false;
		}
	}
}
