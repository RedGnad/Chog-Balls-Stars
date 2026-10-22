using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using UnityEngine;

namespace Chog.Match
{
    public enum WagerState
    {
        /// <summary>The game services were not asked yet, or did not answer.</summary>
        Unknown,
        /// <summary>No match is staked on: no wager contract is set up.</summary>
        Closed,
        /// <summary>Players can stake $CHOG on a match.</summary>
        Open
    }

    public enum WagerStep
    {
        Idle,
        /// <summary>The referee is asked for a ticket.</summary>
        Asking,
        /// <summary>The wallet is asked to be on Monad.</summary>
        Switching,
        /// <summary>The wallet is asked to let the wager take the stake, then that transaction is awaited.</summary>
        Approving,
        /// <summary>The wallet is asked for the transaction itself, which is then awaited.</summary>
        Sending,
        /// <summary>The transaction is on the chain; the game services are asked to see it.</summary>
        Confirming,
        /// <summary>The stakes are being locked before the match begins.</summary>
        Locking,
        /// <summary>It did not go through; "Problem" says why.</summary>
        Failed
    }

    public enum WagerAction
    {
        None,
        Stake,
        Lock,
        /// <summary>Bringing the referee's decision to the contract, which pays or gives back.</summary>
        Claim,
        /// <summary>Taking test tokens from the tap of the test token, on the testnet only.</summary>
        TestTokens
    }

    /// <summary>What became of the stakes of a match, as far as the game has read.</summary>
    public enum WagerOutcome
    {
        /// <summary>Nothing was read yet.</summary>
        Unknown,
        /// <summary>The match is not judged yet.</summary>
        Pending,
        /// <summary>A team won; the referee signed it.</summary>
        Won,
        /// <summary>Nobody won: every stake goes back.</summary>
        Draw,
        /// <summary>The match was called off: every stake goes back.</summary>
        CalledOff,
        /// <summary>
        /// The game server does not know the match (lost with a container, before the mirror of 22 October 2026, or never judged):
        /// its stakes wait on the chain, and the contract gives them back an hour after the first one (refundExpired).
        /// </summary>
        Lost
    }

    public struct WagerStaker
    {
        public string Address;
        public int Team;
    }

    /// <summary>
    /// Staking $CHOG on a match. Before the match begins a signed-in player stakes on its own
    /// team: the game services' referee signs a ticket, the wallet sends the stake to the wager
    /// contract, which holds it. The first stake of a match fixes what everyone stakes. When the
    /// match is started the stakes are locked. After the match the services play it again from
    /// the record of Photon's server and the referee signs who won; anyone brings that signature
    /// to the contract, which pays the winners what the losers staked, less a share that is
    /// burned. Whenever the stakes cannot be trusted the match is called off and every stake
    /// goes back. The $CHOG never passes through the game or its services.
    /// While no wager contract is set up on the services this class stays closed.
    /// </summary>
    public static class GenesisWager
    {
        private const string StakeSelector = "0x0a621329";  // stake(bytes32,uint8,uint256,uint256,bytes)
        private const string SettleSelector = "0x99bcbc7f"; // settle(bytes32,uint8,bytes), version 1
        private const string SettleWithGenesisSelector = "0x061ab97d"; // settle(bytes32,uint8,address[],bytes), version 2
        private const string VoidSelector = "0xe3c01265";   // voidMatch(bytes32,bytes)
        private const string RefundExpiredSelector = "0xcc3e049b"; // refundExpired(bytes32)
        private const string TapSelector = "0x4e71d92d";    // claim() of the test token

        // A first claim from the tap cost 107,537 on a local chain (15 October 2026); Monad
        // prices some storage access higher, hence the margin.
        private const int TapGasLimit = 200000;

        // Monad charges the gas limit of a transaction, not what it uses. Measured on a copy of
        // mainnet (contracts/README.md): a first stake 246,008 and a later one 177,896; paying a
        // match of one a side 139,805 and of three a side 205,710. Giving sixteen stakes back was
        // not measured: the limit of the last two leaves room for it.
        private const int StakeGasLimit = 400000;
        private const int ClaimGasLimit = 900000;

        private const int SeenAttempts = 8;
        private const float SeenSeconds = 2f;
        private const float LockAgainSeconds = 3f;
        private const string LastMatchKey = "chog.wager.last";

