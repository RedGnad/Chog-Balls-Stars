using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Chog.Match
{
    public enum WalletState
    {
        /// <summary>The browser has no wallet: the player can only be a recruit.</summary>
        NoProvider,
        /// <summary>A wallet is there and has shared no address yet.</summary>
        Disconnected,
        /// <summary>Waiting for the player's answer in the wallet.</summary>
        Connecting,
        /// <summary>The address is known; its Genesis are being read from the game services.</summary>
        Reading,
        /// <summary>Address and Genesis are known.</summary>
        Ready,
        /// <summary>The wallet refused or the services could not be read; "Problem" says which.</summary>
        Failed
    }

    public enum SignInState
    {
        /// <summary>The address is only what the wallet declared: nothing is recorded for it.</summary>
        SignedOut,
        /// <summary>Waiting for the services or for the player's signature in the wallet.</summary>
        Asking,
        /// <summary>The wallet signed the services' message: results can be recorded.</summary>
        SignedIn
    }

    /// <summary>
    /// The player's wallet as the game sees it: the address the browser wallet shared, the
    /// Genesis that address holds on Monad, the one the player chose to play, and whether the
    /// player signed in. The list comes from the game services, which read it on the chain. The
    /// address alone is what the wallet declares; signing in proves the player controls it, and
    /// only then do the services record anything for its Genesis.
    /// </summary>
    public sealed class PlayerWallet : MonoBehaviour
    {
        private const string HostName = "Chog Wallet";
        private const string ChosenTokenKey = "chog.wallet.chosenToken";
        private const string SessionKey = "chog.wallet.session";
        private const string SessionAddressKey = "chog.wallet.sessionAddress";
        private const string SessionExpiryKey = "chog.wallet.sessionExpiry";

        private static PlayerWallet s_host;
        // Whether the browser itself gives the page a wallet (an extension, a wallet's own browser).
        private static bool s_pageHasWallet;
        private static readonly int[] NoTokens = new int[0];
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static WalletState State { get; private set; } = WalletState.NoProvider;
        public static string Address { get; private set; } = "";
        /// <summary>The Genesis the address holds.</summary>
        public static int[] Tokens { get; private set; } = NoTokens;

        /// <summary>The Genesis the address may play: those it holds, then those it rents.</summary>
        public static int[] Playable { get; private set; } = NoTokens;
        public static string Problem { get; private set; } = "";

        /// <summary>Token id of the Genesis the player enters matches with; 0 for none.</summary>
        public static int ChosenTokenId { get; private set; }

        /// <summary>
        /// The owner's wallet, which may choose any Genesis to test it (the hero sheet's ADMIN button). The
        /// services admit it only once signed in, and count nothing of such a match (server/src/photon.js, isAdmin).
        /// </summary>
        public const string AdminAddress = "0x981f6e0ea94f45fdb8ee7680dc862212e3c720e0";

        public static bool IsAdminAddress(string address) => string.Equals(address, AdminAddress, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Whether the hero sheets offer the owner's wallet any Genesis (ADMIN). Off for an hour on 22 October 2026, evening, at the
        /// owner's request, on again since: off, a Genesis picked that way is forgotten and the wallet plays as any other.
        /// </summary>
        public const bool AdminTesting = true;

        /// <summary>Whether the connected wallet is the owner's, while admin testing is on.</summary>
        public static bool IsAdmin => AdminTesting && State == WalletState.Ready && IsAdminAddress(Address);

        public static SignInState SignIn { get; private set; } = SignInState.SignedOut;

        /// <summary>The token that proves the sign-in to the services; empty when signed out.</summary>
        public static string Session { get; private set; } = "";

        /// <summary>Why the last sign-in did not succeed; empty when there is nothing to say.</summary>
        public static string SignInProblem { get; private set; } = "";

        /// <summary>
        /// True when the browser has no wallet of its own and the wallet is an app the page
        /// connects to: the player picks it in a window over the game and answers in that app.
        /// </summary>
        public static bool ThroughApp { get; private set; }

        /// <summary>Raised after any of the values above changed.</summary>
        public static event Action Changed;

        // Field names are the JSON keys of the bridge and of the services.
        [Serializable]
        private sealed class WalletAnswer
        {
            public string address;
            public string signature;
            public string error;
            public int code;
            public bool changed;
            public string outcome;
            public int id;
            public string result;
        }

        [Serializable]
        private sealed class OwnedAnswer
        {
            public int[] tokens;
            public string message;
        }

        [Serializable]
        private sealed class AddressRequest
        {
            public string address;
        }

        [Serializable]
        private sealed class Challenge
        {
            public string message;
            public string proof;
            public string signature;
        }

        [Serializable]
        private sealed class SessionAnswer
        {
            public string session;
            public long expiresAt;
            public string error;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int ChogWalletHasProvider();

        [DllImport("__Internal")]
        private static extern void ChogWalletWatchConnector(string objectName, string availableMethodName, string answerMethodName);

        [DllImport("__Internal")]
        private static extern void ChogWalletRequestAddress(string objectName, string methodName, int prompt);

        [DllImport("__Internal")]
        private static extern void ChogWalletSign(string objectName, string methodName, string message, string address);

        [DllImport("__Internal")]
        private static extern void ChogWalletLeaveFor(string url);

        [DllImport("__Internal")]
        private static extern void ChogWalletShare(string objectName, string methodName, string text, string url);

        [DllImport("__Internal")]
        private static extern void ChogWalletRequest(string objectName, string methodName, int id, string rpcMethod, string parametersJson);
#else
        // Outside a web page there is no browser wallet.
        private static int ChogWalletHasProvider() => 0;

        private static void ChogWalletWatchConnector(string objectName, string availableMethodName, string answerMethodName)
        {
        }

        private static void ChogWalletRequestAddress(string objectName, string methodName, int prompt)
        {
        }

        private static void ChogWalletSign(string objectName, string methodName, string message, string address)
        {
        }

        private static void ChogWalletLeaveFor(string url)
        {
        }

        // Outside a web page a link is copied to the clipboard of the computer.
        private static void ChogWalletShare(string objectName, string methodName, string text, string url)
        {
            GUIUtility.systemCopyBuffer = url;
            if (s_host != null)
                s_host.OnShared("{\"outcome\":\"copied\"}");
        }

        private static void ChogWalletRequest(string objectName, string methodName, int id, string rpcMethod, string parametersJson)
        {
            if (s_host != null)
                s_host.OnRequestAnswer("{\"id\":" + id + ",\"error\":\"no wallet outside a web page\",\"code\":0}");
        }
#endif

        // MetaMask's documented link that opens an address in the browser of its mobile app:
        // https://link.metamask.io/dapp/{address without its scheme}.
        private const string MetaMaskBrowserLink = "https://link.metamask.io/dapp/";

        // The challenge being signed, kept until the wallet answers.
        private Challenge _challenge;

        // The read of what the address holds that is under way, so a new address can replace it.
        private Coroutine _ownedRead;

        // Who waits for the outcome of the link being shared.
        private Action<string> _shared;

        // Who waits for the wallet's answer to each request, by request number.
        private readonly Dictionary<int, Action<string, string, int>> _requests = new Dictionary<int, Action<string, string, int>>();
        private int _lastRequest;
        // When each wallet request and the sign-in were asked: a wallet that never answers (a phone app left in the background, a
        // window closed elsewhere) is given up on, so a stake or a sign-in is not stuck for the rest of the visit (23 October 2026,
        // the audit: no request had a timeout, and a busy stake froze the lobby's clock and START NOW).
        private readonly Dictionary<int, float> _requestAskedAt = new Dictionary<int, float>();
        private float _signInAskedAt;
        private const float RequestTimeoutSeconds = 120f;
        private const float SignInTimeoutSeconds = 90f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            s_host = host.AddComponent<PlayerWallet>();
            PerformanceHud.StatusLines.Add(() => Status);
            // A visitor may have come with the pass of a lent Genesis, wallet or not.
            GuestPass.Begin(Application.absoluteURL);
            // What is sold for $CHOG, if anything, is public too; so is what is offered for rent.
            GenesisShop.Begin();
            GenesisRental.Changed += OnRentalsChanged;
            GenesisRental.Begin();
            GenesisWager.Begin();
            // What a $CHOG is worth in dollars, for every amount shown.
            ChogPrice.Begin();

            s_pageHasWallet = ChogWalletHasProvider() != 0;
            if (!s_pageHasWallet)
            {
                // No wallet in this browser: the page may still be able to connect a wallet app
                // (a phone's normal browser). It says so, and gives back a connection made that
                // way on an earlier visit.
                ChogWalletWatchConnector(HostName, nameof(OnConnectorAvailable), nameof(OnWalletAnswer));
                return;
            }

            // A wallet that already shared an address with this page gives it again without asking.
            State = WalletState.Disconnected;
            ChogWalletRequestAddress(HostName, nameof(OnWalletAnswer), 0);
        }

        /// <summary>One line for the performance overlay.</summary>
        public static string Status
        {
            get
            {
                return "wallet " + State + "  " + ShortAddress(Address) + "  genesis " + Tokens.Length +
                       (Playable.Length > Tokens.Length ? "  rented " + (Playable.Length - Tokens.Length) : "") +
                       "  chosen " + ChosenTokenId + "  " + SignIn + (Problem.Length > 0 ? "  problem " + Problem : "");
            }
        }

        /// <summary>Runs a call to the game services on the wallet's own object, which never goes away.</summary>
        public static void Run(IEnumerator call)
        {
            if (s_host != null)
                s_host.StartCoroutine(call);
        }

        /// <summary>Opens the wallet so the player shares an address with the game.</summary>
        public static void Connect()
        {
            if (State == WalletState.NoProvider || State == WalletState.Connecting || s_host == null)
                return;

            Set(WalletState.Connecting, Address, Tokens, "");
            // The window where a wallet app is picked has a search field: while it is open the
            // game must not take every key for itself.
            if (ThroughApp)
                CaptureKeyboard(false);
            ChogWalletRequestAddress(HostName, nameof(OnWalletAnswer), 1);
        }

        private static void CaptureKeyboard(bool capture)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = capture;
#endif
        }

        /// <summary>
        /// The link that reopens a game page in the browser of the MetaMask mobile app, where a
        /// wallet exists; empty for a page that is not served over HTTPS, which the app cannot
        /// reach that way. The query string is left out: the link format documents none.
        /// </summary>
        public static string MetaMaskLink(string pageUrl)
        {
            const string scheme = "https://";
            if (string.IsNullOrEmpty(pageUrl) || !pageUrl.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                return "";

            var end = pageUrl.IndexOfAny(new[] { '?', '#' });
            var page = end < 0 ? pageUrl : pageUrl.Substring(0, end);
            return MetaMaskBrowserLink + page.Substring(scheme.Length);
        }

        /// <summary>Whether this page can be reopened in a wallet's own browser: a phone, over HTTPS, without a wallet.</summary>
        public static bool CanOpenInWalletBrowser
        {
            get
            {
                return OffersWalletBrowser(s_pageHasWallet, Application.isMobilePlatform, MetaMaskLink(Application.absoluteURL), State);
            }
        }

        /// <summary>
        /// Whether to offer reopening the game in MetaMask's browser: on a phone, for a page served
        /// over HTTPS (a link exists), in a browser without a wallet of its own, while no wallet app
        /// is connected or being connected. It stays offered beside the window of wallet apps: on a
        /// phone the game page is often reloaded while the player approves in the wallet app, which
        /// loses a connection under way, and inside the wallet's browser nobody leaves the page.
        /// </summary>
        public static bool OffersWalletBrowser(bool pageHasWallet, bool phone, string link, WalletState state)
        {
            return !pageHasWallet && phone && !string.IsNullOrEmpty(link)
                && (state == WalletState.NoProvider || state == WalletState.Disconnected || state == WalletState.Failed);
        }

        /// <summary>Leaves this page for the same one in the browser of the MetaMask mobile app.</summary>
        public static void OpenInWalletBrowser()
        {
            var link = MetaMaskLink(Application.absoluteURL);
            if (link.Length > 0)
                ChogWalletLeaveFor(link);
        }

        /// <summary>Leaves the game for the page where any Genesis can be looked up as its hero card.</summary>
        public static void OpenHeroPage()
        {
            ChogWalletLeaveFor(GameServices.HeroPageUrl(Application.absoluteURL));
        }

        /// <summary>Copies a link to the clipboard; "done" receives "copied" or "failed".</summary>
        public static void CopyLink(string url, Action<string> done)
        {
            // The bridge reads this prefix of the text as "copy, do not share" (ChogWallet.jslib).
            ShareLink("copy:", url, done);
        }

        /// <summary>
        /// Hands a link to the share sheet of the phone, or copies it where there is none;
        /// "done" receives "shared", "copied" or "failed".
        /// </summary>
        public static void ShareLink(string text, string url, Action<string> done)
        {
            if (s_host == null || string.IsNullOrEmpty(url))
            {
                done?.Invoke("failed");
                return;
            }

            s_host._shared = done;
            ChogWalletShare(HostName, nameof(OnShared), text, url);
        }

        /// <summary>
        /// Sends one request to the wallet as it is: a method of the wallet interface (EIP-1193)
        /// and its parameters as a JSON array. "done" receives what the wallet returned as JSON
        /// text, or null with the wallet's error and its code (4001 when the player refused).
        /// </summary>
        public static void Request(string rpcMethod, string parametersJson, Action<string, string, int> done)
        {
            if (s_host == null || State == WalletState.NoProvider)
            {
                done?.Invoke(null, "no wallet in this browser", 0);
                return;
            }

            var id = ++s_host._lastRequest;
            s_host._requests[id] = done;
            s_host._requestAskedAt[id] = Time.unscaledTime;
            ChogWalletRequest(HostName, nameof(OnRequestAnswer), id, rpcMethod, parametersJson);
        }

        /// <summary>Chooses the Genesis to play; only one the address holds or rents can be chosen, any one by the owner's wallet.</summary>
        public static void Choose(int tokenId)
        {
            // A Genesis rented a moment ago may not be in the list yet: nothing says in which
            // order those who listen to the rental hear of it.
            if (State == WalletState.Ready && Array.IndexOf(Playable, tokenId) < 0)
                Playable = Merge(Tokens, GenesisRental.RentedTokens());

            var allowed = Array.IndexOf(Playable, tokenId) >= 0 || (IsAdmin && tokenId > 0);
            if (!allowed || tokenId == ChosenTokenId)
                return;

            ChosenTokenId = tokenId;
            // Written at once: a browser page has no reliable moment of quitting to save at.
            PlayerPrefs.SetInt(ChosenTokenKey, tokenId);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>The owner's wallet gives back a Genesis it picked with ADMIN: the first one it holds, else the recruit.</summary>
        public static void ChooseRecruit()
        {
            if (!IsAdmin)
                return;
            PlayerPrefs.SetInt(ChosenTokenKey, 0);
            PlayerPrefs.Save();
            ChosenTokenId = PickToken(Playable, 0);
            Changed?.Invoke();
        }

        /// <summary>
        /// Asks the wallet to sign the services' message, which proves the player controls the
        /// address. It costs nothing and sends no transaction.
        /// </summary>
        public static void StartSignIn()
        {
            if (State != WalletState.Ready || SignIn == SignInState.Asking || s_host == null)
                return;

            SetSignIn(SignInState.Asking, "", "");
            s_host._signInAskedAt = Time.unscaledTime;
            var address = Address;
            Run(GameServices.Call("session/challenge", JsonUtility.ToJson(new AddressRequest { address = address }), null,
                (status, text) =>
                {
                    if (address != Address)
                        return;

                    var challenge = status == 200 ? JsonUtility.FromJson<Challenge>(text) : null;
                    if (challenge == null || string.IsNullOrEmpty(challenge.message))
                    {
                        SetSignIn(SignInState.SignedOut, "", "the sign-in could not start: " + status + " " + text);
                        return;
                    }

                    s_host._challenge = challenge;
                    ChogWalletSign(HostName, nameof(OnSignature), challenge.message, address);
                }));
        }

        /// <summary>
        /// The Genesis to play among those held: the one chosen last time if it is still held,
        /// else the first one, else none.
        /// </summary>
        public static int PickToken(int[] held, int remembered)
        {
            if (held == null || held.Length == 0)
                return 0;

            return Array.IndexOf(held, remembered) >= 0 ? remembered : held[0];
        }

        /// <summary>PickToken, except that the owner's wallet keeps the Genesis it chose last, held or not.</summary>
        public static int PickFor(string address, int[] playable, int remembered)
        {
            return AdminTesting && IsAdminAddress(address) && remembered > 0 ? remembered : PickToken(playable, remembered);
        }

        /// <summary>The Genesis an address may play: those it holds, then those it rents and does not hold.</summary>
        public static int[] Merge(int[] held, int[] rented)
        {
            if (rented == null || rented.Length == 0)
                return held ?? NoTokens;

            var all = new List<int>(held ?? NoTokens);
            foreach (var tokenId in rented)
                if (!all.Contains(tokenId))
                    all.Add(tokenId);
            return all.ToArray();
        }

        // What the address rents was read, or changed: the choice follows, and a Genesis rented
        // a moment ago can be chosen.
        private static void OnRentalsChanged()
        {
            if (State != WalletState.Ready)
                return;

            var playable = Merge(Tokens, GenesisRental.RentedTokens());
            if (playable.Length == Playable.Length && Array.TrueForAll(playable, tokenId => Array.IndexOf(Playable, tokenId) >= 0))
                return;

            Playable = playable;
            ChosenTokenId = PickFor(Address, Playable, PlayerPrefs.GetInt(ChosenTokenKey, 0));
            Changed?.Invoke();
        }

        /// <summary>"0x80fb...cd64" for a full address; anything else is returned as it is.</summary>
        public static string ShortAddress(string address)
        {
            return address != null && address.Length == 42 ? address.Substring(0, 6) + "..." + address.Substring(38) : address ?? "";
        }

        /// <summary>
        /// Whether a kept session can still be used: it was made for this address and has not
        /// reached its end, given in milliseconds since 1970 as the services write it.
        /// </summary>
        public static bool IsSessionUsable(string session, string sessionAddress, long expiresAtMs, string address, long nowMs)
        {
            return !string.IsNullOrEmpty(session) && !string.IsNullOrEmpty(address) && sessionAddress == address && expiresAtMs > nowMs;
        }

        // Called by name from the browser bridge when the page, which has no wallet of its own,
        // can connect a wallet app: connecting becomes possible.
        [Preserve]
        private void OnConnectorAvailable(string unused)
        {
            ThroughApp = true;
            Debug.Log("CHOG_WALLET a wallet app can be connected from this page");
            if (State == WalletState.NoProvider)
                Set(WalletState.Disconnected, "", NoTokens, "");
        }

        // Called by name from the browser bridge (ChogWallet.jslib) with one JSON message.
        [Preserve]
        private void OnWalletAnswer(string json)
        {
            var answer = JsonUtility.FromJson<WalletAnswer>(json);
            if (answer == null)
                return;

            // Whatever the answer, the window of the wallet apps is closed by now.
            if (ThroughApp)
                CaptureKeyboard(true);

            if (!string.IsNullOrEmpty(answer.error))
            {
                // 4001 is the player closing the wallet without sharing: not a failure to show.
                Debug.Log("CHOG_WALLET refused " + answer.code + " " + answer.error);
                if (answer.code == 4001)
                    Set(WalletState.Disconnected, "", NoTokens, "");
                else
                    Set(WalletState.Failed, "", NoTokens, answer.error);
                return;
            }

            var address = (answer.address ?? "").ToLowerInvariant();
            if (address.Length != 42)
            {
                Set(WalletState.Disconnected, "", NoTokens, "");
                return;
            }

            if (address == Address && (State == WalletState.Ready || State == WalletState.Reading))
                return;

            Debug.Log("CHOG_WALLET address " + ShortAddress(address) + (answer.changed ? " (account changed)" : ""));
            Set(WalletState.Reading, address, NoTokens, "");
            // Only the read for the address before: the other calls this object runs go on.
            if (_ownedRead != null)
                StopCoroutine(_ownedRead);
            _ownedRead = StartCoroutine(GameServices.Call("genesis/owned?address=" + address, null, null, (status, text) =>
            {
                if (address != Address)
                    return;

                var owned = string.IsNullOrEmpty(text) || text[0] != '{' ? null : JsonUtility.FromJson<OwnedAnswer>(text);
                if (status != 200 || owned == null || owned.tokens == null)
                {
                    var problem = owned != null && !string.IsNullOrEmpty(owned.message) ? owned.message : status + " " + text;
                    Debug.LogWarning("CHOG_WALLET could not read the Genesis of " + ShortAddress(address) + " : " + problem);
                    Set(WalletState.Failed, address, NoTokens, "the Genesis of this wallet could not be read: " + problem);
                    return;
                }

                Debug.Log("CHOG_WALLET genesis " + owned.tokens.Length + " for " + ShortAddress(address));
                Set(WalletState.Ready, address, owned.tokens, "");
                // What this address rents is read next; the choice follows when it is known.
                GenesisRental.ReadRented();
            }));
        }

        // Called by name from the browser bridge once a link was shared or copied.
        [Preserve]
        private void OnShared(string json)
        {
            var answer = JsonUtility.FromJson<WalletAnswer>(json);
            var done = _shared;
            _shared = null;
            done?.Invoke(answer == null || string.IsNullOrEmpty(answer.outcome) ? "failed" : answer.outcome);
        }

        // Called by name from the browser bridge with the wallet's answer to a request.
        [Preserve]
        private void Update()
        {
            if (SignIn == SignInState.Asking && Time.unscaledTime - _signInAskedAt > SignInTimeoutSeconds)
                SetSignIn(SignInState.SignedOut, "", "the wallet did not answer: sign in again");
            if (_requestAskedAt.Count == 0)
                return;
            foreach (var asked in new List<KeyValuePair<int, float>>(_requestAskedAt))
            {
                if (Time.unscaledTime - asked.Value < RequestTimeoutSeconds)
                    continue;
                _requestAskedAt.Remove(asked.Key);
                if (_requests.TryGetValue(asked.Key, out var done))
                {
                    _requests.Remove(asked.Key);
                    // Read as a refusal: nothing was signed, and no other way is tried behind the player's back.
                    done?.Invoke(null, "the wallet did not answer", MonadTransactions.UserRefused);
                }
            }
        }

        private void OnRequestAnswer(string json)
        {
            var answer = JsonUtility.FromJson<WalletAnswer>(json);
            if (answer == null || !_requests.TryGetValue(answer.id, out var done))
                return;

            _requests.Remove(answer.id);
            _requestAskedAt.Remove(answer.id);
            if (string.IsNullOrEmpty(answer.error))
                done?.Invoke(answer.result ?? "null", null, 0);
            else
                done?.Invoke(null, answer.error, answer.code);
        }

        // Called by name from the browser bridge with the signature of the challenge.
        [Preserve]
        private void OnSignature(string json)
        {
            var answer = JsonUtility.FromJson<WalletAnswer>(json);
            var challenge = _challenge;
            _challenge = null;
            if (answer == null || challenge == null || SignIn != SignInState.Asking)
                return;

            if (string.IsNullOrEmpty(answer.signature))
            {
                Debug.Log("CHOG_WALLET signature refused " + answer.code + " " + answer.error);
                SetSignIn(SignInState.SignedOut, "", answer.code == 4001 ? "" : "the wallet did not sign: " + answer.error);
                return;
            }

            var address = Address;
            challenge.signature = answer.signature;
            Run(GameServices.Call("session", JsonUtility.ToJson(challenge), null, (status, text) =>
            {
                if (address != Address)
                    return;

                var session = status == 200 ? JsonUtility.FromJson<SessionAnswer>(text) : null;
                if (session == null || string.IsNullOrEmpty(session.session))
                {
                    Debug.LogWarning("CHOG_WALLET sign-in refused " + status + " " + text);
                    SetSignIn(SignInState.SignedOut, "", "the services refused the signature: " + status + " " + text);
                    return;
                }

                // One session kept per address: switching accounts back and forth asks nothing again.
                PlayerPrefs.SetString(SessionKey + "." + address, session.session);
                PlayerPrefs.SetString(SessionExpiryKey + "." + address, session.expiresAt.ToString(CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
                Debug.Log("CHOG_WALLET signed in " + ShortAddress(address));
                SetSignIn(SignInState.SignedIn, session.session, "");
            }));
        }

        private static void Set(WalletState state, string address, int[] tokens, string problem)
        {
            var addressChanged = address != Address;
            State = state;
            Address = address;
            Tokens = tokens;
            Problem = problem;
            // Only a list read from the services decides the choice: while it is being read, or
            // after a failure, nothing is chosen and the player enters as a recruit.
            Playable = state == WalletState.Ready ? Merge(tokens, GenesisRental.RentedTokens()) : NoTokens;
            ChosenTokenId = state == WalletState.Ready ? PickFor(address, Playable, PlayerPrefs.GetInt(ChosenTokenKey, 0)) : 0;

            if (addressChanged)
            {
                // A session belongs to one address: another account starts signed out, unless a
                // session kept from an earlier visit is still good for it.
                var nowMs = (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
                long.TryParse(PlayerPrefs.GetString(SessionExpiryKey + "." + address, "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiry);
                var kept = PlayerPrefs.GetString(SessionKey + "." + address, "");
                var usable = IsSessionUsable(kept, address, expiry, address, nowMs);
                if (!usable)
                {
                    // The one slot of earlier builds, for the address it was made for.
                    long.TryParse(PlayerPrefs.GetString(SessionExpiryKey, "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out expiry);
                    kept = PlayerPrefs.GetString(SessionKey, "");
                    usable = IsSessionUsable(kept, PlayerPrefs.GetString(SessionAddressKey, ""), expiry, address, nowMs);
                }
                SignIn = usable ? SignInState.SignedIn : SignInState.SignedOut;
                Session = usable ? kept : "";
                SignInProblem = "";
            }

            Changed?.Invoke();
        }

        private static void SetSignIn(SignInState state, string session, string problem)
        {
            SignIn = state;
            Session = session;
            SignInProblem = problem;
            Changed?.Invoke();
        }

        /// <summary>Forgets a session the services no longer accept.</summary>
        public static void ForgetSession()
        {
            PlayerPrefs.DeleteKey(SessionKey);
            PlayerPrefs.DeleteKey(SessionKey + "." + Address);
            PlayerPrefs.Save();
            SetSignIn(SignInState.SignedOut, "", "");
        }
    }
}
