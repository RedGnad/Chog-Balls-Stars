using System.Collections;
using System;
using System.Globalization;
using System.Reflection;
using Chog.Match;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using GenesisRoster = Quantum.GenesisRoster;

namespace Chog.Screens
{
    /// <summary>
    /// The home screen, as the validated mockup lays it out (docs/DESIGN-BRIEF.md): the player's chip at
    /// the top left (the picture and mastery of the Genesis played, the player's name, which a tap
    /// changes), the logo in the middle, the $CHOG held, the wallet and the settings at the top right;
    /// a rail of HEROES, TOP, RENT, FRIENDS and GUIDE down the left; the hero in 3D on a gold pedestal
    /// in the middle with its name on a ribbon and its kit on chips (a tap opens its sheet); the card of
    /// the wallet at the right (WalletPanel); the mode and a large PLAY at the bottom right. The sample's
    /// own buttons keep their behaviour: their screens are opened through the same methods.
    /// </summary>
    public sealed class ChogHome : MonoBehaviour
    {
        private const string ObjectName = "ChogHome";
        private const string RosterResource = "GenesisRoster";
        private Coroutine _rankRefresh;
        private Coroutine _rankReveal;
        private RectTransform _rankToast;
        private CanvasGroup _rankToastGroup;
        private TextMeshProUGUI _rankToastTitle;
        private TextMeshProUGUI _rankToastPoints;
        private TextMeshProUGUI _rankToastNext;
        private RectTransform _rankToastFill;

        // In units of the menu canvas (1920 by 1080 at 16:9), from the corners.
        private const float Margin = 30f;
        private static readonly Vector2 ChipSize = new Vector2(390f, 104f);
        private static readonly Vector2 SettingsSize = new Vector2(92f, 92f);
        private static readonly Vector2 WalletSize = new Vector2(286f, 78f);
        private static readonly Vector2 ChogSize = new Vector2(198f, 68f);
        // The rail uses the otherwise empty column down to the kit chips, not only the top third.
        private static readonly Vector2 RailGridSize = new Vector2(164f, 832f);
        private const float RailTop = 152f;
        private const float RailItemHeight = 160f;
        private const float RailSpacing = 8f;

        // In shares of the screen: the hero owns the left focal column, the Genesis card owns the right.
        private static readonly Vector2 StageMin = new Vector2(0.10f, 0.18f);
        private static readonly Vector2 StageMax = new Vector2(0.51f, 0.86f);
        private static readonly Vector2 RibbonMin = new Vector2(0.20f, 0.122f);
        private static readonly Vector2 RibbonMax = new Vector2(0.42f, 0.168f);
        private static readonly Vector2 ChipsMin = new Vector2(0.10f, 0.035f);
        private static readonly Vector2 ChipsMax = new Vector2(0.53f, 0.095f);
        // The action row shares the exact left and right edges of WalletPanel's card.
        public static readonly Vector2 ModeMin = new Vector2(0.56f, 0.105f);
        public static readonly Vector2 ModeMax = new Vector2(0.72f, 0.278f);
        public static readonly Vector2 PlayMin = new Vector2(0.735f, 0.105f);
        public static readonly Vector2 PlayMax = new Vector2(0.98f, 0.278f);

        private static readonly Color Lilac = ChogTheme.Hex("E9E4FF");

        private QuantumMenuUIMain _main;
        private GenesisRoster _roster;
        // The sample's label of the player's name: a TextMesh Pro text or a legacy one, as its build chose.
        private Component _usernameLabel;
        private GameObject _usernameView;

        private ChogHeroStage _stage;
        private GameObject _betTag;
        private TextMeshProUGUI _ribbonName;
        private TextMeshProUGUI[] _chips;

        private Image _portraitPlate;
        private RawImage _portrait;
        private Image _recruitIcon;
        private GameObject _levelBadge;
        private TextMeshProUGUI _level;
        private TextMeshProUGUI _playerName;
        private Image _roleBadge;
        private TextMeshProUGUI _roleText;
        private Image _rankBadge;
        private TextMeshProUGUI _rankText;

