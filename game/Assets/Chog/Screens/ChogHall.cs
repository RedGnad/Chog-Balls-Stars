using Chog.Match;
using Quantum.Menu;
using GenesisRoster = Quantum.GenesisRoster;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The leaderboard of the collection: TOP on the home screen (ChogHome) opens the Genesis with
    /// the most wins, as the game services keep their careers (GenesisCareers.ReadTop), each with its
    /// picture, its wins, its matches and its mastery. Read each time it opens; nothing is shown that
    /// the services did not send: with no match recorded yet it says so.
    /// </summary>
    public sealed class ChogHall : MonoBehaviour
    {
        private const string ObjectName = "ChogHall";
        private const string RosterResource = "GenesisRoster";
        private const int Columns = 2;
        private const int RowsPerColumn = GenesisCareers.TopCount / Columns;
        private const float PortraitSize = 84f;

        private TextMeshProUGUI _status;
        private TextMeshProUGUI _empty;
        private RectTransform[] _rows;
        private GenesisRoster _roster;

        /// <summary>The hall over the main screen of the menu (TOP on the home screen, ChogHome).</summary>
        public static void Open(Transform main)
        {
            var existing = main.Find(ObjectName);
            var hall = existing != null ? existing.GetComponent<ChogHall>() : null;
            if (hall == null)
            {
                var host = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)host.transform;
                rect.SetParent(main, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                // The dim catches the taps meant for the menu under it.
                host.GetComponent<Image>().color = new Color(ChogTheme.Night.r, ChogTheme.Night.g, ChogTheme.Night.b, 0.88f);
                hall = host.AddComponent<ChogHall>();
                hall.Build();
            }

            hall.gameObject.SetActive(true);
            hall.transform.SetAsLastSibling();
            hall.Load();
        }

        private void Build()
        {
            _roster = Resources.Load<GenesisRoster>(RosterResource);
            var card = Child<Image>("Card", transform, new Vector2(0.14f, 0.07f), new Vector2(0.86f, 0.93f));
            ChogTheme.AsCard(card, raised: true);

            var title = Text("Title", card.transform, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f), 84f);
            ChogTheme.AsTitle(title);
            title.color = ChogTheme.Cream;
            title.text = "TOP GENESIS";

            var about = Text("About", card.transform, new Vector2(0.06f, 0.77f), new Vector2(0.94f, 0.85f), 28f);
            ChogTheme.AsBody(about);
            about.color = ChogTheme.Muted;
            about.text = "The Genesis with the most wins. A match counts for a Genesis when the one who plays it is " +
                         "signed in: its holder, a renter, or a guest the holder lent it to.";

            // Two columns of rows, the best at the top of the left one.
            _rows = new RectTransform[GenesisCareers.TopCount];
            const float top = 0.75f, bottom = 0.18f, left = 0.04f, right = 0.96f, gap = 0.02f;
            var columnWidth = (right - left - gap * (Columns - 1)) / Columns;
            var rowHeight = (top - bottom) / RowsPerColumn;
            for (var i = 0; i < _rows.Length; i++)
            {
                var column = i / RowsPerColumn;
                var line = i % RowsPerColumn;
                var x = left + column * (columnWidth + gap);
                var y = top - (line + 1) * rowHeight;
                var row = Child<Image>("Row" + (i + 1), card.transform,
                    new Vector2(x, y + rowHeight * 0.06f), new Vector2(x + columnWidth, y + rowHeight * 0.94f));
                // A dark rounded plate; the first three ringed in gold, as a podium.
                row.sprite = ChogSprites.Rounded(14, 3, i < 3 ? ChogTheme.Action : ChogTheme.Line, new Color(0.08f, 0.06f, 0.14f, 0.6f));
                row.type = Image.Type.Sliced;
                row.pixelsPerUnitMultiplier = 0.9f;
                row.raycastTarget = false;
                var rank = Text("Rank", row.transform, new Vector2(0f, 0f), new Vector2(0.11f, 1f), 48f);
                ChogTheme.AsTitle(rank);
                rank.color = i < 3 ? ChogTheme.Gold : ChogTheme.PurpleSoft;
                rank.text = (i + 1).ToString();
                var words = Text("Words", row.transform, new Vector2(0.30f, 0.06f), new Vector2(0.98f, 0.94f), 34f);
                ChogTheme.AsBody(words);
                words.alignment = TextAlignmentOptions.Left;
                words.richText = true;
                _rows[i] = (RectTransform)row.transform;
            }

            // Where the rows go, what is said while there is none.
            _empty = Text("Empty", card.transform, new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.66f), 44f);
            ChogTheme.AsBody(_empty);
            _empty.color = ChogTheme.PurpleSoft;

            _status = Text("Status", card.transform, new Vector2(0.05f, 0.04f), new Vector2(0.62f, 0.15f), 30f);
            ChogTheme.AsBody(_status);
            _status.color = ChogTheme.Text;
            _status.alignment = TextAlignmentOptions.Left;

            var close = Child<Image>("Close", card.transform, new Vector2(0.68f, 0.04f), new Vector2(0.95f, 0.15f));
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close;
            Text("Label", close.transform, Vector2.zero, Vector2.one, 40f).text = "CLOSE";
            ChogTheme.Style(closeButton, ChogTheme.ButtonKind.Quiet);
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void Load()
        {
            foreach (var row in _rows)
                row.gameObject.SetActive(false);
            _empty.text = "";
            _status.text = "Reading the careers...";
            GenesisCareers.ReadTop(careers =>
            {
                if (this == null)
                    return;
                if (careers == null)
                {
                    _status.text = "The leaderboard cannot be read right now. Try again in a moment.";
                    return;
                }

                for (var i = 0; i < _rows.Length; i++)
                {
                    var shown = i < careers.Length;
                    _rows[i].gameObject.SetActive(shown);
                    if (shown)
                        Fill(_rows[i], careers[i]);
                }

                _status.text = "";
                _empty.text = careers.Length == 0
                    ? "No Genesis has a recorded match yet.\n\nSign in with a wallet that holds one and play a match: " +
                      "its career starts here."
                    : "";
            });
        }

        // One Genesis: its picture, its name (in gold for the player's own), wins, matches and mastery.
        private void Fill(RectTransform row, GenesisCareer career)
        {
            var index = _roster != null ? _roster.IndexOf(career.tokenId) : -1;
            var known = index >= 0;
            GenesisPortrait.Show(row, known ? _roster.Entries[index].MetadataId : 0, PortraitSize, new Vector2(0.12f, 0.5f),
                new Vector2(0f, 0.5f), Vector2.zero);

            var mine = career.tokenId == PlayerWallet.ChosenTokenId;
            var name = known ? GenesisHeroCard.DisplayName(_roster.Entries[index]) : "Genesis " + career.tokenId;
            var mastery = GenesisCareers.DescribeMastery(career);
            var words = row.Find("Words").GetComponent<TextMeshProUGUI>();
            words.text = "<b>" + (mine ? "<color=#F59E0B>" + name + "  (YOURS)</color>" : name) + "</b>\n<size=78%>" +
                         career.wins + (career.wins == 1 ? " win, " : " wins, ") +
                         career.matches + (career.matches == 1 ? " match" : " matches") +
                         (mastery.Length > 0 ? "\n" + mastery : "") + "</size>";
        }

        // The other panels of the menu are added at the same moment, some after it: it stays in front.
        private void LateUpdate()
        {
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();
        }

        private static T Child<T>(string name, Transform parent, Vector2 min, Vector2 max) where T : Component
        {
            var thing = new GameObject(name, typeof(RectTransform));
            var area = (RectTransform)thing.transform;
            area.SetParent(parent, false);
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            return thing.AddComponent<T>();
        }

        private static TextMeshProUGUI Text(string name, Transform parent, Vector2 min, Vector2 max, float largest)
        {
            var text = Child<TextMeshProUGUI>(name, parent, min, max);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = largest;
            text.raycastTarget = false;
            return text;
        }
    }
}
