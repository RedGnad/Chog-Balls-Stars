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

		// Copies of the attack turned apart, this many more, each SpreadDegrees from the one before. The super
		// attack of a 1:1 used them in the morning of 22 October 2026; it is one wider strike since the evening
		// (GenesisSignatureImporter, AreaScale), and every skill has 0 here.
		public int Spread;
		public FP SpreadDegrees;

		public override EntityRef OnAction(Frame frame, EntityRef source, EntityRef skillEntity, Skill* skill)
		{
			if (ReturnAttackData.IsValid == false)
			{
				EntityRef first = base.OnAction(frame, source, skillEntity, skill);
				for (int copy = 1; copy <= Spread; copy++)
				{
					FireTurned(frame, source, skillEntity, SpreadDegrees * copy * FP.Deg2Rad);
				}
				return first;
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

		// One more copy of the attack, from where the first one leaves, turned by "turn" radians: placed
		// before the attack data reads its way (OnCreate), as the base action does.
		private void FireTurned(Frame frame, EntityRef source, EntityRef skillEntity, FP turn)
		{
			EntityRef attackEntity = frame.Create(AttackPrototype);
			Transform2D* attackTransform = frame.Unsafe.GetPointer<Transform2D>(attackEntity);
			Transform2D from = source != default && frame.Exists(source)
				? frame.Get<Transform2D>(source) : frame.Get<Transform2D>(skillEntity);
			attackTransform->Position = from.Position;
			attackTransform->Rotation = from.Rotation + turn;

			Attack* attack = frame.Unsafe.GetPointer<Attack>(attackEntity);
			attack->Source = source;
			if (AttackDataOverride.IsValid)
			{
				attack->AttackData = AttackDataOverride;
			}
			AttackData data = frame.FindAsset<AttackData>(attack->AttackData.Id);
			data.OnCreate(frame, attackEntity, source, attack);
		}
	}
}
