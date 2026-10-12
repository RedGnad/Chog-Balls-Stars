using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class KnightBasicSkillData : SkillData
	{
		public FP[] Angles;

		public override EntityRef OnCreate(Frame frame, EntityRef source, SkillData data, FPVector2 characterPos, FPVector2 actionDirection)
		{
			EntityRef skillEntity = base.OnCreate(frame, source, data, characterPos, actionDirection);
			Skill* skill = frame.Unsafe.GetPointer<Skill>(skillEntity);

			KnightBasicSkillRD* skillRuntimeData = skill->SkillRuntimeData.KnightBasicSkillRD;
			skillRuntimeData->Attacks = frame.AllocateList<EntityRef>();
			skillRuntimeData->AlreadyAffected = frame.AllocateList<EntityRef>();

			Transform2D sourceTransform = frame.Get<Transform2D>(source);

			for (int i = 0; i < Angles.Length; i++)
			{
				EntityRef attackEntity = OnAction(frame, source, skillEntity, skill);
				FP rotation = sourceTransform.Rotation + Angles[i] * FP.Deg2Rad;
				CreateProjectile(frame, source, skillEntity, attackEntity, skillRuntimeData, sourceTransform.Position, rotation);
			}
			return skillEntity;
		}

		private void CreateProjectile(Frame frame, EntityRef source, EntityRef skillEntity, EntityRef attackEntity, KnightBasicSkillRD* skillRuntimeData, FPVector2 position, FP rotation)
		{
			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			attack->Source = source;

			KnightBasicAttackRD* attackRuntimeData = attack->AttackRuntimeData.KnightBasicAttackRD;
			attackRuntimeData->SkillEntity = skillEntity;

			var projectiles = frame.ResolveList(skillRuntimeData->Attacks);
			projectiles.Add(attackEntity);

			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);

			attackTransform->Rotation = rotation;
			attackTransform->Position = position + attackTransform->Up;
		}

		public override unsafe void OnUpdate(Frame frame, EntityRef source, EntityRef skillEntity, Skill* skill)
		{
			KnightBasicSkillRD* skillRuntimeData = skill->SkillRuntimeData.KnightBasicSkillRD;
			var projectiles = frame.ResolveList(skillRuntimeData->Attacks);

			if (projectiles.Count == 0)
			{
				frame.FreeList(skillRuntimeData->AlreadyAffected);
				frame.FreeList(skillRuntimeData->Attacks);
				OnDeactivate(frame, skillEntity, skill);
			}
		}
	}
}
