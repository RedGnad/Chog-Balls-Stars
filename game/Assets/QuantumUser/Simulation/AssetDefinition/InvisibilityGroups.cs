using Photon.Deterministic;
using System.Collections.Generic;

namespace Quantum
{
	[System.Serializable]
	public struct Group
	{
		public List<int> StaticCollidersIds;
	}

	public unsafe partial class InvisibilityGroups : AssetObject
	{
		// Holds all groups of invisibility spots, using their static colliders ids
		public List<Group> Groups;

		// Gets a random invisibility spot from a specific rgoup
		public FPVector2 GetRandomFromGroup(Frame frame, int groupId)
		{
			Group group = Groups[groupId];
			MapStaticCollider2D[] statics2D = frame.Map.StaticColliders2D;
			int randomColliderId = group.StaticCollidersIds[frame.RNG->Next(0, group.StaticCollidersIds.Count)];
			return statics2D[randomColliderId].Position;
		}

		// Finds the Group based on the static collider Id used as parameter
		public int GetGroupFromColliderId(int colliderId)
		{
			for (int groupId = 0; groupId < Groups.Count; groupId++)
			{
				Group group = Groups[groupId];
				for (int j = 0; j < group.StaticCollidersIds.Count; j++)
				{
					if (group.StaticCollidersIds[j] == colliderId)
					{
						return groupId;
					}
				}
			}

			return -1;
		}
	}
}