        private static readonly List<WagerStaker> Staked = new List<WagerStaker>();

        public static WagerState State { get; private set; } = WagerState.Unknown;

        /// <summary>The wager contract and the $CHOG token, as the services gave them.</summary>
        public static string Address { get; private set; } = "";
        public static string Token { get; private set; } = "";

        /// <summary>The chain the wager is on: Monad, or Monad's testnet where the stakes are a test token.</summary>
        public static MonadTransactions.ChainSpec Chain { get; private set; } = MonadTransactions.Mainnet;
        public static bool IsTest { get; private set; }

        /// <summary>What the stakes are called on screen: a test token is never shown as $CHOG.</summary>
        public static string Currency => IsTest ? "TEST $CHOG" : "$CHOG";

        /// <summary>Whether this page already took test tokens from the tap (once a day per wallet).</summary>
        public static bool TookTestTokens { get; private set; }

        /// <summary>The share of what the losers staked that is burned, in hundredths of a percent.</summary>
        public static int BurnedShare { get; private set; }

        /// <summary>
        /// Share of the lost stakes, in basis points, split between the Genesis players of a match:
        /// version 2 of the contract (22 October 2026); 0 on version 1.
        /// </summary>
        public static int GenesisShare { get; private set; }

        /// <summary>The version of the wager contract: 2 pays the Genesis players of a match.</summary>
        public static int Version { get; private set; } = 1;

        /// <summary>What is taken from the lost stakes before the winners share them: burned, and the Genesis players' share.</summary>
        public static int TakenShare => BurnedShare + GenesisShare;

        /// <summary>Whether the services know the match the signed-in wallet is in.</summary>
        public static bool InMatch { get; private set; }

        /// <summary>The name of that match for the services and its key on the chain.</summary>
        public static string GameId { get; private set; } = "";
        public static string MatchKey { get; private set; } = "";

        /// <summary>What each player stakes on that match, in the smallest unit of $CHOG; empty while nobody asked for a ticket.</summary>
        public static string StakeWei { get; private set; } = "";

        /// <summary>Whether the stakes of that match are locked.</summary>
        public static bool Locked { get; private set; }

        /// <summary>How long the lock still waits for a ticket that is out, in seconds; 0 otherwise.</summary>
        public static int LockWaitSeconds { get; private set; }

        public static WagerStep Step { get; private set; } = WagerStep.Idle;
        public static WagerAction Doing { get; private set; } = WagerAction.None;

        /// <summary>Why the last action did not go through; empty otherwise.</summary>
        public static string Problem { get; private set; } = "";

        /// <summary>The last match this device staked on, until its stake is paid or given back.</summary>
        public static string LastGameId { get; private set; } = "";
        public static int LastTeam { get; private set; }
        public static string LastStakeWei { get; private set; } = "";
        public static WagerOutcome Outcome { get; private set; } = WagerOutcome.Unknown;
        public static int WinnerTeam { get; private set; }
        public static string CalledOffBecause { get; private set; } = "";

        /// <summary>Where the stakes of that last match stand on the chain: "open", "settled", "refunded" or "none".</summary>
        public static string LastChainState { get; private set; } = "";
        public static int LastWinners { get; private set; }
        public static int LastLosers { get; private set; }

        private static string s_lastMatchKey = "";
        private static string s_decisionSignature = "";
        // The wallets the referee signed as the Genesis players of the last match (version 2).
        private static string[] s_genesisPlayers = Array.Empty<string>();

        /// <summary>Whether the week of the signed-in wallet was read (ReadWeek), and for which address.</summary>
        public static bool WeekRead => s_weekOf.Length == 42 && s_weekOf == PlayerWallet.Address;
        private static string s_weekOf = "";

        /// <summary>What the signed-in wallet won and lost on its stakes of the last seven days, in the smallest unit; "0" until read.</summary>
        public static string WeekWonWei { get; private set; } = "0";
        public static string WeekLostWei { get; private set; } = "0";

        /// <summary>How many staked matches the week counts, how many are not judged yet, how many are judged but not paid yet.</summary>
        public static int WeekMatches { get; private set; }
        public static int WeekPending { get; private set; }
        public static int WeekUnpaid { get; private set; }

        /// <summary>Raised after any of the values above changed.</summary>
        public static event Action Changed;

