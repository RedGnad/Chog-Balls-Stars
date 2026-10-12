using System;
using Chog.Diagnostics;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The Genesis the local player asks to enter a match with. In this order: the one chosen
    /// among those the connected wallet holds (PlayerWallet); the one a holder lent with a pass
    /// (GuestPass); then the test entries, which check nothing: the page URL carries
    /// "?token=1795", or in the Editor PlayerPrefs "chog.token" is set from the Chog menu. The
    /// value is the canonical token id, not the number shown in the name.
    ///
    /// The test entries only work where the game is being developed: in the Editor and on a
    /// page served from this computer or the local network. On a public address they are
    /// ignored, so nobody plays a Genesis there without holding it or being lent it. The match
    /// itself still trusts what each player's game declares: the game services check ownership
    /// when they credit a career.
    /// </summary>
    public static class LocalGenesis
    {
        public const string QueryParameter = "token";
        public const string PlayerPrefsKey = "chog.token";

        /// <summary>
        /// Token id to declare, or 0 for none. The wallet choice wins over a lent Genesis, a lent
        /// Genesis over the URL, and the URL over the Editor preference.
        /// </summary>
        public static int RequestedTokenId(int walletChoice, int lentTokenId, string url, bool isEditor, int editorPreference)
        {
            if (walletChoice > 0)
                return walletChoice;

            if (lentTokenId > 0)
                return lentTokenId;

            if (TestEntriesAllowed(url, isEditor) && PerformanceSampling.TryGetQueryInt(url, QueryParameter, out var token))
                return token > 0 ? token : 0;

            return isEditor && editorPreference > 0 ? editorPreference : 0;
        }

        /// <summary>Whether the options meant for tests ("token", "charged", "bots") are honoured for this page.</summary>
        public static bool TestEntriesAllowed(string url, bool isEditor)
        {
            return isEditor || IsPrivatePage(url);
        }

        /// <summary>Whether a test option written as "?name=1" is set on this page and honoured there.</summary>
        public static bool TestFlag(string url, bool isEditor, string name)
        {
            return TestEntriesAllowed(url, isEditor) && PerformanceSampling.HasQueryFlag(url, name);
        }

        /// <summary>
        /// Whether a page address is one only this computer or its local network can reach:
        /// localhost, a ".local" name, or an address of the private IPv4 ranges. Anything else,
        /// a public domain included, is not.
        /// </summary>
        public static bool IsPrivatePage(string url)
        {
            var host = HostOf(url);
            if (host.Length == 0)
                return false;

            if (host == "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal) || host.EndsWith(".local", StringComparison.Ordinal))
                return true;

            var parts = host.Split('.');
            if (parts.Length != 4)
                return false;

            var numbers = new int[4];
            for (var i = 0; i < 4; i++)
                if (!int.TryParse(parts[i], out numbers[i]) || numbers[i] < 0 || numbers[i] > 255)
                    return false;

            return numbers[0] == 127 || numbers[0] == 10
                || (numbers[0] == 192 && numbers[1] == 168)
                || (numbers[0] == 172 && numbers[1] >= 16 && numbers[1] <= 31)
                || (numbers[0] == 169 && numbers[1] == 254);
        }

        // "192.168.1.85" for "http://192.168.1.85:8083/?token=1"; lower case; empty when there is none.
        private static string HostOf(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            var start = url.IndexOf("://", StringComparison.Ordinal);
            if (start < 0)
                return "";

            start += 3;
            var end = url.IndexOfAny(new[] { '/', '?', '#', ':' }, start);
            // A name and password before the host are not part of it.
            var at = url.IndexOf('@', start);
            if (at >= 0 && (end < 0 || at < end))
                return "";

            return (end < 0 ? url.Substring(start) : url.Substring(start, end - start)).ToLowerInvariant();
        }

        public static void ApplyTo(RuntimePlayer player)
        {
            player.GenesisTokenId = RequestedTokenId(PlayerWallet.ChosenTokenId, GuestPass.TokenId, Application.absoluteURL,
                Application.isEditor, PlayerPrefs.GetInt(PlayerPrefsKey, 0));
            player.GenesisForm = GenesisForms.PlayedBy(player.GenesisTokenId);
        }
    }
}
