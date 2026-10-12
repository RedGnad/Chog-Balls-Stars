using Photon.Deterministic;
using Quantum.Physics2D;

namespace Quantum
{
	// An area that applies its effects once to every valid target it touches while it lives.
	// One asset describes one signature shape: narrow and fast is a beam, wide and slow a wave,
	// standing in front of the caster a flash, standing around the caster a nova.
	[System.Serializable]
	public unsafe partial class GenesisSweepAttackData : AttackData
	{
		// Speed along the facing of the attack; 0 keeps the area where it was created.
		public FP Velocity;

		// Distance between the caster and the centre of the area when it is created.
		public FP ForwardOffset;

		public FP Radius;

		// A wall ends a moving area; otherwise it passes over walls.
		public bool StopAtWalls;

		// What the area leaves where it ends, for instance a zone that stays: the prototype of
		// that attack and the data it uses. Nothing is left when the data is not set.
		public AssetRef<EntityPrototype> LeavesBehindPrototype;
		public AssetRef<AttackData> LeavesBehindData;

		public override void OnCreate(Frame frame, EntityRef attackEntity, EntityRef source, Attack* attack)
		{
			base.OnCreate(frame, attackEntity, source, attack);

			attack->AttackRuntimeData.GenesisSweepAttackRD->Targets = frame.AllocateList<EntityRef>();

			Transform2D* transform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			transform->Position += transform->Up * ForwardOffset;
		}

		public override void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			base.OnUpdate(frame, attackEntity, attack);

			Transform2D* transform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			AffectNewTargets(frame, attack, transform);

			if (Velocity == 0)
			{
				return;
			}

			var nextPosition = transform->Position + transform->Up * frame.DeltaTime * Velocity;
			if (StopAtWalls && MeetsWall(frame, transform->Position, nextPosition))
			{
				OnDeactivate(frame, attackEntity);
				return;
			}

			transform->Position = nextPosition;
		}

		public override void OnDeactivate(Frame frame, EntityRef attackEntity)
		{
			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			frame.FreeList(attack->AttackRuntimeData.GenesisSweepAttackRD->Targets);

			if (LeavesBehindData.IsValid && LeavesBehindPrototype.IsValid)
			{
				LeaveBehind(frame, attack->Source, frame.Get<Transform2D>(attackEntity).Position);
			}

			base.OnDeactivate(frame, attackEntity);
		}

		private void LeaveBehind(Frame frame, EntityRef source, FPVector2 position)
		{
			EntityRef leftEntity = frame.Create(LeavesBehindPrototype);
			Attack* left = frame.Unsafe.GetPointer<Attack>(leftEntity);
			left->Source = source;
			left->AttackData = LeavesBehindData;

			Transform2D* leftTransform = frame.Unsafe.GetPointer<Transform2D>(leftEntity);
			leftTransform->Position = position;

			AttackData leftData = frame.FindAsset<AttackData>(LeavesBehindData.Id);
			leftData.OnCreate(frame, leftEntity, source, left);
		}

		private void AffectNewTargets(Frame frame, Attack* attack, Transform2D* transform)
		{
			var hits = frame.Physics2D.OverlapShape(*transform, Shape2D.CreateCircle(Radius),
				frame.Layers.GetLayerMask("Character"),
				QueryOptions.ComputeDetailedInfo | QueryOptions.HitKinematics | QueryOptions.HitStatics);
			var affected = frame.ResolveList(attack->AttackRuntimeData.GenesisSweepAttackRD->Targets);

			for (int i = 0; i < hits.Count; i++)
			{
				EntityRef target = hits[i].Entity;
				if (CanAffect(frame, attack, target) == false || IsListed(affected, target))
				{
					continue;
				}

				affected.Add(target);
				OnApplyEffect(frame, attack->Source, target);
			}
		}

		private static bool IsListed(Collections.QList<EntityRef> entities, EntityRef entity)
		{
			for (int i = 0; i < entities.Count; i++)
			{
				if (entities[i] == entity)
				{
					return true;
				}
			}

			return false;
		}

		// Trigger volumes such as bushes are not walls.
		private static bool MeetsWall(Frame frame, FPVector2 from, FPVector2 to)
		{
			var hits = frame.Physics2D.LinecastAll(from, to, frame.Layers.GetLayerMask("Static"),
				QueryOptions.ComputeDetailedInfo | QueryOptions.HitKinematics | QueryOptions.HitStatics);
			for (int i = 0; i < hits.Count; i++)
			{
				if (hits[i].IsTrigger == false)
				{
					return true;
				}
			}

			return false;
		}
	}
}
