using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	// This system is used to perform "Game Master" commands to facilitate testing
	[Preserve]
	public unsafe class CommandsSystem : SystemMainThreadFilter<CommandsSystem.Filter>
	{
		public struct Filter
		{
			public EntityRef Entity;
			public PlayerLink* PlayerLink;
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			var command = frame.GetPlayerCommand(filter.PlayerLink->PlayerRef);
			
			if (command is SelectCharacterCommands.SelectCharacterCommand)
			{
				var c = command as SelectCharacterCommands.SelectCharacterCommand;
				frame.Signals.OnCreatePlayerCharacter(filter.PlayerLink->PlayerRef, c.CharacterSelected);
			}
			
			if (command is SelectCharacterCommands.FinishCharacterSelectionCommand)
			{
				frame.Signals.OnCommandFinishCharacterSelection();
			}
			
			if (command is SelectCharacterCommands.ChangePlayerTeamCommand)
			{
				frame.Signals.OnChangePlayerTeam(filter.PlayerLink->PlayerRef);
			}

			// Force a teleport for a specific character
			if (command is GameMaster_SetCharacterPosition setCharacterPosition)
			{
				setCharacterPosition.Execute(frame);
			}

			// Force spwaning a specific entity prototype (useful for spawning coins, etc)
			if (command is GameMaster_SpawnPrototype spawnPrototype)
			{
				spawnPrototype.Execute(frame);
			}
		}
	}
}
