using System.Collections.Generic;
using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using GenesisRoster = Quantum.GenesisRoster;

namespace Chog.Screens
{
    /// <summary>
    /// The three cheapest Genesis for rent, for a player without a Genesis: "RENT A GENESIS FOR
    /// 1 DAY", each with its portrait, its name and the price of a day in $CHOG and dollars; a
    /// tap opens the rental of that one (the wallet is connected first when it is not). The shop
    /// is one tap away from the home screen in the references (Brawl Stars and Clash Royale put
    /// their offers on the home screen itself); here the offer is a real Genesis, read on Monad.
    /// Empty, and hidden by whoever placed it, while nothing is offered. Shows only what the
    /// rental read (GenesisRental.Offers): nothing is made up.
    /// </summary>
    public sealed class RentalOffersCard : MonoBehaviour
    {
        private const string RosterResource = "GenesisRoster";
        private const int Shown = 3;
        private const float ReadEverySeconds = 60f;
        private static readonly Color WaitingColor = new Color(0.25f, 0.25f, 0.3f, 0.85f);

        private sealed class Row
        {
            public GameObject Root;
            public RawImage Portrait;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Price;
            public int TokenId;
            public int MetadataId;
        }

        private GenesisRoster _roster;
        private TextMeshProUGUI _title;
        private readonly Row[] _rows = new Row[Shown];
        private float _readAt;

        /// <summary>How many offers the card shows now; 0 while nothing is offered.</summary>
        public int Count { get; private set; }

        /// <summary>Builds the card in "parent", between two points in shares of it.</summary>
        public static RentalOffersCard Create(Transform parent, Vector2 min, Vector2 max)
        {
            var area = ChogUi.Area("RentalOffers", parent, min, max);
            var card = area.gameObject.AddComponent<RentalOffersCard>();
            card.Build(area);
            return card;
        }

        private void Build(RectTransform area)
        {
            _roster = Resources.Load<GenesisRoster>(RosterResource);
            _title = ChogUi.Number("Title", area, new Vector2(0f, 0.86f), new Vector2(1f, 1f), 22f, TextAlignmentOptions.Left, ChogTheme.Purple, false);
            _title.text = "RENT A GENESIS FOR 1 DAY";
            for (var i = 0; i < Shown; i++)
            {
                var top = 0.84f - i * 0.28f;
                var row = ChogUi.Area("Offer" + i, area, new Vector2(0f, top - 0.26f), new Vector2(1f, top));
                var plate = row.gameObject.AddComponent<Image>();
                plate.sprite = ChogSprites.Rounded(14, 2, ChogTheme.Line, new Color(0.08f, 0.06f, 0.14f, 0.12f));
                plate.type = Image.Type.Sliced;
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = plate;
                var made = new Row { Root = row.gameObject };
                button.onClick.AddListener(() => WalletPanel.OpenRentalOf(made.TokenId));

                var holder = ChogUi.Area("PortraitHolder", row, new Vector2(0.02f, 0.08f), new Vector2(0.26f, 0.92f));
                made.Portrait = ChogUi.Area("Portrait", holder, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
                made.Portrait.texture = GenesisArt.Disc();
                made.Portrait.color = WaitingColor;
                made.Portrait.raycastTarget = false;
                var fitter = made.Portrait.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1f;
                made.Name = ChogUi.Title("Name", row, new Vector2(0.30f, 0.50f), new Vector2(0.98f, 0.94f), 26f, TextAlignmentOptions.Left, ChogTheme.Ink);
                ChogTheme.AsTitle(made.Name, outlined: false);
                made.Name.textWrappingMode = TextWrappingModes.NoWrap;
                made.Price = ChogUi.Body("Price", row, new Vector2(0.30f, 0.06f), new Vector2(0.98f, 0.50f), 22f, TextAlignmentOptions.Left, ChogTheme.Purple);
                _rows[i] = made;
            }
            Refresh();
        }

        private void OnEnable()
        {
            GenesisRental.Changed += Refresh;
            ChogPrice.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GenesisRental.Changed -= Refresh;
            ChogPrice.Changed -= Refresh;
        }

        private void Update()
        {
            // The offers move: read again now and then while the card is on screen.
            if (Time.unscaledTime < _readAt || GenesisRental.State != RentalState.Open)
                return;
            _readAt = Time.unscaledTime + ReadEverySeconds;
            GenesisRental.ReadOffers();
        }

        /// <summary>"10 $CHOG ($0.02) a day": the price line of an offer.</summary>
        public static string PriceLine(string pricePerDayWei, double usdPerChog) => ChogPrice.WithUsd(pricePerDayWei, usdPerChog) + " a day";

        private void Refresh()
        {
            if (_title == null)
                return;
            var offers = GenesisRental.Cheapest(Shown);
            Count = offers.Count;
            for (var i = 0; i < Shown; i++)
                ShowRow(_rows[i], i < offers.Count ? offers[i] : default);
        }

        private void ShowRow(Row row, RentalOffer offer)
        {
            row.Root.SetActive(offer.TokenId > 0);
            row.TokenId = offer.TokenId;
            if (offer.TokenId <= 0)
                return;

            var index = _roster != null ? _roster.IndexOf(offer.TokenId) : -1;
            row.Name.text = index >= 0 ? GenesisHeroCard.DisplayName(_roster.Entries[index]).ToUpperInvariant() : "GENESIS " + offer.TokenId;
            row.Price.text = PriceLine(offer.PricePerDayWei, ChogPrice.UsdPerChog);
            var metadataId = index >= 0 ? _roster.Entries[index].MetadataId : 0;
            if (metadataId == row.MetadataId)
                return;

            row.MetadataId = metadataId;
            row.Portrait.texture = GenesisArt.Disc();
            row.Portrait.color = WaitingColor;
            if (metadataId <= 0)
                return;
            GenesisArt.Request(metadataId, texture =>
            {
                if (row.Portrait == null || row.MetadataId != metadataId)
                    return;
                row.Portrait.texture = texture;
                row.Portrait.color = Color.white;
            });
        }
    }
}
