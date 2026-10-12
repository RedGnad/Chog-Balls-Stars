using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public unsafe class AttributesSystem : SystemMainThreadFilter<AttributesSystem.Filter>, ISignalOnComponentAdded<Attributes>
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Attributes* Attributes;
		}

		// Initialises all the data structured contained in this Attributes component, when it is added to the entity
		public void OnAdded(Frame frame, EntityRef entity, Attributes* component)
		{
			var attributes = frame.ResolveDictionary(component->DataDictionary);
			var attributesEnumerator = attributes.GetEnumerator();
			while (attributesEnumerator.MoveNext() == true)
			{
				attributesEnumerator.ValuePtrUnsafe->Init(frame);
			}
		}

		// Updates all the data structured contained in this Attributes component
		// This applies attributes specific logics such as "Cause one time damage", "Cause damage over time", etc
		public override void Update(Frame frame, ref Filter filter)
		{
			var attributes = frame.ResolveDictionary(filter.Attributes->DataDictionary);
			var attributesEnumerator = attributes.GetEnumerator();
			while (attributesEnumerator.MoveNext() == true)
			{
				attributesEnumerator.ValuePtrUnsafe->Update(frame, filter.Entity);
			}
		}
	}
}