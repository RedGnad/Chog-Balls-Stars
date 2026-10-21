using System;
using System.Globalization;
using Chog.Match;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using GenesisRoster = Quantum.GenesisRoster;
using GenesisRosterEntry = Quantum.GenesisRosterEntry;

namespace Chog.Screens
{
    /// <summary>
    /// The sheet of one Genesis, over the whole menu (validated mockup "Hero"): the hero large in 3D on
    /// its gold pedestal, turned by dragging, beside the picture of the NFT; its class, then each part
    /// of its kit with the trait that gives it; its career as recorded on Monad; and what this wallet
    /// can do with it: play it, offer it for rent, lend it, or rent it when its holder offers it. The
    /// second form of its special is switched or bought here. Everything shown is read from the roster,
    /// the careers and the wallet's state; nothing is written by the sheet itself.
    /// </summary>
    public sealed class ChogHeroSheet : MonoBehaviour
    {
        private const string ObjectName = "ChogHeroSheet";
        private const string RosterResource = "GenesisRoster";
        private const float Margin = 30f;

        private static readonly Color RowFill = new Color(0.08f, 0.06f, 0.14f, 0.55f);
        private static readonly Color TraitColour = ChogTheme.Hex("D9D2FF");
        private static readonly Color Gold = ChogTheme.Hex("FFE27A");
        private static readonly string[] StatNames = { "MATCHES", "WINS", "RENTED", "AS A GUEST", "BEST STREAK" };

        private QuantumMenuUIMain _main;
        private GenesisRoster _roster;
        private int _tokenId;
        private int _careerAskedFor;

        private ChogHeroStage _stage;
        private RawImage _picture;
        private int _pictureFor = -1;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _token;
        private Image _classTile;
        private TextMeshProUGUI _classLetter;
        private TextMeshProUGUI _className;
        private TextMeshProUGUI _classTrait;
        private TextMeshProUGUI _formStatus;
        private readonly Image[] _tiles = new Image[4];
        private readonly TextMeshProUGUI[] _rowTitles = new TextMeshProUGUI[4];
        private readonly TextMeshProUGUI[] _rowTraits = new TextMeshProUGUI[4];
        private Button _form;
        private TextMeshProUGUI _formLabel;
        private readonly TextMeshProUGUI[] _stats = new TextMeshProUGUI[StatNames.Length];
        private Button _play;
        private Button _rentOut;
        private TextMeshProUGUI _rentOutLabel;
        private Button _lend;
        private Button _rent;
        private TextMeshProUGUI _rentLabel;

        /// <summary>Opens the sheet of a Genesis over the main screen of the menu.</summary>
        public static void Open(Transform main, int tokenId)
        {
            var existing = main.Find(ObjectName);
            var sheet = existing != null ? existing.GetComponent<ChogHeroSheet>() : null;
            if (sheet == null)
            {
                var host = ChogUi.Area(ObjectName, main, Vector2.zero, Vector2.one);
                sheet = host.gameObject.AddComponent<ChogHeroSheet>();
                sheet.Build(main.GetComponent<QuantumMenuUIMain>());
            }
            sheet._tokenId = tokenId;
            sheet.gameObject.SetActive(true);
            sheet.transform.SetAsLastSibling();
            sheet.Refresh();
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
            ChogPrice.Changed += Refresh;
        }

        private void OnDisable()
        {
            ChogPrice.Changed -= Refresh;
            PlayerWallet.Changed -= Refresh;
            GenesisCareers.Changed -= Refresh;
            GuestPass.Changed -= Refresh;
            GenesisLoan.Changed -= Refresh;
            GenesisForms.Changed -= Refresh;
            GenesisShop.Changed -= Refresh;
            GenesisRental.Changed -= Refresh;
        }

        private void Close() => gameObject.SetActive(false);

