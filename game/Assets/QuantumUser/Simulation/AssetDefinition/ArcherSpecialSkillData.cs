using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class ArcherSpecialSkillData : BallisticSkillData
	{
		protected override void SetAttackTargetPosition(Frame frame, EntityRef attackEntity, FPVector2 targetPosition)
		{
			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			ArcherSpecialAttackRD* attackRuntimeData = attack->AttackRuntimeData.ArcherSpecialAttackRD;
			attackRuntimeData->TargetPosition = targetPosition;
		}
	}
}
