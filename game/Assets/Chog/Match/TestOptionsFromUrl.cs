using System.Collections.Generic;
using System.Globalization;
using Chog.Diagnostics;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Turns test options of the page URL into match settings. "?charged=1" makes every character
    /// enter the match with its special ready, to look at signatures without charging them first.
    /// "?bots=181,70" makes the bots those Genesis, in that order, to watch chosen kits at work.
    /// They only have an effect for the player who creates the room, whose config the match uses,
    /// and only on a page served from this computer or the local network (LocalGenesis).
    /// </summary>
    public static class TestOptionsFromUrl
    {
        public const string ChargedParameter = "charged";
        public const string BotsParameter = "bots";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            // In the Editor these assets are the project files themselves: leave them untouched.
            if (Application.isEditor)
                return;

            // On a public address these options do nothing: they would change a match for everyone in it.
            var url = Application.absoluteURL;
            if (!LocalGenesis.TestEntriesAllowed(url, false))
                return;

            var charged = PerformanceSampling.HasQueryFlag(url, ChargedParameter);
            PerformanceSampling.TryGetQueryValue(url, BotsParameter, out var botList);
            var bots = TokenIds(botList);
            if (!charged && bots.Length == 0)
                return;

            // The menu builds the match config from these scene assets when a room is created.
            foreach (var scene in Resources.LoadAll<QuantumMenuSceneInfo>(""))
            {
                if (charged)
                    scene.RuntimeConfig.StartWithFullSpecial = true;
                if (bots.Length > 0)
                    scene.RuntimeConfig.BotGenesisTokens = bots;
            }
        }

        /// <summary>The token ids of a list such as "181,70"; anything that is not a positive number is left out.</summary>
        public static int[] TokenIds(string list)
        {
            var tokens = new List<int>();
            foreach (var part in (list ?? "").Split(','))
                if (int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var token) && token > 0)
                    tokens.Add(token);
            return tokens.ToArray();
        }
    }
}