        private void Build(QuantumMenuUIMain main)
        {
            _main = main;
            _roster = Resources.Load<GenesisRoster>(RosterResource);

            // The night of the menu, opaque, with a warm light where the hero stands.
            var night = gameObject.AddComponent<Image>();
            night.sprite = ChogSprites.Gradient(ChogTheme.Night, ChogTheme.NightHigh);
            var warm = ChogUi.Area("Warm", transform, new Vector2(-0.02f, 0.02f), new Vector2(0.52f, 0.92f)).gameObject.AddComponent<Image>();
            warm.sprite = ChogSprites.Disc(128, 1f);
            warm.color = new Color(1f, 0.84f, 0.55f, 0.26f);
            warm.raycastTarget = false;

            var back = ChogUi.PinnedButton("Back", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -26f),
                new Vector2(84f, 84f), ChogTheme.ButtonKind.Secondary, "", 30f, ChogIcons.Icon.Back);
            back.onClick.AddListener(Close);
            _name = ChogUi.Title("Name", ChogUi.Pinned("NameArea", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(134f, -22f),
                new Vector2(900f, 62f)), Vector2.zero, Vector2.one, 56f, TextAlignmentOptions.Left);
            _token = ChogUi.Body("Token", ChogUi.Pinned("TokenArea", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(134f, -86f),
                new Vector2(900f, 34f)), Vector2.zero, Vector2.one, 26f, TextAlignmentOptions.Left, ChogTheme.PurpleSoft);

            var stageArea = ChogUi.Area("Stage", transform, new Vector2(0.04f, 0.09f), new Vector2(0.46f, 0.86f));
            _stage = ChogHeroStage.Create(stageArea);
            _stage.Draggable = true;
            ChogUi.Body("Drag", transform, new Vector2(0.04f, 0.03f), new Vector2(0.46f, 0.08f), 24f, TextAlignmentOptions.Center,
                ChogTheme.PurpleSoft).text = "Drag to turn it around";

