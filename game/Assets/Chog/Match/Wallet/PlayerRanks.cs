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
