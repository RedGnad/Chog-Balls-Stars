using Photon.Deterministic;
using System;

namespace Quantum
{
	// Base class for any attack which performs a ballistic trajectory
	// The actual usage of the Y axis is only on the View, in Unity
	// The actual ballistic projectile is an entity which ignores any collision, and creates a post-impact attack
	// when it reaches it's final calculated position

	[System.Serializable]
	public unsafe abstract partial class BallisticAttackData : AttackData
	{
		public FP Velocity;

		// What is created when this type of attack reaches it's final position, which can be another attack, or anything else,
		// like another character which would be an example of how to Summon an entity using a ballistic attack
		public AssetRef<EntityPrototype> PostImpactPrototype;

		// When set, the post-impact attack uses this data instead of the data written in its prefab.
		public AssetRef<AttackData> PostImpactDataOverride;

		// Gets what is the desired final position, which is polled from the attack's runtime data, from it's union type
		public abstract FPVector2 GetTargetPosition(Frame frame, Attack* attack);

		public override unsafe void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			base.OnUpdate(frame, attackEntity, attack);

			MoveAttack(frame, attackEntity, attack);
			CheckImpact(frame, attackEntity, attack);
		}

		private void MoveAttack(Frame frame, EntityRef attackEntity, Attack* attack) {
			FPVector2 targetPosition = GetTargetPosition(frame, attack);
			Transform2D* projectileTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			projectileTransform->Position = FPVector2.MoveTowards(projectileTransform->Position, targetPosition, frame.DeltaTime * Velocity);
		}

		private void CheckImpact(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			FPVector2 targetPosition = GetTargetPosition(frame, attack);
			if (FPVector2.Distance(attackTransform->Position, targetPosition) <= FP._0_03)
			{
				EntityRef postImpactEntity = frame.Create(PostImpactPrototype);
				Attack* areaAttack = frame.Unsafe.GetPointer<Attack>(postImpactEntity);
				
				areaAttack->Source = attack->Source;
				if (PostImpactDataOverride.IsValid)
				{
					areaAttack->AttackData = PostImpactDataOverride;
				}

				var areaAttackData = frame.FindAsset<AreaOfEffectAttackData>(areaAttack->AttackData.Id);
				areaAttackData.OnCreate(frame, postImpactEntity, attack->Source, areaAttack);

				Transform2D* postImpactTransform = frame.Unsafe.GetPointer<Transform2D>(postImpactEntity);
				postImpactTransform->Position = targetPosition;

				OnDeactivate(frame, attackEntity);
				return;
			}
		}
	}
}
