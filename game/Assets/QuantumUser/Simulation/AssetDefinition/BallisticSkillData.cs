using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe abstract partial class BallisticSkillData : SkillData
	{
		public FP MaxDistance = 5;

		public override EntityRef OnAction(Frame frame, EntityRef source, EntityRef skillEntity, Skill* skill)
		{
			EntityRef attackEntity = base.OnAction(frame, source, skillEntity, skill);
			Transform2D sourceTransform = frame.Get<Transform2D>(source);

			FP distance = 0;
			if (skill->ActionVector.Magnitude <= FP._0_10)
			{
				EnemyPositionsHelper.TryGetClosestCharacterDistance(frame, source, sourceTransform, MaxDistance, true, false, out distance);
			}
			else
			{
				distance = FPMath.Clamp(skill->ActionVector.Magnitude, 0, MaxDistance);
			}

			if (distance == 0)
			{
				distance = MaxDistance;
			}

			FPVector2 targetPosition = sourceTransform.Position + (sourceTransform.Up * distance);
			SetAttackTargetPosition(frame, attackEntity, targetPosition);

			return attackEntity;
		}

		protected abstract void SetAttackTargetPosition(Frame frame, EntityRef attackEntity, FPVector2 targetPosition);
	}
}
