using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SensorThreats : Sensor
	{
		public FP ReactionMaxDelay = FP._0;

		public void OnLinearAttackCreated(Frame frame, EntityRef attacker, FPVector2 characterPos,
			SkillData data, FPVector2 actionDirection)
		{
			if (data is ArcherBasicSkillData rangedSkill)
			{
				TeamInfo* attackerCharacterTeamInfo = frame.Unsafe.GetPointer<TeamInfo>(attacker);
				var bots = frame.Filter<Bot, AIMemory, TeamInfo>();
				while (bots.NextUnsafe(out EntityRef botEntity, out Bot* bot, out AIMemory* aiMemory, out TeamInfo* botTeamInfo))
				{
					if (bot->IsActive == false || attackerCharacterTeamInfo->Index == botTeamInfo->Index)
					{
						continue;
					}

					if (IsLinearAttackThreatening(frame, attacker, botEntity) == true)
					{

						FP reactionDelay = ReactionMaxDelay > 0 ? frame.RNG->NextInclusive(FP._0, ReactionMaxDelay) : 0;
						AIMemoryEntry* memoryEntry = aiMemory->Add(frame, EMemoryType.LineAvoidance, reactionDelay, data.RotationLockDuration);
						memoryEntry->Data.LineAvoidance->SetData(attacker);
					}
				}
			}
		}

		private bool IsLinearAttackThreatening(Frame frame, EntityRef attackingCharacter, EntityRef botCharacter)
		{
			Transform2D attackerTransform = frame.Get<Transform2D>(attackingCharacter);

			FPVector2 attackerPosisiton = attackerTransform.Position;
			FPVector2 botPosition = frame.Get<Transform2D>(botCharacter).Position;

			if (FPVector2.DistanceSquared(attackerPosisiton, botPosition) > 100)
			{
				return false;
			}

			FPVector2 dirAttackerBot = (botPosition - attackerPosisiton).Normalized;
			FPVector2 attackerForward = attackerTransform.Up;

			FP lookPercentage = FPVector2.Dot(dirAttackerBot, attackerForward);
			if (lookPercentage > FP._0_75)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public void OnCircularAttackCreated(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			AttackData attackData = frame.FindAsset<AttackData>(attack->AttackData.Id);
			if (attackData is ArcherSpecialSecondAttackData areaDamageAttackData)
			{
				TeamInfo* attackerCharacterTeamInfo = frame.Unsafe.GetPointer<TeamInfo>(attack->Source);
				var bots = frame.Filter<Bot, AIMemory, TeamInfo>();
				while (bots.NextUnsafe(out EntityRef botEntity, out Bot* bot, out AIMemory* aiMemory, out TeamInfo* botTeamInfo))
				{
					if (bot->IsActive == false || attackerCharacterTeamInfo->Index == botTeamInfo->Index)
					{
						continue;
					}

					FP reactionDelay = ReactionMaxDelay > 0 ? frame.RNG->NextInclusive(FP._0, ReactionMaxDelay) : 0;
					AIMemoryEntry* memoryEntry = aiMemory->Add(frame, EMemoryType.AreaAvoidance, reactionDelay, attackData.TTL);
					memoryEntry->Data.AreaAvoidance->SetData(attackEntity, areaDamageAttackData.Shape.CircleRadius);
				}
			}
		}
	}
}
