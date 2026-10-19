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

        // In units of the menu canvas (1920 by 1080 at 16:9), from the corners.
        private const float Margin = 30f;
        private static readonly Vector2 ChipSize = new Vector2(340f, 92f);
        private static readonly Vector2 SettingsSize = new Vector2(76f, 76f);
        private static readonly Vector2 WalletSize = new Vector2(266f, 68f);
        private static readonly Vector2 ChogSize = new Vector2(198f, 68f);
        private static readonly Vector2 RailButton = new Vector2(88f, 82f);
        private const float RailTop = 154f;
        private const float RailStep = 122f;

        // In shares of the screen: the hero owns the left focal column, the Genesis card owns the right.
        private static readonly Vector2 StageMin = new Vector2(0.10f, 0.18f);
        private static readonly Vector2 StageMax = new Vector2(0.51f, 0.86f);
        private static readonly Vector2 RibbonMin = new Vector2(0.17f, 0.115f);
        private static readonly Vector2 RibbonMax = new Vector2(0.46f, 0.180f);
        private static readonly Vector2 ChipsMin = new Vector2(0.10f, 0.035f);
        private static readonly Vector2 ChipsMax = new Vector2(0.53f, 0.095f);
        private static readonly Vector2 PlayMin = new Vector2(0.77f, 0.115f);
        private static readonly Vector2 PlayMax = new Vector2(0.98f, 0.278f);
        private static readonly Vector2 HintMin = new Vector2(0.77f, 0.06f);
        private static readonly Vector2 HintMax = new Vector2(0.98f, 0.105f);

        private static readonly Color Lilac = ChogTheme.Hex("E9E4FF");

        private QuantumMenuUIMain _main;
        private GenesisRoster _roster;
        // The sample's label of the player's name: a TextMesh Pro text or a legacy one, as its build chose.
        private Component _usernameLabel;
        private GameObject _usernameView;

        private ChogHeroStage _stage;
        private TextMeshProUGUI _ribbonName;
        private TextMeshProUGUI[] _chips;

        private Image _portraitPlate;
        private RawImage _portrait;
        private Image _recruitIcon;
        private GameObject _levelBadge;
        private TextMeshProUGUI _level;
        private TextMeshProUGUI _playerName;
        private RectTransform _masteryBar;
        private TextMeshProUGUI _masteryText;

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
            PlayerWallet.Changed += Refresh;
            GenesisCareers.Changed += Refresh;
            GenesisRental.Changed += Refresh;
            GenesisForms.Changed += Refresh;
            if (_ribbonName != null)
                Refresh();
        }

        private void OnDisable()
        {
            WalletPanel.PlayedChanged -= Refresh;
            PlayerWallet.Changed -= Refresh;
            GenesisCareers.Changed -= Refresh;
            GenesisRental.Changed -= Refresh;
            GenesisForms.Changed -= Refresh;
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

        // The player's chip: the Genesis played (or the recruit) with its mastery, and the player's name.
        private void BuildChip()
        {
            var chip = ChogUi.PinnedButton("Chip", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -24f), ChipSize,
                ChogTheme.ButtonKind.Quiet, "", 30f);
            var plate = ChogTheme.ImageOf(chip);
            ChogTheme.AsCard(plate, raised: true);
            chip.onClick.AddListener(Rename);

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
            _masteryBar = ChogUi.Bar("Mastery", chip.transform, new Vector2(0.285f, 0.18f), new Vector2(0.74f, 0.38f), ChogTheme.Confirm);
            _masteryText = ChogUi.Number("Points", chip.transform, new Vector2(0.75f, 0.10f), new Vector2(0.97f, 0.46f), 24f,
                TextAlignmentOptions.Left, Color.white, false);
        }

        // The $CHOG held, the wallet, the settings, from the top right corner.
        private void BuildTopRight()
        {
            var settings = ChogUi.PinnedButton("Settings", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Margin, -26f),
                SettingsSize, ChogTheme.ButtonKind.Quiet, "", 30f, ChogIcons.Icon.Gear);
            settings.onClick.AddListener(() => ChogSettings.Open(_main.transform));

            var walletRight = -Margin - SettingsSize.x - 14f;
            _wallet = ChogUi.PinnedButton("Wallet", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(walletRight, -29f),
                WalletSize, ChogTheme.ButtonKind.Quiet, "", 30f);
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
            chog.onClick.AddListener(WalletPanel.OpenRentals);
            var coin = ChogUi.Pinned("Coin", chog.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 2f), new Vector2(52f, 52f));
            ChogUi.Plate("Plate", coin, Vector2.zero, Vector2.one, ChogTheme.Cheek, 26);
            ChogUi.Title("Letter", coin, new Vector2(0f, 0.06f), Vector2.one, 30f).text = "C";
            _chogWords = ChogUi.Number("Amount", chog.transform, new Vector2(0.33f, 0.14f), new Vector2(0.94f, 0.88f), 30f,
                TextAlignmentOptions.Center, Color.white);
            _chogPill = chog.gameObject;
        }

        // The rail down the left: the collection, the leaderboard, the rentals, friends, the guide.
        private void BuildRail()
        {
            Rail(0, "HEROES", ChogTheme.ButtonKind.Secondary, ChogIcons.Icon.Heroes, () => ChogCollection.Open(_main.transform));
            Rail(1, "TOP", ChogTheme.ButtonKind.Primary, ChogIcons.Icon.Trophy, () => ChogHall.Open(_main.transform));
            Rail(2, "RENT", ChogTheme.ButtonKind.Chog, ChogIcons.Icon.Key, WalletPanel.OpenRentals);
            var friends = Rail(3, "FRIENDS", ChogTheme.ButtonKind.Secondary, ChogIcons.Icon.Friends, () => Call("OnPartyButtonPressed"));
            ChogTheme.ImageOf(friends).color = ChogTheme.Ally;
            Rail(4, "GUIDE", ChogTheme.ButtonKind.Quiet, ChogIcons.Icon.Question, () => ChogGuide.Open(_main.transform));
        }

        private Button Rail(int position, string label, ChogTheme.ButtonKind kind, ChogIcons.Icon icon, UnityEngine.Events.UnityAction tapped)
        {
            var y = -RailTop - position * RailStep;
            var button = ChogUi.PinnedButton("Rail" + label, transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin + 4f, y),
                RailButton, kind, "", 30f, icon);
            button.onClick.AddListener(tapped);
            var words = ChogUi.Pinned("Label" + label, transform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(Margin + 4f + RailButton.x * 0.5f, y - RailButton.y - 5f), new Vector2(130f, 30f));
            ChogUi.Title("Text", words, Vector2.zero, Vector2.one, 21f).text = label;
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
            var stageFloor = ChogUi.Pinned("StageFloor", area, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(330f, 94f)).gameObject.AddComponent<Image>();
            stageFloor.sprite = ChogSprites.Disc(96, 0.14f);
            stageFloor.color = new Color(0.87f, 0.16f, 0.49f, 0.42f);
            stageFloor.raycastTarget = false;
            _stage = ChogHeroStage.Create(ChogUi.Area("Hero", area, Vector2.zero, Vector2.one));

            // The whole stage opens the hero's sheet.
            var tap = area.gameObject.AddComponent<Image>();
            tap.color = Color.clear;
            area.gameObject.AddComponent<Button>().onClick.AddListener(OpenHeroes);

            var ribbon = ChogUi.Plate("Ribbon", transform, RibbonMin, RibbonMax, ChogTheme.Action, 12);
            _ribbonName = ChogUi.Title("Name", ribbon.transform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.94f), 54f);
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
            }
            if (party != null)
                party.gameObject.SetActive(false);

            ChogUi.Body("Hint", transform, HintMin, HintMax, 24f, TextAlignmentOptions.Center, Lilac).text = "3 v 3, bots fill the empty places";
        }

        private void Update()
        {
            if (_stage == null)
                return;
            var played = WalletPanel.PlayedMetadataId;
            _stage.Show(played > 0 && ChogHeroModel.Has(played) ? played : ChogHeroModel.Recruit);

            // The sample writes the player's name when its screen shows and after a change.
            var name = _usernameLabel is TMP_Text tmp ? tmp.text : _usernameLabel is Text legacy ? legacy.text : "";
            if (name != _shownName)
            {
                _shownName = name;
                _playerName.text = name.Length > 0 ? name.ToUpperInvariant() : "PLAYER";
            }
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

            // The chip: the picture of the Genesis played and its mastery; the recruit has neither.
            ShowPortrait(metadata);
            _levelBadge.SetActive(token > 0);
            _masteryBar.gameObject.SetActive(token > 0);
            if (token > 0 && GenesisCareers.TryGet(token, out var career) && career.mastery != null)
            {
                var mastery = career.mastery;
                _level.text = mastery.level.ToString(CultureInfo.InvariantCulture);
                var previous = mastery.level >= 2 && mastery.level - 2 < MasteryRoad.Steps.Length ? MasteryRoad.Steps[mastery.level - 2] : 0;
                ChogUi.SetBar(_masteryBar, mastery.next > previous ? (mastery.points - previous) / (float)(mastery.next - previous) : 1f);
                _masteryText.text = mastery.next > 0 ? mastery.points + "/" + mastery.next : "MAX";
            }
            else
            {
                _level.text = "1";
                ChogUi.SetBar(_masteryBar, 0f);
                _masteryText.text = token > 0 ? "" : "RECRUIT";
            }
            var masteryArea = _masteryText.rectTransform;
            masteryArea.anchorMin = new Vector2(token > 0 ? 0.75f : 0.285f, 0.10f);

            // The wallet: its address once connected, otherwise what a tap does.
            var ready = PlayerWallet.State == WalletState.Ready;
            _walletDot.enabled = ready;
            var words = WalletPanel.ConnectWords();
            _walletWords.text = ready ? PlayerWallet.ShortAddress(PlayerWallet.Address) : words == "CONNECT WALLET" ? "CONNECT" : words;
            _walletWords.rectTransform.anchorMin = new Vector2(ready ? 0.14f : 0.05f, 0.14f);
            ChogTheme.ImageOf(_wallet).color = ready ? ChogTheme.Quiet : ChogTheme.Purple;
            _wallet.interactable = !ready;

            var balance = GenesisRental.BalanceWei;
            _chogPill.SetActive(ready && balance.Length > 0);
            if (balance.Length > 0)
                _chogWords.text = Compact(MonadTransactions.PriceText(balance));

            // The ribbon and the chips: the name of the hero and its kit as it will be played.
            if (index >= 0)
            {
                var hero = _roster.Entries[index];
                _ribbonName.text = GenesisHeroCard.DisplayName(hero).ToUpperInvariant();
                // The kit in four words: the class, the special, the effect, the delivery (the sheet says more).
                var lines = _roster.CardLines(hero, GenesisForms.PlayedBy(token));
                ShowChips(lines[0].Gives, lines[1].Gives, lines[2].Gives, lines[3].Gives);
            }
            else
            {
                _ribbonName.text = "RECRUIT";
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

        // "39.03", "12.4K", "3.1M": an amount that fits its pill.
        private static string Compact(string amount)
        {
            if (!double.TryParse(amount, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return amount;
            if (value >= 1e6)
                return (value / 1e6).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            if (value >= 1e4)
                return (value / 1e3).ToString("0.#", CultureInfo.InvariantCulture) + "K";
            return amount;
        }

        // The stage: the sheet of the Genesis played; a recruit sees every hero instead.
        private void OpenHeroes()
        {
            if (WalletPanel.PlayedTokenId > 0)
                ChogHeroSheet.Open(_main.transform, WalletPanel.PlayedTokenId);
            else
                ChogCollection.Open(_main.transform);
        }

        // The chip: the sample's window that changes the player's name, over everything else.
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
