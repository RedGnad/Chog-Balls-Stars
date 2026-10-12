using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SpellcasterSpecialSkillData : SkillData
	{
		public FP MaxDistance = 5;
		public FP Radius = 1;

		public override EntityRef OnCreate(Frame frame, EntityRef source, SkillData data, FPVector2 characterPos, FPVector2 actionDirection)
		{
			EntityRef skillEntity = base.OnCreate(frame, source, data, characterPos, actionDirection);

			Transform2D sourceTransform = frame.Get<Transform2D>(source);
			FP distance = 0;
			if (actionDirection.Magnitude <= FP._0_10)
			{
				EnemyPositionsHelper.TryGetClosestCharacterDistance(frame, source, sourceTransform, MaxDistance, true, false, out distance);
			}
			else
			{
				distance = FPMath.Clamp(actionDirection.Magnitude, 0, MaxDistance);
			}

			Skill* skill = frame.Unsafe.GetPointer<Skill>(skillEntity);
			SpellcasterSpecialSkillRD* skillRuntimeData = skill->SkillRuntimeData.SpellcasterSpecialSkillRD;

			skillRuntimeData->TargetPosition = sourceTransform.Position + (sourceTransform.Up * distance);
			return skillEntity;
		}

		public override EntityRef OnAction(Frame frame, EntityRef source, EntityRef skillEntity, Skill* skill)
		{
			EntityRef attackEntity = base.OnAction(frame, source, skillEntity, skill);
			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			SpellcasterSpecialAttackRD* attackRuntimeData = attack->AttackRuntimeData.SpellcasterSpecialAttackRD;

			int count = (skill->TTL / ActionInterval).AsInt;

			SpellcasterSpecialSkillRD* skillRuntimeData = skill->SkillRuntimeData.SpellcasterSpecialSkillRD;
			attackRuntimeData->TargetPosition = skillRuntimeData->TargetPosition + GetTargetOffset(count);
			return attackEntity;
		}

		private FPVector2 GetTargetOffset(int index)
		{
			switch (index)
			{
				case 0:
					return FPVector2.Zero;

				case 1:
					return FPVector2.One * Radius;

				case 2:
					return -FPVector2.One * Radius;

				case 3:
					return new FPVector2(1, -1) * Radius;

				case 4:
					return new FPVector2(-1, 1) * Radius;
				default:
					return FPVector2.Zero;
			}
		}
	}
}
