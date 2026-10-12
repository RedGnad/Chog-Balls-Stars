namespace Quantum {
  using System;
  using System.Collections.Generic;

  public static partial class DeterministicSystemSetup {
    static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
    {
      // The towers and the cores: after every system of the sample, so a tower shoots once the
      // heroes of the tick have moved.
      systems.Add(new StructureSystem());
    }
  }
}
