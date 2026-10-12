using Photon.Deterministic;
using Quantum.Collections;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class KnightBasicAttackData : OneTimeDamageAttack
	{
		protected override unsafe EntityRef GetSkillEntity(Frame frame, Attack* attack)
		{
			KnightBasicAttackRD* attackRuntimeData = attack->AttackRuntimeData.KnightBasicAttackRD;
			return attackRuntimeData->SkillEntity;
		}

		protected override unsafe QList<EntityRef> GetAlreadyAffectedList(Frame frame, Attack* attack)
		{
			EntityRef skillEntity = GetSkillEntity(frame, attack);
			Skill* skill = frame.Unsafe.GetPointer<Skill>(skillEntity);
			KnightBasicSkillRD* skillRuntimeData = skill->SkillRuntimeData.KnightBasicSkillRD;
			return frame.ResolveList(skillRuntimeData->AlreadyAffected);
		}

		protected override unsafe QList<EntityRef> GetAttacksList(Frame frame, Attack* attack)
		{
			EntityRef skillEntity = GetSkillEntity(frame, attack);
			Skill* skill = frame.Unsafe.GetPointer<Skill>(skillEntity);
			KnightBasicSkillRD* skillRuntimeData = skill->SkillRuntimeData.KnightBasicSkillRD;
			return frame.ResolveList(skillRuntimeData->Attacks);
		}
	}
}