        // Field names are the JSON keys of the services.
        [Serializable]
        private sealed class TermsAnswer
        {
            public bool enabled;
            public string address;
            public string token;
            public int burnedShare;
            public int genesisShare;
            public int version;
            public int chainId;
            public string chainName;
            public string rpcUrl;
            public bool test;
        }

        [Serializable]
        private sealed class StakerAnswer
        {
            public string address;
            public int team;
        }

        [Serializable]
        private sealed class StakesAnswer
        {
            public string state;
            public string amount;
            public long firstStakeAt;
            public StakerAnswer[] stakers;
        }

        [Serializable]
        private sealed class MineAnswer
        {
            public bool enabled;
            public bool inMatch;
            public string gameId;
            public string matchKey;
            public string amount;
            public string lockedAt;
            public StakesAnswer stakes;
        }

        [Serializable]
        private sealed class TicketAnswer
        {
            public string gameId;
            public string matchKey;
            public int team;
            public string amount;
            public long deadline;
            public string signature;
            public string error;
            public string message;
        }

        [Serializable]
        private sealed class LockAnswer
        {
            public bool locked;
            public int waitSeconds;
            public string error;
            public string message;
        }

        [Serializable]
        private sealed class WeekAnswer
        {
            public bool enabled;
            public string address;
            public int matches;
            public int pending;
            public int unpaid;
            public string won;
            public string lost;
        }

        [Serializable]
        private sealed class OutcomeAnswer
        {
            public bool pending;
            public bool lost;
            public long refundAfterSeconds;
            public string matchKey;
            public int winnerTeam;
            public bool @void;
            public string why;
            public string signature;
            public string[] genesisPlayers;
            public StakesAnswer stakes;
        }

        /// <summary>Whether an action is under way.</summary>
        public static bool IsBusy => Step != WagerStep.Idle && Step != WagerStep.Failed;

        /// <summary>Who staked on the match the wallet is in, as the chain has it.</summary>
        public static IReadOnlyList<WagerStaker> Stakers => Staked;

        /// <summary>Whether the connected wallet staked on the match it is in.</summary>
        public static bool HasStaked => TeamStakedOn(Staked, PlayerWallet.Address) >= 0;

        /// <summary>Whether this device has a stake whose match is not paid or given back yet.</summary>
        public static bool HasLastMatch => LastGameId.Length > 0;

        /// <summary>Asks the game services whether matches are staked on. Called once when the page is up.</summary>
        public static void Begin()
        {
            Recall();
            PlayerWallet.Run(GameServices.Call("wager", null, null, (status, text) =>
            {
                var answer = Parsed<TermsAnswer>(status, text);
                if (answer == null)
                {
                    Debug.Log("CHOG_WAGER not read: " + status);
                    return;
                }

                // Services from before the chain was named answer no chainId: the wager is on mainnet.
                var chain = answer.chainId == 0 ? MonadTransactions.Mainnet : MonadTransactions.ChainOf(answer.chainId, answer.chainName, answer.rpcUrl);
                State = answer.enabled && chain != null && MonadTransactions.LooksLikeAnAddress(answer.address)
                        && MonadTransactions.LooksLikeAnAddress(answer.token)
                    ? WagerState.Open : WagerState.Closed;
                Address = answer.address ?? "";
                Token = answer.token ?? "";
                BurnedShare = answer.burnedShare;
                GenesisShare = answer.genesisShare;
                Version = answer.version >= 2 ? 2 : 1;
                Chain = chain ?? MonadTransactions.Mainnet;
                IsTest = answer.test;
                Debug.Log("CHOG_WAGER " + (State == WagerState.Open ? "open on " + Chain.Name + " (" + Chain.HexId + ")" : "closed"));
                Changed?.Invoke();
            }));
        }

