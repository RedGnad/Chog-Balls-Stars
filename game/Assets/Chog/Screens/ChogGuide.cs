using Chog.Match;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// Three short cards over the menu the first time the game is opened: what a Genesis does in the game,
    /// how to fight on a phone and on a computer, how a match is won. A HOW TO PLAY button under the play
    /// buttons brings them back. Seen once per browser (PlayerPrefs). A page measured or tested by script
    /// (its address carries "perf=1" or "noguide=1") never shows them, so the scripted taps reach the menu.
    /// </summary>
    public sealed class ChogGuide : MonoBehaviour
    {
        private const string ObjectName = "ChogGuide";
        private const string ButtonName = "ChogGuideButton";
        private const string SeenKey = "chog.guide.seen";

        private static readonly (string Title, string Text)[] Cards =
        {
            ("YOUR GENESIS IS YOUR HERO",
             "Connect the wallet that holds your Chog Genesis: its traits build its hero in 3D and decide how it " +
             "fights. Its hat gives its basic attack, its eyes its special, its outfit its class, its aura the " +
             "effect of its special.\n\nNo Genesis? You play a grey recruit: 15% less health and no Genesis attack."),
            ("HOW TO FIGHT",
             "On a phone: the left stick moves, the pink stick throws your basic attack, the gold stick fires your " +
             "special once it is charged.\n\nOn a computer: W A S D to move, the mouse to aim, left click to " +
             "attack, right click for the special."),
            ("HOW TO WIN",
             "Each team defends a tower and a core. Bring down the enemy core and your team wins at once. If no " +
             "core falls, the coins your team picked up decide.\n\nYour Genesis keeps its career: matches, wins, " +
             "badges."),
        };

        private int _card;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _text;
        private TextMeshProUGUI _dots;
        private TextMeshProUGUI _nextLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Attach();
            Attach();
        }

        private static bool Scripted()
        {
            var url = Application.absoluteURL ?? "";
            return url.Contains("perf=1") || url.Contains("noguide=1");
        }

        private static void Attach()
        {
            var main = FindAnyObjectByType<QuantumMenuUIMain>(FindObjectsInactive.Include);
            if (main == null || main.transform.Find(ButtonName) != null)
                return;

            // HOW TO PLAY, under the play buttons of the left column (ChogMenuSkin).
            var area = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)area.transform;
            rect.SetParent(main.transform, false);
            rect.anchorMin = new Vector2(0.06f, 0.035f);
            rect.anchorMax = new Vector2(0.20f, 0.105f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var button = area.GetComponent<Button>();
            button.targetGraphic = area.GetComponent<Image>();
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var labelArea = label.rectTransform;
            labelArea.SetParent(rect, false);
            labelArea.anchorMin = Vector2.zero;
            labelArea.anchorMax = Vector2.one;
            labelArea.offsetMin = Vector2.zero;
            labelArea.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 30f;
            label.text = "HOW TO PLAY";
            label.raycastTarget = false;
            ChogTheme.Style(button, ChogTheme.ButtonKind.Quiet);
            button.onClick.AddListener(() => Open(main.transform));

            if (!Scripted() && PlayerPrefs.GetInt(SeenKey, 0) == 0)
                Open(main.transform);
        }

        private static void Open(Transform main)
        {
            var existing = main.Find(ObjectName);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.GetComponent<ChogGuide>().Show(0);
                return;
            }

            var host = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)host.transform;
            rect.SetParent(main, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            // The dim catches the taps meant for the menu under it.
            host.GetComponent<Image>().color = new Color(ChogTheme.Night.r, ChogTheme.Night.g, ChogTheme.Night.b, 0.86f);
            host.AddComponent<ChogGuide>().Build();
        }

        private void Build()
        {
            var card = Child<Image>("Card", transform, new Vector2(0.17f, 0.12f), new Vector2(0.83f, 0.88f));
            ChogTheme.AsCard(card, raised: true);

            _title = Text("Title", card.transform, new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.94f), 84f);
            ChogTheme.AsTitle(_title);
            _title.color = ChogTheme.Cream;
            _text = Text("Text", card.transform, new Vector2(0.07f, 0.26f), new Vector2(0.93f, 0.74f), 40f);
            ChogTheme.AsBody(_text);
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.color = ChogTheme.Text;
            _dots = Text("Dots", card.transform, new Vector2(0.06f, 0.06f), new Vector2(0.30f, 0.18f), 40f);
            ChogTheme.AsTitle(_dots);
            _dots.color = ChogTheme.PurpleSoft;

            var skip = MakeButton(card.transform, "Skip", new Vector2(0.42f, 0.06f), new Vector2(0.62f, 0.19f), out var skipLabel);
            skipLabel.text = "SKIP";
            ChogTheme.Style(skip, ChogTheme.ButtonKind.Quiet);
            skip.onClick.AddListener(Close);
            var next = MakeButton(card.transform, "Next", new Vector2(0.65f, 0.06f), new Vector2(0.94f, 0.19f), out _nextLabel);
            ChogTheme.Style(next, ChogTheme.ButtonKind.Primary);
            next.onClick.AddListener(() =>
            {
                if (_card + 1 < Cards.Length)
                    Show(_card + 1);
                else
                    Close();
            });
            Show(0);
        }

        // The other panels of the menu are added at the same moment, some after it: it stays in front.
        private void LateUpdate()
        {
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();
        }

        private void Show(int card)
        {
            _card = card;
            _title.text = Cards[card].Title;
            _text.text = Cards[card].Text;
            _nextLabel.text = card + 1 < Cards.Length ? "NEXT" : "LET'S PLAY";
            var dots = "";
            for (var i = 0; i < Cards.Length; i++)
                dots += i == card ? "<color=#D83E7E>O</color> " : "O ";
            _dots.text = dots.Trim();
        }

        private void Close()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }

        private static Button MakeButton(Transform parent, string name, Vector2 min, Vector2 max, out TextMeshProUGUI label)
        {
            var image = Child<Image>(name, parent, min, max);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = Text("Label", image.transform, Vector2.zero, Vector2.one, 40f);
            return button;
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
