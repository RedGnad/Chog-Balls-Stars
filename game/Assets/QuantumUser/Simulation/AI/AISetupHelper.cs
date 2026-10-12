using System.Collections.Generic;

namespace Quantum
{
	public unsafe static class AISetupHelper
	{
		public static void FillWithBots(Frame frame)
		{
			// for each team, have an integer with max players count
			// for every character, subtract from those integers based on player connectivity and their team ids
			// with the remnant integers, create bots and set teams

			List<int> neededPlayerRefs = new List<int>() { 1, 2, 3, 4, 5, 6 };
			int neededBotsTeamA = 3;
			int neededBotsTeamB = 3;

			for (int i = 0; i < frame.MaxPlayerCount; i++)
			{
				var playerLinks = frame.Filter<PlayerLink, TeamInfo>();
				while(playerLinks.Next(out EntityRef entity, out PlayerLink playerLink, out TeamInfo teamInfo))
				{
					if(playerLink.PlayerRef == i)
					{
						if(teamInfo.Index == 0)
						{
							neededBotsTeamA -= 1;
						}
						else
						{
							neededBotsTeamB -= 1;
						}

						neededPlayerRefs.Remove(i);
					}
				}
			}

			for (int i = 0; i < neededBotsTeamA; i++)
			{
				int randomIndex = frame.RNG->Next(0, neededPlayerRefs.Count);
				CreateBot(frame, neededPlayerRefs[randomIndex], 0);
				neededPlayerRefs.RemoveAt(randomIndex);
			}

			for (int i = 0; i < neededBotsTeamB; i++)
			{
				int randomIndex = frame.RNG->Next(0, neededPlayerRefs.Count);
				CreateBot(frame, neededPlayerRefs[randomIndex], 1);
				neededPlayerRefs.RemoveAt(randomIndex);
			}
		}

		public static void CreateBot(Frame frame, PlayerRef playerRef, int teamId)
		{
			// With a Genesis roster the bot is a real Genesis and plays its class.
			var isGenesis = GenesisCast.TryDrawBotHero(frame, out var genesisHero, out var botPrototype);
			if (isGenesis == false)
			{
				int randomBotId = frame.RNG->Next(0, frame.RuntimeConfig.RoomFillBots.Length);
				botPrototype = frame.RuntimeConfig.RoomFillBots[randomBotId];
			}

			EntityRef botCharacter = frame.Create(botPrototype);

			if (isGenesis)
			{
				GenesisCast.Tag(frame, botCharacter, genesisHero);
			}

			// Store it's PlayerRef so we can later use it for input polling
			PlayerLink* playerLink = frame.Unsafe.GetPointer<PlayerLink>(botCharacter);
			playerLink->PlayerRef = playerRef;

			// Save the character's team, also defined on the Menu
			TeamInfo* teamInfo = frame.Unsafe.GetPointer<TeamInfo>(botCharacter);
			teamInfo->Index = teamId;

			// Spawn the character
			frame.Signals.OnRespawnCharacter(botCharacter, true);
			TestOptions.ApplyToNewCharacter(frame, botCharacter);

			Botify(frame, botCharacter);
		}

		public static void Botify(Frame frame, EntityRef entity)
		{
			Bot* bot = frame.Unsafe.GetPointer<Bot>(entity);

			// -- NAVMESH
			var agentConfig = frame.FindAsset<NavMeshAgentConfig>(bot->NavMeshAgentConfig);
			var navMeshPathfinder = NavMeshPathfinder.Create(frame, entity, agentConfig);
			frame.Add<NavMeshPathfinder>(entity, navMeshPathfinder);
			//frame.AddOrGet<NavMeshPathfinder>(entity, out var pathfinder);
			//pathfinder->SetConfig(frame, entity, bot->NavMeshAgentConfig);

			if (frame.Has<NavMeshSteeringAgent>(entity) == false)
			{
				frame.Add<NavMeshSteeringAgent>(entity);
			}

			if (frame.Has<NavMeshAvoidanceAgent>(entity) == false)
			{
				frame.Add<NavMeshAvoidanceAgent>(entity);
			}

			// -- BLACKBOARD
			frame.Add<AIBlackboardComponent>(entity, out var blackboardComponent);
			var blackboardInitializer = frame.FindAsset<AIBlackboardInitializer>(bot->BlackboardInitializer.Id);
			AIBlackboardInitializer.InitializeBlackboard(frame, blackboardComponent, blackboardInitializer);

			// -- HFSM AGENT
			frame.Add<HFSMAgent>(entity, out var hfsmAgent);
			HFSMRoot hfsmRoot = frame.FindAsset<HFSMRoot>(bot->HFSMRoot.Id);
			HFSMManager.Init(frame, entity, hfsmRoot);
			hfsmAgent->Config = bot->AIConfig;

			bot->IsActive = true;

			Quantum.BotSDK.BotSDKDebuggerSystem.AddToDebugger<HFSMAgent>(frame, entity, *hfsmAgent);
		}

		public static void Debotify(Frame frame, EntityRef entity)
		{
			frame.Remove<NavMeshPathfinder>(entity);
			frame.Remove<NavMeshSteeringAgent>(entity);
			frame.Remove<NavMeshAvoidanceAgent>(entity);

			frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->Free(frame);
			frame.Remove<AIBlackboardComponent>(entity);

			frame.Remove<HFSMAgent>(entity);

			frame.Unsafe.GetPointer<Bot>(entity)->IsActive = false;
		}
	}
}
