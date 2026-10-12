using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public unsafe class RespawnSystem : SystemMainThreadFilter<RespawnSystem.Filter>, ISignalOnRespawnCharacter
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Health* Health;
			public Respawn* Respawn;
			public TeamInfo* TeamInfo;
			public Attributes* Attributes;
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (frame.Global->State == GameState.Over)
			{
				return;
			}

			// Every character has it's own respawning timer and, when it reached zero, we trigger it's spawn
			if (filter.Health->IsDead)
			{
				filter.Respawn->SpawnTimer -= frame.DeltaTime;
				if (filter.Respawn->SpawnTimer <= FP._0)
				{
					frame.Signals.OnRespawnCharacter(filter.Entity, false);
				}
			}
		}

		public void OnRespawnCharacter(Frame frame, EntityRef character, QBoolean IsFirstSpawn)
		{
			Attributes* attributes = frame.Unsafe.GetPointer<Attributes>(character);
			TeamInfo* teamInfo = frame.Unsafe.GetPointer<TeamInfo>(character);

			FPVector2 position = FPVector2.Zero;
			var halfSpawnerCount = frame.ComponentCount<SpawnPoint>() / 2;
			if (halfSpawnerCount != 0)
			{
				if (frame.TryGet<PlayerLink>(character, out var playerLink) == false)
				{
					return;
				}

				// If this is the first spawn, we spawn the characters in order
				// If it is not, then we randomize between the 3 spawners available
				var index = IsFirstSpawn ? GetSpawnerIndex(frame, teamInfo, playerLink.PlayerRef) : frame.RNG->Next(0, halfSpawnerCount);
				var count = 0;
				foreach (var (spawn, spawnPoint) in frame.Unsafe.GetComponentBlockIterator<SpawnPoint>())
				{
					// We are only interested in spawners which are from the character's own team
					if (spawnPoint->Team != teamInfo->Index)
					{
						continue;
					}

					if (count == index)
					{
						var spawnTransform = frame.Get<Transform2D>(spawn);
						position = spawnTransform.Position;
						break;
					}
					count++;
				}
			}

			// Once we have the spawn position, we setup the basic stuff, such as marking the character as "not dead",
			// healing it, and throwing an Unity event so we can do some VFX there
			Transform2D* characterTransform = frame.Unsafe.GetPointer<Transform2D>(character);
			characterTransform->Position = position;

			frame.Unsafe.GetPointer<Health>(character)->IsDead = false;
			frame.Unsafe.GetPointer<Immunity>(character)->Timer = 5;

			var dataDictionary = frame.ResolveDictionary(attributes->DataDictionary);
			dataDictionary.TryGetValuePointer(EAttributeType.Health, out var healthAttribute);
			healthAttribute->Init(frame);

			frame.Events.CharacterRespawned(character);
		}

		private int GetSpawnerIndex(Frame frame, TeamInfo* teamInfo, PlayerRef playerRef)
		{
			int spawnerIndex = 0;

			var characters = frame.Filter<PlayerLink, TeamInfo>();
			while (characters.Next(out EntityRef entity, out PlayerLink playerLink, out TeamInfo otherTeamInfo))
			{
				if(otherTeamInfo.Index == teamInfo->Index && playerLink.PlayerRef < playerRef)
				{
					spawnerIndex++;
				}
			}

			return spawnerIndex;
		}
	}
}