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
        Rent
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
    /// The rental of Genesis for $CHOG. A holder offers a Genesis at a price a day; any player
    /// with a wallet pays to play it for a number of days, and the payment goes to the holder,
    /// less a share that is burned. The Genesis never leaves its holder's wallet: a rental is a
    /// right the game reads on the chain. Several players can rent the same Genesis, and the
    /// matches of a renter count for its career. The game reads the offers and what the connected
    /// wallet rents from the game services, which read the contract; while no rental contract is
    /// set up there, this class stays closed and nothing is offered.
    /// </summary>
    public static class GenesisRental
    {
        private const string OfferSelector = "0x92f6d20c";    // offer(uint256,uint128)
        private const string WithdrawSelector = "0x2e1a7d4d"; // withdraw(uint256)
        private const string RentSelector = "0xcfb0c2ab";     // rent(uint256,uint256,uint128)

        // Monad charges the gas limit of a transaction, not what it uses: each one is sent with a
        // limit above its cost. On a copy of mainnet, on 10 October 2026
        // (tools/contracts/measure_rental_gas.sh): a first offer 155,365 and a new price 62,379;
        // taking an offer back 68,365; a first rental 262,740 and more days 172,689. The margin
        // covers what was not measured: the very first offer of the contract, an offer taken
        // back from the middle of the list, a holder who never held $CHOG.
        private const int OfferGasLimit = 250000;
        private const int WithdrawGasLimit = 120000;
        private const int RentGasLimit = 400000;

        private const int SeenAttempts = 8;
        private const float SeenSeconds = 2f;

        private static readonly List<RentalOffer> Listed = new List<RentalOffer>();
        // Token id to the second, since 1970, the connected wallet's rental of it ends.
        private static readonly Dictionary<int, long> RentedUntil = new Dictionary<int, long>();
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly int[] NoTokens = new int[0];
        private static string s_rentedBy = "";

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

        /// <summary>Whether an action is under way.</summary>
        public static bool IsBusy => Step != RentalStep.Idle && Step != RentalStep.Failed;

        /// <summary>Every Genesis offered for rent now, by token id.</summary>
        public static IReadOnlyList<RentalOffer> Offers => Listed;

        /// <summary>The offer for a Genesis, when its holder offers it.</summary>
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

            offer = default;
            return false;
        }

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

        /// <summary>The connected holder takes the offer for a Genesis back. Rentals already paid run to their end.</summary>
        public static void Withdraw(int tokenId)
        {
            if (!CanStart(tokenId))
                return;

            var holder = PlayerWallet.Address;
            SetStep(RentalStep.Switching, RentalAction.Withdraw, tokenId, "");
            MonadTransactions.SwitchToMonad(() =>
            {
                SetStep(RentalStep.Sending, RentalAction.Withdraw, tokenId, "");
                MonadTransactions.Send(holder, Address, WithdrawData(tokenId), WithdrawGasLimit, "taking the offer back",
                    () => Confirm(tokenId, () => !TryGetOffer(tokenId, out _), ReadOffers, "taking the offer back"), Fail);
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
                            () => Confirm(tokenId, () => RentedUntilSeconds(tokenId) > paidUntil, ReadRented, "the rental"), Fail);
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

        /// <summary>The call that rents a Genesis for some days at no more than the price the player saw.</summary>
        public static string RentData(int tokenId, int days, string maxPricePerDayWei)
        {
            return RentSelector + MonadTransactions.Word(tokenId) + MonadTransactions.Word(days) + MonadTransactions.WordOf(maxPricePerDayWei);
        }

        /// <summary>What a holder receives of a payment, in the smallest unit: the payment less the burned share.</summary>
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
