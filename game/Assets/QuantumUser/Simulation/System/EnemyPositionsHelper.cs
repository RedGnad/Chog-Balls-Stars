using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	public static unsafe class EnemyPositionsHelper
	{
		public static bool TryGetClosestCharacter(Frame frame, EntityRef character, FP maxDistance, bool checkLineSight, bool ignoreSameTeam, out EntityRef targetCharacter)
		{
			Transform2D characterTransform = frame.Get<Transform2D>(character);
			TeamInfo characterTeamInfo = frame.Get<TeamInfo>(character);
			FP distance = FP.MaxValue;
			FPVector2 position = FPVector2.Zero;
			targetCharacter = EntityRef.None;

			var charactersFilter = frame.Filter<Transform2D, Character, TeamInfo>();
			while (charactersFilter.NextUnsafe(out var entity, out var transform, out var charc, out var teamInfo))
			{
				if (character == entity)
				{
					continue;
				}
				if (ignoreSameTeam == true && characterTeamInfo.Index == teamInfo->Index)
				{
					continue;
				}
				var currentDistance = FPVector2.Distance(characterTransform.Position, transform->Position);
				if (maxDistance < currentDistance)
				{
					continue;
				}

				if (checkLineSight && LineOfSightHelper.HasLineOfSight(frame, characterTransform.Position, transform->Position) == false)
				{
					continue;
				}

				if (currentDistance < distance)
				{
					distance = currentDistance;
					position = transform->Position;
					targetCharacter = entity;
				}
			}
			if (distance != FP.MaxValue)
			{
				return true;
			}
			return false;
		}

		public static bool TryGetClosestCharacterDistance(Frame frame, EntityRef character, Transform2D characterTransform, FP maxDistance, bool checkTeam, bool checkLineSight, out FP enemyDistance)
		{
			bool value = TryGetClosestCharacter(frame, character, maxDistance, checkLineSight, true, out var targetCharacter);
			enemyDistance = 0;
			if (targetCharacter != EntityRef.None)
			{
				Transform2D targetTransform = frame.Get<Transform2D>(targetCharacter);
				enemyDistance = (targetTransform.Position - characterTransform.Position).Magnitude;
			}
			return value;
		}

		public static bool TryGetClosestCharacterDirection(Frame frame, EntityRef character, Transform2D characterTransform, FP maxDistance, bool checkTeam, bool checkLineSight, out FPVector2 enemyDirection)
		{
			bool value = TryGetClosestCharacter(frame, character, maxDistance, checkLineSight, true, out var targetCharacter);
			enemyDirection = FPVector2.Zero;
			if (targetCharacter != EntityRef.None)
			{
				Transform2D targetTransform = frame.Get<Transform2D>(targetCharacter);
				enemyDirection = (targetTransform.Position - characterTransform.Position).Normalized;
			}
			return value;
		}

		public static bool TryGetClosestEnemyCharacter(Frame frame, EntityRef characterEntity, FP maxDistance, FP angle, out EntityRef targetCharacter)
		{
			TeamInfo characterTeamInfo = frame.Get<TeamInfo>(characterEntity);
			Transform2D characterTransform = frame.Get<Transform2D>(characterEntity);
			FP distance = FP.MaxValue;
			FPVector2 position = FPVector2.Zero;
			targetCharacter = EntityRef.None;

			var charactersFilter = frame.Filter<Character, Transform2D, TeamInfo>();
			while (charactersFilter.NextUnsafe(out var entity, out var character, out var transform, out var currentTeamInfo))
			{
				if (characterEntity == entity)
				{
					continue;
				}

				if (characterTeamInfo.Index == currentTeamInfo->Index)
				{
					continue;
				}

				if (FPVector2.RadiansSigned(characterTransform.Forward, characterTransform.Position - transform->Position) * FP.Rad2Deg > angle)
				{
					continue;
				}

				var currentDistance = FPVector2.Distance(characterTransform.Position, transform->Position);
				if (maxDistance < currentDistance)
				{
					continue;
				}

				if (currentDistance < distance)
				{
					distance = currentDistance;
					position = transform->Position;
					targetCharacter = entity;
				}
			}
			if (distance != FP.MaxValue)
			{
				return true;
			}
			return false;
		}

		public static bool TryGetMostValuableCharacter(Frame frame, EntityRef character, Transform2D characterTransform, FP maxDistance, bool checkLineSight, bool ignoreSameTeam, out EntityRef targetCharacter)
		{
			TeamInfo characterTeamInfo = frame.Get<TeamInfo>(character);
			int collectiblesAmount = -1;
			FPVector2 position = FPVector2.Zero;
			targetCharacter = EntityRef.None;

			var charactersFilter = frame.Filter<Transform2D, Character, TeamInfo, Inventory>();
			while (charactersFilter.NextUnsafe(out var entity, out var transform, out var charc, out var teamInfo, out var inventory))
			{
				if (character == entity)
				{
					continue;
				}
				if (ignoreSameTeam == true && characterTeamInfo.Index == teamInfo->Index)
				{
					continue;
				}
				var currentDistance = FPVector2.Distance(characterTransform.Position, transform->Position);
				if (maxDistance < currentDistance)
				{
					continue;
				}

				if (checkLineSight && LineOfSightHelper.HasLineOfSight(frame, characterTransform.Position, transform->Position) == false)
				{
					continue;
				}

				if (inventory->CollectiblesAmount > collectiblesAmount)
				{
					collectiblesAmount = inventory->CollectiblesAmount;
					position = transform->Position;
					targetCharacter = entity;
				}
			}

			if (collectiblesAmount != -1)
			{
				return true;
			}
			return false;
		}
	}
}