        private Button _wallet;
        private Image _walletDot;
        private TextMeshProUGUI _walletWords;
        private GameObject _chogPill;
        private TextMeshProUGUI _chogWords;

        private int _portraitFor = -1;
        private string _shownName = "";
        // The Genesis the home showed last: a new one may come to life (ChogReveal).
        private int _lastToken = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Attach();
            Attach();
        }

        private static void Attach()
        {
            var main = FindAnyObjectByType<QuantumMenuUIMain>(FindObjectsInactive.Include);
            if (main == null || main.transform.Find(ObjectName) != null)
                return;

            var root = ChogUi.Area(ObjectName, main.transform, Vector2.zero, Vector2.one);
            // Over the night of the screen, under the sample's buttons and its pop-ups.
            var background = main.transform.Find("Background");
            root.SetSiblingIndex(background != null ? background.GetSiblingIndex() + 1 : 0);
            root.gameObject.AddComponent<ChogHome>().Build(main);
        }

        private void Build(QuantumMenuUIMain main)
        {
            _main = main;
            _roster = Resources.Load<GenesisRoster>(RosterResource);
            _usernameLabel = Field<Component>("_usernameLabel");
            _usernameView = Field<GameObject>("_usernameView");

            // The sample's top buttons (the name and the settings) are drawn by this screen instead.
            var top = main.transform.Find("TopButtons");
            if (top != null)
                top.gameObject.SetActive(false);

            BuildBackdrop();
            BuildChip();
            BuildTopRight();
            BuildRail();
            BuildStage();
            ArrangePlay();
            Refresh();
        }

        private void OnEnable()
        {
            WalletPanel.PlayedChanged += Refresh;
            PlayerWallet.Changed += OnWalletChanged;
            PlayerRanks.Changed += Refresh;
            GenesisCareers.Changed += Refresh;
            GenesisRental.Changed += Refresh;
            GenesisForms.Changed += Refresh;
            ChogPrice.Changed += Refresh;
            if (_ribbonName != null)
                Refresh();
            RestartRankRefresh();
        }

        private void OnDisable()
        {
            if (_rankRefresh != null)
            {
                StopCoroutine(_rankRefresh);
                _rankRefresh = null;
            }
            if (_rankReveal != null)
            {
                StopCoroutine(_rankReveal);
                _rankReveal = null;
            }
            if (_rankToast != null)
                _rankToast.gameObject.SetActive(false);
            WalletPanel.PlayedChanged -= Refresh;
            PlayerWallet.Changed -= OnWalletChanged;
            PlayerRanks.Changed -= Refresh;
            GenesisCareers.Changed -= Refresh;
            GenesisRental.Changed -= Refresh;
            GenesisForms.Changed -= Refresh;
            ChogPrice.Changed -= Refresh;
        }

        // The mural is a separate background layer based on the approved Chog-head direction. It contains
        // no controls or character art: the real stage and the live menu stay above it.
        private void BuildBackdrop()
        {
            var art = ChogUi.Area("BackdropArt", transform, Vector2.zero, Vector2.one);
            art.SetAsFirstSibling();

            var wash = art.gameObject.AddComponent<Image>();
            wash.sprite = ChogSprites.Gradient(ChogTheme.Hex("100A22"), ChogTheme.Hex("26184A"));
            wash.type = Image.Type.Sliced;
            wash.raycastTarget = false;

            var muralTexture = Resources.Load<Texture2D>("MenuMural");
            if (muralTexture != null)
            {
                var mural = ChogUi.Area("ChogMural", art, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
                mural.texture = muralTexture;
                mural.color = new Color(1f, 1f, 1f, 0.50f);
                mural.raycastTarget = false;
            }
        }

        // The player's chip: the Genesis played (or the recruit), its level, role and player's name.
        private void BuildChip()
        {
            var chip = ChogUi.PinnedButton("Chip", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -24f), ChipSize,
                ChogTheme.ButtonKind.Quiet, "", 30f);
            var plate = ChogTheme.ImageOf(chip);
            ChogTheme.AsCard(plate, raised: true);
            // The chip opens the player's profile, which can also rename (22 October 2026).
            chip.onClick.AddListener(() => ChogProfile.Open(_main.transform, PlayerName, Rename));

            var portrait = ChogUi.Pinned("Portrait", chip.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f),
                new Vector2(90f, 90f));
            _portraitPlate = ChogUi.Plate("Frame", portrait, Vector2.zero, Vector2.one, ChogTheme.Action, 14);
            _portrait = ChogUi.Area("Picture", portrait, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            _portrait.rectTransform.offsetMin = new Vector2(6f, 6f);
            _portrait.rectTransform.offsetMax = new Vector2(-6f, -6f);
            _portrait.raycastTarget = false;
            _recruitIcon = ChogUi.Icon("Recruit", portrait, ChogIcons.Icon.Heroes, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f));

