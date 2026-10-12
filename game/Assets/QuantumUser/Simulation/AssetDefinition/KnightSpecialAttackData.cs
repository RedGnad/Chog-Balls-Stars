using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class KnightSpecialAttackData : AttackData
	{
		public FP Velocity;
		public Shape2DConfig Shape;

		public override unsafe void OnCreate(Frame frame, EntityRef attackEntity, EntityRef character, Attack* attack)
		{
			base.OnCreate(frame, attackEntity, character, attack);

			KnightSpecialAttackRD* attackRuntimeData = attack->AttackRuntimeData.KnightSpecialAttackRD;
			attackRuntimeData->Targets = frame.AllocateList<EntityRef>();
		}

		public override void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			base.OnUpdate(frame, attackEntity, attack);

			EntityRef targetEntity = CheckTargetEntity(frame, attackEntity, attack);
			ApplyEffect(frame, attack, targetEntity);
			MoveAttack(frame, attackEntity);
		}

		private void ApplyEffect(Frame frame, Attack* attack, EntityRef targetEntity)
		{
			KnightSpecialAttackRD* attackRuntimeData = attack->AttackRuntimeData.KnightSpecialAttackRD;
			var targeted = frame.ResolveList<EntityRef>(attackRuntimeData->Targets);

			bool ignoreEntity = false;
			foreach (var entity in targeted)
			{
				if (entity == targetEntity)
				{
					ignoreEntity = true;
					break;
				}
			}

			if (targetEntity != default && ignoreEntity == false)
			{
				targeted.Add(targetEntity);
				OnApplyEffect(frame, attack->Source, targetEntity);
			}
		}

		private EntityRef CheckTargetEntity(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			Transform2D* projectileTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);

			var layerMask = frame.Layers.GetLayerMask("Static", "Character");
			var hits = PhysicsHelper.OverlapShape(frame, projectileTransform, layerMask, Shape);
			return CheckHits(frame, hits, attackEntity, attack, projectileTransform, out bool wasDisabled);
		}

		private void MoveAttack(Frame frame, EntityRef attackEntity)
		{
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			attackTransform->Position += attackTransform->Up * frame.DeltaTime * Velocity;
		}

		public override void OnDeactivate(Frame frame, EntityRef attackEntity)
		{
			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			KnightSpecialAttackRD* attackRuntimeData = attack->AttackRuntimeData.KnightSpecialAttackRD;
			frame.FreeList(attackRuntimeData->Targets);

			base.OnDeactivate(frame, attackEntity);
		}
	}
}
