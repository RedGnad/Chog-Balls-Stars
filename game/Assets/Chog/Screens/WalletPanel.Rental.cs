using System;
using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace Chog.Screens
{
    /// <summary>
    /// The two screens of the rental, in the window of the wallet panel like the screen that lends: one
    /// where a holder offers the chosen Genesis at a price a day in $CHOG, changes that price or
    /// takes the offer back, sees who rents it and claims what it earned; and one where any
    /// connected wallet picks a Genesis that is offered, a number of days, and pays, adds days to
    /// its rental or stops it, and takes back what it is owed. They show the state kept by
    /// GenesisRental; every payment is a transaction the player approves in the wallet.
    /// </summary>
    public sealed partial class WalletPanel
    {
        private static readonly Color TagColor = new Color(0.04f, 0.05f, 0.1f, 0.85f);
        private const string ProblemColor = "#FF9C8A";

        // The prices a day a holder picks from, in whole $CHOG, and the lengths a renter picks from, in days.
        private static readonly int[] PriceSteps = { 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 };
        private static readonly int[] DayChoices = { 1, 3, 7 };
        private const int FirstPriceStep = 3;

        // The screen where a holder offers a Genesis; _offerFor is that Genesis, 0 while closed.
        private GameObject _offerView;
        private TextMeshProUGUI _offerTitle;
        private TextMeshProUGUI _offerText;
        private TextMeshProUGUI _offerPrice;
        private TextMeshProUGUI _offerShare;
        private TextMeshProUGUI _offerStatus;
        private Button _offerLess;
        private Button _offerMore;
        private Button _offerSend;
        private TextMeshProUGUI _offerSendLabel;
        private Button _offerWithdraw;
        private Button _offerClaim;
        private TextMeshProUGUI _offerClaimLabel;
        private int _offerFor;
        private int _priceStep = FirstPriceStep;

        // The screen where a wallet rents a Genesis.
        private GameObject _marketView;
        private TextMeshProUGUI _marketCount;
        private TextMeshProUGUI _marketChosen;
        private TextMeshProUGUI _marketPageLabel;
        private Button _marketPrevious;
        private Button _marketNext;
        private Button _rent;
        private TextMeshProUGUI _rentLabel;
        // Beside the rent button: TAKE BACK what the wallet is owed, else END NOW for the Genesis it rents.
        private Button _marketAside;
        private TextMeshProUGUI _marketAsideLabel;
        private readonly Slot[] _marketSlots = new Slot[SlotsPerPage];
        private readonly Button[] _dayButtons = new Button[3];
        private bool _marketOpen;
        private int _marketPage;
        private int _marketPick;
        private int _dayChoice;
        // The Genesis whose rental was just asked for, and until when it was rented before that.
        private int _renting;
        private long _rentedUntilBefore;

        private void BuildRentalViews()
        {
            BuildOfferView();
            BuildMarketView();
        }

        private void BuildOfferView()
        {
            var cover = Child("Offer", _modalCard, Vector2.zero, Vector2.one);
            ChogTheme.AsCard(cover.gameObject.AddComponent<Image>(), raised: true); // Opaque: nothing of the panel under it shows through.
            _offerView = cover.gameObject;

            _offerTitle = Text("Title", new Vector2(0.04f, 0.86f), new Vector2(0.62f, 0.98f), TextAlignmentOptions.Left, Color.white, 60f, cover);
            ChogTheme.AsTitle(_offerTitle);
            _offerClaim = MakeButton("Claim", new Vector2(0.64f, 0.865f), new Vector2(0.96f, 0.975f), out _offerClaimLabel, cover);
            _offerClaim.GetComponent<Image>().color = ChogTheme.Gold;
            _offerClaim.onClick.AddListener(() =>
            {
                GenesisRental.Dismiss();
                GenesisRental.Claim();
            });
            _offerText = Text("Text", new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.84f), TextAlignmentOptions.TopLeft, Color.white, 40f, cover);

            _offerLess = MakeButton("Less", new Vector2(0.04f, 0.30f), new Vector2(0.15f, 0.47f), out var lessLabel, cover);
            _offerLess.GetComponent<Image>().color = QuietColor;
            lessLabel.text = "-";
            _offerLess.onClick.AddListener(() => StepPrice(-1));
            _offerPrice = Text("Price", new Vector2(0.16f, 0.30f), new Vector2(0.50f, 0.47f), TextAlignmentOptions.Center, Color.white, 54f, cover);
            _offerMore = MakeButton("More", new Vector2(0.51f, 0.30f), new Vector2(0.62f, 0.47f), out var moreLabel, cover);
            _offerMore.GetComponent<Image>().color = QuietColor;
            moreLabel.text = "+";
            _offerMore.onClick.AddListener(() => StepPrice(1));
            _offerShare = Text("Share", new Vector2(0.64f, 0.30f), new Vector2(0.96f, 0.47f), TextAlignmentOptions.Left, MutedColor, 34f, cover);
            _offerStatus = Text("Status", new Vector2(0.04f, 0.225f), new Vector2(0.96f, 0.295f), TextAlignmentOptions.Left, AccentColor, 34f, cover);

            _offerSend = MakeButton("Send", new Vector2(0.04f, 0.05f), new Vector2(0.44f, 0.22f), out _offerSendLabel, cover);
            _offerSend.onClick.AddListener(() =>
            {
                GenesisRental.Dismiss();
                GenesisRental.Offer(_offerFor, PriceSteps[_priceStep]);
            });
            _offerWithdraw = MakeButton("Withdraw", new Vector2(0.47f, 0.05f), new Vector2(0.77f, 0.22f), out var withdrawLabel, cover);
            _offerWithdraw.GetComponent<Image>().color = QuietColor;
            withdrawLabel.text = "STOP OFFERING";
            _offerWithdraw.onClick.AddListener(() =>
            {
                GenesisRental.Dismiss();
                GenesisRental.Withdraw(_offerFor);
            });
            var back = MakeButton("Back", new Vector2(0.80f, 0.05f), new Vector2(0.96f, 0.22f), out var backLabel, cover);
            back.GetComponent<Image>().color = QuietColor;
            backLabel.text = "BACK";
            back.onClick.AddListener(CloseRentalViews);
        }

        private void BuildMarketView()
        {
            var cover = Child("Market", _modalCard, Vector2.zero, Vector2.one);
            ChogTheme.AsCard(cover.gameObject.AddComponent<Image>(), raised: true); // Opaque: nothing of the panel under it shows through.
            _marketView = cover.gameObject;

            var title = Text("Title", new Vector2(0.04f, 0.86f), new Vector2(0.62f, 0.98f), TextAlignmentOptions.Left, Color.white, 60f, cover);
            ChogTheme.AsTitle(title);
            title.text = "RENT A HERO";
            _marketCount = Text("Count", new Vector2(0.62f, 0.86f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.Right, MutedColor, 40f, cover);

            _marketPrevious = MakeButton("Previous", new Vector2(0.01f, 0.54f), new Vector2(0.07f, 0.78f), out var previousLabel, cover);
            previousLabel.text = "<";
            _marketPrevious.onClick.AddListener(() => TurnMarketPage(-1));
            _marketNext = MakeButton("Next", new Vector2(0.93f, 0.54f), new Vector2(0.99f, 0.78f), out var nextLabel, cover);
            nextLabel.text = ">";
            _marketNext.onClick.AddListener(() => TurnMarketPage(1));
            for (var i = 0; i < SlotsPerPage; i++)
                _marketSlots[i] = MakeSlot(i, cover, PickOffer);
            _marketPageLabel = Text("Page", new Vector2(0.10f, 0.41f), new Vector2(0.90f, 0.48f), TextAlignmentOptions.Center, MutedColor, 30f, cover);

            _marketChosen = Text("Chosen", new Vector2(0.04f, 0.24f), new Vector2(0.60f, 0.41f), TextAlignmentOptions.Left, Color.white, 44f, cover);
            for (var i = 0; i < _dayButtons.Length; i++)
            {
                var choice = i;
                var left = 0.62f + i * 0.115f;
                _dayButtons[i] = MakeButton("Days" + DayChoices[i], new Vector2(left, 0.26f), new Vector2(left + 0.105f, 0.40f), out var dayLabel, cover);
                dayLabel.text = DayChoices[i] + (DayChoices[i] == 1 ? " DAY" : " DAYS");
                _dayButtons[i].onClick.AddListener(() =>
                {
                    _dayChoice = choice;
                    Refresh();
                });
            }

            _rent = MakeButton("Rent", new Vector2(0.04f, 0.05f), new Vector2(0.60f, 0.22f), out _rentLabel, cover);
            _rent.onClick.AddListener(OnRentButton);
            _marketAside = MakeButton("Aside", new Vector2(0.62f, 0.05f), new Vector2(0.78f, 0.22f), out _marketAsideLabel, cover);
            _marketAside.GetComponent<Image>().color = QuietColor;
            _marketAside.onClick.AddListener(OnAsideButton);
            var back = MakeButton("Back", new Vector2(0.80f, 0.05f), new Vector2(0.96f, 0.22f), out var backLabel, cover);
            back.GetComponent<Image>().color = QuietColor;
            backLabel.text = "BACK";
            back.onClick.AddListener(CloseRentalViews);
        }

        // Opens the screen where the holder offers this Genesis, at its present price when it is offered.
        private void OpenOffer(int tokenId)
        {
            if (tokenId <= 0 || GenesisRental.State != RentalState.Open)
                return;

            _marketOpen = false;
            _offerFor = tokenId;
            _priceStep = FirstPriceStep;
            if (GenesisRental.TryGetOffer(tokenId, out var offer))
                _priceStep = ClosestPriceStep(offer.PricePerDayWei);
            GenesisRental.Dismiss();
            GenesisRental.ReadOffers();
            GenesisRental.ReadAccount();
            Refresh();
        }

        private void OpenMarket()
        {
            if (GenesisRental.State != RentalState.Open)
                return;

            _offerFor = 0;
            _marketOpen = true;
            _marketPage = 0;
            _marketPick = 0;
            GenesisRental.Dismiss();
            GenesisRental.ReadOffers();
            GenesisRental.ReadAccount();
            Refresh();
        }

        private void CloseRentalViews()
        {
            _offerFor = 0;
            _marketOpen = false;
            _renting = 0;
            GenesisRental.Dismiss();
            Refresh();
        }

        private void StepPrice(int direction)
        {
            _priceStep = Mathf.Clamp(_priceStep + direction, 0, PriceSteps.Length - 1);
            Refresh();
        }

        private void TurnMarketPage(int direction)
        {
            _marketPage += direction;
            Refresh();
        }

        private void PickOffer(int tokenId)
        {
            if (GenesisRental.IsBusy)
                return;

            _marketPick = tokenId;
            GenesisRental.Dismiss();
            Refresh();
        }

        private void OnRentButton()
        {
            if (_marketPick <= 0)
                return;

            _renting = _marketPick;
            _rentedUntilBefore = GenesisRental.RentedUntilSeconds(_marketPick);
            GenesisRental.Dismiss();
            GenesisRental.Rent(_marketPick, DayChoices[_dayChoice]);
        }

        // Takes back what the wallet is owed when there is something, otherwise stops the rental of the picked Genesis.
        private void OnAsideButton()
        {
            GenesisRental.Dismiss();
            if (GenesisRental.HasMoneyToClaim)
                GenesisRental.Claim();
            else if (_marketPick > 0)
                GenesisRental.EndRental(_marketPick);
        }

        /// <summary>The position, among the prices a holder picks from, of the one closest to a price in the smallest unit of $CHOG.</summary>
        public static int ClosestPriceStep(string priceWei)
        {
            var closest = FirstPriceStep;
            if (!double.TryParse(MonadTransactions.PriceText(priceWei), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var price))
                return closest;

            for (var i = 0; i < PriceSteps.Length; i++)
                if (Math.Abs(PriceSteps[i] - price) < Math.Abs(PriceSteps[closest] - price))
                    closest = i;
            return closest;
        }

        private void RefreshRentalViews()
        {
            // Without a connected wallet, or without a rental, there is nothing to offer or rent.
            var possible = PlayerWallet.State == WalletState.Ready && GenesisRental.State == RentalState.Open;
            if (!possible || (_offerFor > 0 && Array.IndexOf(PlayerWallet.Tokens, _offerFor) < 0))
            {
                _offerFor = 0;
                _marketOpen = _marketOpen && possible;
            }

            // A rental that went through: the Genesis is chosen and the screen closes.
            if (_renting > 0 && !GenesisRental.IsBusy)
            {
                if (GenesisRental.RentedUntilSeconds(_renting) > _rentedUntilBefore)
                {
                    // Put away first: choosing refreshes this panel again.
                    var rented = _renting;
                    _marketOpen = false;
                    _renting = 0;
                    PlayerWallet.Choose(rented);
                }
                else if (GenesisRental.Step == RentalStep.Idle)
                {
                    // The player closed the wallet: nothing was rented, the screen stays.
                    _renting = 0;
                }
            }

            RefreshOfferView();
            RefreshMarketView();
        }

        private void RefreshOfferView()
        {
            _offerView.SetActive(_offerFor > 0);
            if (_offerFor <= 0)
                return;

            var hero = NameOf(_offerFor);
            var offered = GenesisRental.TryGetOffer(_offerFor, out var offer);
            var claiming = GenesisRental.Doing == RentalAction.Claim;
            var mine = (GenesisRental.ActionTokenId == _offerFor && GenesisRental.Doing != RentalAction.Rent) || claiming;
            var busy = mine && GenesisRental.IsBusy;
            var failed = mine && GenesisRental.Step == RentalStep.Failed;
            var price = PriceSteps[_priceStep];
            var samePrice = offered && offer.PricePerDayWei == MonadTransactions.WholeToWei(price);
            var lentUntil = GenesisRental.LentUntilSeconds(_offerFor);

            _offerTitle.text = "RENT OUT " + hero.ToUpperInvariant();
            _offerText.text = "One player at a time can pay to play " + hero + " for 1 to " + GenesisRental.MaxDays + " days. It never leaves " +
                              "your wallet, you keep playing it, and their matches count for its career. You earn the payment by the " +
                              "second and claim it when you like. If you sell " + hero + ", the rental ends and the renter gets the days " +
                              "not played back.\n" +
                              (lentUntil > 0
                                  ? "<color=" + LabelColor + ">Rented by " + PlayerWallet.ShortAddress(GenesisRental.LentToAddress(_offerFor)) + " for " +
                                    GenesisRental.TimeLeft(lentUntil, NowMs() / 1000) + " more.</color> "
                                  : offered ? "It is offered at " + ChogPrice.WithUsd(offer.PricePerDayWei) + " a day. " : "");
            var priceUsd = ChogPrice.UsdText(MonadTransactions.WholeToWei(price));
            _offerPrice.text = price + " <size=60%>$CHOG A DAY" + (priceUsd.Length > 0 ? " (" + priceUsd + ")" : "") + "</size>";
            _offerShare.text = "you keep " + ChogPrice.WithUsd(GenesisRental.HolderShare(MonadTransactions.WholeToWei(price), GenesisRental.BurnedShare)) +
                               (GenesisRental.BurnedShare > 0 ? "\n" + BurnedText() + " is burned" : "");
            var earned = GenesisRental.EarnedWei;
            _offerClaim.gameObject.SetActive(earned != "0" || (claiming && GenesisRental.IsBusy));
            _offerClaim.interactable = !GenesisRental.IsBusy;
            _offerClaimLabel.text = claiming && GenesisRental.IsBusy
                ? "<size=60%>" + RentalStepText(GenesisRental.Step).ToUpperInvariant() + "</size>"
                : "CLAIM " + ChogPrice.WithUsd(GenesisRental.HolderShare(earned, GenesisRental.BurnedShare));
            _offerStatus.text = failed ? "<color=" + ProblemColor + ">" + GenesisRental.Problem + "</color>"
                : busy ? "Answer in your wallet: " + RentalStepText(GenesisRental.Step) : "";

            _offerLess.interactable = !busy && _priceStep > 0;
            _offerMore.interactable = !busy && _priceStep < PriceSteps.Length - 1;
            _offerSend.interactable = !busy && !samePrice;
            _offerSendLabel.text = samePrice ? "OFFERED AT THIS PRICE" : offered ? "CHANGE THE PRICE" : "OFFER IT";
            _offerWithdraw.gameObject.SetActive(offered);
            _offerWithdraw.interactable = !busy;
        }

        private void RefreshMarketView()
        {
            _marketView.SetActive(_marketOpen);
            if (!_marketOpen)
                return;

            var offers = GenesisRental.Offers;
            var pages = (offers.Count + SlotsPerPage - 1) / SlotsPerPage;
            _marketPage = Mathf.Clamp(_marketPage, 0, Mathf.Max(0, pages - 1));
            // The first offer is picked until the player picks another; a withdrawn one is let go.
            if (!GenesisRental.TryGetOffer(_marketPick, out var picked))
            {
                _marketPick = offers.Count > 0 ? offers[0].TokenId : 0;
                GenesisRental.TryGetOffer(_marketPick, out picked);
            }

            for (var i = 0; i < SlotsPerPage; i++)
            {
                var index = _marketPage * SlotsPerPage + i;
                var shown = index < offers.Count ? offers[index].TokenId : 0;
                ShowSlot(_marketSlots[i], shown, _marketPick);
                TagSlot(_marketSlots[i], shown > 0 ? OfferTag(offers[index].PricePerDayWei, ChogPrice.UsdPerChog) : "");
            }

            _marketPrevious.gameObject.SetActive(pages > 1);
            _marketNext.gameObject.SetActive(pages > 1);
            _marketPrevious.interactable = _marketPage > 0;
            _marketNext.interactable = _marketPage < pages - 1;
            _marketCount.text = !GenesisRental.OffersRead ? "reading..." : offers.Count == 1 ? "1 offered" : offers.Count + " offered";

            var mine = GenesisRental.Doing == RentalAction.Rent || GenesisRental.Doing == RentalAction.EndRental || GenesisRental.Doing == RentalAction.Claim;
            var busy = mine && GenesisRental.IsBusy;
            var failed = mine && GenesisRental.Step == RentalStep.Failed;
            var days = DayChoices[_dayChoice];
            for (var i = 0; i < _dayButtons.Length; i++)
            {
                _dayButtons[i].gameObject.SetActive(_marketPick > 0);
                _dayButtons[i].interactable = !busy;
                _dayButtons[i].GetComponent<Image>().color = i == _dayChoice ? AccentColor : QuietColor;
            }

            // The line under the offers: why a rental did not go through, else which page this is.
            _marketPageLabel.text = failed ? "<color=" + ProblemColor + ">" + GenesisRental.Problem + "</color>"
                : pages > 1 ? (_marketPage + 1) + " / " + pages : "";
            if (_marketPick <= 0)
            {
                _marketChosen.text = GenesisRental.OffersRead
                    ? "No Genesis is offered for rent yet. A holder offers one from this menu, with RENT IT OUT."
                    : "Reading the offers on Monad...";
                _rent.gameObject.SetActive(false);
                ShowAside(busy, 0);
                return;
            }

            var own = Array.IndexOf(PlayerWallet.Tokens, _marketPick) >= 0;
            var until = GenesisRental.RentedUntilSeconds(_marketPick);
            var cost = ChogPrice.WithUsd(MonadTransactions.Times(picked.PricePerDayWei, days));
            _marketChosen.text = DescribeChoice(_marketPick) + "\n<size=60%><color=" + LabelColor + ">" +
                                 (own ? "THIS ONE IS YOURS"
                                     : until > 0 ? "YOU RENT IT FOR " + GenesisRental.TimeLeft(until, NowMs() / 1000).ToUpperInvariant()
                                     : "PAID TO ITS HOLDER " + PlayerWallet.ShortAddress(picked.Lender)) + "</color></size>";

            _rent.gameObject.SetActive(!own);
            _rent.interactable = !busy;
            _rentLabel.text = busy ? "<size=55%>ANSWER IN YOUR WALLET</size>\n" + RentalStepText(GenesisRental.Step).ToUpperInvariant()
                : (until > 0 ? "ADD " : "RENT ") + days + (days == 1 ? " DAY FOR " : " DAYS FOR ") + cost;
            ShowAside(busy, until > 0 ? _marketPick : 0);
        }

        /// <summary>"10 A DAY ($0.02)": the tag under a Genesis offered for rent; without the dollars while the price is unknown.</summary>
        public static string OfferTag(string pricePerDayWei, double usdPerChog)
        {
            var usd = ChogPrice.UsdText(pricePerDayWei, usdPerChog);
            return MonadTransactions.PriceText(pricePerDayWei) + " A DAY" + (usd.Length > 0 ? " (" + usd + ")" : "");
        }

        // The $CHOG this wallet may take out (owed back as a renter, or earned as a holder, which a
        // holder who sold every Genesis can only reach here), else END NOW for the Genesis it rents.
        private void ShowAside(bool busy, int rentedPick)
        {
            var owed = GenesisRental.OwedBackWei;
            var earned = GenesisRental.EarnedWei;
            var show = GenesisRental.HasMoneyToClaim || rentedPick > 0;
            _marketAside.gameObject.SetActive(show);
            if (!show)
                return;
            _marketAside.interactable = !busy;
            _marketAsideLabel.text = owed != "0"
                ? "<size=70%>GET YOUR REFUND</size>\n" + ChogPrice.WithUsd(owed)
                : earned != "0"
                    ? "<size=70%>CLAIM</size>\n" + ChogPrice.WithUsd(GenesisRental.HolderShare(earned, GenesisRental.BurnedShare))
                    : "END NOW";
        }

        // What the wallet is being asked, in the order of a payment.
        private static string RentalStepText(RentalStep step)
        {
            switch (step)
            {
                case RentalStep.Switching:
                    return "Monad...";
                case RentalStep.Approving:
                    return "1/2 allow";
                case RentalStep.Sending:
                    return GenesisRental.Doing == RentalAction.Rent ? "2/2 rent" : GenesisRental.Doing == RentalAction.Claim ? "claim" : "confirm";
                default:
                    return "confirming";
            }
        }

        // "10%" for a share given in hundredths of a percent.
        private static string BurnedText()
        {
            var share = GenesisRental.BurnedShare;
            return share % 100 == 0 ? share / 100 + "%" : (share / 100f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }
    }
}
