using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public unsafe class HealthSystem : SystemMainThreadFilter<HealthSystem.Filter>, ISignalOnComponentAdded<Health>, ISignalOnCharacterDamage
		, ISignalOnCreateSkill
	{
		private static readonly FP HealPercentage = FP._0_10 + FP._0_03;
		private static readonly FP DelayToStartHealing = 3;
		private static readonly FP DelayToHeal = 1;
		private static readonly FP DelayToRespawn = 3;

		public struct Filter
		{
			public EntityRef Entity;
			public Attributes* Attributes;
			public Health* Health;
		}

		public void OnAdded(Frame frame, EntityRef entity, Health* component)
		{
			SetNextHealTick(frame, component);
		}
		
		// When a character attacks or is damaged, a delay is added for the next time that the character will start healing
		// So this method basically deals with checking if the time to start healing has come, and actually applying the heal
		// StartHealingTime = when the character will start healing
		// NextHealTime = the next tick in which the character will recover a percentage of it's health, repeatedly
		public override void Update(Frame frame, ref Filter filter)
		{
			AttributeData healthAttribute = AttributesHelper.GetAttributeData(frame, filter.Entity, EAttributeType.Health);

			// A tower or a core is not a hero: it never heals, drops nothing and does not come back.
			// It has its own end (Structures.Destroyed).
			if (frame.Has<Structure>(filter.Entity))
			{
				if (filter.Health->IsDead == false && healthAttribute.CurrentValue <= 0)
				{
					Structures.Destroyed(frame, filter.Entity);
				}

				return;
			}

			// If the character was not defeated, but it's health value reached zero, we then signalise that the character is defeated
			if (filter.Health->IsDead == false && healthAttribute.CurrentValue <= 0)
			{
				CharacterDefeated(frame, filter.Entity);
			}

			// If the character health is full or the time to start healing wasn't reached yet, we just return
			if (healthAttribute.IsFull == true || frame.Number * frame.DeltaTime < filter.Health->StartHealingTime)
			{
				return;
			}

			if (filter.Health->CanSelfHeal == false || filter.Health->IsDead)
			{
				return;
			}

			// If the next heal tick is already here, we add to it's health and update the next heal time again
			if (frame.Number * frame.DeltaTime >= filter.Health->NextHealTime)
			{
				FP healValue = FPMath.Ceiling(healthAttribute.MaxValue * HealPercentage);
				AttributesHelper.ChangeAttribute(frame, filter.Entity, EAttributeType.Health, EModifierAppliance.OneTime, EModifierOperation.Add, healValue, 0);
				SetNextHealTick(frame, filter.Health);

				frame.Events.CharacterHealed(filter.Entity);
			}
		}

		public void OnCharacterDamage(Frame frame, EntityRef character)
		{
			if (frame.Unsafe.TryGetPointer<Health>(character, out var healthComponent))
			{
				SetNextStartHealingTick(frame, healthComponent);
			}
		}

		public void OnCreateSkill(Frame frame, EntityRef character, FPVector2 characterPos, SkillData data, FPVector2 actionDirection)
		{
			Health* healthComponent = frame.Unsafe.GetPointer<Health>(character);
			SetNextStartHealingTick(frame, healthComponent);
		}

		private void SetNextStartHealingTick(Frame frame, Health* healthComponent)
		{
			healthComponent->StartHealingTime = frame.Number * frame.DeltaTime + DelayToStartHealing;
			SetNextHealTick(frame, healthComponent, healthComponent->StartHealingTime + DelayToHeal);
		}

		private void SetNextHealTick(Frame frame, Health* healthComponent, FP value = default)
		{
			if (value == default)
			{
				value = frame.Number * frame.DeltaTime + DelayToHeal;
			}

			healthComponent->NextHealTime = value;
		}

		private void CharacterDefeated(Frame frame, EntityRef character)
		{
			// It is important to call this here so the Inventory System will drop the character's coins
			// in it's current position before it is hidden far away
			frame.Signals.OnCharacterDefeated(character);

			Health* health = frame.Unsafe.GetPointer<Health>(character);
			health->IsDead = true;

			// Reset the character's power 
			AttributesHelper.SetCurrentValue(frame, character, EAttributeType.Power, 1);

			if (frame.Unsafe.TryGetPointer<Respawn>(character, out var respawn))
			{
				respawn = frame.Unsafe.GetPointer<Respawn>(character);
				respawn->SpawnTimer = DelayToRespawn;
			}

			frame.Unsafe.GetPointer<Transform2D>(character)->Position = FPVector2.One * 100;

			frame.Events.CharacterDefeated(character);
		}
	}
}
