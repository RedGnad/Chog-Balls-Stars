using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	// Observes the game state and saves it in appropriate storages to be accessed by the characters
	// This contains relevant information for the AI Director of each team, which will decide what are the
	// strategy types that are available
	[Preserve]
	public unsafe class TeamDataSystem : SystemMainThread
	{
		// Authored in ticks at 60 Hz (a quarter of a second); converted to the session rate in OnInit.
		private const byte TICK_INTERVAL = 15;

		public override void OnInit(Frame frame)
		{
			// Setup the teams data structures

			var teamsData = frame.ResolveList(frame.Global->TeamsData);
			TeamData* teamA = teamsData.GetPointer(0);
			TeamData* teamB = teamsData.GetPointer(1);

			teamA->Index = 0;
			teamB->Index = 1;

			var tickInterval = (byte)TickIntervals.FromAuthoringTicks(frame, TICK_INTERVAL);
			teamA->AIDirector.TickInterval = tickInterval;
			teamB->AIDirector.TickInterval = tickInterval;
		}

		public override void Update(Frame frame)
		{
			// Update the data for both teams

			var teamsData = frame.ResolveList(frame.Global->TeamsData);
			TeamData* teamA = teamsData.GetPointer(0);
			TeamData* teamB = teamsData.GetPointer(1);

			UpdateTeamData(frame, teamA);
			UpdateTeamData(frame, teamB);
		}

		private void UpdateTeamData(Frame frame, TeamData* teamData)
		{
			// Every team has it's own set of data stored
			// It is useful for the teams to have information about the 

			// Reset the flags before re-inserting them
			var teamsData = frame.ResolveList(frame.Global->TeamsData);
			var teamA = teamsData.GetPointer(0);
			var teamB = teamsData.GetPointer(1);
			teamA->TeamStatus = default;
			teamB->TeamStatus = default;

			// Set the winning flags for both teams
			SetWinningFlags(frame, teamA, teamB);

			// Set the health flags for both teams
			SetHealthFlags(frame, teamA, teamB);

			if (frame.Number % teamData->AIDirector.TickInterval == 0)
			{
				// How many coins are currently available on the map
				CountCoins(frame, teamData);

				// What are the health values (current sum, max, average and percentage)
				SetHealth(frame, teamData);

				// If the team is in the Winning condition, does it have an agent which would not affect that status if defeated?
				SenseLowScoreAgentState(frame, teamData);

				// With these info, the Director can set the team's strategies
				teamData->AIDirector.SetStrategies(frame, teamData->Index);
			}
		}

		private void CountCoins(Frame frame, TeamData* teamData)
		{
			// Counts how many coins are available
			teamData->AIDirector.Memory.AvailableCoins = (byte)frame.ComponentCount<Collectible>();
		}

		private void SetHealth(Frame frame, TeamData* team)
		{
			FP currentHealth = 0;
			FP maxHealth = 0;
			int charactersCount = 0;

			var allCharacters = frame.Filter<Character, Attributes, TeamInfo>();
			while (allCharacters.NextUnsafe(out var entity, out var character, out var attributes, out var teamInfo))
			{
				if(team->Index != teamInfo->Index)
				{
					continue;
				}

				FP current = AttributesHelper.GetCurrentValue(frame, entity, EAttributeType.Health);
				FP max = AttributesHelper.GetMaxValue(frame, entity, EAttributeType.Health);
				currentHealth += current;
				maxHealth += max;
				charactersCount++;
			}

			team->HealthsCurrent = currentHealth;
			team->HealthsMax = maxHealth;

			if (maxHealth != 0)
			{
				team->HealthsAverage = currentHealth / charactersCount;
				team->HealthsPercentage = currentHealth / maxHealth;
			}
		}

		private void SetWinningFlags(Frame frame, TeamData* teamA, TeamData* teamB)
		{
			// Score Flags
			int scoreA = teamA->Score;
			int scoreB = teamB->Score;
			if (scoreA < 10 && scoreB < 10)
				return;

			var difference = scoreA - scoreB;
			if (difference > 0)
			{
				if (difference > 3)
				{
					teamA->TeamStatus |= ETeamStatus.SafelyWinning;
				}
				else
				{
					teamA->TeamStatus |= ETeamStatus.Winning;
				}
			}
			else if (difference < 0)
			{
				if (difference < -3)
				{
					teamB->TeamStatus |= ETeamStatus.SafelyWinning;
				}
				else
				{
					teamB->TeamStatus |= ETeamStatus.Winning;
				}
			}
		}

		private void SetHealthFlags(Frame frame, TeamData* teamA, TeamData* teamB)
		{
			// Health Flags
			if (teamA->HealthsPercentage > FP._0_50 + FP._0_33)
			{
				teamA->TeamStatus |= ETeamStatus.HighHealth;
			}
			else if (teamA->HealthsPercentage > FP._0_10 + FP._0_33)
			{
				teamA->TeamStatus |= ETeamStatus.MidHealth;
			}
			else
			{
				teamA->TeamStatus |= ETeamStatus.LowHealth;
			}

			if (teamB->HealthsPercentage > FP._0_50 + FP._0_33)
			{
				teamB->TeamStatus |= ETeamStatus.HighHealth;
			}
			else if (teamB->HealthsPercentage > FP._0_10 + FP._0_33)
			{
				teamB->TeamStatus |= ETeamStatus.MidHealth;
			}
			else
			{
				teamB->TeamStatus |= ETeamStatus.LowHealth;
			}
		}

		private void SenseLowScoreAgentState(Frame frame, TeamData* teamData)
		{
			if (teamData->Score < 10)
			{
				teamData->HasLowScoreAgent = false;
				return;
			}

			var allCharacters = frame.Filter<Character, Transform2D, TeamInfo, Inventory>();
			while (allCharacters.NextUnsafe(out var entity, out var character, out var transform, out var teamInfo, out var inventory))
			{
				if (teamData->Index == teamInfo->Index)
				{
					if (teamData->Score - inventory->CollectiblesAmount >= 10)
					{
						teamData->HasLowScoreAgent = true;
						return;
					}
				}
			}

			teamData->HasLowScoreAgent = false;
		}
	}
}
