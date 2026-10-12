using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public static partial class DeterministicCommandSetup
	{
		static partial void AddCommandFactoriesUser(ICollection<IDeterministicCommandFactory> factories, RuntimeConfig gameConfig, SimulationConfig simulationConfig)
		{
			factories.Add(new GameMaster_SetCharacterPosition());
			factories.Add(new GameMaster_SpawnPrototype());
			factories.Add(new SelectCharacterCommands.FinishCharacterSelectionCommand());
			factories.Add(new SelectCharacterCommands.SelectCharacterCommand());
			factories.Add(new SelectCharacterCommands.ChangePlayerTeamCommand());
			
		}
	}
}
