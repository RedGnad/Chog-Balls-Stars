using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct AIMemory
	{
		public void Init(Frame frame)
		{
			MemoryEntries = frame.AllocateList<AIMemoryEntry>();
		}

		public void Free(Frame frame)
		{
			frame.FreeList(MemoryEntries);
		}

		public AIMemoryEntry* Add(Frame frame, EMemoryType memoryType, FP availableDelay, FP unavailableDelay)
		{
			QList<AIMemoryEntry> memoryEntries = frame.ResolveList(MemoryEntries);

			AIMemoryEntry newEntry = new AIMemoryEntry
			{
				Type = memoryType,
				Data = new MemoryData(),
			};
			newEntry.SetTicks(frame, availableDelay, unavailableDelay);

			memoryEntries.Add(newEntry);

			return memoryEntries.GetPointer(memoryEntries.Count - 1);
		}

		public AIMemoryEntry* AddInfiniteMemory(Frame frame, EMemoryType memoryType)
		{
			QList<AIMemoryEntry> memoryEntries = frame.ResolveList(MemoryEntries);

			AIMemoryEntry newEntry = new AIMemoryEntry
			{
				Type = memoryType,
				Data = new MemoryData(),
			};
			newEntry.IsInfinite = true;

			memoryEntries.Add(newEntry);

			return memoryEntries.GetPointer(memoryEntries.Count - 1);
		}

		public void SetOrAdd(Frame frame, EMemoryType memoryType, MemoryData memoryData, FP availableDelay, FP unavailableDelay)
		{
			QList<AIMemoryEntry> memoryEntries = frame.ResolveList(MemoryEntries);

			for (int i = 0; i < memoryEntries.Count; i++)
			{
				AIMemoryEntry* entry = memoryEntries.GetPointer(i);
				if (entry->Type == memoryType)
				{
					entry->Data = memoryData;
					entry->SetTicks(frame, availableDelay, unavailableDelay);
					return;
				}
			}

			AIMemoryEntry newEntry = new AIMemoryEntry
			{
				Type = memoryType,
				Data = memoryData,
			};
			newEntry.SetTicks(frame, availableDelay, unavailableDelay);

			memoryEntries.Add(newEntry);
		}

		public void Remove(Frame frame, EMemoryType memoryType)
		{
			QList<AIMemoryEntry> memoryEntries = frame.ResolveList(MemoryEntries);

			for (int i = 0; i < memoryEntries.Count; i++)
			{
				if (memoryEntries.GetPointer(i)->Type == memoryType)
				{
					memoryEntries.RemoveAt(i);
				}
			}
		}

		public AIMemoryEntry* Get(Frame frame, EMemoryType memoryType, bool checkAvailability)
		{
			QList<AIMemoryEntry> memoryEntries = frame.ResolveList(MemoryEntries);

			for (int i = 0; i < memoryEntries.Count; i++)
			{
				AIMemoryEntry* entry = memoryEntries.GetPointer(i);
				if (entry->Type == memoryType)
				{
					if(checkAvailability == false || (checkAvailability == true && entry->IsAvailable(frame) == true))
					{
						return entry;
					}
				}
			}

			return null;
		}

		public bool TryGet(Frame frame, EMemoryType memoryType, bool checkAvailability, out AIMemoryEntry* memoryEntry)
		{
			memoryEntry = null;

			QList<AIMemoryEntry> memoryEntries = frame.ResolveList(MemoryEntries);

			for (int i = 0; i < memoryEntries.Count; i++)
			{
				AIMemoryEntry* entry = memoryEntries.GetPointer(i);
				if (entry->Type == memoryType)
				{
					if (checkAvailability == false || (checkAvailability == true && entry->IsAvailable(frame) == true))
					{
						memoryEntry = entry;
						return true;
					}
				}
			}

			return false;
		}

		public void Update(Frame frame)
		{
			QList<AIMemoryEntry> memoryEntries = frame.ResolveList(MemoryEntries);

			for (int i = memoryEntries.Count - 1; i >= 0; i--)
			{
				var memoryEntry = memoryEntries.GetPointer(i);

				if(memoryEntry->IsInfinite == true)
				{
					continue;
				}

				if (memoryEntry->IsForgotten(frame) == true)
				{
					memoryEntries.RemoveAt(i);
				}
			}
		}
	}
}
