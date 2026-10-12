using Photon.Deterministic;

namespace Quantum
{
	// The special attack a Genesis fires, chosen by its traits and not by the body it wears.
	// What makes a signature is the attack data named by AttackDataOverride; the "return"
	// delivery adds a second strike that comes back to the caster.
	[System.Serializable]
	public unsafe partial class GenesisSignatureSkillData : SkillData
	{
		// The return delivery: the attack of the way back, fired by the second action of the
		// skill. Not set for the other deliveries.
		public AssetRef<AttackData> ReturnAttackData;

		// How far ahead of the cast point the way back starts. 0 for a shape that does not
		// travel: its second strike is made where the caster then stands.
		public FP ReturnDistance;

		public override EntityRef OnAction(Frame frame, EntityRef source, EntityRef skillEntity, Skill* skill)
		{
			if (ReturnAttackData.IsValid == false)
			{
				return base.OnAction(frame, source, skillEntity, skill);
			}

			bool hasSource = source != default && frame.Exists(source);

			// The first strike leaves as usual; the skill remembers which way it went.
			if (skill->TTL == 0)
			{
				if (hasSource)
				{
					skill->ActionVector = frame.Get<Transform2D>(source).Up;
				}

				return base.OnAction(frame, source, skillEntity, skill);
			}

			Transform2D skillTransform = frame.Get<Transform2D>(skillEntity);
			Transform2D casterTransform = hasSource ? frame.Get<Transform2D>(source) : skillTransform;

			EntityRef attackEntity = frame.Create(AttackPrototype);
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			FPVector2 start = skillTransform.Position + skill->ActionVector * ReturnDistance;
			FPVector2 back = casterTransform.Position - start;
			if (ReturnDistance == 0 || back.SqrMagnitude < FP._0_01)
			{
				attackTransform->Position = casterTransform.Position;
				attackTransform->Rotation = casterTransform.Rotation;
			}
			else
			{
				attackTransform->Position = start;
				attackTransform->Rotation = FPVector2.RadiansSigned(FPVector2.Up, back);
			}

			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			attack->Source = source;
			attack->AttackData = ReturnAttackData;

			AttackData data = frame.FindAsset<AttackData>(ReturnAttackData.Id);
			data.OnCreate(frame, attackEntity, source, attack);
			frame.Events.SkillAction(skill->SkillData.Id);

			return attackEntity;
		}
	}
}
