using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>What a Genesis has done, as the game services keep it. Field names are their JSON keys.</summary>
    [Serializable]
    public sealed class GenesisCareer
    {
        public int tokenId;
        public int matches;
        public int wins;
        public int guestMatches;
        public int rentedMatches;
        public int bestStreak;
        public int confirmed;
        public CareerMastery mastery;
        public CareerBadge[] badges;
    }

    /// <summary>
    /// What a Genesis has learnt by playing: a level, the points it has, and the points of the
    /// next level (0 at the last one). The services compute it from the career.
    /// </summary>
    [Serializable]
    public sealed class CareerMastery
    {
        public int level;
        public int points;
        public int next;
    }

    [Serializable]
    public sealed class CareerBadge
    {
        public string id;
        public string name;
    }

    /// <summary>
    /// The careers the game has read, by token id. A career belongs to the Genesis, not to the
    /// wallet: anyone can read it, and only the game of its signed-in holder, of a signed-in
    /// player who rents it, or of a guest the holder lent it to, adds a match to it.
    /// The results it counts are reported by the players' own pages; the services do not replay
    /// a match.
    /// </summary>
    public static class GenesisCareers
    {
        /// <summary>How many careers the services give in one read; "Read" asks for the first ones only.</summary>
        public const int MaxTokensPerRead = 64;

        private static readonly Dictionary<int, GenesisCareer> Known = new Dictionary<int, GenesisCareer>();
        // Matches already sent in this visit, so a second game-over event does not send them again.
        private static readonly HashSet<string> Reported = new HashSet<string>();

        /// <summary>Raised after a career was read or changed.</summary>
        public static event Action Changed;

        /// <summary>Raised when the services answered a reported match: the career, and whether this match was added to it.</summary>
        public static event Action<GenesisCareer, bool> Recorded;

        [Serializable]
        private sealed class CareerList
        {
            public GenesisCareer[] careers;
        }

        [Serializable]
        private sealed class ResultRequest
        {
            public int tokenId;
            public string matchId;
            public int team;
            public int winnerTeam;
        }

        [Serializable]
        private sealed class ResultAnswer
        {
            public bool credited;
            public string reason;
            public string error;
            public GenesisCareer career;
        }

        public static bool TryGet(int tokenId, out GenesisCareer career) => Known.TryGetValue(tokenId, out career);

        /// <summary>Reads the careers of these Genesis from the services; "Changed" follows.</summary>
        public static void Read(int[] tokenIds)
        {
            if (tokenIds == null || tokenIds.Length == 0)
                return;

            var list = new StringBuilder();
            for (var i = 0; i < tokenIds.Length && i < MaxTokensPerRead; i++)
                list.Append(i == 0 ? "" : ",").Append(tokenIds[i]);

            PlayerWallet.Run(GameServices.Call("career?tokens=" + list, null, null, (status, text) =>
            {
                var answer = status == 200 ? JsonUtility.FromJson<CareerList>(text) : null;
                if (answer == null || answer.careers == null)
                {
                    Debug.LogWarning("CHOG_CAREER could not be read: " + status + " " + text);
                    return;
                }

                foreach (var career in answer.careers)
                    Known[career.tokenId] = career;
                Changed?.Invoke();
            }));
        }

        /// <summary>How many Genesis the leaderboard asks the services for.</summary>
        public const int TopCount = 10;

        /// <summary>
        /// Reads the Genesis with the most wins (then the most matches), best first. "done" gets them,
        /// an empty list when no Genesis has a match yet, or null when the services did not answer.
        /// </summary>
        public static void ReadTop(Action<GenesisCareer[]> done)
        {
            PlayerWallet.Run(GameServices.Call("career/top?limit=" + TopCount, null, null, (status, text) =>
            {
                var answer = status == 200 ? JsonUtility.FromJson<CareerList>(text) : null;
                if (answer == null || answer.careers == null)
                {
                    Debug.LogWarning("CHOG_CAREER the leaderboard could not be read: " + status);
                    done(null);
                    return;
                }

                foreach (var career in answer.careers)
                    Known[career.tokenId] = career;
                Debug.Log("CHOG_CAREER leaderboard read: " + answer.careers.Length + " Genesis");
                done(answer.careers);
                Changed?.Invoke();
            }));
        }

        /// <summary>
        /// How the game may report a match for a Genesis: with the session of a signed-in wallet
        /// that holds or rents it, as a guest with a pass, or not at all.
        /// </summary>
        public enum Reporter
        {
            Nobody,
            Holder,
            Guest
        }

        /// <summary>
        /// Who may report a match played with "tokenId". The wallet: the Genesis is the one
        /// chosen among those it holds or rents and the player signed in; the services then
        /// check which of the two it is. A guest: no Genesis is chosen in a wallet and the pass
        /// that lends this one was accepted.
        /// </summary>
        public static Reporter ReporterFor(int tokenId, int walletChoice, bool signedIn, int lentTokenId)
        {
            if (tokenId <= 0)
                return Reporter.Nobody;

            if (walletChoice > 0)
                return tokenId == walletChoice && signedIn ? Reporter.Holder : Reporter.Nobody;

            return tokenId == lentTokenId ? Reporter.Guest : Reporter.Nobody;
        }

        /// <summary>
        /// Sends the result of a finished match for the Genesis the player chose in the wallet,
        /// held or rented, or for the one a holder lent with a pass. Nothing is sent otherwise:
        /// the services would refuse it.
        /// </summary>
        public static void Report(int tokenId, string matchId, int team, int winnerTeam)
        {
            var reporter = ReporterFor(tokenId, PlayerWallet.ChosenTokenId, PlayerWallet.SignIn == SignInState.SignedIn, GuestPass.TokenId);
            if (reporter == Reporter.Nobody)
            {
                Debug.Log("CHOG_CAREER not sent: " + (tokenId > 0 && tokenId == PlayerWallet.ChosenTokenId
                    ? "not signed in" : "this Genesis was not chosen from a wallet nor lent with a pass"));
                return;
            }

            if (!Reported.Add(matchId + "/" + tokenId))
                return;

            var body = JsonUtility.ToJson(new ResultRequest { tokenId = tokenId, matchId = matchId, team = team, winnerTeam = winnerTeam });
            PlayerWallet.Run(Send(tokenId, body, 1, reporter == Reporter.Guest ? GuestPass.Pass : PlayerWallet.Session,
                reporter == Reporter.Guest));
        }

        // The services read the chain before a credit; when that read or the network fails, the
        // same result is sent again a little later. The services credit a match only once.
        private const int MaxAttempts = 3;
        private const float RetrySeconds = 6f;

        // "credential" is the holder's session, or the pass of a guest.
        private static IEnumerator Send(int tokenId, string body, int attempt, string credential, bool asGuest)
        {
            long status = 0;
            string text = null;
            yield return GameServices.Call("career/result", body, credential, (answeredStatus, answeredText) =>
            {
                status = answeredStatus;
                text = answeredText;
            }, asGuest ? GameServices.PassScheme : GameServices.SessionScheme);

            if (ShouldSendAgain(status, attempt))
            {
                Debug.LogWarning("CHOG_CAREER attempt " + attempt + " failed (" + status + "), trying again");
                yield return new WaitForSecondsRealtime(RetrySeconds);
                yield return Send(tokenId, body, attempt + 1, credential, asGuest);
                yield break;
            }

            var answer = !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<ResultAnswer>(text) : null;
            if (asGuest && status == 403 && answer != null && GuestPass.IsFinalRefusal(answer.error))
            {
                // The loan ended while the guest was playing.
                Debug.LogWarning("CHOG_CAREER refused: the pass no longer counts (" + answer.error + ")");
                GuestPass.Refuse(answer.error);
                yield break;
            }

            if (status == 401)
            {
                // The session ended or was made by other services: the player signs in again.
                Debug.LogWarning("CHOG_CAREER refused: the sign-in is no longer valid");
                PlayerWallet.ForgetSession();
                yield break;
            }

            if (status != 200 || answer == null || answer.career == null)
            {
                Debug.LogWarning("CHOG_CAREER refused: " + status + " " + text);
                yield break;
            }

            Debug.Log("CHOG_CAREER " + (answer.credited ? "credited" : "not credited (" + answer.reason + ")") +
                      " token " + tokenId + " : " + answer.career.matches + " matches, " + answer.career.wins + " wins");
            Known[tokenId] = answer.career;
            Changed?.Invoke();
            Recorded?.Invoke(answer.career, answer.credited);
        }

        /// <summary>
        /// Whether a result that got this HTTP status is worth sending again: the request did not
        /// arrive (0) or the services failed (5xx), and attempts remain. A refusal (4xx) is final.
        /// </summary>
        public static bool ShouldSendAgain(long status, int attempt)
        {
            return attempt < MaxAttempts && (status == 0 || status >= 500);
        }

        /// <summary>
        /// Name of a match for the services: the same on every device of that match, and never
        /// reused. Characters the services do not accept are replaced.
        /// </summary>
        public static string MatchId(string roomName, int seed)
        {
            var id = new StringBuilder();
            foreach (var letter in roomName ?? "")
                id.Append(letter < 128 && (char.IsLetterOrDigit(letter) || letter == '-' || letter == '_') ? letter : '_');
            if (id.Length > 60)
                id.Length = 60;
            return id.Append(':').Append(seed < 0 ? "n" : "p")
                .Append(Math.Abs((long)seed).ToString("D10", System.Globalization.CultureInfo.InvariantCulture)).ToString();
        }

        /// <summary>Mastery level of a Genesis whose career was read; 1 for one that never played or is not known.</summary>
        public static int MasteryLevel(int tokenId)
        {
            return Known.TryGetValue(tokenId, out var career) && career.mastery != null && career.mastery.level > 1 ? career.mastery.level : 1;
        }

        /// <summary>"Mastery 2  5/8", or "Mastery 6" at the last level; empty when the services sent none.</summary>
        public static string DescribeMastery(GenesisCareer career)
        {
            if (career == null || career.mastery == null || career.mastery.level <= 0)
                return "";

            return "Mastery " + career.mastery.level +
                   (career.mastery.next > 0 ? "  " + career.mastery.points + "/" + career.mastery.next : "");
        }

        /// <summary>Mastery, then matches, wins and badges: everything a career says, on one line.</summary>
        public static string DescribeAll(GenesisCareer career)
        {
            var mastery = DescribeMastery(career);
            return mastery.Length == 0 ? Describe(career) : mastery + "   " + Describe(career);
        }

        /// <summary>"12 matches, 7 wins", or "no match yet".</summary>
        public static string DescribeMatches(GenesisCareer career)
        {
            if (career == null || career.matches == 0)
                return "no match yet";

            return career.matches + (career.matches == 1 ? " match, " : " matches, ") + career.wins + (career.wins == 1 ? " win" : " wins");
        }

        /// <summary>"12 matches, 7 wins" and the names of the badges, for a screen.</summary>
        public static string Describe(GenesisCareer career)
        {
            if (career == null || career.matches == 0)
                return "no match yet";

            var text = new StringBuilder(DescribeMatches(career));
            if (career.badges != null)
                foreach (var badge in career.badges)
                    text.Append("  [").Append(badge.name).Append(']');
            return text.ToString();
        }
    }
}
