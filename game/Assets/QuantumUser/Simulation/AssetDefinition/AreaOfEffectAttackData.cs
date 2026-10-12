using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe abstract partial class AreaOfEffectAttackData : AttackData
	{
		// Applies effects on an area accordingly to some intervaled repetitions

		public FP EffectInterval = 1;
		public FP EffectRepeatCount = 1;
		public Shape2DConfig Shape;

		protected abstract void UpdateEffectInterval(Frame frame, EntityRef attackEntity, Attack* attack);

		protected void PerformDamage(Frame frame, EntityRef attackEntity, Attack attack)
		{
			TeamInfo sourceTeamInfo = frame.Get<TeamInfo>(attack.Source);
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);

			var layerMask = frame.Layers.GetLayerMask("Character");
			var hits = PhysicsHelper.OverlapShape(frame, attackTransform, layerMask, Shape);
			for (int i = 0; i < hits.Count; i++)
			{
				EntityRef target = hits[i].Entity;
				TeamInfo* teamInfo = frame.Unsafe.GetPointer<TeamInfo>(target);

				if (HitAlies == false && sourceTeamInfo.Index == teamInfo->Index)
				{
					continue;
				}

				if (IgnoreOwner == true && target.Equals(attack.Source) == true)
				{
					continue;
				}
				OnApplyEffect(frame, attack.Source, target);
			}
		}
	}
}
