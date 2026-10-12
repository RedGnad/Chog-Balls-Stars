using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class ArcherSpecialSecondAttackData : AreaOfEffectAttackData
	{
		// This is an Area of Damage attack. Characters inside it's shape will be damaged upon reaching the effect intervals
		// The timer logic is contained within the attack's runtime data

		public override unsafe void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			base.OnUpdate(frame, attackEntity, attack);
			UpdateEffectInterval(frame, attackEntity, attack);
		}

		protected override unsafe void UpdateEffectInterval(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			ArcherSpecialAttackRD* attackRuntimeData = attack->AttackRuntimeData.ArcherSpecialAttackRD;
			attackRuntimeData->EffectInterval -= frame.DeltaTime;

			if (attackRuntimeData->EffectInterval <= 0)
			{
				attackRuntimeData->EffectInterval = EffectInterval;
				PerformDamage(frame, attackEntity, *attack);
			}
		}
	}
}
