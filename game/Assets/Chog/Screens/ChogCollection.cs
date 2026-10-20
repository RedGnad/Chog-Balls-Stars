using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using GenesisRoster = Quantum.GenesisRoster;
using GenesisRosterEntry = Quantum.GenesisRosterEntry;

namespace Chog.Screens
{
    /// <summary>
    /// The 1,969 Chog Genesis as heroes (HEROES on the home screen): pages of 48 in the order of
    /// their numbers, each with its picture, its number and its class; MINE lists the Genesis this
    /// wallet plays (held, rented or lent), FOR RENT those their holders offer, with the price a day.
    /// A tap opens the hero's sheet (ChogHeroSheet). A page of ALL is one picture of 48 portraits
    /// (tools/genesis-art/make_atlas.py), loaded when it is shown and let go when another is, so a
    /// phone never holds more than one; the other lists load each portrait (GenesisArt).
    /// </summary>
    public sealed class ChogCollection : MonoBehaviour
    {
        private const string ObjectName = "ChogCollection";
        private const string RosterResource = "GenesisRoster";
        private const int Columns = 8;
        private const int Rows = 6;
        private const int PerPage = Columns * Rows;
        private const int LastMetadataId = 1969;
        private const float Margin = 30f;

        private enum List { All, Mine, ForRent }

        private sealed class Cell
        {
            public GameObject Root;
            public Image Frame;
            public RawImage Picture;
            public TextMeshProUGUI Number;
            public TextMeshProUGUI Detail;
            public int TokenId;
            public int MetadataId;
        }

        private Transform _main;
        private GenesisRoster _roster;
        private readonly Dictionary<int, int> _byMetadata = new Dictionary<int, int>();
        private readonly Cell[] _cells = new Cell[PerPage];
        private readonly Button[] _tabs = new Button[3];
        private TextMeshProUGUI _pageLabel;
        private TextMeshProUGUI _empty;
        private Button _previous;
        private Button _next;
        private List _list = List.All;
        private int _page;
        private Texture2D _sheet;
        private int _sheetPage = -1;
        private int _loading = -1;

        /// <summary>Opens the collection over the main screen of the menu.</summary>
        public static void Open(Transform main)
        {
            var existing = main.Find(ObjectName);
            var collection = existing != null ? existing.GetComponent<ChogCollection>() : null;
            if (collection == null)
            {
                var host = ChogUi.Area(ObjectName, main, Vector2.zero, Vector2.one);
                collection = host.gameObject.AddComponent<ChogCollection>();
                collection.Build(main);
            }
            collection.gameObject.SetActive(true);
            collection.transform.SetAsLastSibling();
            // The page of the Genesis played opens first.
            var played = WalletPanel.PlayedMetadataId;
            if (collection._list == List.All && played > 0)
                collection._page = (played - 1) / PerPage;
            if (GenesisRental.State == RentalState.Open)
                GenesisRental.ReadOffers();
            collection.Refresh();
        }

        private void OnEnable()
        {
            PlayerWallet.Changed += Refresh;
            GenesisRental.Changed += Refresh;
            GuestPass.Changed += Refresh;
        }

        private void OnDisable()
        {
            PlayerWallet.Changed -= Refresh;
            GenesisRental.Changed -= Refresh;
            GuestPass.Changed -= Refresh;
            // A closed collection keeps no page in memory.
            ForgetSheet();
        }

        private void Build(Transform main)
        {
            _main = main;
            _roster = Resources.Load<GenesisRoster>(RosterResource);
            if (_roster != null && _roster.Entries != null)
                for (var i = 0; i < _roster.Entries.Length; i++)
                    _byMetadata[_roster.Entries[i].MetadataId] = i;

            var night = gameObject.AddComponent<Image>();
            night.sprite = ChogSprites.Gradient(ChogTheme.Night, ChogTheme.NightHigh);

            var back = ChogUi.PinnedButton("Back", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -26f),
                new Vector2(84f, 84f), ChogTheme.ButtonKind.Secondary, "", 30f, ChogIcons.Icon.Back);
            back.onClick.AddListener(() => gameObject.SetActive(false));
            ChogUi.Title("Title", ChogUi.Pinned("TitleArea", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(134f, -22f),
                new Vector2(600f, 62f)), Vector2.zero, Vector2.one, 56f, TextAlignmentOptions.Left).text = "HEROES";
            ChogUi.Body("Count", ChogUi.Pinned("CountArea", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(134f, -86f),
                new Vector2(900f, 34f)), Vector2.zero, Vector2.one, 26f, TextAlignmentOptions.Left, ChogTheme.PurpleSoft).text =
                "1,969 Chog Genesis, each a hero of its own. Tap one to see what its traits make of it.";

