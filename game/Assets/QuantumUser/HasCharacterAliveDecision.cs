using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class HasCharacterAliveDecision : HFSMDecision
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Health* Health;
			public PlayerLink* PlayerLink;
		}

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var characterFilter = frame.Unsafe.FilterStruct<Filter>();
			var characterStruct = default(Filter);
			while (characterFilter.Next(&characterStruct))
			{
				if (characterStruct.Health->IsDead == false)
				{
					return true;
				}
			}
			return false;
		}
	}
}
