using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SpellcasterSpecialSecondAttackData : AreaOfEffectAttackData
	{
		public override unsafe void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			base.OnUpdate(frame, attackEntity, attack);
			UpdateEffectInterval(frame, attackEntity, attack);
		}

		protected override unsafe void UpdateEffectInterval(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			SpellcasterSpecialAttackRD* attackRuntimeData = attack->AttackRuntimeData.SpellcasterSpecialAttackRD;
			attackRuntimeData->EffectInterval -= frame.DeltaTime;

			if (attackRuntimeData->EffectInterval <= 0)
			{
				attackRuntimeData->EffectInterval = EffectInterval;
				PerformDamage(frame, attackEntity, *attack);
			}
		}
	}
}