            var badge = ChogUi.Pinned("Level", portrait, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-4f, 6f), new Vector2(44f, 44f));
            ChogUi.Plate("Plate", badge, Vector2.zero, Vector2.one, ChogTheme.Purple, 10);
            _level = ChogUi.Title("Number", badge, new Vector2(0f, 0.06f), Vector2.one, 30f);
            _levelBadge = badge.gameObject;

            _playerName = ChogUi.Title("Name", chip.transform, new Vector2(0.285f, 0.50f), new Vector2(0.96f, 0.92f), 34f, TextAlignmentOptions.Left);
            _playerName.textWrappingMode = TextWrappingModes.NoWrap;
            _roleBadge = ChogUi.Plate("Role", chip.transform, new Vector2(0.285f, 0.10f), new Vector2(0.59f, 0.43f), ChogTheme.Action, 10);
            _roleText = ChogUi.Title("Words", _roleBadge.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.92f), 22f);
            _roleText.textWrappingMode = TextWrappingModes.NoWrap;
            _rankBadge = ChogUi.Plate("PlayerRank", chip.transform, new Vector2(0.61f, 0.10f), new Vector2(0.97f, 0.43f), ChogTheme.Purple, 10);
            _rankText = ChogUi.Title("Rank", _rankBadge.transform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.92f), 17f);
            _rankText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // The $CHOG held, the wallet, the settings, from the top right corner.
        private void BuildTopRight()
        {
            var settings = ChogUi.PinnedButton("Settings", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Margin, -26f),
                SettingsSize, ChogTheme.ButtonKind.Quiet, "", 30f, ChogIcons.Icon.Gear);
            ChogTheme.Paint(settings, ChogTheme.ButtonKind.Quiet);
            settings.onClick.AddListener(() => ChogSettings.Open(_main.transform));

            var walletRight = -Margin - SettingsSize.x - 14f;
            _wallet = ChogUi.PinnedButton("Wallet", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(walletRight, -29f),
                WalletSize, ChogTheme.ButtonKind.Quiet, "", 30f);
            ChogTheme.Paint(_wallet, ChogTheme.ButtonKind.Quiet);
            _wallet.onClick.AddListener(() =>
            {
                if (PlayerWallet.State != WalletState.Ready)
                    WalletPanel.ConnectWallet();
            });
            var dot = ChogUi.Pinned("Dot", _wallet.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 2f), new Vector2(18f, 18f));
            _walletDot = dot.gameObject.AddComponent<Image>();
            _walletDot.sprite = ChogSprites.Disc(32, 0.15f);
            _walletDot.color = ChogTheme.Ok;
            _walletDot.raycastTarget = false;
            _walletWords = ChogUi.Number("Words", _wallet.transform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.88f), 28f,
                TextAlignmentOptions.Center, Color.white);

            // The $CHOG of the connected wallet; a tap opens what it pays for, the rentals.
            var chogRight = walletRight - WalletSize.x - 14f;
            var chog = ChogUi.PinnedButton("Chog", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(chogRight, -29f), ChogSize,
                ChogTheme.ButtonKind.Quiet, "", 30f);
            ChogTheme.Paint(chog, ChogTheme.ButtonKind.Quiet);
            chog.onClick.AddListener(WalletPanel.OpenRentals);
            var coin = ChogUi.Pinned("Coin", chog.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 2f), new Vector2(52f, 52f));
            // The coin of the token itself (22 October 2026: the logo $CHOG has on CoinGecko, the Chog
            // in a gold rim), a coin as Supercell draws its currencies rather than a letter.
            var token = Resources.Load<Texture2D>("ChogTokenCoin");
            if (token != null)
            {
                var picture = coin.gameObject.AddComponent<Image>();
                picture.sprite = Sprite.Create(token, new Rect(0f, 0f, token.width, token.height), new Vector2(0.5f, 0.5f), 100f);
                picture.preserveAspect = true;
                picture.raycastTarget = false;
            }
            else
            {
                ChogUi.Plate("Plate", coin, Vector2.zero, Vector2.one, ChogTheme.Cheek, 26);
                ChogUi.Title("Letter", coin, new Vector2(0f, 0.06f), Vector2.one, 30f).text = "C";
            }
            _chogWords = ChogUi.Number("Amount", chog.transform, new Vector2(0.33f, 0.14f), new Vector2(0.94f, 0.88f), 30f,
                TextAlignmentOptions.Center, Color.white);
            _chogPill = chog.gameObject;
        }

        // The rail down the left: the collection, the leaderboard, the rentals, friends, the guide.
        private void BuildRail()
        {
            var rail = ChogUi.Pinned("RailGrid", transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(Margin, -RailTop), RailGridSize);
            var layout = rail.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = RailSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Rail(rail, "HEROES", ChogTheme.ButtonKind.Secondary, ChogIcons.Icon.Heroes, () => ChogCollection.Open(_main.transform));
            Rail(rail, "TOP", ChogTheme.ButtonKind.Primary, ChogIcons.Icon.Trophy, () => ChogHall.Open(_main.transform));
            Rail(rail, "RENT", ChogTheme.ButtonKind.Chog, ChogIcons.Icon.Key, WalletPanel.OpenRentals);
            Rail(rail, "FRIENDS", ChogTheme.ButtonKind.Secondary, ChogIcons.Icon.Friends, () => Call("OnPartyButtonPressed"));
            Rail(rail, "GUIDE", ChogTheme.ButtonKind.Quiet, ChogIcons.Icon.Question, () => ChogGuide.Open(_main.transform));
        }

        private Button Rail(Transform rail, string label, ChogTheme.ButtonKind kind, ChogIcons.Icon icon, UnityEngine.Events.UnityAction tapped)
        {
            var item = new GameObject("Rail" + label, typeof(RectTransform)).GetComponent<RectTransform>();
            item.SetParent(rail, false);
            var size = item.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = RailItemHeight;
            size.minHeight = RailItemHeight;
            var button = ChogUi.Button("Button", item, new Vector2(0.025f, 0.235f), new Vector2(0.975f, 1f), kind, "", 30f, icon);
            ChogTheme.Paint(button, kind);
            button.onClick.AddListener(tapped);
            ChogUi.Title("Label", item, new Vector2(0f, 0f), new Vector2(1f, 0.21f), 24f).text = label;
            return button;
        }

        // The hero on its pedestal, a warm light behind it, its name on a gold ribbon and its kit on chips.
        private void BuildStage()
        {
            var area = ChogUi.Area("Stage", transform, StageMin, StageMax);
            var glow = ChogUi.Area("Glow", area, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.77f)).gameObject.AddComponent<Image>();
            glow.sprite = ChogSprites.Disc(128, 1f);
            glow.color = new Color(1f, 0.72f, 0.24f, 0.22f);
            glow.raycastTarget = false;
            // (22 October 2026: the flat pink floor that stood here, from before the 3D pedestal, showed as a
            // second pink oval behind it; the pedestal carries its own contact paint.)
            _stage = ChogHeroStage.Create(ChogUi.Area("Hero", area, Vector2.zero, Vector2.one));
            // Dragging across the hero turns it, as on its sheet; a tap still opens the sheet (a drag
            // cancels the tap of the button below).
            _stage.Draggable = true;

            // The whole stage opens the hero's sheet.
            var tap = area.gameObject.AddComponent<Image>();
            tap.color = Color.clear;
            area.gameObject.AddComponent<Button>().onClick.AddListener(OpenHeroes);

            // The selected hero or recruit gets the same gold role badge language as the player chip.
            var ribbon = ChogUi.Plate("Ribbon", transform, RibbonMin, RibbonMax, ChogTheme.Action, 12);
            _ribbonName = ChogUi.Title("Name", ribbon.transform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.94f), 36f);
            _ribbonName.textWrappingMode = TextWrappingModes.NoWrap;

            var chips = ChogUi.Row("Chips", transform, ChipsMin, ChipsMax, 12f);
            _chips = new TextMeshProUGUI[4];
            for (var i = 0; i < _chips.Length; i++)
                _chips[i] = ChogUi.Chip("Chip" + i, chips, "", 24f);
        }

        // PLAY large at the bottom right, beside the mode; PLAY WITH FRIENDS moves to the rail (FRIENDS).
        private void ArrangePlay()
        {
            var play = Field<Button>("_playButton");
            var party = Field<Button>("_partyButton");
            var column = play != null ? play.transform.parent as RectTransform : null;
            if (column != null)
            {
                // The column covers the screen and lays nothing out: the button is placed in screen shares.
                foreach (var layout in column.GetComponents<LayoutGroup>())
                    layout.enabled = false;
                foreach (var fitter in column.GetComponents<ContentSizeFitter>())
                    fitter.enabled = false;
                ChogUi.Stretch(column, Vector2.zero, Vector2.one);
            }
            if (play != null)
            {
                foreach (var element in play.GetComponents<LayoutElement>())
                    element.ignoreLayout = true;
                ChogUi.Stretch((RectTransform)play.transform, PlayMin, PlayMax);
                ChogTheme.Style(play, ChogTheme.ButtonKind.Primary);
                ChogTheme.PaintRelief(play, ChogTheme.ButtonKind.Primary);
                var label = play.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "PLAY";
                    label.alignment = TextAlignmentOptions.Center;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 14f;
                    label.fontSizeMax = 86f;
                    var area = label.rectTransform;
                    area.anchorMin = Vector2.zero;
                    area.anchorMax = Vector2.one;
                    area.offsetMin = new Vector2(12f, 8f);
                    area.offsetMax = new Vector2(-12f, -4f);
                }

                // The game's promise on the button everyone looks at: a match can be played for $CHOG (23 October
                // 2026, the product walk: the bet appeared nowhere before the preparation screen). A tag over the
                // corner of PLAY, tilted like a sticker; shown only to a connected wallet while the services take
                // stakes (Update), as a visitor without a wallet cannot stake.
                if (play.transform.Find("ChogBetTag") == null)
                {
                    var tag = ChogUi.Plate("ChogBetTag", play.transform, new Vector2(0.40f, 0.80f), new Vector2(0.99f, 1.10f), ChogTheme.Cheek, 14);
                    tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
                    ChogUi.Icon("Coin", tag.transform, ChogIcons.Icon.Coin, new Vector2(0.03f, 0.12f), new Vector2(0.23f, 0.88f)).raycastTarget = false;
                    var words = ChogUi.Title("Words", tag.transform, new Vector2(0.22f, 0.06f), new Vector2(0.97f, 0.94f), 30f);
                    words.text = "BET $CHOG";
                    words.raycastTarget = false;
                    _betTag = tag.gameObject;
                    _betTag.SetActive(false);
                }
            }
            if (party != null)
                party.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_betTag != null)
            {
                // "?bettag=1" on a page of this computer shows it without a wager, to look at its design.
                var canBet = PlayerWallet.State == WalletState.Ready && GenesisWager.State == WagerState.Open
                             || LocalGenesis.TestFlag(Application.absoluteURL, Application.isEditor, "bettag");
                if (_betTag.activeSelf != canBet)
                    _betTag.SetActive(canBet);
            }
            if (_stage == null)
                return;
            var played = WalletPanel.PlayedMetadataId;
            _stage.Show(played > 0 && ChogHeroModel.Has(played) ? played : ChogHeroModel.Recruit);

            // The sample writes the player's name when its screen shows and after a change.
            var name = PlayerName();
            if (name != _shownName)
            {
                _shownName = name;
                _playerName.text = name.Length > 0 ? name.ToUpperInvariant() : "PLAYER";
            }
        }

        private void OnWalletChanged()
        {
            if (_rankReveal != null)
            {
                StopCoroutine(_rankReveal);
                _rankReveal = null;
            }
            if (_rankToast != null)
                _rankToast.gameObject.SetActive(false);
            Refresh();
            RestartRankRefresh();
        }

        private void RestartRankRefresh()
        {
            if (_rankRefresh != null)
                StopCoroutine(_rankRefresh);
            _rankRefresh = StartCoroutine(RefreshPendingRank());
        }

        // The exact finished room is retained across leaving the result and page reloads.
        // Never animate a different match or an unverified point change.
        private IEnumerator RefreshPendingRank()
        {
            var address = PlayerWallet.Address;
            var gameId = PlayerWallet.SignIn == SignInState.SignedIn
                ? ChogResultScreen.PendingRankGameId(address) : "";
            for (var attempt = 0; attempt < 90 && isActiveAndEnabled && !string.IsNullOrEmpty(gameId);)
            {
                if (!string.Equals(address, PlayerWallet.Address, StringComparison.OrdinalIgnoreCase) ||
                    PlayerWallet.SignIn != SignInState.SignedIn)
                    break;
                if (ChogResultScreen.PendingRankGameId(address) != gameId)
                    break;
                if (ChogResultScreen.IsShown)
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                    continue;
                }
                attempt++;
                PlayerRankAnswer answer = null;
                var returned = false;
                PlayerRanks.Read(value => { answer = value; returned = true; });
                var deadline = Time.realtimeSinceStartup + 10f;
                while (!returned && Time.realtimeSinceStartup < deadline && isActiveAndEnabled)
                    yield return null;
                if (answer != null && !answer.enabled)
                {
                    ChogResultScreen.ClearPendingRank(address, gameId);
                    break;
                }
                var gain = answer?.rank?.last;
                if (gain != null && gain.gameId == gameId)
                {
                    // The result screen already played these points the moment the siege ended.
                    if ((gain.delta != 0 || gain.tierBefore != gain.tierAfter) && !ChogResultScreen.WasRevealed(gameId))
                    {
                        _rankReveal = StartCoroutine(RevealRankOnHome(gain, answer.rank, address, gameId));
                        yield return _rankReveal;
                        _rankReveal = null;
                    }
                    else
                        ChogResultScreen.ClearPendingRank(address, gameId);
                    break;
                }
                var match = answer?.latestMatch;
                if (match != null && match.gameId == gameId &&
                    match.status != "verifying" && match.status != "preparing")
                {
                    ChogResultScreen.ClearPendingRank(address, gameId);
                    break;
                }
                yield return new WaitForSecondsRealtime(3f);
            }
            _rankRefresh = null;
        }

        private void BuildRankToast()
        {
            _rankToast = ChogUi.Area("RankUpdate", transform, new Vector2(0.28f, 0.72f), new Vector2(0.72f, 0.92f));
            var panel = _rankToast.gameObject.AddComponent<Image>();
            panel.sprite = ChogSprites.Rounded(24, 4, ChogTheme.Ink, ChogTheme.CardRaised);
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;
            _rankToastGroup = _rankToast.gameObject.AddComponent<CanvasGroup>();
            _rankToastGroup.blocksRaycasts = false;
            _rankToastTitle = ChogUi.Title("RankTitle", _rankToast, new Vector2(0.04f, 0.67f), new Vector2(0.96f, 0.94f), 38f);
            _rankToastTitle.color = ChogTheme.Gold;
            _rankToastPoints = ChogUi.Title("RankPoints", _rankToast, new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.70f), 60f);
            var track = ChogUi.Plate("ProgressTrack", _rankToast, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.32f), ChogTheme.Night, 12);
            _rankToastFill = ChogUi.Area("ProgressFill", track.transform, Vector2.zero, new Vector2(0.001f, 1f));
            var fill = _rankToastFill.gameObject.AddComponent<Image>();
            fill.sprite = ChogSprites.Rounded(16, 3, ChogTheme.Ink, ChogTheme.Gold);
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            _rankToastNext = ChogUi.Body("NextRank", _rankToast, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f),
                23f, TextAlignmentOptions.Center, ChogTheme.Cream);
            _rankToast.gameObject.SetActive(false);
        }

        private IEnumerator RevealRankOnHome(PlayerRankGain gain, PlayerRank rank, string address, string gameId)
        {
            if (_rankToast == null)
                BuildRankToast();
            _rankToast.SetAsLastSibling();
            _rankToast.gameObject.SetActive(true);
            _rankToastTitle.text = gain.tierBefore != gain.tierAfter
                ? (gain.delta > 0 ? "RANK UP: " : "RANK CHANGED: ") + gain.tierAfter : "MATCH RANK UPDATE";
            _rankToastNext.text = rank.next > 0 ? "NEXT RANK AT " + rank.next + " POINTS" : "TOP RANK";
            var start = Time.unscaledTime;
            const float duration = 0.80f;
            ChogSounds.Reward(gain.tierBefore != gain.tierAfter ? 2 : 0);
            while (Time.unscaledTime - start < duration)
            {
                var t = ChogTheme.ReducedMotion ? 1f : Mathf.Clamp01((Time.unscaledTime - start) / duration);
                var eased = t * t * (3f - 2f * t);
                var displayed = Mathf.RoundToInt(Mathf.Lerp(gain.before, gain.after, eased));
                _rankToastPoints.text = (gain.delta > 0 ? "+" : "") + gain.delta + " PTS  |  " + displayed + " TOTAL";
                _rankToastFill.anchorMax = new Vector2(rank.next > 0
                    ? Mathf.Clamp((float)displayed / rank.next, 0.001f, 1f) : 1f, 1f);
                _rankToastGroup.alpha = t;
                yield return null;
            }
            _rankToastPoints.text = (gain.delta > 0 ? "+" : "") + gain.delta + " PTS  |  " + gain.after + " TOTAL";
            _rankToastGroup.alpha = 1f;
            ChogResultScreen.ClearPendingRank(address, gameId);
            yield return new WaitForSecondsRealtime(2f);
            _rankToast.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (_ribbonName == null)
                return;
            var token = WalletPanel.PlayedTokenId;
            var metadata = WalletPanel.PlayedMetadataId;
            var index = token > 0 && _roster != null ? _roster.IndexOf(token) : -1;
            if (token != _lastToken)
            {
                _lastToken = token;
                if (token > 0 && isActiveAndEnabled)
                    ChogReveal.ShowOnce(_main.transform, token);
            }

            // The chip: the Genesis picture and level, with the role readable in a gold badge.
            ShowPortrait(metadata);
            var rank = PlayerRanks.Current?.rank;
            _rankBadge.gameObject.SetActive(rank != null);
            if (rank != null)
            {
                _rankText.text = rank.tier + " " + rank.points.ToString(CultureInfo.InvariantCulture);
                _rankBadge.color = rank.tier == "STAR" ? ChogTheme.Gold : ChogTheme.Purple;
            }
            _levelBadge.SetActive(token > 0);
            if (token > 0 && GenesisCareers.TryGet(token, out var career) && career.mastery != null)
            {
                var mastery = career.mastery;
                _level.text = mastery.level.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                _level.text = "1";
            }

            // The wallet: its address once connected, otherwise what a tap does.
            var ready = PlayerWallet.State == WalletState.Ready;
            _walletDot.enabled = ready;
            var words = WalletPanel.ConnectWords();
            _walletWords.text = ready ? PlayerWallet.ShortAddress(PlayerWallet.Address) : words == "CONNECT WALLET" ? "CONNECT" : words;
            _walletWords.rectTransform.anchorMin = new Vector2(ready ? 0.14f : 0.05f, 0.14f);
            ChogTheme.SetButtonFill(_wallet, ready ? ChogTheme.Quiet : ChogTheme.Purple);
            _wallet.interactable = !ready;

            var balance = GenesisRental.BalanceWei;
            _chogPill.SetActive(ready && balance.Length > 0);
            if (balance.Length > 0)
                _chogWords.text = ChogPrice.PillText(balance);

            // The ribbon and the chips: the name of the hero and its kit as it will be played.
            if (index >= 0)
            {
                var hero = _roster.Entries[index];
                _ribbonName.text = GenesisHeroCard.DisplayName(hero).ToUpperInvariant();
                // The kit in four words: the class, the special, the effect, the delivery (the sheet says more).
                var lines = _roster.CardLines(hero, GenesisForms.PlayedBy(token));
                _roleText.text = (lines[0].Gives ?? "GENESIS").ToUpperInvariant();
                ShowChips(lines[0].Gives, lines[1].Gives, lines[2].Gives, lines[3].Gives);
            }
            else
            {
                _ribbonName.text = "RECRUIT";
                _roleText.text = "RECRUIT";
                ShowChips("NO GENESIS", (100 - Quantum.Recruit.HealthPercent) + "% LESS HEALTH", "NO GENESIS ATTACK");
            }
        }

        private void ShowChips(params string[] words)
        {
            for (var i = 0; i < _chips.Length; i++)
                ChogUi.SetChip(_chips[i], i < words.Length ? (words[i] ?? "").ToUpperInvariant() : "");
        }

        private void ShowPortrait(int metadata)
        {
            if (metadata == _portraitFor)
                return;
            _portraitFor = metadata;
            _recruitIcon.enabled = metadata <= 0;
            _portrait.enabled = metadata > 0;
            _portraitPlate.color = metadata > 0 ? ChogTheme.Action : ChogTheme.Hex("6E6A80");
            if (metadata <= 0)
                return;
            _portrait.texture = GenesisArt.Disc();
            GenesisArt.Request(metadata, texture =>
            {
                if (_portrait != null && _portraitFor == metadata)
                    _portrait.texture = texture;
            });
        }

        // The stage: the sheet of the Genesis played; a recruit sees every hero instead.
        private void OpenHeroes()
        {
            if (WalletPanel.PlayedTokenId > 0)
                ChogHeroSheet.Open(_main.transform, WalletPanel.PlayedTokenId);
            else
                ChogCollection.Open(_main.transform);
        }

        private string PlayerName() => _usernameLabel is TMP_Text tmp ? tmp.text : _usernameLabel is Text legacy ? legacy.text : "";

        // The sample's window that changes the player's name, over everything else (from the profile).
        private void Rename()
        {
            if (_usernameView != null)
                _usernameView.transform.SetAsLastSibling();
            Call("OnUsernameButtonPressed");
        }

        private void Call(string method)
        {
            var found = typeof(QuantumMenuUIMain).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (found == null)
                Debug.LogWarning("CHOG_HOME the menu has no " + method);
            else
                found.Invoke(_main, null);
        }

        private T Field<T>(string name) where T : class
        {
            var field = typeof(QuantumMenuUIMain).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(_main) as T;
        }
    }
}
