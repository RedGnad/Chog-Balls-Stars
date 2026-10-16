using UnityEngine.Scripting;

namespace Quantum
{
	// Builds the towers and the cores when a match begins (none in a match of coins) and lets the
	// towers shoot while it is played; in a siege, the score of each team follows the damage done
	// to the structures. The rules themselves are in Structures and MatchModes.
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
			MatchModes.PlayBegins(frame);
			if (MatchModes.HasStructures(frame))
			{
				Structures.Build(frame);
			}
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