            var nft = ChogUi.Pinned("Nft", transform, new Vector2(0.475f, 0.86f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(104f, 104f));
            ChogUi.Plate("Frame", nft, Vector2.zero, Vector2.one, ChogTheme.Ink, 16);
            _picture = ChogUi.Area("Picture", nft, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            _picture.rectTransform.offsetMin = new Vector2(5f, 5f);
            _picture.rectTransform.offsetMax = new Vector2(-5f, -5f);
            _picture.raycastTarget = false;
            ChogUi.Number("Label", nft, new Vector2(-0.3f, -0.36f), new Vector2(1.3f, -0.06f), 20f, TextAlignmentOptions.Center,
                ChogTheme.PurpleSoft, false).text = "THE NFT";

            var card = ChogUi.Card("Card", transform, new Vector2(0.535f, 0.04f), new Vector2(0.98f, 0.91f), raised: true).transform;

            // The class, from the outfit.
            var tile = ChogUi.Pinned("ClassTile", card, new Vector2(0.04f, 0.915f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(72f, 72f));
            _classTile = ChogUi.Plate("Plate", tile, Vector2.zero, Vector2.one, ChogTheme.Danger, 14);
            _classLetter = ChogUi.Title("Letter", tile, new Vector2(0f, 0.06f), Vector2.one, 40f);
            _className = ChogUi.Title("Class", card, new Vector2(0.15f, 0.905f), new Vector2(0.62f, 0.975f), 46f, TextAlignmentOptions.Left);
            _classTrait = ChogUi.Body("Trait", card, new Vector2(0.15f, 0.86f), new Vector2(0.62f, 0.905f), 22f, TextAlignmentOptions.Left, TraitColour);
            _formStatus = ChogUi.Number("Form", card, new Vector2(0.60f, 0.87f), new Vector2(0.96f, 0.96f), 22f, TextAlignmentOptions.Right, Gold, false);

            // Its basic attack (the hat), its special (the eyes), the effect (the aura), the delivery (the rarity).
            var icons = new[] { ChogIcons.Icon.Crosshair, ChogIcons.Icon.Bolt, ChogIcons.Icon.Star, ChogIcons.Icon.Play };
            var fills = new[] { ChogTheme.Cheek, ChogTheme.Action, ChogTheme.Confirm, ChogTheme.Purple };
            for (var i = 0; i < 4; i++)
            {
                var top = 0.835f - i * 0.11f;
                var row = ChogUi.Area("Row" + i, card, new Vector2(0.04f, top - 0.10f), new Vector2(0.96f, top));
                var plate = row.gameObject.AddComponent<Image>();
                plate.sprite = ChogSprites.Rounded(14, 2, ChogTheme.Line, RowFill);
                plate.type = Image.Type.Sliced;
                plate.raycastTarget = false;
                var square = ChogUi.Pinned("Tile", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(62f, 62f));
                _tiles[i] = ChogUi.Plate("Plate", square, Vector2.zero, Vector2.one, fills[i], 12);
                ChogUi.Icon("Icon", square, icons[i], new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
                _rowTitles[i] = ChogUi.Title("Title", row, new Vector2(0.14f, 0.48f), new Vector2(0.98f, 0.94f), 30f, TextAlignmentOptions.Left);
                _rowTitles[i].textWrappingMode = TextWrappingModes.NoWrap;
                _rowTraits[i] = ChogUi.Body("Trait", row, new Vector2(0.14f, 0.06f), new Vector2(0.98f, 0.50f), 22f, TextAlignmentOptions.Left, TraitColour);
            }

            // The second form of the special: switched once unlocked, bought before that where a shop is open.
            _form = ChogUi.Button("Form", card.Find("Row1"), new Vector2(0.66f, 0.10f), new Vector2(0.985f, 0.90f), ChogTheme.ButtonKind.Secondary, "x", 24f);
            _formLabel = ChogUi.LabelOf(_form);
            _formLabel.textWrappingMode = TextWrappingModes.Normal;
            _form.onClick.AddListener(OnFormButton);

            // The career, as the careers contract keeps it on Monad.
            ChogUi.Title("Career", card, new Vector2(0.04f, 0.33f), new Vector2(0.50f, 0.39f), 32f, TextAlignmentOptions.Left).text = "CAREER ON MONAD";
            ChogUi.Body("Follows", card, new Vector2(0.48f, 0.33f), new Vector2(0.96f, 0.39f), 22f, TextAlignmentOptions.Right, Gold).text =
                "it follows the NFT to its next holder";
            for (var i = 0; i < StatNames.Length; i++)
            {
                var left = 0.04f + i * 0.186f;
                var cell = ChogUi.Area("Stat" + i, card, new Vector2(left, 0.175f), new Vector2(left + 0.172f, 0.32f));
                var plate = cell.gameObject.AddComponent<Image>();
                plate.sprite = ChogSprites.Rounded(14, 0, Color.clear, RowFill);
                plate.type = Image.Type.Sliced;
                plate.raycastTarget = false;
                _stats[i] = ChogUi.Title("Value", cell, new Vector2(0f, 0.38f), new Vector2(1f, 0.96f), 44f);
                ChogUi.Number("Name", cell, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.38f), 18f, TextAlignmentOptions.Center,
                    TraitColour, false).text = StatNames[i];
            }

            // What this wallet can do with it.
            _play = ChogUi.Button("Play", card, new Vector2(0.04f, 0.03f), new Vector2(0.46f, 0.15f), ChogTheme.ButtonKind.Primary, "PLAY IT", 46f);
            _play.onClick.AddListener(PlayIt);
            _rentOut = ChogUi.Button("RentOut", card, new Vector2(0.48f, 0.03f), new Vector2(0.75f, 0.15f), ChogTheme.ButtonKind.Secondary, "RENT OUT", 34f);
            _rentOutLabel = ChogUi.LabelOf(_rentOut);
            _rentOutLabel.textWrappingMode = TextWrappingModes.Normal;
            _rentOut.onClick.AddListener(() => WalletPanel.OpenOfferOf(_tokenId));
            _lend = ChogUi.Button("Lend", card, new Vector2(0.77f, 0.03f), new Vector2(0.96f, 0.15f), ChogTheme.ButtonKind.Quiet, "LEND", 34f);
            _lend.onClick.AddListener(() => GenesisLoan.Lend(_tokenId));
            _rent = ChogUi.Button("Rent", card, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.15f), ChogTheme.ButtonKind.Chog, "RENT IT", 40f);
            _rentLabel = ChogUi.LabelOf(_rent);
            _rent.onClick.AddListener(() => WalletPanel.OpenRentalOf(_tokenId));
        }

        private void Update()
        {
            if (_stage == null)
                return;
            var index = _roster != null ? _roster.IndexOf(_tokenId) : -1;
            var metadata = index >= 0 ? _roster.Entries[index].MetadataId : 0;
            _stage.Show(metadata > 0 && ChogHeroModel.Has(metadata) ? metadata : ChogHeroModel.Recruit);
        }

        private void Refresh()
        {
            if (_name == null || !isActiveAndEnabled)
                return;
            var index = _roster != null ? _roster.IndexOf(_tokenId) : -1;
            if (index < 0)
            {
                Close();
                return;
            }

            var hero = _roster.Entries[index];
            _name.text = GenesisHeroCard.DisplayName(hero).ToUpperInvariant();
            _token.text = "Chog Genesis, token " + hero.TokenId.ToString(CultureInfo.InvariantCulture);
            ShowPicture(hero.MetadataId);

            var form = GenesisForms.PlayedBy(_tokenId);
            var lines = _roster.CardLines(hero, form);
            var role = (lines[0].Gives ?? "").ToUpperInvariant();
            _className.text = role;
            _classLetter.text = role.Length > 0 ? role.Substring(0, 1) : "?";
            _classTile.color = role == "BRUTE" ? ChogTheme.Danger : role == "RANGER" ? ChogTheme.Confirm : role == "CONTROLLER" ? ChogTheme.Purple : ChogTheme.Quiet;
            _classTrait.text = "Class from its outfit: " + lines[0].Value;

            Row(0, "BASIC: " + ChogHeroModel.AttackLine(hero.MetadataId), "From its hat.");
            Row(1, "SPECIAL: " + lines[1].Gives, Source("eyes", lines[1].Value, lines[1].Summary));
            Row(2, "EFFECT: " + lines[2].Gives, Source("aura", lines[2].Value, lines[2].Summary));
            Row(3, "DELIVERY: " + lines[3].Gives, Source("rarity", lines[3].Value, lines[3].Summary));
            // The effect's tile in the colour its hits are drawn with, unless that colour is too dark to read.
            var effect = ChogTheme.Confirm;
            if (_roster.EffectColors != null && hero.Effect < _roster.EffectColors.Length)
            {
                var colour = _roster.EffectColors[hero.Effect];
                if (colour.a > 0.1f && colour.r + colour.g + colour.b > 0.6f)
                    effect = new Color(colour.r, colour.g, colour.b, 1f);
            }
            _tiles[2].color = effect;

            RefreshForm(hero);
            RefreshCareer();
            RefreshActions();
        }

        // "From its eyes, Modern VR. A fan of light...", or "No aura. Full damage..." for a trait it does not wear.
        private static string Source(string trait, string value, string summary)
        {
            var said = string.IsNullOrEmpty(summary) ? "" : " " + char.ToUpperInvariant(summary[0]) + summary.Substring(1);
            if (!said.EndsWith(".") && said.Length > 0)
                said += ".";
            return (value == "none" ? "No " + trait + "." : "From its " + trait + ", " + value + ".") + said;
        }

        private void Row(int index, string title, string trait)
        {
            _rowTitles[index].text = title.ToUpperInvariant();
            _rowTraits[index].text = trait;
        }

        private void ShowPicture(int metadataId)
        {
            if (metadataId == _pictureFor)
                return;
            _pictureFor = metadataId;
            _picture.texture = GenesisArt.Disc();
            GenesisArt.Request(metadataId, texture =>
            {
                if (_picture != null && _pictureFor == metadataId)
                    _picture.texture = texture;
            });
        }

        private void RefreshCareer()
        {
            if (!GenesisCareers.TryGet(_tokenId, out var career))
            {
                if (_careerAskedFor != _tokenId)
                {
                    _careerAskedFor = _tokenId;
                    GenesisCareers.Read(new[] { _tokenId });
                }
                foreach (var stat in _stats)
                    stat.text = "...";
                return;
            }

            var values = new[] { career.matches, career.wins, career.rentedMatches, career.guestMatches, career.bestStreak };
            for (var i = 0; i < _stats.Length; i++)
                _stats[i].text = values[i].ToString(CultureInfo.InvariantCulture);
        }

        // The form of the special this Genesis enters with: switched by a tap once mastery unlocked the
        // second form; before that, bought where a shop is open, or the level to reach.
        private void RefreshForm(GenesisRosterEntry hero)
        {
            var hasForms = _roster.HasSecondForm(hero.Shape);
            _form.gameObject.SetActive(hasForms);
            _rowTraits[1].rectTransform.anchorMax = new Vector2(hasForms ? 0.64f : 0.98f, 0.50f);
            _rowTitles[1].rectTransform.anchorMax = new Vector2(hasForms ? 0.64f : 0.98f, 0.94f);
            if (!hasForms)
            {
                _formStatus.text = "";
                return;
            }

            var unlocked = GenesisForms.IsUnlockedFor(_tokenId);
            var canBuy = !unlocked && GenesisShop.State == ShopState.Open && PlayerWallet.State == WalletState.Ready
                         && Array.IndexOf(PlayerWallet.Tokens, _tokenId) >= 0;
            var mine = GenesisShop.PurchaseTokenId == _tokenId;
            var buying = canBuy && mine && GenesisShop.IsBusy;
            var failed = canBuy && mine && GenesisShop.Purchase == PurchaseState.Failed;

            _formStatus.text = unlocked ? "SECOND FORM UNLOCKED" : "";
            _form.interactable = unlocked || (canBuy && !GenesisShop.IsBusy);
            ChogTheme.SetButtonFill(_form, unlocked ? ChogTheme.Purple : canBuy ? ChogTheme.Cheek : ChogTheme.Quiet);
            if (unlocked)
            {
                var picked = GenesisForms.Picked(_tokenId);
                _formLabel.text = "<size=60%>" + (picked == 1 ? "SECOND FORM, TAP FOR THE FIRST" : "FIRST FORM, TAP FOR THE SECOND") +
                                  "</size>\n" + _roster.FormName(hero.Shape, picked).ToUpperInvariant();
            }
            else if (buying)
            {
                _formLabel.text = "<size=60%>ANSWER IN YOUR WALLET</size>\n" + PurchaseStep(GenesisShop.Purchase);
            }
            else if (failed)
            {
                _formLabel.text = "<size=60%>NOT BOUGHT</size>\nTRY AGAIN";
                _rowTraits[1].text = "<color=#FF9C8A>" + GenesisShop.Problem + "</color>";
            }
            else if (canBuy)
            {
                _formLabel.text = "<size=60%>" + _roster.FormName(hero.Shape, 1).ToUpperInvariant() + " NOW FOR</size>\n" +
                                  ChogPrice.WithUsd(GenesisShop.PriceWei);
            }
            else
            {
                _formLabel.text = "<size=60%>" + _roster.FormName(hero.Shape, 1).ToUpperInvariant() + " AT</size>\nMASTERY " + GenesisForms.UnlockLevel;
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

        private void OnFormButton()
        {
            if (GenesisForms.IsUnlockedFor(_tokenId))
            {
                GenesisForms.Pick(_tokenId, 1 - GenesisForms.Picked(_tokenId));
                return;
            }

            GenesisShop.Dismiss();
            GenesisShop.Buy(_tokenId);
        }

        private void RefreshActions()
        {
            var ready = PlayerWallet.State == WalletState.Ready;
            var owns = ready && Array.IndexOf(PlayerWallet.Tokens, _tokenId) >= 0;
            var playable = (ready && Array.IndexOf(PlayerWallet.Playable, _tokenId) >= 0)
                           || (GuestPass.State == GuestPassState.Valid && GuestPass.TokenId == _tokenId);
            var rentalOpen = GenesisRental.State == RentalState.Open;
            var canRentOut = owns && rentalOpen;
            var canLend = owns && PlayerWallet.SignIn == SignInState.SignedIn && GenesisLoan.CanLend(_tokenId);
            var offered = GenesisRental.TryGetOffer(_tokenId, out var offer);
            var canRent = !playable && rentalOpen && offered;

            _play.gameObject.SetActive(playable);
            _rentOut.gameObject.SetActive(canRentOut);
            _lend.gameObject.SetActive(canLend);
            _rent.gameObject.SetActive(canRent);

            // PLAY IT takes the whole row when nothing else is offered.
            var playArea = (RectTransform)_play.transform;
            playArea.anchorMax = new Vector2(canRentOut ? 0.46f : canLend ? 0.75f : 0.96f, 0.15f);
            var rentOutArea = (RectTransform)_rentOut.transform;
            rentOutArea.anchorMax = new Vector2(canLend ? 0.75f : 0.96f, 0.15f);

            if (canRentOut)
            {
                var lentUntil = GenesisRental.LentUntilSeconds(_tokenId);
                var now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                _rentOutLabel.text = lentUntil > 0
                    ? "<size=60%>RENTED FOR</size>\n" + GenesisRental.TimeLeft(lentUntil, now).ToUpperInvariant()
                    : offered ? "<size=60%>RENTED OUT AT</size>\n" + ChogPrice.WithUsd(offer.PricePerDayWei) + " A DAY"
                    : "RENT OUT";
            }
            if (canRent)
                _rentLabel.text = "RENT IT, " + ChogPrice.WithUsd(offer.PricePerDayWei) + " A DAY";
        }

        // PLAY IT: this Genesis is chosen, the sheet closes and the match is looked for.
        private void PlayIt()
        {
            if (Array.IndexOf(PlayerWallet.Playable, _tokenId) >= 0)
                PlayerWallet.Choose(_tokenId);
            Close();
            if (_main != null)
                _main.SendMessage("OnPlayButtonPressed", SendMessageOptions.DontRequireReceiver);
        }
    }
}
