using Photon.Deterministic;
using Quantum.Physics2D;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quantum
{
	// Base class for all attack assets

	public unsafe abstract partial class AttackData : AssetObject
	{
#if QUANTUM_UNITY
		[Header("View Configuration", order = 9)]
		public Sound SFX;

		// Effect shown while the attack lives, by an attack prefab whose view reads its look from
		// this data (GenesisAttackView), and the scale that fits it to the area of the attack.
		public GameObject Visual;
		public float VisualScale = 1;

		// Colour given to that effect; a transparent colour leaves the effect as it was authored.
		public Color VisualTint;

		// How the attack is drawn when it is not the effect prefab above: a shape built from the
		// colours of the caster's Eyes (GenesisAttackView), with this unlit material, and the size
		// of that shape where it has one (the reach of a fan, the radius of a ground disc).
		public GenesisVisualKind VisualKind;
		public Material ShapeMaterial;
		public float VisualRadius;
#endif

		// Set on the attacks of a Genesis signature: each target they affect raises
		// GenesisSignatureHit with this effect (a position in the roster's effect table, 0 is raw)
		// and how long it stays on the target, so the view can show what happened.
		public bool IsGenesisSignature;
		public byte GenesisEffect;
		public FP GenesisEffectDuration;
		
		// For how many time the Attack is activated
		public FP TTL;

		// Should this attack ignored it's owner
		public bool IgnoreOwner;

		// Should this attack hit enemies
		public bool HitEnemies;

		// Should this attack hit allies
		public bool HitAlies;

		// Is this attack destroyed upon contact to any Dynamic collider?
		public bool DestroyOnHitDynamic;

		// Is this attack destroyed upon contact to any Static collider?
		public bool DestroyOnHitStatic;

		// The effects that are applied to OTHERS
		public Effect[] EffectsOnOthers;

		// The effects that are applied to the attack's OWNER whtn it hits another entity
		public Effect[] EffectsOnSource;

		// Polymorphic OnCreate logic
		public virtual void OnCreate(Frame frame, EntityRef attackEntity, EntityRef source, Attack* attack)
		{
			frame.Signals.OnCreateAttack(attackEntity, attack);
			frame.Events.OnCreateAttack(Guid);
		}

		// Polymorphic OnUpdate logic
		public virtual void OnUpdate(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			attack->TTL += frame.DeltaTime;
		}

		// Polymorphic OnUpdate logic
		public virtual void OnDeactivate(Frame frame, EntityRef attackEntity)
		{
			frame.Signals.OnDisableAttack(attackEntity);
			frame.Destroy(attackEntity);
		}

		// Apply the effects, defined in the Unity inspector
		public virtual void OnApplyEffect(Frame frame, EntityRef source, EntityRef target)
		{
			foreach (var effect in EffectsOnOthers)
			{
				EffectsHelper.OnApply(frame, source, target, effect);
			}

			for (int i = 0; i < EffectsOnSource.Length; i++)
			{
				Effect effect = EffectsOnSource[i];
				EffectsHelper.OnApply(frame, source, source, effect);
			}

			if (IsGenesisSignature)
			{
				frame.Events.GenesisSignatureHit(target, GenesisEffect, GenesisEffectDuration);
			}
		}

		// Collision logic, investigating what on the hits collection should be affected
		protected EntityRef CheckHits(Frame frame, HitCollection hits, EntityRef attackEntity,
			Attack* attack, Transform2D* attackTransform, out bool wasDisabled)
		{
			wasDisabled = false;

			for (var i = 0; i < hits.Count; i++)
			{
				if (hits[i].IsDynamic == false)
				{
					if (hits[i].IsTrigger == true)
					{
						continue;
					}
					else if (DestroyOnHitStatic == true)
					{
						OnDeactivate(frame, attackEntity);
						wasDisabled = true;
						return default;
					}
				}

				var target = hits[i].Entity;
				if (CanAffect(frame, attack, target))
				{
					if (DestroyOnHitDynamic == true)
					{
						attackTransform->Position = hits[i].Point;
						OnDeactivate(frame, attackEntity);
						wasDisabled = true;
					}
					return target;
				}
			}

			return default;
		}

		// Whether this attack may affect that entity: it has health, is alive, and is on a side the attack hits.
		protected bool CanAffect(Frame frame, Attack* attack, EntityRef target)
		{
			if (frame.Exists(target) == false || frame.TryGet(target, out Health targetHealth) == false)
			{
				return false;
			}

			if (IgnoreOwner && target == attack->Source)
			{
				return false;
			}

			if (targetHealth.IsDead)
			{
				return false;
			}

			TeamInfo sourceTeamInfo = frame.Get<TeamInfo>(attack->Source);
			if (frame.TryGet<TeamInfo>(target, out var teamInfo))
			{
				if (HitEnemies == false && sourceTeamInfo.Index != teamInfo.Index)
				{
					return false;
				}

				if (HitAlies == false && sourceTeamInfo.Index == teamInfo.Index)
				{
					return false;
				}
			}

			return true;
		}
	}
}
