using Photon.Deterministic;

namespace Quantum
{
	public partial struct AttributeModifier
	{
		public void Init(Frame frame)
		{
			Timer = Duration;
		}

		public void Tick(Frame frame, out bool ttlOver)
		{
			if (Timer > 0 || ModifierAppliance == EModifierAppliance.OneTime)
			{
				Timer -= frame.DeltaTime;
				if (Timer <= 0)
				{
					ttlOver = true;
					return;
				}
			}

			ttlOver = false;
			return;
		}

		public void Apply(Frame frame, ref FP attributeValue, FP maxValue, EModifierOperation forcedOperation = EModifierOperation.None)
		{
			FP valueToApply = ModifierAppliance == EModifierAppliance.Continuous ? Amount * frame.DeltaTime : Amount;

			EModifierOperation operation = forcedOperation != EModifierOperation.None ? forcedOperation : ModifierOperation;
			switch (operation)
			{
				case EModifierOperation.Add:
					attributeValue = FPMath.Clamp(attributeValue + valueToApply, 0, maxValue);
					break;
				case EModifierOperation.Subtract:
					attributeValue = FPMath.Clamp(attributeValue - valueToApply, 0, maxValue);
					break;
				default:
					break;
			}
		}

		public void DeApply(Frame frame, ref FP attributeValue, FP maxValue)
		{
			EModifierOperation reversedOperation = EModifierOperation.None;
			switch (ModifierOperation)
			{
				case EModifierOperation.Add:
					reversedOperation = EModifierOperation.Subtract;
					break;
				case EModifierOperation.Subtract:
					reversedOperation = EModifierOperation.Add;
					break;
				default:
					break;
			}

			Apply(frame, ref attributeValue, maxValue, reversedOperation);
		}
	}
}