        /// <summary>Reads the stakes of the match the signed-in wallet is in; "Changed" follows.</summary>
        public static void ReadMine(Action then = null)
        {
            if (State != WagerState.Open || string.IsNullOrEmpty(PlayerWallet.Session))
            {
                then?.Invoke();
                return;
            }

            PlayerWallet.Run(GameServices.Call("wager/mine", null, PlayerWallet.Session, (status, text) =>
            {
                var answer = Parsed<MineAnswer>(status, text);
                if (answer != null)
                {
                    InMatch = answer.inMatch && !string.IsNullOrEmpty(answer.gameId);
                    GameId = InMatch ? answer.gameId : "";
                    MatchKey = InMatch ? answer.matchKey ?? "" : "";
                    StakeWei = InMatch && MonadTransactions.IsDigits(answer.amount) ? answer.amount : "";
                    Locked = InMatch && !string.IsNullOrEmpty(answer.lockedAt);
                    Staked.Clear();
                    if (InMatch && answer.stakes != null && answer.stakes.stakers != null)
                        foreach (var staker in answer.stakes.stakers)
                            Staked.Add(new WagerStaker { Address = (staker.address ?? "").ToLowerInvariant(), Team = staker.team });
                    Changed?.Invoke();
                }

                then?.Invoke();
            }));
        }

        /// <summary>
        /// The signed-in player stakes a whole number of $CHOG on its own team in the match it is
        /// in; when the match already has a stake, that one is staked whatever is asked. The
        /// wallet is asked up to three things: to be on Monad, to let the wager take the stake
        /// (skipped when it already may), and to stake.
        /// </summary>
        public static void Stake(int team, int wholeChog)
        {
            if (State != WagerState.Open || IsBusy || !InMatch || Locked || HasStaked || PlayerWallet.State != WalletState.Ready
                || (team != 0 && team != 1) || wholeChog <= 0)
                return;

            var player = PlayerWallet.Address;
            var amount = StakeWei.Length > 0 ? StakeWei : MonadTransactions.WholeToWei(wholeChog);
            SetStep(WagerStep.Asking, WagerAction.Stake, "");
            var body = "{\"team\":" + team + ",\"amount\":\"" + amount + "\"}";
            PlayerWallet.Run(GameServices.Call("wager/ticket", body, PlayerWallet.Session, (status, text) =>
            {
                var ticket = text != null && text.Length > 0 && text[0] == '{' ? JsonUtility.FromJson<TicketAnswer>(text) : null;
                if (status != 200 || ticket == null || !MonadTransactions.IsDigits(ticket.amount) || string.IsNullOrEmpty(ticket.signature))
                {
                    Fail(0, ticket != null && !string.IsNullOrEmpty(ticket.message) ? ticket.message : "the referee gave no ticket (" + status + ")");
                    return;
                }

                SetStep(WagerStep.Switching, WagerAction.Stake, "");
                MonadTransactions.SwitchTo(Chain, () =>
                {
                    MonadTransactions.AllowPayment(player, Token, Address, ticket.amount, "the stake is",
                        () => SetStep(WagerStep.Approving, WagerAction.Stake, ""),
                        () =>
                        {
                            SetStep(WagerStep.Sending, WagerAction.Stake, "");
                            MonadTransactions.Send(player, Address, StakeData(ticket.matchKey, ticket.team, ticket.amount, ticket.deadline, ticket.signature),
                                StakeGasLimit, "the stake", () =>
                                {
                                    Remember(ticket.gameId, ticket.matchKey, ticket.team, ticket.amount);
                                    Confirm(() => HasStaked, ReadMine, "the stake");
                                }, Fail);
                        }, Fail, Currency);
                }, Fail);
            }));
        }

        /// <summary>
        /// Calls "start" once the match may begin: at once when nobody staked on it, else when
        /// its stakes are locked. While a ticket handed out can still be used the lock waits,
        /// "LockWaitSeconds" says how long, and it is asked again.
        /// </summary>
        public static void WhenLocked(Action start)
        {
            if (State != WagerState.Open || string.IsNullOrEmpty(PlayerWallet.Session) || IsBusy)
            {
                // Without a wager, without a signed-in wallet, or while a stake is being sent,
                // the match starts as it always did. Stakes that were not locked are given back.
                if (!IsBusy)
                    start();
                return;
            }

            SetStep(WagerStep.Locking, WagerAction.Lock, "");
            ReadMine(() =>
            {
                if (!InMatch || Staked.Count == 0 || Locked)
                {
                    SetStep(WagerStep.Idle, WagerAction.None, "");
                    start();
                    return;
                }

                PlayerWallet.Run(LockThen(start));
            });
        }

