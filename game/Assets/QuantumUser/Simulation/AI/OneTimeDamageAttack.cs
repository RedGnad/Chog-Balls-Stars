using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe abstract partial class OneTimeDamageAttack : AttackData
	{
		// This type of attack shares, with other attacks, a common list of targets that were already affected
		// so the projectiles doesn't cause damage to a target more than once

		public FP Velocity;
		public Shape2DConfig Shape;

		// Polymorphic logic as the Skill Entity, AlreadyAffected and Projectiles lists are stored in a union type
		// so other implementations of this attack type can get it from the specific union field

		// Get the Skill entity which originated this Attack entity
		protected abstract EntityRef GetSkillEntity(Frame frame, Attack* attack);

		// Get the list of entities that were already added as a target so we don't cause extra damage to them
		protected abstract Collections.QList<EntityRef> GetAlreadyAffectedList(Frame frame, Attack* attack);

		// Get the list of Attacks which are "siblings" to this one
		protected abstract Collections.QList<EntityRef> GetAttacksList(Frame frame, Attack* attack);

		// Perform the collision checks and try applying the effect to the characters that it finds
		public override void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			base.OnUpdate(frame, attackEntity, attack);

			EntityRef targetEntity = CheckHits(frame, attackEntity, attack, out bool wasDisabled);

			if (wasDisabled == true)
			{
				return;
			}

			if (targetEntity != default)
			{
				ApplyEffect(frame, attack, targetEntity);
			}
			
			MoveAttack(frame, attackEntity);
		}

		private void MoveAttack(Frame frame, EntityRef attackEntity)
		{
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			attackTransform->Position += attackTransform->Up * frame.DeltaTime * Velocity;
		}

		// We only apply the effect if that entity is not yet in the targets list, to avoid causing extra damage
		private void ApplyEffect(Frame frame, Attack* attack, EntityRef targetEntity)
		{
			var alreadyAffected = GetAlreadyAffectedList(frame, attack);

			bool ignoreEntity = false;
			foreach (var entity in alreadyAffected)
			{
				if (entity == targetEntity)
				{
					ignoreEntity = true;
					break;
				}
			}

			if (targetEntity != default && ignoreEntity == false)
			{
				alreadyAffected.Add(targetEntity);
				OnApplyEffect(frame, attack->Source, targetEntity);
			}
		}

		private EntityRef CheckHits(Frame frame, EntityRef attackEntity, Attack* attack, out bool wasDisabled)
		{
			wasDisabled = false;

			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			var layerMask = frame.Layers.GetLayerMask("Static", "Character");
			var hits = PhysicsHelper.OverlapShape(frame, attackTransform, layerMask, Shape);
			if (hits.Count == 0)
			{
				return default;
			}
			return CheckHits(frame, hits, attackEntity, attack, attackTransform, out wasDisabled);
		}

		// When this attack is disabled, remove it from the attacks list
		public override void OnDeactivate(Frame frame, EntityRef attackEntity)
		{
			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			var attacksList = GetAttacksList(frame, attack);
			attacksList.Remove(attackEntity);

			base.OnDeactivate(frame, attackEntity);
		}
	}
}
