using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SpellcasterBasicSkillData : BallisticSkillData
	{
		protected override void SetAttackTargetPosition(Frame frame, EntityRef attackEntity, FPVector2 targetPosition)
		{
			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			SpellcasterBasicAttackRD* attackRuntimeData = attack->AttackRuntimeData.SpellcasterBasicAttackRD;
			attackRuntimeData->TargetPosition = targetPosition;
		}
	}
}