        private static IEnumerator LockThen(Action start)
        {
            while (true)
            {
                LockAnswer answer = null;
                long answered = -1;
                yield return GameServices.Call("wager/lock", "{}", PlayerWallet.Session, (status, text) =>
                {
                    answered = status;
                    answer = text != null && text.Length > 0 && text[0] == '{' ? JsonUtility.FromJson<LockAnswer>(text) : null;
                });

                if (answered == 200 && answer != null && answer.locked)
                {
                    Locked = true;
                    LockWaitSeconds = 0;
                    Debug.Log("CHOG_WAGER stakes locked");
                    SetStep(WagerStep.Idle, WagerAction.None, "");
                    start();
                    yield break;
                }

                if (answered != 200 || answer == null)
                {
                    LockWaitSeconds = 0;
                    Fail(0, answer != null && !string.IsNullOrEmpty(answer.message) ? answer.message : "the stakes could not be locked (" + answered + ")");
                    yield break;
                }

                LockWaitSeconds = Mathf.Max(1, answer.waitSeconds);
                Changed?.Invoke();
                yield return new WaitForSecondsRealtime(LockAgainSeconds);
            }
        }

        /// <summary>Reads what became of the last match this device staked on; "Changed" follows.</summary>
        public static void ReadOutcome(Action then = null)
        {
            if (State != WagerState.Open || !HasLastMatch)
            {
                then?.Invoke();
                return;
            }

            var asked = LastGameId;
            PlayerWallet.Run(GameServices.Call("wager/result?gameId=" + Uri.EscapeDataString(asked), null, null, (status, text) =>
            {
                var answer = Parsed<OutcomeAnswer>(status, text);
                if (answer != null && asked == LastGameId)
                {
                    Outcome = answer.lost ? WagerOutcome.Lost
                        : answer.pending ? WagerOutcome.Pending
                        : answer.@void ? WagerOutcome.CalledOff
                        : answer.winnerTeam == 2 ? WagerOutcome.Draw : WagerOutcome.Won;
                    WinnerTeam = answer.winnerTeam;
                    CalledOffBecause = answer.why ?? "";
                    s_decisionSignature = answer.signature ?? "";
                    s_genesisPlayers = answer.genesisPlayers ?? Array.Empty<string>();
                    LastChainState = answer.stakes != null ? answer.stakes.state ?? "" : "";
                    RefundableAt = answer.stakes != null && answer.stakes.firstStakeAt > 0 && answer.refundAfterSeconds > 0
                        ? answer.stakes.firstStakeAt + answer.refundAfterSeconds : 0;
                    LastWinners = 0;
                    LastLosers = 0;
                    if (answer.stakes != null && answer.stakes.stakers != null)
                    {
                        foreach (var staker in answer.stakes.stakers)
                        {
                            if (Outcome == WagerOutcome.Won && staker.team == WinnerTeam)
                                LastWinners++;
                            else
                                LastLosers++;
                        }
                    }

                    Changed?.Invoke();
                }

                then?.Invoke();
            }));
        }

        /// <summary>Reads what the signed-in wallet won and lost on its stakes of the last seven days; "Changed" follows.</summary>
        public static void ReadWeek(Action then = null)
        {
            var who = PlayerWallet.Address;
            if (State != WagerState.Open || string.IsNullOrEmpty(PlayerWallet.Session) || who.Length != 42)
            {
                then?.Invoke();
                return;
            }

            PlayerWallet.Run(GameServices.Call("wager/week", null, PlayerWallet.Session, (status, text) =>
            {
                var answer = Parsed<WeekAnswer>(status, text);
                if (answer != null && answer.enabled && who == PlayerWallet.Address)
                {
                    WeekWonWei = MonadTransactions.IsDigits(answer.won) ? answer.won : "0";
                    WeekLostWei = MonadTransactions.IsDigits(answer.lost) ? answer.lost : "0";
                    WeekMatches = answer.matches;
                    WeekPending = answer.pending;
                    WeekUnpaid = answer.unpaid;
                    s_weekOf = who;
                    Changed?.Invoke();
                }

                then?.Invoke();
            }));
        }

        /// <summary>Whether the referee's decision can be brought to the contract now.</summary>
        public static bool CanClaim => HasLastMatch && !IsBusy && LastChainState == "open" && s_decisionSignature.Length > 0
            && (Outcome == WagerOutcome.Won || Outcome == WagerOutcome.Draw || Outcome == WagerOutcome.CalledOff)
            && PlayerWallet.State == WalletState.Ready;

