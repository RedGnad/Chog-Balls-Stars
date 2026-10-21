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

    [Serializable]
    public sealed class PlayerRankAnswer
    {
        public bool enabled;
        public PlayerRank rank;
        public PlayerRating rating;
        public PlayerRankMatch latestMatch;
    }

    /// <summary>Server-confirmed player ladder, separate from the career of a Genesis.</summary>
    public static class PlayerRanks
    {
        public static PlayerRankAnswer Current { get; private set; }
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
