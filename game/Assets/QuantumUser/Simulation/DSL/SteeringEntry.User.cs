using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct SteeringEntryContext
	{
		public void SetData(EntityRef enemy, FP runDistance, FP threatDistance)
		{
			this.CharacterRef = enemy;
			this.RunDistance = runDistance;
			this.ThreatDistance = threatDistance;
		}
	}

	public unsafe partial struct SteeringEntryNavMesh
	{
		public void SetData(FPVector2 desiredDirection)
		{
			this.NavMeshDirection = desiredDirection;
		}
	}

	public unsafe partial struct MemoryDataAreaAvoidance
	{
		public void SetData(EntityRef areaRef, FP runDistance)
		{
			this.Entity = areaRef;
			this.RunDistance = runDistance;

			this.Weight = 1;
		}
	}

	public unsafe partial struct MemoryDataLineAvoidance
	{
		public void SetData(EntityRef lineRef)
		{
			this.Entity = lineRef;

			this.Weight = 1;
		}
	}
}
