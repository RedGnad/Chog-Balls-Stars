using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Chog.Match
{
    public enum RentalState
    {
        /// <summary>The game services were not asked yet, or did not answer.</summary>
        Unknown,
        /// <summary>Nothing is rented: no rental contract is set up.</summary>
        Closed,
        /// <summary>Genesis can be offered and rented for $CHOG.</summary>
        Open
    }

    public enum RentalStep
    {
        Idle,
        /// <summary>The wallet is asked to be on Monad.</summary>
        Switching,
        /// <summary>The wallet is asked to let the rental take the cost, then that transaction is awaited.</summary>
        Approving,
        /// <summary>The wallet is asked for the transaction itself, which is then awaited.</summary>
        Sending,
        /// <summary>The transaction is on the chain; the game services are asked to see it.</summary>
        Confirming,
        /// <summary>It did not go through; "Problem" says why.</summary>
        Failed
    }

    public enum RentalAction
    {
        None,
        /// <summary>A holder offers a Genesis for rent, or changes its price.</summary>
        Offer,
        /// <summary>A holder takes an offer back.</summary>
        Withdraw,
        /// <summary>A player pays to play a Genesis for some days.</summary>
        Rent,
        /// <summary>A holder takes out what they earned, a renter what they are owed back.</summary>
        Claim,
        /// <summary>A renter stops their rental now and is owed the time left.</summary>
        EndRental
    }

    /// <summary>A Genesis its holder offers for rent.</summary>
    public struct RentalOffer
    {
        public int TokenId;
        /// <summary>The price of one day in the smallest unit of $CHOG, as decimal digits.</summary>
        public string PricePerDayWei;
        /// <summary>The holder who is paid.</summary>
        public string Lender;
    }

    /// <summary>
    /// The rental of Genesis for $CHOG (contracts/src/ChogRental.sol, version 2). A holder offers a
    /// Genesis at a price a day; one player at a time pays to play it for a number of days. The
    /// payment is held by the contract and the holder earns it by the second, then claims it, less a
    /// share that is burned. The Genesis never leaves its holder's wallet: a rental is a right the
    /// game reads on the chain, and it ends when the holder no longer holds the Genesis; the renter
    /// is then owed back the time not delivered, as when they stop early. The matches of a renter
    /// count for its career. The game reads the offers, what the connected wallet rents and its money
    /// in the rental from the game services, which read the contract; while no rental contract is
    /// set up there, this class stays closed and nothing is offered.
    /// </summary>
    public static class GenesisRental
    {
        private const string OfferSelector = "0x92f6d20c";    // offer(uint256,uint128)
        private const string WithdrawSelector = "0x2e1a7d4d"; // withdraw(uint256)
        private const string RentSelector = "0xcfb0c2ab";     // rent(uint256,uint256,uint128)
        private const string ClaimSelector = "0x6ba4c138";    // claim(uint256[])
        private const string EndRentalSelector = "0x97491d6f"; // endRental(uint256)

        // Monad charges the gas limit of a transaction, not what it uses: each one is sent with a
        // limit above its cost. Measured on version 2, each as a transaction of its own
        // (contracts/README.md, ChogRental): a first offer 163,865, a new price 62,467, taking an
        // offer back 50,733, a first rental 232,013, more days 126,050, a holder's claim for one
        // Genesis 107,737, a renter's early end 58,854 and refund 49,957, a close after a sale 88,808.
        // A claim settles each Genesis it names first: its limit grows with them.
        private const int OfferGasLimit = 250000;
        private const int WithdrawGasLimit = 100000;
        private const int RentGasLimit = 320000;
        private const int ClaimGasLimit = 120000;
        private const int ClaimGasPerGenesis = 100000;
        private const int EndRentalGasLimit = 100000;

        private const int SeenAttempts = 8;
        private const float SeenSeconds = 2f;

        private static readonly List<RentalOffer> Listed = new List<RentalOffer>();
        // Token id to the second, since 1970, the connected wallet's rental of it ends.
        private static readonly Dictionary<int, long> RentedUntil = new Dictionary<int, long>();
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly int[] NoTokens = new int[0];
        private static string s_rentedBy = "";

        // The connected wallet's money in the rental, as the services last read it.
        private static string s_accountOf = "";
        private static readonly List<RentalOffer> MineRented = new List<RentalOffer>();
        private static readonly List<RentalOffer> MineLent = new List<RentalOffer>();
        private static readonly Dictionary<int, long> LentUntil = new Dictionary<int, long>();
        private static readonly Dictionary<int, string> LentTo = new Dictionary<int, string>();
        private static int[] s_toSettle = new int[0];

        /// <summary>What the connected wallet earned as a holder and may claim, before the burned share, in the smallest unit of $CHOG.</summary>
        public static string EarnedWei { get; private set; } = "0";

        /// <summary>What the connected wallet is owed back as a renter, in the smallest unit of $CHOG.</summary>
        public static string OwedBackWei { get; private set; } = "0";

        public static RentalState State { get; private set; } = RentalState.Unknown;

        /// <summary>The rental contract and the $CHOG token, as the services gave them.</summary>
        public static string Address { get; private set; } = "";
        public static string Token { get; private set; } = "";

        /// <summary>The most days a renter can be paid up, and the share of a payment that is burned, in hundredths of a percent.</summary>
        public static int MaxDays { get; private set; } = 30;
        public static int BurnedShare { get; private set; }

        /// <summary>Whether the list of offers was read at least once.</summary>
        public static bool OffersRead { get; private set; }

        public static RentalStep Step { get; private set; } = RentalStep.Idle;
        public static RentalAction Doing { get; private set; } = RentalAction.None;

        /// <summary>The Genesis the action under way is about; 0 when idle.</summary>
        public static int ActionTokenId { get; private set; }

        /// <summary>Why the last action did not go through; empty otherwise.</summary>
        public static string Problem { get; private set; } = "";

        /// <summary>Raised after any of the values above changed, the offers, or what the wallet rents.</summary>
        public static event Action Changed;

        // Field names are the JSON keys of the services.
        [Serializable]
        private sealed class TermsAnswer
        {
            public bool enabled;
            public string address;
            public string token;
            public int maxDays;
            public int burnedShare;
        }

        [Serializable]
        private sealed class OfferAnswer
        {
            public int tokenId;
            public string pricePerDay;
            public string lender;
        }

        [Serializable]
        private sealed class OffersAnswer
        {
            public OfferAnswer[] offers;
        }

        [Serializable]
        private sealed class RentedEntry
        {
            public int tokenId;
            public long until;
        }

        [Serializable]
        private sealed class RentedAnswer
        {
            public string address;
            public RentedEntry[] rentals;
        }

        [Serializable]
        private sealed class AccountEntry
        {
            public int tokenId;
            public string renter;
            public string lender;
            public long until;
            public bool counting;
            public string pricePerDay;
        }

        [Serializable]
        private sealed class AccountAnswer
        {
            public bool enabled;
            public string address;
            public string earned;
            public string owedBack;
            public int[] toSettle;
            public AccountEntry[] lent;
            public AccountEntry[] rented;
        }

        /// <summary>Whether an action is under way.</summary>
        public static bool IsBusy => Step != RentalStep.Idle && Step != RentalStep.Failed;

        /// <summary>
        /// Every Genesis that can be rented now, by token id, and those the connected wallet rents
        /// (nobody else may rent them meanwhile, so the contract no longer lists them; their renter
        /// adds days there).
        /// </summary>
        public static IReadOnlyList<RentalOffer> Offers
        {
            get
            {
                if (!AccountRead() || MineRented.Count == 0)
                    return Listed;
                var all = new List<RentalOffer>(Listed);
                foreach (var rented in MineRented)
                    if (all.FindIndex(listed => listed.TokenId == rented.TokenId) < 0)
                        all.Add(rented);
                all.Sort((a, b) => a.TokenId.CompareTo(b.TokenId));
                return all;
            }
        }

        /// <summary>The offer for a Genesis, when its holder offers it: listed, rented by the connected wallet, or its own rented out.</summary>
        public static bool TryGetOffer(int tokenId, out RentalOffer offer)
        {
            foreach (var listed in Listed)
            {
                if (listed.TokenId == tokenId)
                {
                    offer = listed;
                    return true;
                }
            }

            if (AccountRead())
            {
                foreach (var mine in MineRented)
                {
                    if (mine.TokenId == tokenId && mine.PricePerDayWei != "0")
                    {
                        offer = mine;
                        return true;
                    }
                }

                foreach (var mine in MineLent)
                {
                    if (mine.TokenId == tokenId && mine.PricePerDayWei != "0")
                    {
                        offer = mine;
                        return true;
                    }
                }
            }

            offer = default;
            return false;
        }

        /// <summary>The second, since 1970, the rental of the connected holder's Genesis ends; 0 when nobody rents it.</summary>
        public static long LentUntilSeconds(int tokenId)
        {
            return AccountRead() && LentUntil.TryGetValue(tokenId, out var until) && until > NowSeconds() ? until : 0;
        }

        /// <summary>Who rents the connected holder's Genesis, or empty.</summary>
        public static string LentToAddress(int tokenId)
        {
            return LentUntilSeconds(tokenId) > 0 && LentTo.TryGetValue(tokenId, out var renter) ? renter : "";
        }

        /// <summary>Whether the connected wallet has $CHOG to take out: earned as a holder or owed back as a renter.</summary>
        public static bool HasMoneyToClaim => AccountRead() && (EarnedWei != "0" || OwedBackWei != "0");

        private static bool AccountRead() => s_accountOf.Length == 42 && s_accountOf == PlayerWallet.Address;

        /// <summary>Whether the connected wallet rents this Genesis at this moment, as far as the game has read.</summary>
        public static bool IsRenting(int tokenId)
        {
            return s_rentedBy == PlayerWallet.Address && RentedUntil.TryGetValue(tokenId, out var until) && until > NowSeconds();
        }

        /// <summary>The second, since 1970, the connected wallet's rental of a Genesis ends; 0 when it rents nothing.</summary>
        public static long RentedUntilSeconds(int tokenId)
        {
            return IsRenting(tokenId) ? RentedUntil[tokenId] : 0;
        }

        /// <summary>The Genesis the connected wallet rents now, by token id.</summary>
        public static int[] RentedTokens()
        {
            if (s_rentedBy != PlayerWallet.Address || RentedUntil.Count == 0)
                return NoTokens;

            var now = NowSeconds();
            var tokens = new List<int>();
            foreach (var rented in RentedUntil)
                if (rented.Value > now)
                    tokens.Add(rented.Key);
            tokens.Sort();
            return tokens.ToArray();
        }

        /// <summary>Asks the game services whether Genesis are rented, then what is offered. Called once when the page is up.</summary>
        public static void Begin()
        {
            PlayerWallet.Run(GameServices.Call("rental", null, null, (status, text) =>
            {
                var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<TermsAnswer>(text) : null;
                if (answer == null)
                {
                    // Services without this answer, or that could not read the chain: nothing is offered.
                    Debug.Log("CHOG_RENTAL not read: " + status);
                    return;
                }

                State = answer.enabled && MonadTransactions.LooksLikeAnAddress(answer.address) && MonadTransactions.LooksLikeAnAddress(answer.token)
                    ? RentalState.Open : RentalState.Closed;
                Address = answer.address ?? "";
                Token = answer.token ?? "";
                MaxDays = answer.maxDays > 0 ? answer.maxDays : 30;
                BurnedShare = answer.burnedShare;
                Debug.Log("CHOG_RENTAL " + (State == RentalState.Open ? "open" : "closed"));
                Changed?.Invoke();
                if (State == RentalState.Open)
                {
                    ReadOffers();
                    ReadRented();
                    ReadAccount();
                }
            }));
        }

        /// <summary>Reads again which Genesis are offered; "Changed" follows.</summary>
        public static void ReadOffers(Action then = null)
        {
            if (State != RentalState.Open)
                return;

            PlayerWallet.Run(GameServices.Call("rental/offers", null, null, (status, text) =>
            {
                var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<OffersAnswer>(text) : null;
                if (answer != null && answer.offers != null)
                {
                    Listed.Clear();
                    foreach (var offer in answer.offers)
                        if (offer.tokenId > 0 && MonadTransactions.IsDigits(offer.pricePerDay))
                            Listed.Add(new RentalOffer { TokenId = offer.tokenId, PricePerDayWei = offer.pricePerDay, Lender = (offer.lender ?? "").ToLowerInvariant() });
                    OffersRead = true;
                    Debug.Log("CHOG_RENTAL offers " + Listed.Count);
                    Changed?.Invoke();
                }

                then?.Invoke();
            }));
        }

        /// <summary>Reads again what the connected wallet rents; "Changed" follows.</summary>
        public static void ReadRented(Action then = null)
        {
            var renter = PlayerWallet.Address;
            if (State != RentalState.Open || renter.Length != 42)
                return;

            PlayerWallet.Run(GameServices.Call("rental/rented?address=" + renter, null, null, (status, text) =>
            {
                var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<RentedAnswer>(text) : null;
                if (answer != null && answer.rentals != null && renter == PlayerWallet.Address)
                {
                    RentedUntil.Clear();
                    foreach (var rented in answer.rentals)
                        RentedUntil[rented.tokenId] = rented.until;
                    s_rentedBy = renter;
                    Debug.Log("CHOG_RENTAL rented " + RentedUntil.Count + " by " + PlayerWallet.ShortAddress(renter));
                    Changed?.Invoke();
                }

                then?.Invoke();
            }));
        }

        /// <summary>Reads again the connected wallet's money and rentals both ways; "Changed" follows.</summary>
        public static void ReadAccount(Action then = null)
        {
            var who = PlayerWallet.Address;
            if (State != RentalState.Open || who.Length != 42)
                return;

            PlayerWallet.Run(GameServices.Call("rental/account?address=" + who, null, null, (status, text) =>
            {
                var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<AccountAnswer>(text) : null;
                if (answer != null && answer.enabled && who == PlayerWallet.Address)
                {
                    EarnedWei = MonadTransactions.IsDigits(answer.earned) ? answer.earned : "0";
                    OwedBackWei = MonadTransactions.IsDigits(answer.owedBack) ? answer.owedBack : "0";
                    s_toSettle = answer.toSettle ?? NoTokens;
                    MineRented.Clear();
                    MineLent.Clear();
                    LentUntil.Clear();
                    LentTo.Clear();
                    foreach (var rented in answer.rented ?? new AccountEntry[0])
                        if (rented.counting && rented.tokenId > 0)
                            MineRented.Add(new RentalOffer { TokenId = rented.tokenId, PricePerDayWei = MonadTransactions.IsDigits(rented.pricePerDay) ? rented.pricePerDay : "0", Lender = (rented.lender ?? "").ToLowerInvariant() });
                    foreach (var lent in answer.lent ?? new AccountEntry[0])
                    {
                        if (!lent.counting || lent.tokenId <= 0)
                            continue;
                        MineLent.Add(new RentalOffer { TokenId = lent.tokenId, PricePerDayWei = MonadTransactions.IsDigits(lent.pricePerDay) ? lent.pricePerDay : "0", Lender = who });
                        LentUntil[lent.tokenId] = lent.until;
                        LentTo[lent.tokenId] = (lent.renter ?? "").ToLowerInvariant();
                    }
                    s_accountOf = who;
                    Debug.Log("CHOG_RENTAL account earned " + EarnedWei + " owed back " + OwedBackWei + " lent " + MineLent.Count + " rented " + MineRented.Count);
                    Changed?.Invoke();
                }

                then?.Invoke();
            }));
        }

        /// <summary>
        /// The connected wallet takes out its money: what it earned as a holder (less the burned
        /// share) and what it is owed back as a renter. The rentals it is part of are settled first,
        /// and those whose Genesis was sold or whose time ran out are closed.
        /// </summary>
        public static void Claim()
        {
            if (State != RentalState.Open || IsBusy || PlayerWallet.State != WalletState.Ready || !HasMoneyToClaim)
                return;

            var who = PlayerWallet.Address;
            var tokens = s_toSettle;
            SetStep(RentalStep.Switching, RentalAction.Claim, 0, "");
            MonadTransactions.SwitchToMonad(() =>
            {
                SetStep(RentalStep.Sending, RentalAction.Claim, 0, "");
                MonadTransactions.Send(who, Address, ClaimData(tokens), ClaimGasLimit + ClaimGasPerGenesis * tokens.Length, "taking the $CHOG out",
                    () => Confirm(0, () => !HasMoneyToClaim, ReadAccount, "taking the $CHOG out"), Fail);
            }, Fail);
        }

        /// <summary>The connected renter stops renting a Genesis now; the time left is owed back to them.</summary>
        public static void EndRental(int tokenId)
        {
            if (!CanStart(tokenId) || !IsRenting(tokenId))
                return;

            var renter = PlayerWallet.Address;
            SetStep(RentalStep.Switching, RentalAction.EndRental, tokenId, "");
            MonadTransactions.SwitchToMonad(() =>
            {
                SetStep(RentalStep.Sending, RentalAction.EndRental, tokenId, "");
                MonadTransactions.Send(renter, Address, EndRentalData(tokenId), EndRentalGasLimit, "the end of the rental",
                    () => Confirm(tokenId, () => !IsRenting(tokenId), then => ReadRented(() => ReadAccount(then)), "the end of the rental"), Fail);
            }, Fail);
        }

        /// <summary>The connected holder offers a Genesis for rent at a whole number of $CHOG a day, or changes its price.</summary>
        public static void Offer(int tokenId, int chogPerDay)
        {
            if (!CanStart(tokenId) || chogPerDay <= 0)
                return;

            var holder = PlayerWallet.Address;
            var price = MonadTransactions.WholeToWei(chogPerDay);
            SetStep(RentalStep.Switching, RentalAction.Offer, tokenId, "");
            MonadTransactions.SwitchToMonad(() =>
            {
                SetStep(RentalStep.Sending, RentalAction.Offer, tokenId, "");
                MonadTransactions.Send(holder, Address, OfferData(tokenId, price), OfferGasLimit, "the offer",
                    () => Confirm(tokenId, () => TryGetOffer(tokenId, out var seen) && seen.PricePerDayWei == price && seen.Lender == holder, ReadOffers,
                        "the offer"), Fail);
            }, Fail);
        }

        /// <summary>The connected holder takes the offer for a Genesis back. A rental that runs goes on to its end.</summary>
        public static void Withdraw(int tokenId)
        {
            if (!CanStart(tokenId))
                return;

            var holder = PlayerWallet.Address;
            SetStep(RentalStep.Switching, RentalAction.Withdraw, tokenId, "");
            MonadTransactions.SwitchToMonad(() =>
            {
                SetStep(RentalStep.Sending, RentalAction.Withdraw, tokenId, "");
                // The holder's own rented-out Genesis is known from the account too: both are read again.
                MonadTransactions.Send(holder, Address, WithdrawData(tokenId), WithdrawGasLimit, "taking the offer back",
                    () => Confirm(tokenId, () => !TryGetOffer(tokenId, out _), then => ReadOffers(() => ReadAccount(then)), "taking the offer back"), Fail);
            }, Fail);
        }

        /// <summary>
        /// The connected wallet rents an offered Genesis for a number of days. The wallet is asked
        /// up to three things: to be on Monad, to let the rental take the cost (skipped when it
        /// already may), and to rent. The price the player saw is sent along: the rental is
        /// refused if the holder raised it meanwhile.
        /// </summary>
        public static void Rent(int tokenId, int days)
        {
            if (!CanStart(tokenId) || days <= 0 || days > MaxDays || !TryGetOffer(tokenId, out var offer))
                return;

            var renter = PlayerWallet.Address;
            var cost = MonadTransactions.Times(offer.PricePerDayWei, days);
            var paidUntil = RentedUntilSeconds(tokenId);
            SetStep(RentalStep.Switching, RentalAction.Rent, tokenId, "");
            MonadTransactions.SwitchToMonad(() =>
            {
                MonadTransactions.AllowPayment(renter, Token, Address, cost, days == 1 ? "one day costs" : days + " days cost",
                    () => SetStep(RentalStep.Approving, RentalAction.Rent, tokenId, ""),
                    () =>
                    {
                        SetStep(RentalStep.Sending, RentalAction.Rent, tokenId, "");
                        MonadTransactions.Send(renter, Address, RentData(tokenId, days, offer.PricePerDayWei), RentGasLimit, "the rental",
                            () => Confirm(tokenId, () => RentedUntilSeconds(tokenId) > paidUntil, then => ReadRented(() => ReadAccount(then)), "the rental"), Fail);
                    }, Fail);
            }, Fail);
        }

        /// <summary>Puts a failed action away, so it can be tried again.</summary>
        public static void Dismiss()
        {
            if (Step == RentalStep.Failed)
                SetStep(RentalStep.Idle, RentalAction.None, 0, "");
        }

        private static bool CanStart(int tokenId)
        {
            return State == RentalState.Open && !IsBusy && tokenId > 0 && PlayerWallet.State == WalletState.Ready;
        }

        // The services read the chain a moment after the wallet's own endpoint: ask until they see it.
        private static void Confirm(int tokenId, Func<bool> seen, Action<Action> read, string what)
        {
            SetStep(RentalStep.Confirming, Doing, tokenId, "");
            PlayerWallet.Run(WaitUntilSeen(tokenId, seen, read, what));
        }

        private static IEnumerator WaitUntilSeen(int tokenId, Func<bool> seen, Action<Action> read, string what)
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
                Debug.Log("CHOG_RENTAL done: " + what + " for token " + tokenId);
                SetStep(RentalStep.Idle, RentalAction.None, 0, "");
            }
            else
            {
                Fail(0, what + " is on the chain but the game does not see it yet; open the menu again in a moment");
            }
        }

        private static void Fail(int code, string problem)
        {
            Debug.LogWarning("CHOG_RENTAL stopped (" + code + "): " + problem);
            // A player who closes the wallet did not fail at anything.
            if (code == MonadTransactions.UserRefused)
                SetStep(RentalStep.Idle, RentalAction.None, 0, "");
            else
                SetStep(RentalStep.Failed, Doing, ActionTokenId, problem);
        }

        private static void SetStep(RentalStep step, RentalAction action, int tokenId, string problem)
        {
            Step = step;
            Doing = action;
            ActionTokenId = tokenId;
            Problem = problem;
            Changed?.Invoke();
        }

        private static long NowSeconds() => (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

        // What follows turns an action into the bytes the contract expects.

        /// <summary>The call that offers a Genesis at a price a day, given in the smallest unit of $CHOG.</summary>
        public static string OfferData(int tokenId, string pricePerDayWei)
        {
            return OfferSelector + MonadTransactions.Word(tokenId) + MonadTransactions.WordOf(pricePerDayWei);
        }

        /// <summary>The call that takes the offer for a Genesis back.</summary>
        public static string WithdrawData(int tokenId)
        {
            return WithdrawSelector + MonadTransactions.Word(tokenId);
        }

        /// <summary>The call that takes out the caller's money after settling the rentals of these Genesis.</summary>
        public static string ClaimData(int[] tokenIds)
        {
            var data = new System.Text.StringBuilder(ClaimSelector);
            // abi.encode(uint256[]): the offset of the list, its length, then each token id.
            data.Append(MonadTransactions.Word(32)).Append(MonadTransactions.Word(tokenIds.Length));
            foreach (var tokenId in tokenIds)
                data.Append(MonadTransactions.Word(tokenId));
            return data.ToString();
        }

        /// <summary>The call with which a renter stops renting a Genesis now.</summary>
        public static string EndRentalData(int tokenId)
        {
            return EndRentalSelector + MonadTransactions.Word(tokenId);
        }

        /// <summary>The call that rents a Genesis for some days at no more than the price the player saw.</summary>
        public static string RentData(int tokenId, int days, string maxPricePerDayWei)
        {
            return RentSelector + MonadTransactions.Word(tokenId) + MonadTransactions.Word(days) + MonadTransactions.WordOf(maxPricePerDayWei);
        }

        /// <summary>What a holder receives of what they earned, in the smallest unit: that amount less the burned share.</summary>
        public static string HolderShare(string paidWei, int burnedShare)
        {
            if (!MonadTransactions.IsDigits(paidWei))
                return "0";

            var paid = System.Numerics.BigInteger.Parse(paidWei, CultureInfo.InvariantCulture);
            return (paid - paid * Mathf.Clamp(burnedShare, 0, 10000) / 10000).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>"2 d 5 h", "5 h", "12 min": the time left until a second since 1970.</summary>
        public static string TimeLeft(long untilSeconds, long nowSeconds)
        {
            var left = untilSeconds - nowSeconds;
            if (left <= 0)
                return "over";
            if (left >= 86400)
                return left / 86400 + " d " + left % 86400 / 3600 + " h";
            if (left >= 3600)
                return left / 3600 + " h";
            return Math.Max(1, left / 60) + " min";
        }
    }
}
