using UnityEngine.Scripting;

namespace Quantum
{
	// Builds the towers and the cores when a match begins and lets the towers shoot while it is
	// played. The rules themselves are in Structures.
	[Preserve]
	public unsafe class StructureSystem : SystemMainThreadFilter<StructureSystem.Filter>, ISignalOnGameStart, ISignalOnCharacterDamage
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Structure* Structure;
			public Transform2D* Transform;
			public TeamInfo* TeamInfo;
			public Health* Health;
		}

		public void OnGameStart(Frame frame)
		{
			Structures.Build(frame);
		}

		// The signal is named after heroes, but every loss of health passes through it.
		public void OnCharacterDamage(Frame frame, EntityRef character)
		{
			Structures.Hurt(frame, character);
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			// Registered beside the systems of the match and not among them: it must stop by
			// itself once the match is over.
			if (frame.Global->State != GameState.Playing)
			{
				return;
			}

			Structures.Shoot(frame, filter.Entity, filter.Structure, filter.Transform, filter.TeamInfo, filter.Health);
		}
	}
}
