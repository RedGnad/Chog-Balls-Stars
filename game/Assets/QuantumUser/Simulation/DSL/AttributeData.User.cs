using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct AttributeData
	{
		public bool IsFull => CurrentValue == MaxValue;
		public bool IsNotZero => CurrentValue > 0;

		public void Init(Frame frame)
		{
			CurrentValue = InitialValue;
			if (CurrentValue > MaxValue) CurrentValue = MaxValue;

			OnInit(frame);
		}

		private void OnInit(Frame frame)
		{
			ApplyModifiers(frame, EModifierAppliance.Temporary);
			ApplyModifiers(frame, EModifierAppliance.OneTime);

			InitModifiers(frame);
		}

		public void Update(Frame frame, EntityRef entity)
		{
			ApplyModifiers(frame, EModifierAppliance.Continuous);
			TickModifiers(frame);
		}

		private void InitModifiers(Frame frame)
		{
			if (Modifiers.Ptr == default)
			{
				Modifiers = frame.AllocateList<AttributeModifier>();
				return;
			}

			var modifiersList = frame.ResolveList(Modifiers);

			if (modifiersList.Count == 0)
				return;

			for (int i = 0; i < modifiersList.Count; i++)
			{
				AttributeModifier* modifier = modifiersList.GetPointer(i);
				modifier->Init(frame);
			}
		}

		public void AddModifier(Frame frame, AttributeModifier modifier)
		{
			if (Modifiers.Ptr == default)
				return;

			var modifiersList = frame.ResolveList(Modifiers);
			modifier.Init(frame);
			modifiersList.Add(modifier);

			if (modifier.ModifierAppliance == EModifierAppliance.Temporary || modifier.ModifierAppliance == EModifierAppliance.OneTime)
			{
				modifier.Apply(frame, ref CurrentValue, MaxValue);
			}
		}

		public void RemoveModifier(Frame frame, AttributeModifier modifier)
		{
			if (Modifiers.Ptr == default)
				return;

			var modifiersList = frame.ResolveList(Modifiers);
			modifiersList.Remove(modifier);

			if (modifier.ModifierAppliance == EModifierAppliance.Temporary)
			{
				modifier.DeApply(frame, ref CurrentValue, MaxValue);
			}
		}

		private void ApplyModifiers(Frame frame, EModifierAppliance desiredType)
		{
			if (Modifiers.Ptr == default)
				return;

			var modifiersList = frame.ResolveList(Modifiers);

			if (modifiersList.Count == 0)
				return;

			for (int i = 0; i < modifiersList.Count; i++)
			{
				AttributeModifier* modifier = modifiersList.GetPointer(i);
				if (modifier->ModifierAppliance == desiredType)
				{
					modifier->Apply(frame, ref CurrentValue, MaxValue);
				}
			}
		}

		private void TickModifiers(Frame frame)
		{
			if (Modifiers.Ptr == default)
				return;

			var modifiersList = frame.ResolveList(Modifiers);

			if (modifiersList.Count == 0)
				return;

			for (int i = modifiersList.Count - 1; i >= 0; i--)
			{
				AttributeModifier* modifier = modifiersList.GetPointer(i);
				modifier->Tick(frame, out bool isOver);

				if (isOver == true)
				{
					RemoveModifier(frame, *modifier);
				}
			}
		}
	}
}
