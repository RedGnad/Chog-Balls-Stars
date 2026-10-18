using System;
using Quantum;

namespace Chog.Match
{
    /// <summary>
    /// The scenes a match is played in and the map each mode plays on. Since 18 October 2026 the siege has
    /// its own map (SiegeMap, scene GameplaySiege: three lanes, a walled base; built by
    /// Chog.Editor.SiegeMapBuilder) and the coins keep the sample's (Map1, scene Gameplay). The menu writes
    /// the map of the chosen mode into the config of the match it starts (Chog.Screens.MatchModeChoice);
    /// Quantum then loads that map's scene. What paints, sounds and warms a match recognises both scenes
    /// by the start of their name.
    /// </summary>
    public static class ArenaScenes
    {
        /// <summary>Every scene of a match is named starting with this: "Gameplay", "GameplaySiege".</summary>
        public const string Prefix = "Gameplay";

        /// <summary>The paths of the maps in the Quantum asset database (QuantumUnityDB).</summary>
        public const string CoinsMapPath = "QuantumUser/Resources/Map1";
        public const string SiegeMapPath = "QuantumUser/Resources/SiegeMap";

        /// <summary>Whether a scene of that name is an arena a match is played in.</summary>
        public static bool IsArena(string sceneName)
        {
            return sceneName != null && sceneName.StartsWith(Prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// The map a match of that mode plays on: the siege on its own map, everything else on the sample's
        /// (coins, and the towers-and-coins of the records made before 16 October, which the menu never
        /// chooses).
        /// </summary>
        public static string MapPathOf(EMatchMode mode)
        {
            return mode == EMatchMode.Siege ? SiegeMapPath : CoinsMapPath;
        }
    }
}
