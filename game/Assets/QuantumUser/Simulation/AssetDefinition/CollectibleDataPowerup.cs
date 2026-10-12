using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class CollectibleDataPowerup : CollectibleData
	{
		// Collecting/Dropping powerups increases/reduces that character's attributes
		// For Health and Speed, we increase a percentage
		// For Power, we just add a single value on top of the character's normal damage

		public FP HealthPercentageIncrease = FP._0_10;
		public FP SpeedPercentageIncrease = FP._0_10;
		public FP PowerIncrease = FP._0_10;

		public override void OnCollect(Frame frame, EntityRef entity, Inventory* inventory)
		{
			AttributesHelper.ApplyPowerupEffect(frame, entity, EAttributeType.Health, HealthPercentageIncrease);
			AttributesHelper.ApplyPowerupEffect(frame, entity, EAttributeType.Speed, SpeedPercentageIncrease);
			AttributesHelper.AddValue(frame, entity, EAttributeType.Power, PowerIncrease);
		}

		public override void OnDrop(Frame frame, EntityRef entity, Inventory* inventory, int amount, FPVector2 position)
		{
			base.OnDrop(frame, entity, inventory, amount, position);
			AttributesHelper.RemovePowerupEffects(frame, entity, EAttributeType.Health);
			AttributesHelper.RemovePowerupEffects(frame, entity, EAttributeType.Speed);
			AttributesHelper.AddValue(frame, entity, EAttributeType.Power, -PowerIncrease);
		}
	}
}
