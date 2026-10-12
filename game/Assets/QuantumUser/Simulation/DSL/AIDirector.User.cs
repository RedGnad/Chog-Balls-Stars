using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct AIDirector
	{
		public void SetStrategies(Frame frame, int teamId)
		{
			TeamData* ourTeamData = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId);
			TeamData* theirTeamData = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId == 0 ? 1 : 0);

			ourTeamData->StrategyFightActivated = ToggleStrategyFight(ourTeamData, theirTeamData);
			ourTeamData->StrategyScoreActivated = ToggleStrategyScore(ourTeamData, theirTeamData);
			ourTeamData->StrategyRunActivated = ToggleStrategyRun(ourTeamData, theirTeamData);
		}

		private bool ToggleStrategyFight(TeamData* ourTeamData, TeamData* theirTeamData)
		{
			// There are enemies alive
			if(theirTeamData->HealthsMax <= 0)
			{
				return false;
			}

			// We are Safely Winning but we don't have any low score agent (sacrifice agent)
			if(ourTeamData->TeamStatus.HasFlag(ETeamStatus.SafelyWinning) == true && ourTeamData->HasLowScoreAgent != true)
			{
				return false;
			}

			// We are not (Safely) Winning && The other team has considerable health disadvantage
			if (theirTeamData->TeamStatus.HasFlag(ETeamStatus.SafelyWinning) == false
				&& theirTeamData->HealthsPercentage > ourTeamData->HealthsPercentage + FP._0_33)
			{
				return false;
			}

			return true;
		}

		private bool ToggleStrategyScore(TeamData* ourTeamData, TeamData* theirTeamData)
		{
			// There is at least one collectible
			if(Memory.AvailableCoins == 0)
			{
				return false;
			}

			// Opposite team is not Safely Winning
			if(theirTeamData->TeamStatus.HasFlag(ETeamStatus.SafelyWinning) == true)
			{
				return false;
			}

			// We are (Safely) Winning || We are (Safely) Winning && There are no threats
			if(ourTeamData->TeamStatus.HasFlag(ETeamStatus.Winning) == true
				|| ourTeamData->TeamStatus.HasFlag(ETeamStatus.SafelyWinning) == true
				&& theirTeamData->HealthsMax > 0)
			{
				return false;
			}

			return true;
		}

		private bool ToggleStrategyRun(TeamData* ourTeamData, TeamData* theirTeamData)
		{
			// There are enemies alive
			if(theirTeamData->HealthsMax == 0)
			{
				return false;
			}

			// We are safely winning
			if(ourTeamData->TeamStatus.HasFlag(ETeamStatus.SafelyWinning) == true)
			{
				return true;
			}

			// Opposite team is not Safely Winning
			if (theirTeamData->TeamStatus.HasFlag(ETeamStatus.SafelyWinning) == true)
			{
				return false;
			}

			// The other team has considerable health disadvantage
			if (theirTeamData->HealthsPercentage > ourTeamData->HealthsPercentage + FP._0_33)
			{
				return true;
			}

			return false;
		}

		public bool CanChangeTactics(ETactics desiredTactic, TeamData* teamData)
		{
			bool result = false;
			switch (desiredTactic)
			{
				case ETactics.Engage:
				case ETactics.Ambush:
					if (teamData->StrategyFightActivated == true)
					{
						result = true;
					}
					break;

				case ETactics.Collect:
					if (teamData->StrategyScoreActivated == true)
					{
						result = true;
					}
					break;

				case ETactics.Run:
				case ETactics.Hide:
				case ETactics.TakeCover:
					if (teamData->StrategyRunActivated == true)
					{
						result = true;
					}
					break;

				case ETactics.GuardObjective:
					result = true;
					break;

				default:
					break;
			}

			//Log.Info($"[AI Director] Permission to change to {desiredTactic} was {result}");

			return result;
		}
	}
}
