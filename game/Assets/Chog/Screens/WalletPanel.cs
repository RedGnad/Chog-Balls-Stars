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
    /// The card of the home screen that says which hero the player enters with. A holder connects
    /// the wallet of the browser, picks, among the Chog Genesis that wallet holds or rents, the one
    /// to play, and signs in so its career is recorded; the card then shows that Genesis's mastery
    /// road. Any connected wallet can rent a Genesis that is offered. A guest who came with a link
    /// sees the Genesis they were lent. Anyone else reads what a recruit is. Lending a Genesis,
    /// offering it for rent and renting one open in a window over the whole menu (the hero sheet,
    /// ChogHeroSheet, and RENT on the home screen open them). It shows the state kept by
    /// PlayerWallet, GuestPass, GenesisLoan, GenesisRental and GenesisCareers and builds its
    /// elements in code, as a child of the menu's main screen, so it appears and leaves with it.
    /// The two screens of the rental are in WalletPanel.Rental.cs.
    /// </summary>
    public sealed partial class WalletPanel : MonoBehaviour
    {
        private const string ObjectName = "ChogWalletPanel";
        private const string RosterResource = "GenesisRoster";
        private const int CollectionExampleMetadataId = 1078;
        private const int SlotsPerPage = 6;

        // The right column is one Genesis card and its wallet action. It replaces the old generic text slab
        // while keeping the same wallet, rental and career behaviour.
        // Its foot stands clear of the mode and PLAY buttons under it (ChogHome, their tops at 0.278), with room for them to grow
        // when hovered (22 October 2026: the card cut into them).
        private static readonly Vector2 PanelMin = new Vector2(0.56f, 0.305f);
        private static readonly Vector2 PanelMax = new Vector2(0.98f, 0.89f);

        // Without a Genesis to play: the main button wide, or beside the one for any other wallet app.
        private static readonly Vector2 ConnectMin = new Vector2(0.05f, 0.035f);
        private static readonly Vector2 ConnectMax = new Vector2(0.47f, 0.18f);
        private static readonly Vector2 ConnectBesideOtherMax = new Vector2(0.47f, 0.18f);
        private static readonly Vector2 ConnectBesideOtherMin = new Vector2(0.05f, 0.035f);

        private static readonly Vector2 WideMin = new Vector2(0.05f, 0.035f);
        private static readonly Vector2 WideMax = new Vector2(0.95f, 0.18f);

        // With a Genesis to play: its mastery road on top, the Genesis of the wallet under it.
        private static readonly Vector2 RoadMin = new Vector2(0.03f, 0.47f);
        private static readonly Vector2 RoadMax = new Vector2(0.97f, 0.97f);
        private static readonly Vector2 RosterMin = new Vector2(0f, 0f);
        private static readonly Vector2 RosterMax = new Vector2(1f, 0.44f);

        // The window the screens that lend, rent out and rent open in, over the whole menu.
        private static readonly Vector2 ModalMin = new Vector2(0.15f, 0.07f);
        private static readonly Vector2 ModalMax = new Vector2(0.85f, 0.93f);

        // The colours of ChogTheme (the Chog identity): the cheek pink for the thing to do, a muted
        // purple for the others, white letters on both.
        private static readonly Color AccentColor = ChogTheme.Cheek;
        private static readonly Color ButtonTextColor = Color.white;
        private static readonly Color QuietColor = ChogTheme.Quiet;
        private static readonly Color MutedColor = ChogTheme.Muted;
        private static readonly Color WaitingColor = new Color(0.25f, 0.25f, 0.3f, 0.85f);
        private const string CareerColor = "#FFD27A";
        private const string LabelColor = "#A9ADC2";

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string RecruitNote =
            "No Genesis? You play a grey recruit, with " + (100 - Recruit.HealthPercent) + "% less health and no Genesis attack.";

        private GenesisRoster _roster;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _address;
        private TextMeshProUGUI _message;
        private Button _connect;
        private TextMeshProUGUI _connectLabel;
        // On a phone, beside OPEN IN METAMASK: the window where any other wallet app is picked.
        private Button _otherApp;
        private Button _signIn;
        private TextMeshProUGUI _signInLabel;
        private Button _market;
        // The Genesis the panel shows as the one played, 0 for none.
        private int _played;

        // Before wallet selection, the card shows a labelled example from the real Genesis collection.
        // It never presents the example as a hero the visitor owns.
        private RectTransform _poster;
        private static readonly Vector2 MedallionSize = new Vector2(210f, 210f);
        private RawImage _posterPortrait;
        private TextMeshProUGUI _posterKicker;
        private TextMeshProUGUI _posterExampleLabel;
        private bool _posterPortraitRequested;
        // In place of the example, when holders offer Genesis for rent: the three cheapest.
        private RentalOffersCard _offers;

        // With a Genesis to play: its mastery road, then the Genesis of the wallet.
        private MasteryRoad _road;
        // The kit of the Genesis played, in place of its mastery road (22 October 2026, the owner: the road read as a window nobody
        // understood): its class, what its basic attack throws, its special.
        private RectTransform _kit;
        private readonly TextMeshProUGUI[] _kitTitles = new TextMeshProUGUI[3];
        private readonly TextMeshProUGUI[] _kitLines = new TextMeshProUGUI[3];
        private RectTransform _rosterArea;
        private TextMeshProUGUI _rosterTitle;
        private TextMeshProUGUI _rosterInfo;

        // The window over the whole menu where the screens that lend, rent out and rent open.
        private GameObject _modal;
        private RectTransform _modalCard;

        private static WalletPanel s_current;

        /// <summary>
        /// The number shown in the name of the Genesis the menu says will be played (its metadata id),
        /// 0 when the player enters as a recruit. The menu's hero stage shows it (ChogHome).
        /// </summary>
        public static int PlayedMetadataId { get; private set; }

        /// <summary>The token id of the Genesis the menu says will be played, 0 for a recruit.</summary>
        public static int PlayedTokenId { get; private set; }

        /// <summary>Raised when the Genesis the menu says will be played changes.</summary>
        public static event Action PlayedChanged;

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
        // The $CHOG account (balance, earnings) was last read for this address.
        private string _accountOf = "";
        // RENT was tapped before a wallet was connected: the offers open once it is.
        private bool _marketWhenReady;

        // The screen that lends a Genesis, laid over the menu.
        private GameObject _loanView;
        private TextMeshProUGUI _loanTitle;
        private TextMeshProUGUI _loanText;
        private TextMeshProUGUI _loanLink;
        private TextMeshProUGUI _loanNotice;
        private Button _share;
        private TextMeshProUGUI _shareLabel;
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

        /// <summary>
        /// The offers of Genesis for rent (RENT on the home screen). Without a connected wallet, the
        /// wallet is asked first and the offers open once it answers.
        /// </summary>
        public static void OpenRentals()
        {
            if (s_current == null)
                return;
            if (PlayerWallet.State == WalletState.Ready)
            {
                s_current.OpenMarket();
                return;
            }
            s_current._marketWhenReady = true;
            OnMainButton();
        }

        /// <summary>The offers, with this Genesis picked (a hero sheet's RENT IT).</summary>
        public static void OpenRentalOf(int tokenId)
        {
            OpenRentals();
            if (s_current != null && s_current._marketOpen)
            {
                s_current._marketPick = tokenId;
                s_current.Refresh();
            }
        }

        /// <summary>The screen where a holder offers this Genesis for rent (a hero sheet's RENT OUT).</summary>
        public static void OpenOfferOf(int tokenId) => s_current?.OpenOffer(tokenId);

        /// <summary>Connects the wallet, or opens the game in the wallet's browser on a phone without one.</summary>
        public static void ConnectWallet() => OnMainButton();

        /// <summary>The words of the main button of a wallet that is not ready: what tapping it does.</summary>
        public static string ConnectWords()
        {
            var state = PlayerWallet.State;
            if (PlayerWallet.CanOpenInWalletBrowser)
                return "OPEN IN METAMASK";
            switch (state)
            {
                case WalletState.Connecting:
                    return "ANSWER IN YOUR WALLET";
                case WalletState.Reading:
                    return "READING...";
                case WalletState.NoProvider:
                    return "NO WALLET HERE";
                case WalletState.Failed:
                    return "TRY AGAIN";
                default:
                    return "CONNECT WALLET";
            }
        }

        private void OnEnable()
        {
            s_current = this;
            PlayerWallet.Changed += Refresh;
            GenesisCareers.Changed += Refresh;
            GuestPass.Changed += Refresh;
            GenesisLoan.Changed += Refresh;
            GenesisForms.Changed += Refresh;
            GenesisShop.Changed += Refresh;
            GenesisRental.Changed += Refresh;
            GenesisWager.Changed += Refresh;
            ChogPrice.Changed += Refresh;
            if (_title != null)
            {
                // Back on the menu, for instance after a match: the careers and the money may have moved.
                _careersOf = "";
                _careerAskedFor = 0;
                _purchasesOf = "";
                _accountOf = "";
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
            ChogPrice.Changed -= Refresh;
            // The window of the panel belongs to the main screen: it leaves with it.
            if (_modal != null)
                _modal.SetActive(false);
        }

        private void Build()
        {
            s_current = this;
            _roster = Resources.Load<GenesisRoster>(RosterResource);

            var area = (RectTransform)transform;
            area.anchorMin = PanelMin;
            area.anchorMax = PanelMax;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            var panelImage = GetComponent<Image>();
            ChogTheme.AsCard(panelImage);
            panelImage.color = Color.clear;
            panelImage.raycastTarget = false;

            BuildPoster();

            // Without a Genesis to play: what to do to play one.
            _title = Text("Title", new Vector2(0.075f, 0.61f), new Vector2(0.56f, 0.79f), TextAlignmentOptions.Left, ChogTheme.Text, 38f);
            ChogTheme.AsTitle(_title);
            _address = Text("Address", new Vector2(0.075f, 0.36f), new Vector2(0.56f, 0.43f), TextAlignmentOptions.Left,
                ChogTheme.Purple, 22f);
            _message = Text("Message", new Vector2(0.075f, 0.30f), new Vector2(0.56f, 0.60f), TextAlignmentOptions.TopLeft,
                ChogTheme.Muted, 25f);

            _connect = MakeButton("Connect", ConnectMin, ConnectMax, out _connectLabel, kind: ChogTheme.ButtonKind.Secondary);
            _connect.onClick.AddListener(OnMainButton);
            _otherApp = MakeButton("OtherApp", new Vector2(0.53f, 0.035f), new Vector2(0.95f, 0.18f), out var otherAppLabel,
                kind: ChogTheme.ButtonKind.Quiet);
            otherAppLabel.text = "OTHER WALLET";
            _otherApp.onClick.AddListener(PlayerWallet.Connect);
            _signIn = MakeButton("SignIn", WideMin, WideMax, out _signInLabel, kind: ChogTheme.ButtonKind.Confirm);
            _signIn.onClick.AddListener(PlayerWallet.StartSignIn);

            // For a connected wallet without a Genesis: the Genesis that holders offer for rent.
            _market = MakeButton("Market", WideMin, WideMax, out var marketLabel, kind: ChogTheme.ButtonKind.Chog);
            marketLabel.text = "RENT A HERO";
            _market.onClick.AddListener(OpenMarket);

            // With a Genesis to play: its mastery, then the Genesis of the wallet, picked by a tap.
            _road = MasteryRoad.Create(transform, RoadMin, RoadMax);
            _road.gameObject.SetActive(false);
            _kit = Child("Kit", transform, RoadMin, RoadMax);
            for (var row = 0; row < 3; row++)
            {
                var top = 0.97f - row * 0.32f;
                _kitTitles[row] = ChogUi.Title("KitTitle" + row, _kit, new Vector2(0.05f, top - 0.13f), new Vector2(0.95f, top), 34f,
                    TextAlignmentOptions.Left);
                _kitTitles[row].color = row == 0 ? ChogTheme.Gold : ChogTheme.Cream;
                _kitLines[row] = ChogUi.Body("KitLine" + row, _kit, new Vector2(0.05f, top - 0.30f), new Vector2(0.95f, top - 0.13f), 22f,
                    TextAlignmentOptions.TopLeft, ChogTheme.PurpleSoft);
            }
            _rosterArea = Child("Roster", transform, RosterMin, RosterMax);
            var line = ChogUi.Plate("Line", _rosterArea, new Vector2(0.04f, 0.985f), new Vector2(0.96f, 1f), ChogTheme.Line, 2);
            line.sprite = null;
            _rosterTitle = ChogUi.Title("Title", _rosterArea, new Vector2(0.04f, 0.72f), new Vector2(0.50f, 0.95f), 36f, TextAlignmentOptions.Left);
            _rosterInfo = ChogUi.Body("Info", _rosterArea, new Vector2(0.46f, 0.72f), new Vector2(0.96f, 0.95f), 26f, TextAlignmentOptions.Right,
                ChogTheme.PurpleSoft);

            _previous = MakeButton("Previous", new Vector2(0.01f, 0.14f), new Vector2(0.075f, 0.62f), out var previousLabel, _rosterArea,
                ChogTheme.ButtonKind.Quiet);
            previousLabel.text = "<";
            _previous.onClick.AddListener(() => TurnPage(-1));
            _next = MakeButton("Next", new Vector2(0.925f, 0.14f), new Vector2(0.99f, 0.62f), out var nextLabel, _rosterArea,
                ChogTheme.ButtonKind.Quiet);
            nextLabel.text = ">";
            _next.onClick.AddListener(() => TurnPage(1));

            for (var i = 0; i < SlotsPerPage; i++)
                _slots[i] = MakeSlot(i, _rosterArea, PlayerWallet.Choose, 0.04f, 0.70f);

            // The window: a dim over the whole menu (a tap on it closes what is open), a card in the middle.
            var dim = ChogUi.Dim("ChogWalletWindow", transform.parent, 0.84f);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(CloseWindow);
            _modal = dim.gameObject;
            _modalCard = Child("Card", _modal.transform, ModalMin, ModalMax);
            // The card itself takes the taps, so only a tap beside it closes the window.
            _modalCard.gameObject.AddComponent<Image>().color = Color.clear;
            _modal.SetActive(false);

            BuildLoanView();
            BuildRentalViews();
            Refresh();
        }

        // The card has one outline and functional text. The portrait is real collection art, clearly
        // labelled as an example until a wallet has chosen its own Genesis. (22 October 2026: the cream
        // paper the owner found out of place became a raised card of the theme, as the other cards of the
        // home, the portrait a round medallion in a gold rim, as the $CHOG coin.)
        private void BuildPoster()
        {
            _poster = Child("GenesisCard", transform, new Vector2(0.01f, 0.18f), new Vector2(0.99f, 0.99f));
            ChogUi.Plate("Card", _poster, Vector2.zero, Vector2.one, ChogTheme.CardRaised, 12);

            // A fixed circle at the middle of the card's right half (an aspect fitter grew it over the text).
            var medallion = ChogUi.Pinned("Medallion", _poster, new Vector2(0.77f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, MedallionSize);
            var rim = medallion.gameObject.AddComponent<Image>();
            rim.sprite = ChogSprites.Disc(128, 0.02f);
            rim.color = ChogTheme.Ink;
            rim.raycastTarget = false;
            var gold = Child("Rim", medallion, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.975f)).gameObject.AddComponent<Image>();
            gold.sprite = ChogSprites.Disc(128, 0.02f);
            gold.color = ChogTheme.Action;
            gold.raycastTarget = false;
            _posterPortrait = Child("CollectionExample", medallion, new Vector2(0.09f, 0.09f), new Vector2(0.91f, 0.91f))
                .gameObject.AddComponent<RawImage>();
            _posterPortrait.texture = GenesisArt.Disc();
            _posterPortrait.color = WaitingColor;
            _posterPortrait.raycastTarget = false;

            _posterKicker = ChogUi.Number("Kicker", _poster, new Vector2(0.065f, 0.82f), new Vector2(0.55f, 0.92f), 22f,
                TextAlignmentOptions.Left, ChogTheme.Action, false);
            _posterExampleLabel = ChogUi.Body("ExampleLabel", _poster, new Vector2(0.59f, 0.10f), new Vector2(0.96f, 0.22f),
                19f, TextAlignmentOptions.Center, ChogTheme.PurpleSoft);
            // The right half of the card: the three cheapest Genesis for rent, in place of the example.
            _offers = RentalOffersCard.Create(_poster, new Vector2(0.56f, 0.05f), new Vector2(0.97f, 0.80f));
            _offers.gameObject.SetActive(false);
        }

        // Laid over the menu while a Genesis is being lent; it takes the taps of what it covers.
        private void BuildLoanView()
        {
            var cover = Child("Loan", _modalCard, Vector2.zero, Vector2.one);
            ChogTheme.AsCard(cover.gameObject.AddComponent<Image>(), raised: true);
            _loanView = cover.gameObject;

            _loanTitle = Text("Title", new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.97f), TextAlignmentOptions.Left, Color.white, 60f, cover);
            ChogTheme.AsTitle(_loanTitle);
            _loanText = Text("Text", new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.84f), TextAlignmentOptions.TopLeft, Color.white, 42f, cover);
            _loanLink = Text("Link", new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.49f), TextAlignmentOptions.TopLeft, MutedColor, 26f, cover);
            _loanLink.richText = false;
            _loanNotice = Text("Notice", new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.34f), TextAlignmentOptions.Left, ChogTheme.Ok, 38f, cover);

            _share = MakeButton("Share", new Vector2(0.04f, 0.05f), new Vector2(0.44f, 0.20f), out _shareLabel, cover,
                ChogTheme.ButtonKind.Confirm);
            // A computer copies the link; a phone hands it to its share sheet.
            _share.onClick.AddListener(() =>
            {
                if (ControlModeWatcher.Mode == ControlMode.Desktop)
                    GenesisLoan.Copy();
                else
                    GenesisLoan.Share(NameOf(GenesisLoan.TokenId));
            });
            _stop = MakeButton("Stop", new Vector2(0.47f, 0.05f), new Vector2(0.77f, 0.20f), out var stopLabel, cover, ChogTheme.ButtonKind.Danger);
            stopLabel.text = "STOP LENDING";
            _stop.onClick.AddListener(GenesisLoan.Stop);
            var back = MakeButton("Back", new Vector2(0.80f, 0.05f), new Vector2(0.96f, 0.20f), out var backLabel, cover, ChogTheme.ButtonKind.Quiet);
            backLabel.text = "BACK";
            back.onClick.AddListener(GenesisLoan.Close);
        }

        // A tap beside the window's card: what is open closes, as its BACK does.
        private void CloseWindow()
        {
            if (GenesisLoan.State != LoanState.Idle)
                GenesisLoan.Close();
            CloseRentalViews();
        }

        // Set when the player connects a wallet on this page: once it shows a Genesis, the signature
        // that lets the player play it is asked for at once, instead of waiting for a second button.
        private string _signInAskedFor = "";

        private void Refresh()
        {
            var state = PlayerWallet.State;
            // What the wallet may play: the Genesis it holds, then those it rents.
            var tokens = PlayerWallet.Playable;
            // A Genesis the owner's wallet picked with ADMIN (ChogHeroSheet) is shown and played like one it holds.
            var adminPick = PlayerWallet.IsAdmin && PlayerWallet.ChosenTokenId > 0 && Array.IndexOf(tokens, PlayerWallet.ChosenTokenId) < 0;
            // The owner sees the home exactly as a holder of that Genesis does (22 October 2026: no admin wording on it).
            if (adminPick)
            {
                var withPick = new int[tokens.Length + 1];
                Array.Copy(tokens, withPick, tokens.Length);
                withPick[tokens.Length] = PlayerWallet.ChosenTokenId;
                tokens = withPick;
            }
            var holds = state == WalletState.Ready && tokens.Length > 0;
            // A Genesis of the player's own wallet, held or rented, wins over one that was lent.
            var guest = !holds && GuestPass.State == GuestPassState.Valid;
            var checkingPass = !holds && GuestPass.State == GuestPassState.Checking;
            var played = holds ? PlayerWallet.ChosenTokenId : guest ? GuestPass.TokenId : 0;
            var owns = holds && (Array.IndexOf(PlayerWallet.Tokens, played) >= 0 || adminPick);
            var rents = holds && !owns && played > 0;

            // Careers are read once it is known which Genesis the panel shows, and again when a rental adds one.
            var careersOf = holds ? PlayerWallet.Address + " with " + tokens.Length + (adminPick ? " picking " + played : "")
                : guest ? "guest of " + GuestPass.TokenId : "";
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

            // The $CHOG of a connected wallet (its balance on the home screen, what it may claim).
            if (state == WalletState.Ready && GenesisRental.State == RentalState.Open && _accountOf != PlayerWallet.Address)
            {
                _accountOf = PlayerWallet.Address;
                GenesisRental.ReadAccount();
            }

            // RENT was tapped before the wallet answered: the offers open now, or never if it did not connect.
            if (_marketWhenReady && state == WalletState.Ready && GenesisRental.State == RentalState.Open)
            {
                _marketWhenReady = false;
                OpenMarket();
                return;
            }
            if (state == WalletState.Failed || state == WalletState.NoProvider)
                _marketWhenReady = false;

            var playing = holds || guest;
            _kit.gameObject.SetActive(playing);
            _rosterArea.gameObject.SetActive(playing);
            _title.gameObject.SetActive(!playing);
            // The address is on the wallet button at the top right already.
            _address.gameObject.SetActive(false);
            _message.gameObject.SetActive(!playing);

            _title.text = checkingPass ? "ONE MOMENT" : state == WalletState.Ready ? "NO GENESIS HERE" : "PLAY YOUR GENESIS";
            _address.text = PlayerWallet.ShortAddress(PlayerWallet.Address);
            _message.text = playing ? "" : checkingPass ? "Checking your invitation..." : PassProblem() + MessageFor(state);
            // On a phone browser without a wallet, the same button reopens the game where one is.
            var canOpen = PlayerWallet.CanOpenInWalletBrowser;
            _connectLabel.text = canOpen ? "OPEN IN METAMASK" : state == WalletState.Failed ? "TRY AGAIN" : "CONNECT WALLET";
            // The wallet button at the top right of the home connects (22 October 2026: the owner wants that
            // one only; this card's own CONNECT WALLET repeated it). The card keeps its words.
            _connect.gameObject.SetActive(false);
            var other = !guest && !checkingPass && canOpen && PlayerWallet.ThroughApp;
            _otherApp.gameObject.SetActive(other);
            var connectArea = (RectTransform)_connect.transform;
            connectArea.anchorMin = other ? ConnectBesideOtherMin : ConnectMin;
            connectArea.anchorMax = other ? ConnectBesideOtherMax : ConnectMax;
            // The RENT rail is the one collection route. The home card reports the account state;
            // it never duplicates that route with another large rental call to action.
            _market.gameObject.SetActive(false);
            var marketArea = (RectTransform)_market.transform;

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

            var previouslyPlayed = PlayedTokenId;
            _played = played;
            PlayedTokenId = played;
            var playedIndex = played > 0 && _roster != null ? _roster.IndexOf(played) : -1;
            PlayedMetadataId = playedIndex >= 0 ? _roster.Entries[playedIndex].MetadataId : 0;
            if (previouslyPlayed != played)
                PlayedChanged?.Invoke();
            RefreshPoster(playing);

            if (playing)
                ShowKit(played, playedIndex);

            _rosterTitle.text = guest ? "LENT TO YOU" : PlayerWallet.Tokens.Length > 0 || adminPick ? "YOUR GENESIS" : "YOUR RENTED GENESIS";
            _rosterInfo.text = guest ? "by " + PlayerWallet.ShortAddress(GuestPass.Lender) + ", " + GuestPass.TimeLeft(GuestPass.ExpiresAtMs, NowMs()) + " left"
                : rents ? RentedInfo(GenesisRental.RentedUntilSeconds(played), NowMs() / 1000)
                : tokens.Length > 1 ? (pages > 1 ? (_page + 1) + " / " + pages + "   " : "") + "tap one to play it"
                : "";

            // A signature records Genesis career or player rank. A connected recruit can sign
            // for rank points without making staking part of the home screen.
            var signedIn = PlayerWallet.SignIn == SignInState.SignedIn;
            _signIn.gameObject.SetActive(state == WalletState.Ready && !guest && !signedIn);
            var signInArea = (RectTransform)_signIn.transform;
            if (holds)
            {
                // Over the right of the roster's title line.
                _signIn.transform.SetParent(_rosterArea, false);
                signInArea.anchorMin = new Vector2(0.48f, 0.72f);
                signInArea.anchorMax = new Vector2(0.96f, 0.99f);
                _rosterInfo.gameObject.SetActive(signedIn);
            }
            else
            {
                _signIn.transform.SetParent(transform, false);
                signInArea.anchorMin = WideMin;
                signInArea.anchorMax = WideMax;
                marketArea.anchorMin = WideMin;
                marketArea.anchorMax = WideMax;
                _rosterInfo.gameObject.SetActive(true);
            }
            signInArea.offsetMin = Vector2.zero;
            signInArea.offsetMax = Vector2.zero;
            _signIn.interactable = PlayerWallet.SignIn != SignInState.Asking;
            _signInLabel.text = PlayerWallet.SignIn == SignInState.Asking ? "ANSWER IN YOUR WALLET..."
                : PlayerWallet.SignInProblem.Length > 0 ? "SIGN-IN FAILED, TRY AGAIN"
                : holds ? (PhotonIdentity.SignInNeededToPlay ? "SIGN IN TO PLAY IT" : "SIGN IN TO RECORD ITS CAREER")
                : "SIGN ONCE TO EARN RANK";
            if (_poster != null && _poster.gameObject.activeSelf)
            {
                // When no wallet action is available, the poster fills the reserved footer instead of
                // leaving a floating card above the mode and PLAY group.
                var footer = _connect.gameObject.activeSelf || _otherApp.gameObject.activeSelf ||
                    _market.gameObject.activeSelf || _signIn.gameObject.activeSelf;
                _poster.anchorMin = new Vector2(0.01f, footer ? 0.18f : 0.01f);
            }
            // The signature is asked by itself whenever this wallet has no session: right after
            // connecting, after a rental, after a switch of account; for a wallet without a Genesis too
            // (22 October 2026: a connected player saw "SIGN IN FOR RANK" and took it for a second
            // connection; its rank points need the same one signature). Also when the page opens on a wallet
            // already connected (the owner, 22 October: a new address of the game had no session kept, and the
            // button was one click too many). Once per address in a visit, so a player who closes the wallet
            // is not asked again and keeps the button.
            var wantsSession = holds ? PhotonIdentity.SignInNeededToPlay : state == WalletState.Ready && !guest;
            if (wantsSession && PlayerWallet.SignIn == SignInState.SignedOut
                && PlayerWallet.SignInProblem.Length == 0 && _signInAskedFor != PlayerWallet.Address)
            {
                _signInAskedFor = PlayerWallet.Address;
                PlayerWallet.StartSignIn();
            }

            // A wallet with more Genesis than one read covers: the chosen one is asked for by itself.
            var chosen = PlayerWallet.ChosenTokenId;
            if (holds && chosen != _careerAskedFor && Array.IndexOf(tokens, chosen) >= GenesisCareers.MaxTokensPerRead)
            {
                _careerAskedFor = chosen;
                GenesisCareers.Read(new[] { chosen });
            }

            RefreshLoanView();
            RefreshRentalViews();

            // The window shows while one of its screens is open, over everything else of the menu.
            var open = _loanView.activeSelf || _offerView.activeSelf || _marketView.activeSelf;
            if (open && !_modal.activeSelf)
                _modal.transform.SetAsLastSibling();
            _modal.SetActive(open);
        }

        private void RefreshPoster(bool playing)
        {
            if (_poster == null)
                return;

            GetComponent<Image>().color = playing ? Color.white : Color.clear;
            // The owned-roster view remains the interaction for changing an equipped Genesis.
            _poster.gameObject.SetActive(!playing);
            if (playing)
                return;

            _posterKicker.text = "1,969 CHOG GENESIS HEROES";
            _posterExampleLabel.text = "EXAMPLE: CHOG #1078";
            // Real Genesis for rent take the place of the example whenever holders offer some.
            var offered = GenesisRental.State == RentalState.Open && GenesisRental.Cheapest(1).Count > 0;
            _offers.gameObject.SetActive(offered);
            _posterPortrait.gameObject.SetActive(!offered);
            _posterExampleLabel.gameObject.SetActive(!offered);
            if (_posterPortraitRequested)
                return;
            _posterPortraitRequested = true;
            GenesisArt.Request(CollectionExampleMetadataId, texture =>
            {
                if (_posterPortrait != null)
                {
                    _posterPortrait.texture = texture;
                    _posterPortrait.color = Color.white;
                }
            });
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
            _shareLabel.text = ControlModeWatcher.Mode == ControlMode.Desktop ? "COPY THE LINK" : "SHARE THE LINK";
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
                    return "Open this page in MetaMask, Rabby or Backpack to play a Genesis you hold.";
                case WalletState.Disconnected:
                    return "Connect the wallet that owns your Chog Genesis.";
                case WalletState.Connecting:
                    return PlayerWallet.ThroughApp ? "Pick your wallet app and approve there, then come back here..."
                        : "Answer in your wallet...";
                case WalletState.Reading:
                    return "Reading your Genesis on Monad...";
                case WalletState.Failed:
                    return PlayerWallet.Problem;
                default:
                    return "This wallet holds no Chog Genesis." + (GenesisRental.State == RentalState.Open ? " Browse RENT in the left menu to play one." : "");
            }
        }

        // The kit of the Genesis played: its class (from its outfit), what its basic attack throws, its special (from its eyes).
        private void ShowKit(int tokenId, int index)
        {
            if (_roster == null || index < 0)
            {
                for (var row = 0; row < 3; row++)
                    _kitTitles[row].text = _kitLines[row].text = "";
                return;
            }
            var hero = _roster.Entries[index];
            var lines = _roster.CardLines(hero, GenesisForms.PlayedBy(tokenId));
            var roleId = (lines[0].Gives ?? "").ToLowerInvariant();
            var attackLine = ChogHeroModel.AttackLine(hero.MetadataId);
            var thrown = ChogHeroModel.AttackWords.Parts(attackLine).thing;
            var kind = ChogHeroModel.AttackWords.Kind(roleId);
            _kitTitles[0].text = (lines[0].Gives ?? "").ToUpperInvariant();
            _kitLines[0].text = "Outfit: " + lines[0].Value;
            _kitTitles[1].text = "BASIC: " + (thrown.Length > 0 ? thrown : kind);
            var detail = ChogHeroModel.AttackWords.Detail(attackLine, roleId);
            _kitLines[1].text = detail.Length > 0 ? kind + ". " + detail : kind;
            _kitTitles[2].text = "SPECIAL: " + (lines[1].Gives ?? "").ToUpperInvariant();
            _kitLines[2].text = lines[1].Value == "none" ? "No eyes trait." : "Eyes: " + lines[1].Value + ".";
        }

        private static long NowMs() => (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;

        /// <summary>
        /// The line under the roster for a rented Genesis: the time left, and, under twelve hours,
        /// a reminder in the problem colour to add days (RENT), as the references put a timer on
        /// what runs out (the offers of Brawl Stars' shop, the chests of Clash Royale).
        /// </summary>
        public static string RentedInfo(long untilSeconds, long nowSeconds)
        {
            var left = GenesisRental.TimeLeft(untilSeconds, nowSeconds);
            return GenesisRental.EndsSoon(untilSeconds, nowSeconds)
                ? "<color=" + ProblemColor + ">rental ends in " + left + ": add days in RENT</color>"
                : "rented, " + left + " left";
        }

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

            slot.Ring.color = tokenId == played ? ChogTheme.Action : Color.clear;
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

        private Slot MakeSlot(int position, RectTransform parent, Action<int> tapped, float bottom = 0.48f, float top = 0.85f)
        {
            // Six equal cells between the two page arrows; each holds a square, whatever the screen.
            const float left = 0.08f, right = 0.92f;
            var width = (right - left) / SlotsPerPage;
            var cell = Child("Slot" + position, parent, new Vector2(left + position * width, bottom),
                new Vector2(left + (position + 1) * width, top));
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

        private Button MakeButton(string name, Vector2 min, Vector2 max, out TextMeshProUGUI label, RectTransform parent = null,
                                  ChogTheme.ButtonKind kind = ChogTheme.ButtonKind.Chog)
        {
            var area = Child(name, parent == null ? transform : parent, min, max);
            var background = area.gameObject.AddComponent<Image>();
            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            label = Text("Label", new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f), TextAlignmentOptions.Center, ButtonTextColor, 52f, area);
            ChogTheme.Style(button, kind);
            if (parent == null)
                ChogTheme.Paint(button, kind);
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
            ChogTheme.AsBody(text);
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
