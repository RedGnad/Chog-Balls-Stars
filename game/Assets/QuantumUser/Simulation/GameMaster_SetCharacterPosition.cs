using Photon.Deterministic;

namespace Quantum
{
	// Forces a teleport on a character's position

	public unsafe class GameMaster_SetCharacterPosition : DeterministicCommand
	{
		public int CharacterNumber;
		public FPVector2 TargetPosition;

		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref CharacterNumber);
			stream.Serialize(ref TargetPosition);
		}

		public void Execute(Frame frame)
		{
			var allCharacters = frame.Filter<Character, Transform2D>();
			int number = 0;
			while (allCharacters.NextUnsafe(out var entity, out var character, out var transform))
			{
				if (number == CharacterNumber)
				{
					frame.Unsafe.GetPointer<Transform2D>(entity)->Position = TargetPosition;

					if(frame.Unsafe.TryGetPointer<NavMeshPathfinder>(entity, out var pathfinder) == true)
					{
						NavMesh navMesh = frame.Map.GetNavMesh("NavMesh");
						pathfinder->SetTarget(frame, TargetPosition.XOY, navMesh);
					}
				}

				number++;
			}
		}
	}
}