            // The three lists, from the top right corner.
            var names = new[] { "ALL", "MINE", "FOR RENT" };
            for (var i = 0; i < 3; i++)
            {
                var list = (List)i;
                var right = -Margin - (2 - i) * 236f;
                _tabs[i] = ChogUi.PinnedButton("Tab" + names[i], transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(right, -30f),
                    new Vector2(220f, 76f), ChogTheme.ButtonKind.Quiet, names[i], 32f);
                _tabs[i].onClick.AddListener(() =>
                {
                    _list = list;
                    _page = 0;
                    Refresh();
                });
            }

            // Eight by six, the rows from the top.
            var grid = ChogUi.Area("Grid", transform, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.86f));
            for (var i = 0; i < PerPage; i++)
            {
                var column = i % Columns;
                var row = i / Columns;
                var area = ChogUi.Area("Cell" + i, grid, new Vector2(column / (float)Columns, 1f - (row + 1) / (float)Rows),
                    new Vector2((column + 1) / (float)Columns, 1f - row / (float)Rows));
                var hit = area.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var cell = new Cell { Root = area.gameObject };
                area.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSheet(cell));

                var picture = ChogUi.Pinned("Picture", area, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(112f, 112f));
                cell.Frame = ChogUi.Plate("Frame", picture, Vector2.zero, Vector2.one, ChogTheme.Ink, 14);
                cell.Picture = ChogUi.Area("Image", picture, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
                cell.Picture.rectTransform.offsetMin = new Vector2(5f, 5f);
                cell.Picture.rectTransform.offsetMax = new Vector2(-5f, -5f);
                cell.Picture.raycastTarget = false;
                cell.Number = ChogUi.Title("Number", area, new Vector2(0.56f, 0.50f), new Vector2(1f, 0.86f), 30f, TextAlignmentOptions.Left);
                cell.Number.textWrappingMode = TextWrappingModes.NoWrap;
                cell.Detail = ChogUi.Number("Detail", area, new Vector2(0.56f, 0.16f), new Vector2(1f, 0.50f), 20f, TextAlignmentOptions.TopLeft,
                    ChogTheme.PurpleSoft, false);
                _cells[i] = cell;
            }