        /// <summary>
        /// Brings the referee's decision to the contract: it pays the winners, or gives every
        /// stake back. Anyone may send it; the first to do so settles the match for everyone.
        /// </summary>
        public static void Claim()
        {
            if (!CanClaim)
                return;

            var sender = PlayerWallet.Address;
            var data = Outcome == WagerOutcome.CalledOff
                ? VoidData(s_lastMatchKey, s_decisionSignature)
                : SettleData(s_lastMatchKey, WinnerTeam, s_decisionSignature, Version >= 2 ? s_genesisPlayers : null);
            SetStep(WagerStep.Switching, WagerAction.Claim, "");
            MonadTransactions.SwitchTo(Chain, () =>
            {
                SetStep(WagerStep.Sending, WagerAction.Claim, "");
                MonadTransactions.Send(sender, Address, data, ClaimGasLimit, "the payment of the match",
                    () => Confirm(() => LastChainState == "settled" || LastChainState == "refunded", ReadOutcome, "the payment of the match"), Fail);
            }, Fail);
        }

        /// <summary>When the contract gives the stakes of the last match back to anyone who asks (unix seconds); 0 when unknown.</summary>
        public static long RefundableAt { get; private set; }

        /// <summary>
        /// Whether the stakes of the last match wait on the chain with no decision to bring (the server lost the match, or never judged
        /// it) and the hour after which the contract gives them back has passed.
        /// </summary>
        public static bool CanRefundExpired(long nowSeconds) => HasLastMatch && !IsBusy && LastChainState == "open"
            && (Outcome == WagerOutcome.Lost || Outcome == WagerOutcome.Pending) && RefundableAt > 0 && nowSeconds >= RefundableAt
            && PlayerWallet.State == WalletState.Ready;

        /// <summary>Gives every stake of the last match back (refundExpired): no signature needed once the hour has passed.</summary>
        public static void RefundExpired()
        {
            var now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            if (!CanRefundExpired(now))
                return;
            var sender = PlayerWallet.Address;
            var data = RefundExpiredSelector + s_lastMatchKey.Substring(2);
            SetStep(WagerStep.Switching, WagerAction.Claim, "");
            MonadTransactions.SwitchTo(Chain, () =>
            {
                SetStep(WagerStep.Sending, WagerAction.Claim, "");
                MonadTransactions.Send(sender, Address, data, ClaimGasLimit, "the refund of the stakes",
                    () => Confirm(() => LastChainState == "refunded", ReadOutcome, "the refund of the stakes"), Fail);
            }, Fail);
        }

        /// <summary>Whether the connected wallet can take test tokens from the tap now (testnet only).</summary>
        public static bool CanTakeTestTokens => State == WagerState.Open && IsTest && !TookTestTokens && !IsBusy
            && PlayerWallet.State == WalletState.Ready;

        /// <summary>
        /// On the testnet only: the wallet takes test tokens from the tap of the test token (a
        /// thousand, once a day), so the player can stake without being sent any. The wallet pays
        /// the gas in testnet MON.
        /// </summary>
        public static void TakeTestTokens()
        {
            if (!CanTakeTestTokens)
                return;

            var player = PlayerWallet.Address;
            SetStep(WagerStep.Switching, WagerAction.TestTokens, "");
            MonadTransactions.SwitchTo(Chain, () =>
            {
                SetStep(WagerStep.Sending, WagerAction.TestTokens, "");
                MonadTransactions.Send(player, Token, TapSelector, TapGasLimit, "the claim of test tokens (one a day)", () =>
                {
                    TookTestTokens = true;
                    Debug.Log("CHOG_WAGER took test tokens");
                    SetStep(WagerStep.Idle, WagerAction.None, "");
                }, Fail);
            }, Fail);
        }

