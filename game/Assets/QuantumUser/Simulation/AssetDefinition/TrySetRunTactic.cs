using Photon.Deterministic;
using Quantum;

namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe class TrySetRunTactic : TacticalSensor
	{
		public override bool TrySetTactic(Frame frame, EntityRef entity, Bot* bot)
		{
			AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(entity);
			
			int teamId = frame.Unsafe.GetPointer<TeamInfo>(entity)->Index;
			TeamData* teamData = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId);
			ETeamStatus teamStatus = teamData->TeamStatus;
			int collectiblesAmount = frame.Get<Inventory>(entity).CollectiblesAmount;
			bool weAreWinning = (teamStatus.HasFlag(ETeamStatus.SafelyWinning) || teamStatus.HasFlag(ETeamStatus.Winning)) && collectiblesAmount > 5;

			if (aiMemory->HealthStatus == EHealthStatus.Low || weAreWinning == true)
			{
				AIDirector myDirector = teamData->AIDirector;

				bool foundThreat = EnemyPositionsHelper.TryGetClosestCharacter(frame, entity, 12, false, true, out var threat);

				if (foundThreat == false)
				{
					if (myDirector.CanChangeTactics(ETactics.Hide, teamData) == true)
					{
						HFSMManager.TriggerEvent(frame, entity, AIConstants.TACTIC_EVENT_HIDE);
						bot->CurrentTactic = ETactics.Hide;
						return true;
					}
				}
				else
				{
					Transform2D transform = frame.Get<Transform2D>(entity);
					FP distanceToThreat = FPVector2.Distance(transform.Position, frame.Get<Transform2D>(threat).Position);

					if (distanceToThreat > 3)
					{
						if (myDirector.CanChangeTactics(ETactics.TakeCover, teamData) == true)
						{
							HFSMManager.TriggerEvent(frame, entity, AIConstants.TACTIC_EVENT_TAKE_COVER);
							bot->CurrentTactic = ETactics.TakeCover;
							return true;
						}
					}
					else
					{
						if (myDirector.CanChangeTactics(ETactics.Run, teamData) == true)
						{
							HFSMManager.TriggerEvent(frame, entity, AIConstants.TACTIC_EVENT_RUN);
							bot->CurrentTactic = ETactics.Run;
							return true;
						}
					}
				}
			}

			return false;
		}
	}
}