            _empty = ChogUi.Body("Empty", transform, new Vector2(0.15f, 0.40f), new Vector2(0.85f, 0.60f), 40f, TextAlignmentOptions.Center,
                ChogTheme.PurpleSoft);
            _previous = ChogUi.Button("Previous", transform, new Vector2(0.30f, 0.025f), new Vector2(0.37f, 0.10f), ChogTheme.ButtonKind.Quiet, "<", 44f);
            _previous.onClick.AddListener(() => Turn(-1));
            _pageLabel = ChogUi.Title("Page", transform, new Vector2(0.37f, 0.03f), new Vector2(0.63f, 0.095f), 34f);
            _next = ChogUi.Button("Next", transform, new Vector2(0.63f, 0.025f), new Vector2(0.70f, 0.10f), ChogTheme.ButtonKind.Quiet, ">", 44f);
            _next.onClick.AddListener(() => Turn(1));
        }

        private void Update()
        {
            // On a computer the arrow keys and the wheel turn the pages too.
            var keys = Keyboard.current;
            if (keys != null && keys.rightArrowKey.wasPressedThisFrame)
                Turn(1);
            else if (keys != null && keys.leftArrowKey.wasPressedThisFrame)
                Turn(-1);
            else if (keys != null && keys.escapeKey.wasPressedThisFrame && !SheetOpen())
                gameObject.SetActive(false);
            var mouse = Mouse.current;
            if (mouse != null && !SheetOpen())
            {
                var scroll = mouse.scroll.ReadValue().y;
                if (scroll < -0.5f)
                    Turn(1);
                else if (scroll > 0.5f)
                    Turn(-1);
            }
        }

        private bool SheetOpen()
        {
            var sheet = _main != null ? _main.Find("ChogHeroSheet") : null;
            return sheet != null && sheet.gameObject.activeSelf && sheet.GetSiblingIndex() > transform.GetSiblingIndex();
        }

        private void Turn(int direction)
        {
            var pages = PageCount();
            var page = Mathf.Clamp(_page + direction, 0, Mathf.Max(0, pages - 1));
            if (page == _page)
                return;
            _page = page;
            Refresh();
        }

        private int PageCount()
        {
            var count = _list == List.All ? LastMetadataId : Listed().Count;
            return (count + PerPage - 1) / PerPage;
        }

        // The token ids of MINE or FOR RENT, in the order of their numbers.
        private List<int> Listed()
        {
            var tokens = new List<int>();
            if (_list == List.Mine)
            {
                if (PlayerWallet.State == WalletState.Ready)
                    tokens.AddRange(PlayerWallet.Playable);
                if (GuestPass.State == GuestPassState.Valid && !tokens.Contains(GuestPass.TokenId))
                    tokens.Add(GuestPass.TokenId);
            }
            else if (_list == List.ForRent)
            {
                foreach (var offer in GenesisRental.Offers)
                    tokens.Add(offer.TokenId);
            }
            tokens.Sort((a, b) => MetadataOf(a).CompareTo(MetadataOf(b)));
            return tokens;
        }

        private int MetadataOf(int tokenId)
        {
            var index = _roster != null ? _roster.IndexOf(tokenId) : -1;
            return index >= 0 ? _roster.Entries[index].MetadataId : int.MaxValue;
        }

        private void Refresh()
        {
            if (_pageLabel == null || !isActiveAndEnabled)
                return;
            for (var i = 0; i < _tabs.Length; i++)
                ChogTheme.SetButtonFill(_tabs[i], (int)_list == i ? ChogTheme.Purple : ChogTheme.Quiet);
            ChogUi.LabelOf(_tabs[1]).text = "MINE" + (PlayerWallet.State == WalletState.Ready ? " " + PlayerWallet.Playable.Length : "");
            ChogUi.LabelOf(_tabs[2]).text = "FOR RENT" + (GenesisRental.OffersRead ? " " + GenesisRental.Offers.Count : "");

            var pages = PageCount();
            _page = Mathf.Clamp(_page, 0, Mathf.Max(0, pages - 1));
            _previous.interactable = _page > 0;
            _next.interactable = _page < pages - 1;
            _previous.gameObject.SetActive(pages > 1);
            _next.gameObject.SetActive(pages > 1);

            var played = WalletPanel.PlayedTokenId;
            if (_list == List.All)
            {
                var first = _page * PerPage + 1;
                var last = Mathf.Min(LastMetadataId, first + PerPage - 1);
                _pageLabel.text = "#" + first + " TO #" + last + "  <size=70%>" + (_page + 1) + " / " + pages + "</size>";
                _empty.text = "";
                ShowSheet(_page);
                for (var i = 0; i < PerPage; i++)
                {
                    var metadata = first + i;
                    var index = metadata <= LastMetadataId && _byMetadata.TryGetValue(metadata, out var found) ? found : -1;
                    ShowCell(_cells[i], index, i, played, "");
                }
                return;
            }

            ForgetSheet();
            var tokens = Listed();
            _pageLabel.text = pages > 1 ? (_page + 1) + " / " + pages : "";
            _empty.text = tokens.Count > 0 ? ""
                : _list == List.Mine
                    ? PlayerWallet.State == WalletState.Ready
                        ? "This wallet plays no Genesis yet. Rent one in FOR RENT, or play a recruit."
                        : "Connect the wallet that holds your Genesis on the home screen to see them here."
                    : GenesisRental.OffersRead ? "No Genesis is offered for rent yet." : "Reading the offers on Monad...";
            for (var i = 0; i < PerPage; i++)
            {
                var at = _page * PerPage + i;
                var token = at < tokens.Count ? tokens[at] : 0;
                var index = token > 0 && _roster != null ? _roster.IndexOf(token) : -1;
                var detail = "";
                if (index >= 0 && _list == List.ForRent && GenesisRental.TryGetOffer(token, out var offer))
                    detail = MonadTransactions.PriceText(offer.PricePerDayWei) + " A DAY";
                else if (index >= 0 && _list == List.Mine && Array.IndexOf(PlayerWallet.Tokens, token) < 0)
                    detail = GuestPass.State == GuestPassState.Valid && GuestPass.TokenId == token ? "LENT TO YOU" : "RENTED";
                ShowCell(_cells[i], index, -1, played, detail);
            }
        }

        // A cell: "index" in the roster (-1 for none), "onSheet" its place on the loaded page of ALL
        // (-1 when its own portrait is loaded instead), "detail" a line under its class.
        private void ShowCell(Cell cell, int index, int onSheet, int played, string detail)
        {
            cell.Root.SetActive(index >= 0);
            if (index < 0)
            {
                cell.TokenId = 0;
                return;
            }

            var hero = _roster.Entries[index];
            cell.TokenId = hero.TokenId;
            cell.Number.text = "#" + hero.MetadataId.ToString(CultureInfo.InvariantCulture);
            var role = (_roster.CardLines(hero)[0].Gives ?? "").ToUpperInvariant();
            cell.Detail.text = role + (detail.Length > 0 ? "\n<color=#FFE27A>" + detail + "</color>" : "");
            cell.Frame.color = hero.TokenId == played ? ChogTheme.Action
                : PlayerWallet.State == WalletState.Ready && Array.IndexOf(PlayerWallet.Tokens, hero.TokenId) >= 0 ? ChogTheme.Confirm
                : ChogTheme.Ink;

            if (onSheet >= 0)
            {
                // A share of the page's picture: its column from the left, its row from the top.
                cell.MetadataId = 0;
                cell.Picture.texture = _sheet != null ? _sheet : GenesisArt.Disc();
                cell.Picture.color = _sheet != null ? Color.white : new Color(0.25f, 0.22f, 0.4f, 1f);
                cell.Picture.uvRect = new Rect((onSheet % Columns) / (float)Columns, 1f - (onSheet / Columns + 1) / (float)Rows,
                    1f / Columns, 1f / Rows);
                return;
            }

            cell.Picture.uvRect = new Rect(0f, 0f, 1f, 1f);
            if (cell.MetadataId == hero.MetadataId)
                return;
            cell.MetadataId = hero.MetadataId;
            cell.Picture.texture = GenesisArt.Disc();
            cell.Picture.color = new Color(0.25f, 0.22f, 0.4f, 1f);
            var metadata = hero.MetadataId;
            GenesisArt.Request(metadata, texture =>
            {
                if (cell.Picture == null || cell.MetadataId != metadata)
                    return;
                cell.Picture.texture = texture;
                cell.Picture.color = Color.white;
            });
        }

        private void OpenSheet(Cell cell)
        {
            if (cell.TokenId > 0)
                ChogHeroSheet.Open(_main, cell.TokenId);
        }

        // The picture of a page of ALL: asked once, the one before let go.
        private void ShowSheet(int page)
        {
            if (page == _sheetPage || page == _loading)
                return;
            ForgetSheet();
            _loading = page;
            StartCoroutine(LoadSheet(page));
        }

        private IEnumerator LoadSheet(int page)
        {
            var url = GenesisArt.PagesBaseUrl(Application.absoluteURL) + page + ".jpg";
            using (var request = UnityWebRequestTexture.GetTexture(url, true))
            {
                UnityWebRequestAsyncOperation sending = null;
                try
                {
                    sending = request.SendWebRequest();
                }
                catch (InvalidOperationException refused)
                {
                    Debug.LogWarning("CHOG_COLLECTION refused " + url + " : " + refused.Message);
                }
                if (sending != null)
                    yield return sending;
                if (_loading != page)
                    yield break;
                _loading = -1;
                if (sending == null || request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("CHOG_COLLECTION page " + page + " not loaded: " + request.error);
                    yield break;
                }
                _sheet = DownloadHandlerTexture.GetContent(request);
                _sheet.wrapMode = TextureWrapMode.Clamp;
                _sheetPage = page;
            }
            Refresh();
        }

        private void ForgetSheet()
        {
            _loading = -1;
            _sheetPage = -1;
            if (_sheet != null)
                Destroy(_sheet);
            _sheet = null;
        }
    }
}