        /// <summary>Forgets the last staked match, once the player has seen what became of it.</summary>
        public static void ForgetLast()
        {
            LastGameId = "";
            s_lastMatchKey = "";
            LastStakeWei = "";
            Outcome = WagerOutcome.Unknown;
            RefundableAt = 0;
            LastChainState = "";
            s_decisionSignature = "";
            PlayerPrefs.DeleteKey(LastMatchKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Puts a failed action away, so it can be tried again.</summary>
        public static void Dismiss()
        {
            if (Step == WagerStep.Failed)
                SetStep(WagerStep.Idle, WagerAction.None, "");
        }

        // The stake outlives the page: the match is judged after its room closes, when the
        // player is back on the menu, maybe after a reload.
        private static void Remember(string gameId, string matchKey, int team, string stakeWei)
        {
            LastGameId = gameId ?? "";
            s_lastMatchKey = matchKey ?? "";
            LastTeam = team;
            LastStakeWei = stakeWei ?? "";
            Outcome = WagerOutcome.Unknown;
            RefundableAt = 0;
            LastChainState = "";
            PlayerPrefs.SetString(LastMatchKey, Kept(LastGameId, s_lastMatchKey, team, LastStakeWei));
            PlayerPrefs.Save();
        }

        private static void Recall()
        {
            if (TryRead(PlayerPrefs.GetString(LastMatchKey, ""), out var gameId, out var matchKey, out var team, out var stakeWei))
            {
                LastGameId = gameId;
                s_lastMatchKey = matchKey;
                LastTeam = team;
                LastStakeWei = stakeWei;
            }
        }

        /// <summary>What is kept of a staked match between two visits: its name, its key, the team and the stake.</summary>
        public static string Kept(string gameId, string matchKey, int team, string stakeWei)
        {
            return matchKey + "|" + team + "|" + stakeWei + "|" + gameId;
        }

        /// <summary>Reads back what "Kept" wrote; false for anything else.</summary>
        public static bool TryRead(string kept, out string gameId, out string matchKey, out int team, out string stakeWei)
        {
            gameId = matchKey = stakeWei = "";
            team = 0;
            // The name of a match comes last: it is the only part that may hold the separator.
            var parts = (kept ?? "").Split(new[] { '|' }, 4);
            if (parts.Length != 4 || !LooksLikeAKey(parts[0]) || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out team)
                || team > 1 || !MonadTransactions.IsDigits(parts[2]) || parts[3].Length == 0)
                return false;

            matchKey = parts[0];
            stakeWei = parts[2];
            gameId = parts[3];
            return true;
        }

        // The services read the chain a moment after the wallet's own endpoint: ask until they see it.
        private static void Confirm(Func<bool> seen, Action<Action> read, string what)
        {
            SetStep(WagerStep.Confirming, Doing, "");
            PlayerWallet.Run(WaitUntilSeen(seen, read, what));
        }

        private static IEnumerator WaitUntilSeen(Func<bool> seen, Action<Action> read, string what)
        {
            for (var attempt = 0; attempt < SeenAttempts && !seen(); attempt++)
            {
                var answered = false;
                read(() => answered = true);
                yield return new WaitForSecondsRealtime(SeenSeconds);
                while (!answered)
                    yield return null;
            }

            if (seen())
            {
                Debug.Log("CHOG_WAGER done: " + what);
                SetStep(WagerStep.Idle, WagerAction.None, "");
            }
            else
            {
                Fail(0, what + " is on the chain but the game does not see it yet");
            }
        }

        private static void Fail(int code, string problem)
        {
            Debug.LogWarning("CHOG_WAGER stopped (" + code + "): " + problem);
            // A player who closes the wallet did not fail at anything.
            if (code == MonadTransactions.UserRefused)
                SetStep(WagerStep.Idle, WagerAction.None, "");
            else
                SetStep(WagerStep.Failed, Doing, problem);
        }

        private static void SetStep(WagerStep step, WagerAction action, string problem)
        {
            Step = step;
            Doing = action;
            Problem = problem;
            Changed?.Invoke();
        }

        private static T Parsed<T>(long status, string text) where T : class
        {
            return status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<T>(text) : null;
        }

        // What follows is plain arithmetic and the bytes the contract expects.

        /// <summary>The team an address staked on among those stakers, or -1 when it did not stake.</summary>
        public static int TeamStakedOn(IReadOnlyList<WagerStaker> stakers, string address)
        {
            if (string.IsNullOrEmpty(address))
                return -1;

            foreach (var staker in stakers)
                if (string.Equals(staker.Address, address, StringComparison.OrdinalIgnoreCase))
                    return staker.Team;

            return -1;
        }

        /// <summary>
        /// What each winner gains on top of its own stake, in the smallest unit: an equal share
        /// of what the losers staked, less the burned share. Nothing when a side has nobody.
        /// </summary>
        public static string WinnerGain(string stakeWei, int winners, int losers, int burnedShare)
        {
            if (!MonadTransactions.IsDigits(stakeWei) || winners <= 0 || losers <= 0)
                return "0";

            var lost = BigInteger.Parse(stakeWei, CultureInfo.InvariantCulture) * losers;
            return ((lost - lost * Mathf.Clamp(burnedShare, 0, 10000) / 10000) / winners).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Whether a text is the key of a match on the chain: 0x and 64 hex digits.</summary>
        public static bool LooksLikeAKey(string text)
        {
            if (text == null || text.Length != 66 || text[0] != '0' || text[1] != 'x')
                return false;

            for (var i = 2; i < text.Length; i++)
                if (!Uri.IsHexDigit(text[i]))
                    return false;

            return true;
        }

        /// <summary>The call that stakes with the referee's ticket.</summary>
        public static string StakeData(string matchKey, int team, string amountWei, long deadline, string signature)
        {
            // Four plain words, then where the signature starts: after the five words of the head.
            return StakeSelector + matchKey.Substring(2) + MonadTransactions.Word(team) + MonadTransactions.WordOf(amountWei)
                + MonadTransactions.Word(deadline) + MonadTransactions.Word(5 * 32) + Bytes(signature);
        }

        /// <summary>
        /// The call that pays a match on the result the referee signed; on version 2, with the wallets
        /// that played a Genesis (null for version 1): the list, then the signature, after the head.
        /// </summary>
        public static string SettleData(string matchKey, int winnerTeam, string signature, string[] genesisPlayers = null)
        {
            if (genesisPlayers == null)
                return SettleSelector + matchKey.Substring(2) + MonadTransactions.Word(winnerTeam) + MonadTransactions.Word(3 * 32) + Bytes(signature);

            var list = MonadTransactions.Word(genesisPlayers.Length);
            foreach (var player in genesisPlayers)
                list += MonadTransactions.AddressWord(player);
            return SettleWithGenesisSelector + matchKey.Substring(2) + MonadTransactions.Word(winnerTeam)
                + MonadTransactions.Word(4 * 32) + MonadTransactions.Word(4 * 32 + 32 * (1 + genesisPlayers.Length))
                + list + Bytes(signature);
        }

        /// <summary>
        /// What one Genesis player of the last match earns, in the smallest unit, when the connected
        /// wallet is one of them and the match was won by a side ("0" otherwise): an equal part of the
        /// Genesis share of what the losers staked.
        /// </summary>
        public static string EarnedAsGenesisPlayer()
        {
            if (Version < 2 || Outcome != WagerOutcome.Won || s_genesisPlayers.Length == 0)
                return "0";
            var mine = false;
            foreach (var player in s_genesisPlayers)
                mine |= string.Equals(player, PlayerWallet.Address, StringComparison.OrdinalIgnoreCase);
            return mine ? GenesisPlayerShare(LastStakeWei, LastLosers, GenesisShare, s_genesisPlayers.Length) : "0";
        }

        /// <summary>One Genesis player's part, as the contract pays it: lost stakes times the share, split equally.</summary>
        public static string GenesisPlayerShare(string stakeWei, int losers, int genesisShare, int genesisPlayers)
        {
            if (!MonadTransactions.IsDigits(stakeWei) || losers <= 0 || genesisPlayers <= 0)
                return "0";
            var lost = BigInteger.Parse(stakeWei, CultureInfo.InvariantCulture) * losers;
            return (lost * Mathf.Clamp(genesisShare, 0, 10000) / 10000 / genesisPlayers).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The call that gives every stake of a match back on the referee's signature.</summary>
        public static string VoidData(string matchKey, string signature)
        {
            return VoidSelector + matchKey.Substring(2) + MonadTransactions.Word(2 * 32) + Bytes(signature);
        }

        // Bytes as a contract reads them: their length, then the bytes filled up to whole words.
        private static string Bytes(string hex)
        {
            var digits = hex.StartsWith("0x", StringComparison.Ordinal) ? hex.Substring(2) : hex;
            var filled = digits.PadRight((digits.Length + 63) / 64 * 64, '0');
            return MonadTransactions.Word(digits.Length / 2) + filled.ToLowerInvariant();
        }
    }
}
