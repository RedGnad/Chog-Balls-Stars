using Photon.Deterministic;
using System;

namespace Quantum
{
	// Base class for any attack which shoots a simple projectile with no extra logic other than causing damage

	[System.Serializable]
	public unsafe abstract partial class RangedAttackData : AttackData
	{
		public FP Velocity;

		public override void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			base.OnUpdate(frame, attackEntity, attack);
			bool wasDisabled = CheckHit(frame, attackEntity, attack);

			if(wasDisabled == true)
			{
				return;
			}

			MoveAttack(frame, attackEntity);
		}

		private bool CheckHit(Frame frame, EntityRef attackEntity, Attack* attack) {
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			var hits = PhysicsHelper.RaycastCollision(frame, attackTransform, Velocity);
			if (hits.Count == 0)
			{
				return false;
			}

			EntityRef targetEntity = CheckHits(frame, hits, attackEntity, attack, attackTransform, out bool wasDisabled);
			if (targetEntity != default)
			{
				OnApplyEffect(frame, attack->Source, targetEntity);
			}

			return wasDisabled;
		}

		private void MoveAttack(Frame frame, EntityRef attackEntity) {
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			attackTransform->Position += attackTransform->Up * frame.DeltaTime * Velocity;
		}
	}
}
