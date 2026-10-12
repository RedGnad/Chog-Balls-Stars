using UnityEngine.Scripting;

namespace Quantum
{
	// The memory concept here is used basically to allow a character to record something in the Memory component
	// and to allow that data to be only available after some interval, and to have some automatic timer logic
	// used to make the character "forget" that info
	// Adding memory entries with some delay is useful for mimicking the reaction time of human players as they
	// take a while to actually understand the information that they are noticing
	[Preserve]
	public unsafe class MemorySystem : SystemMainThreadFilter<MemorySystem.Filter>,
		ISignalOnComponentAdded<AIMemory>, ISignalOnComponentRemoved<AIMemory>
	{
		public struct Filter
		{
			public EntityRef Entity;
			public AIMemory* AIMemory;
		}

		public void OnAdded(Frame frame, EntityRef entity, AIMemory* component)
		{
			component->Init(frame);
		}

		public void OnRemoved(Frame frame, EntityRef entity, AIMemory* component)
		{
			component->Free(frame);
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			filter.AIMemory->Update(frame);
		}
	}
}
