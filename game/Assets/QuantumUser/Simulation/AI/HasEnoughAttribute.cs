using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class HasEnoughAttribute : HFSMDecision
	{
		public EAttributeType AttributeType;
		public FP ExpectedAmount;
		public bool IsPercentage;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			FP amount;

			if (IsPercentage == false)
			{
				amount = GetValue(frame, entity);
			}
			else
			{
				amount = GetPercentage(frame, entity);
			}

			return amount >= ExpectedAmount;
		}

		private FP GetValue(Frame frame, EntityRef entity)
		{
			switch (AttributeType)
			{
				case EAttributeType.Health:
					return AttributesHelper.GetCurrentValue(frame, entity, EAttributeType.Health);
				case EAttributeType.Energy:
					return AttributesHelper.GetCurrentValue(frame, entity, EAttributeType.Energy);
				case EAttributeType.Speed:
					return AttributesHelper.GetCurrentValue(frame, entity, EAttributeType.Speed);
				case EAttributeType.Special:
					return AttributesHelper.GetCurrentValue(frame, entity, EAttributeType.Special);
				default:
					return 0;
			}
		}

		private FP GetPercentage(Frame frame, EntityRef entity)
		{
			switch (AttributeType)
			{
				case EAttributeType.Health:
					return AttributesHelper.GetPercentage(frame, entity, EAttributeType.Health);
				case EAttributeType.Energy:
					return AttributesHelper.GetPercentage(frame, entity, EAttributeType.Energy);
				case EAttributeType.Speed:
					return AttributesHelper.GetPercentage(frame, entity, EAttributeType.Speed);
				case EAttributeType.Special:
					return AttributesHelper.GetPercentage(frame, entity, EAttributeType.Special);
				default:
					return 0;
			}
		}
	}
}
