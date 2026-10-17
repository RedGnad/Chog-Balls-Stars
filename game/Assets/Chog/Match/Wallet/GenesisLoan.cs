using System;
using UnityEngine;

namespace Chog.Match
{
    public enum LoanState
    {
        /// <summary>Nothing is being lent from this screen.</summary>
        Idle,
        /// <summary>The game services are making the pass, or stopping the loans.</summary>
        Asking,
        /// <summary>A link is ready to hand out.</summary>
        Ready,
        /// <summary>The holder stopped lending: the links handed out so far are dead.</summary>
        Stopped,
        /// <summary>The services refused or could not be reached; "Notice" says which.</summary>
        Failed
    }

    /// <summary>
    /// The holder's side of lending a Genesis. A signed-in holder asks the game services for a
    /// pass and hands out the link that carries it: whoever opens the link plays that Genesis
    /// without a wallet (GuestPass), for thirty days at most, and those matches count for the
    /// career of the Genesis. The loan ends earlier when the holder stops it or when the Genesis
    /// leaves the wallet. Nothing is signed and nothing is sent to the chain for a loan: the
    /// sign-in is what proves the holder.
    /// </summary>
    public static class GenesisLoan
    {
        public static LoanState State { get; private set; } = LoanState.Idle;

        /// <summary>Token id of the Genesis the screen is lending; 0 when idle.</summary>
        public static int TokenId { get; private set; }

        /// <summary>The address to hand out; empty unless a link is ready.</summary>
        public static string Link { get; private set; } = "";

        public static long ExpiresAtMs { get; private set; }

        /// <summary>One sentence for the player about the last thing that happened; may be empty.</summary>
        public static string Notice { get; private set; } = "";

        /// <summary>Raised after any of the values above changed.</summary>
        public static event Action Changed;

        // Field names are the JSON keys of the services.
        [Serializable]
        private sealed class LoanRequest
        {
            public int tokenId;
        }

        [Serializable]
        private sealed class LoanAnswer
        {
            public string pass;
            public long expiresAt;
            public long stoppedAt;
            public string error;
            public string message;
        }

        /// <summary>Asks for a link that lends this Genesis; the player must be signed in and hold it.</summary>
        public static void Lend(int tokenId)
        {
            if (State == LoanState.Asking || !CanLend(tokenId))
                return;

            Set(LoanState.Asking, tokenId, "", 0, "");
            PlayerWallet.Run(GameServices.Call("loan", JsonUtility.ToJson(new LoanRequest { tokenId = tokenId }), PlayerWallet.Session,
                (status, text) =>
                {
                    if (State != LoanState.Asking || TokenId != tokenId)
                        return;

                    var answer = Parse(text);
                    if (status != 200 || answer == null || string.IsNullOrEmpty(answer.pass))
                    {
                        Fail(tokenId, status, answer);
                        return;
                    }

                    Debug.Log("CHOG_LOAN link ready for token " + tokenId);
                    Set(LoanState.Ready, tokenId, GuestPass.LinkFor(Application.absoluteURL, answer.pass), answer.expiresAt, "");
                }));
        }

        /// <summary>Takes the Genesis back: every link handed out for it so far stops working.</summary>
        public static void Stop()
        {
            var tokenId = TokenId;
            if (State == LoanState.Asking || !CanLend(tokenId))
                return;

            Set(LoanState.Asking, tokenId, "", 0, "");
            PlayerWallet.Run(GameServices.Call("loan/stop", JsonUtility.ToJson(new LoanRequest { tokenId = tokenId }), PlayerWallet.Session,
                (status, text) =>
                {
                    if (State != LoanState.Asking || TokenId != tokenId)
                        return;

                    var answer = Parse(text);
                    if (status != 200 || answer == null || answer.stoppedAt <= 0)
                    {
                        Fail(tokenId, status, answer);
                        return;
                    }

                    Debug.Log("CHOG_LOAN stopped for token " + tokenId);
                    Set(LoanState.Stopped, tokenId, "", 0, "");
                }));
        }

        /// <summary>Hands the link to the phone's share sheet, or copies it; the outcome comes back as a notice.</summary>
        public static void Share(string heroName)
        {
            if (State != LoanState.Ready)
                return;

            PlayerWallet.ShareLink("Play my " + heroName + " in Chog Arena", Link, outcome =>
            {
                if (State != LoanState.Ready)
                    return;

                Set(LoanState.Ready, TokenId, Link, ExpiresAtMs,
                    outcome == "copied" ? "Link copied." : outcome == "shared" ? "" : "The link could not be copied from here.");
            });
        }

        /// <summary>Copies the link, as a computer does best; the outcome comes back as a notice.</summary>
        public static void Copy()
        {
            if (State != LoanState.Ready)
                return;

            PlayerWallet.CopyLink(Link, outcome =>
            {
                if (State != LoanState.Ready)
                    return;

                Set(LoanState.Ready, TokenId, Link, ExpiresAtMs,
                    outcome == "copied" ? "Link copied: paste it in another browser or send it." : "The link could not be copied from here.");
            });
        }

        /// <summary>Leaves the lending screen; links already handed out keep working.</summary>
        public static void Close()
        {
            if (State != LoanState.Idle)
                Set(LoanState.Idle, 0, "", 0, "");
        }

        /// <summary>Whether the player may lend this Genesis now: signed in, and it is one the wallet holds.</summary>
        public static bool CanLend(int tokenId)
        {
            return tokenId > 0 && PlayerWallet.SignIn == SignInState.SignedIn && Array.IndexOf(PlayerWallet.Tokens, tokenId) >= 0;
        }

        private static LoanAnswer Parse(string text)
        {
            return !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<LoanAnswer>(text) : null;
        }

        private static void Fail(int tokenId, long status, LoanAnswer answer)
        {
            Debug.LogWarning("CHOG_LOAN refused " + status + " " + (answer == null ? "" : answer.error));
            if (status == 401)
            {
                // The session ended or was made by other services: the player signs in again.
                Set(LoanState.Idle, 0, "", 0, "");
                PlayerWallet.ForgetSession();
                return;
            }

            Set(LoanState.Failed, tokenId, "", 0, status == 403 ? "This wallet no longer holds that Genesis."
                : "The game services could not be reached. Try again in a moment.");
        }

        private static void Set(LoanState state, int tokenId, string link, long expiresAtMs, string notice)
        {
            State = state;
            TokenId = tokenId;
            Link = link;
            ExpiresAtMs = expiresAtMs;
            Notice = notice;
            Changed?.Invoke();
        }
    }
}
