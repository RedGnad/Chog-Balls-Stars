using Photon.Deterministic;

namespace Quantum
{
	public static unsafe class AttributesHelper
	{
		public static void ChangeAttribute(Frame frame, EntityRef entity, EAttributeType type, EModifierAppliance appliance, EModifierOperation operation, FP value, FP duration)
		{
			if (type == EAttributeType.Health && operation == EModifierOperation.Subtract)
			{
				if (frame.TryGet<Immunity>(entity, out var immunity))
				{
					if (immunity.Timer > 0)
					{
						return;
					}
				}

				frame.Events.CharacterDamaged(entity, value);
				frame.Signals.OnCharacterDamage(entity);
			}

			Attributes targetAttributes = frame.Get<Attributes>(entity);
			var targetAttributesDict = frame.ResolveDictionary(targetAttributes.DataDictionary);

			targetAttributesDict.TryGetValuePointer(type, out var attribute);
			// An entity may lack an attribute (a tower has no speed to lower): nothing to change.
			if (attribute == null)
			{
				return;
			}

			AttributeModifier modifier = new AttributeModifier
			{
				Amount = value,
				ModifierAppliance = appliance,
				ModifierOperation = operation,
				Duration = duration,
			};
			attribute->AddModifier(frame, modifier);
		}

		/// <summary>Whether that entity has attributes and that one among them.</summary>
		public static bool Has(Frame frame, EntityRef entity, EAttributeType type)
		{
			return frame.TryGet<Attributes>(entity, out var attributes)
				&& frame.ResolveDictionary(attributes.DataDictionary).ContainsKey(type);
		}

		public static FP GetCurrentValue(Frame frame, EntityRef entity, EAttributeType type)
		{
			Attributes attributes = frame.Get<Attributes>(entity);
			var attributesDictionary = frame.ResolveDictionary(attributes.DataDictionary);
			AttributeData attribute = attributesDictionary[type];

			AttributeModifier greatestAbsoluteModifier = GetGreatestAbsoluteModifier(frame, entity, type);
			if(greatestAbsoluteModifier.Amount != 0)
			{
				greatestAbsoluteModifier.Apply(frame, ref attribute.CurrentValue, attribute.MaxValue);
			}

			return attribute.CurrentValue;
		}

		public static FP GetMaxValue(Frame frame, EntityRef entity, EAttributeType type)
		{
			Attributes attributes = frame.Get<Attributes>(entity);
			var attributesDictionary = frame.ResolveDictionary(attributes.DataDictionary);
			AttributeData attribute = attributesDictionary[type];
			return attribute.MaxValue;
		}

		public static FP GetPercentage(Frame frame, EntityRef entity, EAttributeType type)
		{
			Attributes attributes = frame.Get<Attributes>(entity);
			var attributesDictionary = frame.ResolveDictionary(attributes.DataDictionary);
			AttributeData attribute = attributesDictionary[type];
			return (attribute.CurrentValue / attribute.MaxValue) * 100;
		}

		private static AttributeModifier GetGreatestAbsoluteModifier(Frame frame, EntityRef entity, EAttributeType type)
		{
			Attributes attributes = frame.Get<Attributes>(entity);
			var attributesDictionary = frame.ResolveDictionary(attributes.DataDictionary);
			AttributeData attribute = attributesDictionary[type];

			var modifiersList = frame.ResolveList(attribute.Modifiers);
			FP maxValue = 0;
			AttributeModifier greatestModifier = default;
			foreach (var modifier in modifiersList)
			{
				if (modifier.ModifierAppliance != EModifierAppliance.TemporaryGreatestOnly)
				{
					continue;
				}

				if (modifier.Amount > maxValue)
				{
					greatestModifier = modifier;
					maxValue = modifier.Amount;
				}
			}

			return greatestModifier;
		}

		public static AttributeData GetAttributeData(Frame frame, EntityRef entity, EAttributeType type)
		{
			Attributes* attributes = frame.Unsafe.GetPointer<Attributes>(entity);
			var dataDictionary = frame.ResolveDictionary(attributes->DataDictionary);
			dataDictionary.TryGetValuePointer(type, out var attributeData);
			return *attributeData;
		}

		public static void SetCurrentValue(Frame frame, EntityRef entity, EAttributeType type, FP value)
		{
			FP currentValue = GetCurrentValue(frame, entity, type);
			FP difference = value - currentValue;
			ChangeAttribute(frame, entity, type, EModifierAppliance.OneTime, EModifierOperation.Add, difference, 0);
		}

		public static void AddValue(Frame frame, EntityRef entity, EAttributeType type, FP value)
		{
			FP currentValue = GetCurrentValue(frame, entity, type);
			ChangeAttribute(frame, entity, type, EModifierAppliance.OneTime, EModifierOperation.Add, currentValue + value, 0);
		}

		public static void SetMaxValue(Frame frame, EntityRef entity, EAttributeType type, FP value)
		{
			Attributes targetAttributes = frame.Get<Attributes>(entity);
			var targetAttributesDict = frame.ResolveDictionary(targetAttributes.DataDictionary);
			targetAttributesDict.TryGetValuePointer(type, out var attribute);
			attribute->MaxValue = value;
		}

		public static void ApplyPowerupEffect(Frame frame, EntityRef entity, EAttributeType type, FP value)
		{
			FP maxAttributeValue = GetMaxValue(frame, entity, type);
			FP newValue = maxAttributeValue + (maxAttributeValue * value);
			SetMaxValue(frame, entity, type, newValue);
			if (type == EAttributeType.Speed)
			{
				SetCurrentValue(frame, entity, EAttributeType.Speed, newValue);
			}
		}

		public static void RemovePowerupEffects(Frame frame, EntityRef entity, EAttributeType type)
		{
			FP initialValue = GetAttributeData(frame, entity, type).InitialValue;
			SetMaxValue(frame, entity, type, initialValue);
			if (type == EAttributeType.Speed)
			{
				SetCurrentValue(frame, entity, EAttributeType.Speed, initialValue);
			}
		}
	}
}
