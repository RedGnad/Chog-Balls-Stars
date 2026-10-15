using System;
using Chog.Match;
using Quantum;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace Chog.Screens
{
    /// <summary>
    /// The panel of the main menu that says which hero the player enters with. A holder connects
    /// the wallet of the browser, picks, among the Chog Genesis that wallet holds or rents, the
    /// one to play, signs in so its career is recorded, and can lend it with a link or offer it
    /// for rent in $CHOG. Any connected wallet can rent a Genesis that is offered. A guest who
    /// came with a link sees the Genesis they were lent. Anyone else reads what a recruit is.
    /// It shows the state kept by PlayerWallet, GuestPass, GenesisLoan, GenesisRental and
    /// GenesisCareers and builds its few elements in code, as a child of the menu's main screen,
    /// so it appears and leaves with it. The two screens of the rental are in WalletPanel.Rental.cs.
    /// </summary>
    public sealed partial class WalletPanel : MonoBehaviour
    {
        private const string ObjectName = "ChogWalletPanel";
        private const string RosterResource = "GenesisRoster";
        private const int SlotsPerPage = 6;

        // Share of the main screen the panel takes: right of the menu buttons, under the player name.
        private static readonly Vector2 PanelMin = new Vector2(0.34f, 0.10f);
        private static readonly Vector2 PanelMax = new Vector2(0.97f, 0.72f);

        // The career line: the whole width, or the room left by the buttons that lend and rent out.
        private static readonly Vector2 CareerMin = new Vector2(0.04f, 0.03f);
        private static readonly Vector2 CareerMax = new Vector2(0.96f, 0.18f);
        private static readonly Vector2 CareerMaxBesideLend = new Vector2(0.72f, 0.18f);
        private static readonly Vector2 CareerMaxBesideRentOut = new Vector2(0.50f, 0.18f);

        // The button that signs in: wide, or beside the button that rents out.
        private static readonly Vector2 SignInMin = new Vector2(0.14f, 0.035f);
        private static readonly Vector2 SignInMax = new Vector2(0.86f, 0.175f);
        private static readonly Vector2 SignInMinBesideRentOut = new Vector2(0.04f, 0.035f);
        private static readonly Vector2 SignInMaxBesideRentOut = new Vector2(0.50f, 0.175f);

        // The title: the whole room left of the address, or the room left by the button that opens the offers.
        private static readonly Vector2 TitleMax = new Vector2(0.62f, 0.98f);
        private static readonly Vector2 TitleMaxBesideMarket = new Vector2(0.39f, 0.98f);

        // The name and kit of the Genesis played: the whole width, or the room left by the form button.
        private static readonly Vector2 ChosenMin = new Vector2(0.04f, 0.19f);
        private static readonly Vector2 ChosenMax = new Vector2(0.96f, 0.41f);
        private static readonly Vector2 ChosenMaxBesideForm = new Vector2(0.68f, 0.41f);

        private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.16f, 0.88f);
        private static readonly Color CoverColor = new Color(0.07f, 0.09f, 0.16f, 1f);
        private static readonly Color AccentColor = new Color(0.33f, 0.82f, 0.56f, 1f);
        private static readonly Color ButtonTextColor = new Color(0.05f, 0.12f, 0.16f, 1f);
        private static readonly Color QuietColor = new Color(0.93f, 0.91f, 0.85f, 1f);
        private static readonly Color MutedColor = new Color(0.66f, 0.68f, 0.76f, 1f);
        private static readonly Color WaitingColor = new Color(0.25f, 0.25f, 0.3f, 0.85f);
        private static readonly Color LockedColor = new Color(0.45f, 0.47f, 0.55f, 1f);
        private const string CareerColor = "#FFD27A";
        private const string LabelColor = "#A9ADC2";

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string RecruitNote =
            "Without one you play a recruit: " + (100 - Recruit.HealthPercent) + "% less health and no Genesis attack. " +
            "A holder can also lend you a Genesis with a link.";

        private GenesisRoster _roster;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _address;
        private TextMeshProUGUI _message;
        private TextMeshProUGUI _chosen;
        private TextMeshProUGUI _career;
        private TextMeshProUGUI _pageLabel;
        private Button _connect;
        private TextMeshProUGUI _connectLabel;
        // On a phone, beside OPEN IN METAMASK: the window where any other wallet app is picked.
        private Button _otherApp;
        private Button _signIn;
        private TextMeshProUGUI _signInLabel;
        private Button _lookUp;
        private Button _lend;
        private Button _rentOut;
        private TextMeshProUGUI _rentOutLabel;
        private Button _market;
        private Button _form;
        private TextMeshProUGUI _formLabel;
        // The Genesis the panel shows as the one played, 0 for none.
        private int _played;
        private Button _previous;
        private Button _next;
        private readonly Slot[] _slots = new Slot[SlotsPerPage];
        private int _page;
        // The choice the pages were last arranged for.
        private int _shownChoice;
        // Whose careers were last asked for (a wallet, or the Genesis of a guest), and the Genesis last asked for by itself.
        private string _careersOf = "";
        private int _careerAskedFor;
        // Whose purchases were last asked for, in the same words.
        private string _purchasesOf = "";

        // The screen that lends a Genesis, laid over the panel.
        private GameObject _loanView;
        private TextMeshProUGUI _loanTitle;
        private TextMeshProUGUI _loanText;
        private TextMeshProUGUI _loanLink;
        private TextMeshProUGUI _loanNotice;
        private Button _share;
        private Button _stop;

        private sealed class Slot
        {
            public GameObject Root;
            public RawImage Ring;
            public RawImage Portrait;
            public GameObject TagRoot;
            public TextMeshProUGUI Tag;
            public int TokenId;
            public int MetadataId;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Attach();
            Attach();
        }

        // Adds the panel to the main screen of the menu, once per menu.
        private static void Attach()
        {
            var menu = FindAnyObjectByType<QuantumMenuUIMain>(FindObjectsInactive.Include);
            if (menu == null)
                return;

            MenuTitle.Apply(menu.transform.root);
            if (menu.transform.Find(ObjectName) != null)
                return;

            var panel = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(menu.transform, false);
            panel.AddComponent<WalletPanel>().Build();
        }

        private void OnEnable()
        {
            PlayerWallet.Changed += Refresh;
            GenesisCareers.Changed += Refresh;
            GuestPass.Changed += Refresh;
            GenesisLoan.Changed += Refresh;
            GenesisForms.Changed += Refresh;
            GenesisShop.Changed += Refresh;
            GenesisRental.Changed += Refresh;
            GenesisWager.Changed += Refresh;
            if (_title != null)
            {
                // Back on the menu, for instance after a match: the careers may have moved.
                _careersOf = "";
                _careerAskedFor = 0;
                _purchasesOf = "";
                Refresh();
            }
        }

        private void OnDisable()
        {
            PlayerWallet.Changed -= Refresh;
            GenesisCareers.Changed -= Refresh;
            GuestPass.Changed -= Refresh;
            GenesisLoan.Changed -= Refresh;
            GenesisForms.Changed -= Refresh;
            GenesisShop.Changed -= Refresh;
            GenesisRental.Changed -= Refresh;
            GenesisWager.Changed -= Refresh;
        }

        private void Build()
        {
            _roster = Resources.Load<GenesisRoster>(RosterResource);

            var area = (RectTransform)transform;
            area.anchorMin = PanelMin;
            area.anchorMax = PanelMax;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            GetComponent<Image>().color = PanelColor;

            // Sizes are in units of the menu canvas, about twice the pixels of a phone in landscape.
            _title = Text("Title", new Vector2(0.04f, 0.86f), TitleMax, TextAlignmentOptions.Left, Color.white, 60f);
            _title.fontStyle = FontStyles.Bold;
            _address = Text("Address", new Vector2(0.62f, 0.86f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.Right, MutedColor, 40f);
            _message = Text("Message", new Vector2(0.04f, 0.41f), new Vector2(0.96f, 0.84f), TextAlignmentOptions.TopLeft, Color.white, 44f);
            _pageLabel = Text("Page", new Vector2(0.10f, 0.41f), new Vector2(0.90f, 0.48f), TextAlignmentOptions.Center, MutedColor, 30f);
            _chosen = Text("Chosen", ChosenMin, ChosenMax, TextAlignmentOptions.Center, Color.white, 54f);
            _career = Text("Career", CareerMin, CareerMax, TextAlignmentOptions.Center, Color.white, 40f);

            _connect = MakeButton("Connect", new Vector2(0.27f, 0.06f), new Vector2(0.73f, 0.25f), out _connectLabel);
            _connect.onClick.AddListener(OnMainButton);
            _otherApp = MakeButton("OtherApp", new Vector2(0.74f, 0.06f), new Vector2(0.96f, 0.25f), out var otherAppLabel);
            _otherApp.GetComponent<Image>().color = QuietColor;
            otherAppLabel.text = "OTHER WALLET";
            _otherApp.onClick.AddListener(PlayerWallet.Connect);
            _signIn = MakeButton("SignIn", SignInMin, SignInMax, out _signInLabel);
            _signIn.onClick.AddListener(PlayerWallet.StartSignIn);

            // For a visitor without a Genesis: the page where any Genesis shows as its hero card.
            _lookUp = MakeButton("LookUp", new Vector2(0.27f, 0.27f), new Vector2(0.73f, 0.38f), out var lookUpLabel);
            _lookUp.GetComponent<Image>().color = QuietColor;
            lookUpLabel.text = "SEE EVERY HERO";
            _lookUp.onClick.AddListener(PlayerWallet.OpenHeroPage);

            // For a signed-in holder, beside the career: lend the chosen Genesis with a link.
            _lend = MakeButton("Lend", new Vector2(0.74f, 0.045f), new Vector2(0.96f, 0.165f), out var lendLabel);
            _lend.GetComponent<Image>().color = QuietColor;
            lendLabel.text = "LEND IT";
            _lend.onClick.AddListener(() => GenesisLoan.Lend(PlayerWallet.ChosenTokenId));

            // For a holder, left of it: offer the chosen Genesis for rent in $CHOG, when a rental is set up.
            _rentOut = MakeButton("RentOut", new Vector2(0.52f, 0.045f), new Vector2(0.73f, 0.165f), out _rentOutLabel);
            _rentOut.GetComponent<Image>().color = QuietColor;
            _rentOut.onClick.AddListener(() => OpenOffer(_played));

            // For any connected wallet: the Genesis that holders offer for rent.
            _market = MakeButton("Market", MarketSmallMin, MarketSmallMax, out var marketLabel);
            marketLabel.text = "RENT A HERO";
            _market.onClick.AddListener(OpenMarket);

            // Beside the Genesis played: which form of its attack it enters with. A button once
            // mastery unlocked the second form, and the level to reach before that.
            _form = MakeButton("Form", new Vector2(0.70f, 0.215f), new Vector2(0.96f, 0.385f), out _formLabel);
            _form.onClick.AddListener(OnFormButton);

            _previous = MakeButton("Previous", new Vector2(0.01f, 0.54f), new Vector2(0.07f, 0.78f), out var previousLabel);
            previousLabel.text = "<";
            _previous.onClick.AddListener(() => TurnPage(-1));
            _next = MakeButton("Next", new Vector2(0.93f, 0.54f), new Vector2(0.99f, 0.78f), out var nextLabel);
            nextLabel.text = ">";
            _next.onClick.AddListener(() => TurnPage(1));

            for (var i = 0; i < SlotsPerPage; i++)
                _slots[i] = MakeSlot(i, (RectTransform)transform, PlayerWallet.Choose);

            BuildLoanView();
            BuildRentalViews();
            Refresh();
        }

        // Laid over the whole panel while a Genesis is being lent; it takes the taps of what it covers.
        private void BuildLoanView()
        {
            var cover = Child("Loan", transform, Vector2.zero, Vector2.one);
            cover.gameObject.AddComponent<Image>().color = CoverColor;
            _loanView = cover.gameObject;

            _loanTitle = Text("Title", new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.Left, Color.white, 60f, cover);
            _loanTitle.fontStyle = FontStyles.Bold;
            _loanText = Text("Text", new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.84f), TextAlignmentOptions.TopLeft, Color.white, 42f, cover);
            _loanLink = Text("Link", new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.49f), TextAlignmentOptions.TopLeft, MutedColor, 26f, cover);
            _loanLink.richText = false;
            _loanNotice = Text("Notice", new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.34f), TextAlignmentOptions.Left, AccentColor, 38f, cover);

            _share = MakeButton("Share", new Vector2(0.04f, 0.05f), new Vector2(0.44f, 0.22f), out var shareLabel, cover);
            shareLabel.text = "SHARE THE LINK";
            _share.onClick.AddListener(() => GenesisLoan.Share(NameOf(GenesisLoan.TokenId)));
            _stop = MakeButton("Stop", new Vector2(0.47f, 0.05f), new Vector2(0.77f, 0.22f), out var stopLabel, cover);
            _stop.GetComponent<Image>().color = QuietColor;
            stopLabel.text = "STOP LENDING";
            _stop.onClick.AddListener(GenesisLoan.Stop);
            var back = MakeButton("Back", new Vector2(0.80f, 0.05f), new Vector2(0.96f, 0.22f), out var backLabel, cover);
            back.GetComponent<Image>().color = QuietColor;
            backLabel.text = "BACK";
            back.onClick.AddListener(GenesisLoan.Close);
        }

        private void Refresh()
        {
            var state = PlayerWallet.State;
            // What the wallet may play: the Genesis it holds, then those it rents.
            var tokens = PlayerWallet.Playable;
            var holds = state == WalletState.Ready && tokens.Length > 0;
            // A Genesis of the player's own wallet, held or rented, wins over one that was lent.
            var guest = !holds && GuestPass.State == GuestPassState.Valid;
            var checkingPass = !holds && GuestPass.State == GuestPassState.Checking;
            var played = holds ? PlayerWallet.ChosenTokenId : guest ? GuestPass.TokenId : 0;
            var owns = holds && Array.IndexOf(PlayerWallet.Tokens, played) >= 0;
            var rents = holds && !owns && played > 0;

            // Careers are read once it is known which Genesis the panel shows, and again when a rental adds one.
            var careersOf = holds ? PlayerWallet.Address + " with " + tokens.Length : guest ? "guest of " + GuestPass.TokenId : "";
            if (careersOf.Length > 0 && _careersOf != careersOf)
            {
                _careersOf = careersOf;
                GenesisCareers.Read(holds ? tokens : new[] { GuestPass.TokenId });
            }

            // So is what was bought for them, once it is known that a shop is open.
            if (careersOf.Length > 0 && GenesisShop.State == ShopState.Open && _purchasesOf != careersOf)
            {
                _purchasesOf = careersOf;
                GenesisShop.ReadOwned(holds ? tokens : new[] { GuestPass.TokenId });
            }

            _title.text = holds ? (PlayerWallet.Tokens.Length > 0 ? "YOUR GENESIS" : "YOUR RENTED GENESIS")
                : guest ? "YOU ARE A GUEST" : "PLAY YOUR GENESIS";
            _address.text = guest ? "lent by " + PlayerWallet.ShortAddress(GuestPass.Lender) : PlayerWallet.ShortAddress(PlayerWallet.Address);
            _message.text = holds || guest ? "" : checkingPass ? "Checking your invitation..." : PassProblem() + MessageFor(state);
            // On a phone browser without a wallet, the same button reopens the game where one is.
            var canOpen = PlayerWallet.CanOpenInWalletBrowser;
            _connectLabel.text = canOpen ? "OPEN IN METAMASK" : state == WalletState.Failed ? "TRY AGAIN" : "CONNECT WALLET";
            _connect.gameObject.SetActive(!guest && !checkingPass && (canOpen || state == WalletState.Disconnected || state == WalletState.Failed));
            _otherApp.gameObject.SetActive(!guest && !checkingPass && canOpen && PlayerWallet.ThroughApp);
            _lookUp.gameObject.SetActive(!holds && !guest && !checkingPass && state != WalletState.Connecting && state != WalletState.Reading);

            // A connected wallet can rent a Genesis that is offered: a large button when it has
            // none to play, a small one beside the title when it has.
            var canRent = state == WalletState.Ready && GenesisRental.State == RentalState.Open && !checkingPass;
            var large = canRent && !holds && !guest;
            _market.gameObject.SetActive(canRent);
            var marketArea = (RectTransform)_market.transform;
            marketArea.anchorMin = large ? MarketLargeMin : MarketSmallMin;
            marketArea.anchorMax = large ? MarketLargeMax : MarketSmallMax;
            _title.rectTransform.anchorMax = canRent && !large ? TitleMaxBesideMarket : TitleMax;

            // A new choice opens its page; turning pages by hand is otherwise left alone.
            var pages = holds ? (tokens.Length + SlotsPerPage - 1) / SlotsPerPage : 0;
            if (PlayerWallet.ChosenTokenId != _shownChoice)
            {
                _shownChoice = PlayerWallet.ChosenTokenId;
                var chosenIndex = Array.IndexOf(tokens, _shownChoice);
                if (chosenIndex >= 0)
                    _page = chosenIndex / SlotsPerPage;
            }

            _page = Mathf.Clamp(_page, 0, Mathf.Max(0, pages - 1));

            for (var i = 0; i < SlotsPerPage; i++)
            {
                var index = _page * SlotsPerPage + i;
                // A guest sees the one Genesis they were lent.
                var shown = holds ? (index < tokens.Length ? tokens[index] : 0) : guest && i == 0 ? GuestPass.TokenId : 0;
                ShowSlot(_slots[i], shown, played);
                // A rented Genesis says so, and for how long.
                var rentedUntil = shown > 0 && holds ? GenesisRental.RentedUntilSeconds(shown) : 0;
                TagSlot(_slots[i], rentedUntil > 0 && Array.IndexOf(PlayerWallet.Tokens, shown) < 0
                    ? "RENTED " + GenesisRental.TimeLeft(rentedUntil, NowMs() / 1000) : "");
            }

            _previous.gameObject.SetActive(pages > 1);
            _next.gameObject.SetActive(pages > 1);
            _previous.interactable = _page > 0;
            _next.interactable = _page < pages - 1;
            _pageLabel.text = !holds || tokens.Length < 2 ? ""
                : (pages > 1 ? (_page + 1) + " / " + pages + "   " : "") + "tap a Genesis to play it";
            _chosen.text = played > 0 ? DescribeChoice(played) : "";
            _played = played;
            RefreshForm(played);

            // Signed in, the career of the chosen Genesis and the way to lend it; otherwise the
            // way to have a career recorded. A guest reads how long the loan lasts.
            var signedIn = PlayerWallet.SignIn == SignInState.SignedIn;
            var canRentOut = owns && GenesisRental.State == RentalState.Open;
            // Where matches are staked on, a wallet without a Genesis signs in too: it stakes as a recruit.
            var signsInToStake = !holds && !guest && !checkingPass && state == WalletState.Ready && GenesisWager.State == WagerState.Open;
            _signIn.gameObject.SetActive((holds || signsInToStake) && !signedIn);
            var signInArea = (RectTransform)_signIn.transform;
            signInArea.anchorMin = canRentOut ? SignInMinBesideRentOut : SignInMin;
            signInArea.anchorMax = canRentOut ? SignInMaxBesideRentOut : SignInMax;
            _signIn.interactable = PlayerWallet.SignIn != SignInState.Asking;
            _signInLabel.text = PlayerWallet.SignIn == SignInState.Asking ? "ANSWER IN YOUR WALLET..."
                : PlayerWallet.SignInProblem.Length > 0 ? "SIGN-IN FAILED, TRY AGAIN"
                : holds ? "SIGN IN TO RECORD ITS CAREER" : "SIGN IN TO STAKE " + GenesisWager.Currency;
            var canLend = owns && signedIn && GenesisLoan.CanLend(played);
            _lend.gameObject.SetActive(canLend);
            _rentOut.gameObject.SetActive(canRentOut);
            if (canRentOut)
            {
                _rentOutLabel.text = GenesisRental.TryGetOffer(played, out var offered)
                    ? "<size=55%>RENTED OUT AT</size>\n" + MonadTransactions.PriceText(offered.PricePerDayWei) + " A DAY"
                    : "RENT IT OUT";
            }

            _career.rectTransform.anchorMax = canRentOut ? CareerMaxBesideRentOut : canLend ? CareerMaxBesideLend : CareerMax;
            _career.text = holds && signedIn ? (rents ? DescribeRental(played) + "   " : "") + DescribeCareer(played)
                : guest ? DescribeLoan() + "   " + DescribeCareer(played) : "";

            // A wallet with more Genesis than one read covers: the chosen one is asked for by itself.
            var chosen = PlayerWallet.ChosenTokenId;
            if (holds && chosen != _careerAskedFor && Array.IndexOf(tokens, chosen) >= GenesisCareers.MaxTokensPerRead)
            {
                _careerAskedFor = chosen;
                GenesisCareers.Read(new[] { chosen });
            }

            RefreshLoanView();
            RefreshRentalViews();
        }

        // The form of the attack the played Genesis enters with: picked by a tap once its mastery
        // unlocked the second form; before that the button says what to reach.
        private void RefreshForm(int played)
        {
            var index = played > 0 && _roster != null ? _roster.IndexOf(played) : -1;
            var hasForms = index >= 0 && _roster.HasSecondForm(_roster.Entries[index].Shape);
            _form.gameObject.SetActive(hasForms);
            _chosen.rectTransform.anchorMax = hasForms ? ChosenMaxBesideForm : ChosenMax;
            if (!hasForms)
                return;

            var unlocked = GenesisForms.IsUnlockedFor(played);
            // The second form is sold, when a shop is open, to a connected wallet for a Genesis
            // it holds; a guest waits for mastery or for the holder to buy it.
            var canBuy = !unlocked && GenesisShop.State == ShopState.Open && PlayerWallet.State == WalletState.Ready
                         && Array.IndexOf(PlayerWallet.Tokens, played) >= 0;
            var mine = GenesisShop.PurchaseTokenId == played;
            var buying = canBuy && mine && GenesisShop.IsBusy;
            var failed = canBuy && mine && GenesisShop.Purchase == PurchaseState.Failed;

            _form.interactable = unlocked || (canBuy && !GenesisShop.IsBusy);
            _form.GetComponent<Image>().color = unlocked ? QuietColor : canBuy ? AccentColor : LockedColor;
            if (unlocked)
            {
                _formLabel.text = "<size=55%>FORM, TAP TO CHANGE</size>\n" +
                                  _roster.FormName(_roster.Entries[index].Shape, GenesisForms.Picked(played)).ToUpperInvariant();
            }
            else if (buying)
            {
                _formLabel.text = "<size=55%>ANSWER IN YOUR WALLET</size>\n" + PurchaseStep(GenesisShop.Purchase);
            }
            else if (failed)
            {
                _formLabel.text = "<size=55%>NOT BOUGHT</size>\nTRY AGAIN";
                _pageLabel.text = "<color=#FF9C8A>" + GenesisShop.Problem + "</color>";
            }
            else if (canBuy)
            {
                _formLabel.text = "<size=55%>MASTERY " + GenesisForms.UnlockLevel + ", OR NOW FOR</size>\n" +
                                  MonadTransactions.PriceText(GenesisShop.PriceWei) + " $CHOG";
            }
            else
            {
                _formLabel.text = "<size=55%>SECOND FORM AT</size>\nMASTERY " + GenesisForms.UnlockLevel;
            }
        }

        // What the wallet is being asked, in the order of a purchase.
        private static string PurchaseStep(PurchaseState step)
        {
            switch (step)
            {
                case PurchaseState.Switching:
                    return "MONAD...";
                case PurchaseState.Approving:
                    return "1/2 ALLOW";
                case PurchaseState.Buying:
                    return "2/2 BUY";
                default:
                    return "CONFIRMING";
            }
        }

        // The form button: switches the form once it is unlocked, buys it before that.
        private void OnFormButton()
        {
            if (GenesisForms.IsUnlockedFor(_played))
            {
                GenesisForms.Pick(_played, 1 - GenesisForms.Picked(_played));
                return;
            }

            GenesisShop.Dismiss();
            GenesisShop.Buy(_played);
        }

        private void RefreshLoanView()
        {
            // The wallet changed account or signed out while the screen was open: there is nothing to lend any more.
            if (GenesisLoan.State != LoanState.Idle && !GenesisLoan.CanLend(GenesisLoan.TokenId))
            {
                GenesisLoan.Close();
                return;
            }

            var loan = GenesisLoan.State;
            _loanView.SetActive(loan != LoanState.Idle);
            if (loan == LoanState.Idle)
                return;

            var hero = NameOf(GenesisLoan.TokenId);
            _loanTitle.text = "LEND " + hero.ToUpperInvariant();
            _loanText.text = loan == LoanState.Asking ? "One moment..."
                : loan == LoanState.Stopped ? "Lending is stopped: every link handed out for " + hero + " so far is dead."
                : loan == LoanState.Failed ? GenesisLoan.Notice
                : "Whoever opens this link plays " + hero + " without a wallet, for " +
                  GuestPass.TimeLeft(GenesisLoan.ExpiresAtMs, NowMs()) + ". Their matches count for its career.\n" +
                  "The link dies if you stop lending or if the Genesis leaves your wallet.";
            _loanLink.text = loan == LoanState.Ready ? GenesisLoan.Link : "";
            _loanNotice.text = loan == LoanState.Ready ? GenesisLoan.Notice : "";
            _share.gameObject.SetActive(loan == LoanState.Ready);
            _stop.gameObject.SetActive(loan == LoanState.Ready);
        }

        private static void OnMainButton()
        {
            if (PlayerWallet.CanOpenInWalletBrowser)
                PlayerWallet.OpenInWalletBrowser();
            else
                PlayerWallet.Connect();
        }

        // What to say about a pass that did not let the visitor in; empty when there was none.
        private static string PassProblem()
        {
            return GuestPass.State == GuestPassState.Refused ? "Your invitation does not work: " + GuestPass.Problem + ".\n\n" : "";
        }

        private static string MessageFor(WalletState state)
        {
            switch (state)
            {
                case WalletState.NoProvider:
                    return "No wallet in this browser. To play a Chog Genesis you hold, open this page in the browser of your " +
                           "wallet (MetaMask, Rabby, Backpack) or install its extension.\n\n" + RecruitNote;
                case WalletState.Disconnected:
                    return "Connect the wallet that holds your Chog Genesis: each one is a hero, with the class, the attack " +
                           "and the effect its traits give it." +
                           (PlayerWallet.CanOpenInWalletBrowser
                               ? " OPEN IN METAMASK plays inside the MetaMask app, where your wallet answers without leaving the game." +
                                 (PlayerWallet.ThroughApp ? " OTHER WALLET: pick your app, approve there, then come back here." : "")
                               : PlayerWallet.ThroughApp ? " You pick your wallet app, approve there, then come back here." : "") +
                           "\n\n" + RecruitNote;
                case WalletState.Connecting:
                    return PlayerWallet.ThroughApp ? "Pick your wallet app and approve there, then come back here..."
                        : "Answer in your wallet...";
                case WalletState.Reading:
                    return "Reading your Genesis on Monad...";
                case WalletState.Failed:
                    return PlayerWallet.Problem + "\n\n" + RecruitNote;
                default:
                    return "This wallet holds no Chog Genesis." +
                           (GenesisRental.State == RentalState.Open ? " You can rent one from its holder for $CHOG." : "") +
                           "\n\n" + RecruitNote;
            }
        }

        private static long NowMs() => (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;

        // "CHOG #1287" for a token id; "this Genesis" when the roster does not know it.
        private string NameOf(int tokenId)
        {
            var index = _roster == null ? -1 : _roster.IndexOf(tokenId);
            return index < 0 ? "this Genesis" : GenesisHeroCard.DisplayName(_roster.Entries[index]);
        }

        private string DescribeChoice(int tokenId)
        {
            var index = _roster == null ? -1 : _roster.IndexOf(tokenId);
            if (index < 0)
                return "";

            var hero = _roster.Entries[index];
            // The kit as it will be played: with the second form of the attack when it is equipped.
            var lines = _roster.CardLines(hero, GenesisForms.PlayedBy(tokenId));
            var kit = lines[0].Gives + " / " + lines[1].Gives + " / " + lines[2].Gives + " / " + lines[3].Gives;
            return "<b>" + GenesisHeroCard.DisplayName(hero) + "</b>\n<size=70%>" + kit.ToUpperInvariant() + "</size>";
        }

        // Mastery and matches; the badges are on the hero card, where there is room for their names.
        private static string DescribeCareer(int tokenId)
        {
            if (!GenesisCareers.TryGet(tokenId, out var career))
                return "";

            var mastery = GenesisCareers.DescribeMastery(career);
            return (mastery.Length == 0 ? "" : "<color=" + CareerColor + ">" + mastery.ToUpperInvariant() + "</color>   ") +
                   "<size=70%><color=" + LabelColor + ">CAREER</color></size>  <color=" + CareerColor + ">" +
                   GenesisCareers.DescribeMatches(career) + "</color>";
        }

        private static string DescribeLoan()
        {
            return "<size=70%><color=" + LabelColor + ">YOURS FOR</color></size>  " + GuestPass.TimeLeft(GuestPass.ExpiresAtMs, NowMs());
        }

        private static string DescribeRental(int tokenId)
        {
            return "<size=70%><color=" + LabelColor + ">RENTED FOR</color></size>  " +
                   GenesisRental.TimeLeft(GenesisRental.RentedUntilSeconds(tokenId), NowMs() / 1000);
        }

        private void TurnPage(int direction)
        {
            _page += direction;
            Refresh();
        }

        private void ShowSlot(Slot slot, int tokenId, int played)
        {
            var index = tokenId > 0 && _roster != null ? _roster.IndexOf(tokenId) : -1;
            slot.Root.SetActive(tokenId > 0);
            slot.TokenId = tokenId;
            if (tokenId <= 0)
                return;

            slot.Ring.color = tokenId == played ? AccentColor : Color.clear;
            var metadataId = index < 0 ? 0 : _roster.Entries[index].MetadataId;
            if (metadataId == slot.MetadataId)
                return;

            slot.MetadataId = metadataId;
            slot.Portrait.texture = GenesisArt.Disc();
            slot.Portrait.color = WaitingColor;
            GenesisArt.Request(metadataId, texture =>
            {
                // The slot may be gone, or showing another Genesis, by the time the image arrives.
                if (slot.Portrait == null || slot.MetadataId != metadataId)
                    return;

                slot.Portrait.texture = texture;
                slot.Portrait.color = Color.white;
            });
        }

        private Slot MakeSlot(int position, RectTransform parent, Action<int> tapped)
        {
            // Six equal cells between the two page arrows; each holds a square, whatever the screen.
            const float left = 0.08f, right = 0.92f;
            var width = (right - left) / SlotsPerPage;
            var cell = Child("Slot" + position, parent, new Vector2(left + position * width, 0.48f),
                new Vector2(left + (position + 1) * width, 0.85f));
            var hit = cell.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var slot = new Slot { Root = cell.gameObject };
            cell.gameObject.AddComponent<Button>().onClick.AddListener(() => tapped(slot.TokenId));

            slot.Ring = Square("Ring", cell, 1f);
            slot.Portrait = Square("Portrait", cell, 0.88f);

            // A short word under the image, on a dark band so it reads over any Genesis.
            var band = Child("Tag", cell, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.2f));
            var bandImage = band.gameObject.AddComponent<Image>();
            bandImage.color = TagColor;
            bandImage.raycastTarget = false;
            slot.TagRoot = band.gameObject;
            slot.Tag = Text("Text", new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), TextAlignmentOptions.Center, Color.white, 30f, band);
            slot.TagRoot.SetActive(false);
            return slot;
        }

        private static void TagSlot(Slot slot, string text)
        {
            slot.TagRoot.SetActive(slot.TokenId > 0 && text.Length > 0);
            slot.Tag.text = text;
        }

        // A round image centred in its parent, as large as fits, scaled by "share".
        private static RawImage Square(string name, RectTransform parent, float share)
        {
            var holder = Child(name, parent, new Vector2(0.5f - share / 2f, 0.5f - share / 2f),
                new Vector2(0.5f + share / 2f, 0.5f + share / 2f));
            var image = Child("Image", holder, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            image.texture = GenesisArt.Disc();
            image.raycastTarget = false;
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            return image;
        }

        private Button MakeButton(string name, Vector2 min, Vector2 max, out TextMeshProUGUI label, RectTransform parent = null)
        {
            var area = Child(name, parent == null ? transform : parent, min, max);
            var background = area.gameObject.AddComponent<Image>();
            background.color = AccentColor;
            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            label = Text("Label", new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f), TextAlignmentOptions.Center, ButtonTextColor, 52f, area);
            label.fontStyle = FontStyles.Bold;
            return button;
        }

        private TextMeshProUGUI Text(string name, Vector2 min, Vector2 max, TextAlignmentOptions alignment, Color color,
                                     float largest, RectTransform parent = null)
        {
            var text = Child(name, parent == null ? transform : parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.richText = true;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = largest;
            return text;
        }

        private static RectTransform Child(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
