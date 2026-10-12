using System;
using System.Collections;
using Chog.Diagnostics;
using UnityEngine;

namespace Chog.Match
{
    public enum GuestPassState
    {
        /// <summary>The visitor came without a pass.</summary>
        None,
        /// <summary>The game services are being asked whether the pass still counts.</summary>
        Checking,
        /// <summary>The pass counts: the visitor plays the lent Genesis.</summary>
        Valid,
        /// <summary>The pass does not count, or could not be checked; "Problem" says which.</summary>
        Refused
    }

    /// <summary>
    /// The pass a visitor arrived with. A holder lends one Genesis by handing out a link
    /// ("?pass=..."): whoever opens the game with it plays that Genesis without a wallet, and
    /// the matches count for the career of the Genesis. The game services decide whether a pass
    /// still counts: it runs out, the holder can stop lending, and it dies when the Genesis
    /// leaves the holder's wallet. The browser keeps the pass, so coming back to the plain
    /// address of the game still plays the lent Genesis.
    /// </summary>
    public static class GuestPass
    {
        public const string QueryParameter = "pass";
        private const string PassKey = "chog.guest.pass";

        // A check that never reached the services is tried again a few times.
        private const int MaxAttempts = 3;
        private const float RetrySeconds = 4f;
        private const long MillisecondsPerDay = 24L * 3600L * 1000L;

        public static GuestPassState State { get; private set; } = GuestPassState.None;

        /// <summary>Token id of the lent Genesis; 0 unless the pass was accepted.</summary>
        public static int TokenId { get; private set; }

        /// <summary>Address of the holder who lent it.</summary>
        public static string Lender { get; private set; } = "";

        /// <summary>End of the loan, in milliseconds since 1970 as the services write it.</summary>
        public static long ExpiresAtMs { get; private set; }

        /// <summary>The pass itself; empty when there is none or it was refused for good.</summary>
        public static string Pass { get; private set; } = "";

        /// <summary>Why the pass does not count, in words for the player; empty otherwise.</summary>
        public static string Problem { get; private set; } = "";

        /// <summary>Raised after any of the values above changed.</summary>
        public static event Action Changed;

        // Field names are the JSON keys of the services.
        [Serializable]
        private sealed class LoanAnswer
        {
            public int tokenId;
            public string lender;
            public long expiresAt;
            public string error;
        }

        /// <summary>
        /// Called once when the page is up: takes the pass of the address, else the one kept
        /// from an earlier visit, and has it checked.
        /// </summary>
        public static void Begin(string pageUrl)
        {
            var arrived = FromUrl(pageUrl);
            var pass = arrived.Length > 0 ? arrived : PlayerPrefs.GetString(PassKey, "");
            if (!LooksLikeAPass(pass))
                return;

            if (arrived.Length > 0)
            {
                PlayerPrefs.SetString(PassKey, arrived);
                PlayerPrefs.Save();
            }

            Set(GuestPassState.Checking, pass, 0, "", 0, "");
            PlayerWallet.Run(Check(pass, 1));
        }

        /// <summary>The pass carried by a page address, or empty.</summary>
        public static string FromUrl(string pageUrl)
        {
            return PerformanceSampling.TryGetQueryValue(pageUrl, QueryParameter, out var value) && LooksLikeAPass(value) ? value : "";
        }

        /// <summary>Whether a text has the shape of a pass: two parts of letters, digits, "-" and "_" around one dot.</summary>
        public static bool LooksLikeAPass(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 600)
                return false;

            var dot = text.IndexOf('.');
            if (dot <= 0 || dot == text.Length - 1 || text.IndexOf('.', dot + 1) >= 0)
                return false;

            foreach (var letter in text)
                if (!(letter == '.' || letter == '-' || letter == '_' || (letter < 128 && char.IsLetterOrDigit(letter))))
                    return false;
            return true;
        }

        /// <summary>The address that opens the game with a pass: the page without its options, then the pass.</summary>
        public static string LinkFor(string pageUrl, string pass)
        {
            if (string.IsNullOrEmpty(pageUrl) || string.IsNullOrEmpty(pass))
                return "";

            var end = pageUrl.IndexOfAny(new[] { '?', '#' });
            return (end < 0 ? pageUrl : pageUrl.Substring(0, end)) + "?" + QueryParameter + "=" + pass;
        }

        /// <summary>"29 more days", "1 more day" or "less than a day" until an end given in milliseconds.</summary>
        public static string TimeLeft(long expiresAtMs, long nowMs)
        {
            var days = (expiresAtMs - nowMs) / MillisecondsPerDay;
            return days < 1 ? "less than a day" : days == 1 ? "1 more day" : days + " more days";
        }

        /// <summary>The services' reason for refusing a pass, in words for the player.</summary>
        public static string InWords(string code)
        {
            switch (code)
            {
                case "pass_expired":
                    return "it has run out";
                case "lending_stopped":
                    return "its holder stopped lending this Genesis";
                case "lender_not_the_holder":
                    return "the wallet that lent this Genesis no longer holds it";
                default:
                    return "it was not made by this game";
            }
        }

        /// <summary>Whether a refusal code of the services means the pass is dead for good.</summary>
        public static bool IsFinalRefusal(string code)
        {
            return code == "pass_expired" || code == "lending_stopped" || code == "lender_not_the_holder" || code == "bad_pass";
        }

        /// <summary>The services refused this pass while it was being used: it is forgotten.</summary>
        public static void Refuse(string code)
        {
            if (State == GuestPassState.None)
                return;

            Debug.Log("CHOG_GUEST pass refused: " + code);
            PlayerPrefs.DeleteKey(PassKey);
            PlayerPrefs.Save();
            Set(GuestPassState.Refused, "", 0, "", 0, InWords(code));
        }

        private static IEnumerator Check(string pass, int attempt)
        {
            long status = 0;
            string text = null;
            yield return GameServices.Call("loan", null, pass, (answeredStatus, answeredText) =>
            {
                status = answeredStatus;
                text = answeredText;
            }, GameServices.PassScheme);

            if (pass != Pass)
                yield break;

            var answer = !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<LoanAnswer>(text) : null;
            if (status == 200 && answer != null && answer.tokenId > 0)
            {
                Debug.Log("CHOG_GUEST lent token " + answer.tokenId + " by " + PlayerWallet.ShortAddress(answer.lender));
                Set(GuestPassState.Valid, pass, answer.tokenId, answer.lender ?? "", answer.expiresAt, "");
                yield break;
            }

            if (status == 403 && answer != null && IsFinalRefusal(answer.error))
            {
                Refuse(answer.error);
                yield break;
            }

            if (attempt < MaxAttempts)
            {
                yield return new WaitForSecondsRealtime(RetrySeconds);
                yield return Check(pass, attempt + 1);
                yield break;
            }

            // Not a refusal: the pass is kept and checked again at the next visit.
            Debug.LogWarning("CHOG_GUEST pass could not be checked: " + status + " " + text);
            Set(GuestPassState.Refused, pass, 0, "", 0, "the game services could not be reached; open the link again in a moment");
        }

        private static void Set(GuestPassState state, string pass, int tokenId, string lender, long expiresAtMs, string problem)
        {
            State = state;
            Pass = pass;
            TokenId = tokenId;
            Lender = lender;
            ExpiresAtMs = expiresAtMs;
            Problem = problem;
            Changed?.Invoke();
        }
    }
}
