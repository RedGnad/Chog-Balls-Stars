using System;
using UnityEngine;

namespace Chog.Match
{
    [Serializable]
    public sealed class PlayerRank
    {
        public int points;
        public string tier;
        public int next;
        public int matches;
        public int wins;
        public PlayerRankGain last;
        public PlayerRankReset lastReset;
    }

    /// <summary>The last season's end that brought this wallet down toward the floor; null until one did.</summary>
    [Serializable]
    public sealed class PlayerRankReset
    {
        public string seasonId;
        public string at;
        public int before;
        public int after;
        public string tierBefore;
        public string tierAfter;
    }

    /// <summary>The hidden rating as the server shows it: a band for matchmaking, never the number.</summary>
    [Serializable]
    public sealed class PlayerRating
    {
        /// <summary>0 to 6; 3 is where every wallet starts. A room carries its creator's band as "C0".</summary>
        public int band;
        public bool rated;
        public int matches;
    }

    [Serializable]
    public sealed class PlayerRankGain
    {
        public string gameId;
        public int delta;
        public int before;
        public int after;
        public string tierBefore;
        public string tierAfter;
    }

    [Serializable]
    public sealed class PlayerRankMatch
    {
        public string gameId;
        public string status;
    }

    /// <summary>The ladder season the services run, as /api/rank/me gives it; null without one.</summary>
    [Serializable]
    public sealed class PlayerSeason
    {
        public string id;
        public string endsAt;
        public bool ended;
    }

    [Serializable]
    public sealed class PlayerRankAnswer
    {
        public bool enabled;
        public PlayerRank rank;
        public PlayerRating rating;
        public PlayerRankMatch latestMatch;
        public PlayerSeason season;
    }

    /// <summary>
    /// The ladder's scoring, the same as the services' (server/src/ranks.js: TIERS, pointsFor, scoringFor,
    /// deltaFor), so the result screen can show a siege's points the moment it ends; the services' answer
    /// after their replay of the match stays the one that counts, and replaces this one when they differ.
    /// </summary>
    public static class RankRules
    {
        public static readonly string[] TierNames = { "BRONZE", "SILVER", "GOLD", "STAR" };
        public static readonly int[] TierStarts = { 0, 100, 250, 500 };
        // A match with one authenticated human cannot raise a wallet past this many points.
        public const int SoloCap = 100;

        public static string TierFor(int points)
        {
            for (var i = TierStarts.Length - 1; i > 0; i--)
                if (points >= TierStarts[i])
                    return TierNames[i];
            return TierNames[0];
        }

        /// <summary>The points where the next tier starts, 0 at the top.</summary>
        public static int NextFor(int points)
        {
            foreach (var start in TierStarts)
                if (points < start)
                    return start;
            return 0;
        }

        /// <summary>
        /// The change of a wallet's points for one siege: "humans" authenticated players in it (1 to 6), the
        /// points before, and whether it was won (a draw is "won" null).
        /// </summary>
        public static int DeltaFor(int humans, int before, bool? won)
        {
            if (won == null || humans < 1 || humans > 6)
                return 0;
            var win = Round(3f + (humans - 1) * 17f / 5f);
            var loss = 2 * humans;
            if (humans == 1 && before >= SoloCap)
                return 0;
            // From Gold a win pays 75% and a loss costs 125%; from Star 50% and 150%.
            int winShare = 100, lossShare = 100;
            if (before >= 500) { winShare = 50; lossShare = 150; }
            else if (before >= 250) { winShare = 75; lossShare = 125; }
            var banded = before >= 250;
            if (won == true)
            {
                var amount = banded ? Mathf.Max(1, Round(win * winShare / 100f)) : win;
                return humans == 1 ? Mathf.Min(amount, SoloCap - before) : amount;
            }
            var lost = banded ? Mathf.Max(1, Round(loss * lossShare / 100f)) : loss;
            return -Mathf.Min(lost, before);
        }

        // JavaScript's Math.round for these positive amounts (halves up); Mathf.RoundToInt rounds halves to even.
        private static int Round(float value) => Mathf.FloorToInt(value + 0.5f);
    }

    /// <summary>Server-confirmed player ladder, separate from the career of a Genesis.</summary>
    public static class PlayerRanks
    {
        public static PlayerRankAnswer Current { get; private set; }

        /// <summary>"SEASON 1 ENDS 6 NOV" for the season the services run, "" without one.</summary>
        public static string SeasonLine() => SeasonLine(Current?.season);

        public static string SeasonLine(PlayerSeason season)
        {
            if (season == null || string.IsNullOrEmpty(season.id))
                return "";
            var name = season.id.Replace('-', ' ').ToUpperInvariant();
            if (season.ended)
                return name + " ENDED";
            return DateTime.TryParse(season.endsAt, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var ends)
                ? name + " ENDS " + ends.Day + " " + ends.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant()
                : name;
        }
        public static event Action Changed;
        private static string _currentAddress = "";

        public static void Read(Action<PlayerRankAnswer> done = null)
        {
            if (PlayerWallet.SignIn != SignInState.SignedIn || string.IsNullOrEmpty(PlayerWallet.Session))
            {
                Current = null;
                _currentAddress = "";
                Changed?.Invoke();
                done?.Invoke(null);
                return;
            }

            var address = PlayerWallet.Address;
            if (!string.Equals(_currentAddress, address, StringComparison.OrdinalIgnoreCase))
            {
                Current = null;
                _currentAddress = address;
                Changed?.Invoke();
            }
            PlayerWallet.Run(GameServices.Call("rank/me", null, PlayerWallet.Session, (status, text) =>
            {
                if (!string.Equals(address, PlayerWallet.Address, StringComparison.OrdinalIgnoreCase))
                    return;
                var answer = status == 200 ? JsonUtility.FromJson<PlayerRankAnswer>(text) : null;
                if (answer == null)
                {
                    Debug.LogWarning("CHOG_RANK could not be read: " + status);
                    Current = null;
                    Changed?.Invoke();
                    done?.Invoke(null);
                    return;
                }
                Current = answer.enabled ? answer : null;
                Changed?.Invoke();
                done?.Invoke(answer);
            }));
        }
    }
}
