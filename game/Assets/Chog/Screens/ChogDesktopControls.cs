using Chog.Match;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// What a computer shows in place of the touch sticks: three cards along the bottom of a match
    /// that name the keys (W A S D to move, left click to attack, right click or Space for the
    /// special) and fill with what they wait for, the energy of the basic attack and the charge of
    /// the special, as the sticks do on a phone. Shown while ControlModeWatcher says Desktop and the
    /// local player has a hero; nothing on it takes a click.
    /// </summary>
    public sealed class ChogDesktopControls : MonoBehaviour
    {
        private const float Margin = 32f;
        private const float Gap = 16f;
        private const float CardHeight = 112f;
        // Under the canvases of the match (order 0): its menus cover the cards.
        private const int SortingOrder = -5;

        private static readonly Color Dim = new Color(1f, 1f, 1f, 0.82f);

        private GameObject _canvas;
        private Card _attack, _special;
        private string _specialName;
        private EntityRef _hero;

        private sealed class Card
        {
            public TextMeshProUGUI Action;
            public RectTransform Fill;
            public Image Frame;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Desktop Controls");
            DontDestroyOnLoad(host);
            host.AddComponent<ChogDesktopControls>();
        }

        private void Update()
        {
            QuantumGame game = null;
            // Not on the end of a match, which has its own buttons and keys.
            var frame = ControlModeWatcher.Mode == ControlMode.Desktop && !ChogResultScreen.IsShown ? LocalFrame(out game) : null;
            var hero = frame != null ? LocalHero(frame, game) : EntityRef.None;
            if (hero == EntityRef.None)
            {
                if (_canvas != null)
                    _canvas.SetActive(false);
                return;
            }

            if (_canvas == null)
                Build();
            _canvas.SetActive(true);
            if (hero != _hero)
            {
                _hero = hero;
                var name = SpecialButtonLabel.Describe(game);
                _specialName = name != null ? name.Replace("\n", " ") : "SPECIAL";
            }

            var attacks = frame.Get<CharacterAttacks>(hero);
            Show(_attack, frame, hero, EAttributeType.Energy, attacks.BasicSkillData, "ATTACK", ChogTheme.Cheek);
            Show(_special, frame, hero, EAttributeType.Special, attacks.SpecialSkillData, _specialName, ChogTheme.Gold);
        }

        private static Frame LocalFrame(out QuantumGame game)
        {
            var runner = QuantumRunner.Default;
            game = runner != null ? runner.Game : null;
            return game != null && game.Frames != null ? game.Frames.Predicted : null;
        }

        private static EntityRef LocalHero(Frame frame, QuantumGame game)
        {
            var heroes = frame.Filter<PlayerLink, CharacterAttacks>();
            while (heroes.Next(out var entity, out var link, out _))
                if (game.PlayerIsLocal(link.PlayerRef))
                    return entity;
            return EntityRef.None;
        }

        // The bar fills with the attribute; the card lights up once there is enough for one use.
        private static void Show(Card card, Frame frame, EntityRef hero, EAttributeType type,
                                 AssetRef<SkillData> skill, string action, Color colour)
        {
            if (!AttributesHelper.Has(frame, hero, type))
                return;
            float current = AttributesHelper.GetCurrentValue(frame, hero, type).AsFloat;
            float most = AttributesHelper.GetMaxValue(frame, hero, type).AsFloat;
            var data = frame.FindAsset<SkillData>(skill.Id);
            bool ready = data == null || current >= data.Cost.AsFloat;

            card.Fill.anchorMax = new Vector2(most > 0f ? Mathf.Clamp01(current / most) : 0f, 1f);
            card.Frame.color = ready ? Color.white : Dim;
            var tint = "#" + ColorUtility.ToHtmlStringRGB(ready ? colour : ChogTheme.Muted);
            card.Action.text = "<color=" + tint + ">" + action + "</color>" + (ready && type == EAttributeType.Special
                ? "  <color=#" + ColorUtility.ToHtmlStringRGB(ChogTheme.Ok) + ">READY</color>"
                : "");
        }

        private void Build()
        {
            _canvas = new GameObject("Chog Desktop Controls Canvas", typeof(Canvas), typeof(CanvasScaler));
            _canvas.transform.SetParent(transform, false);
            var canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = (RectTransform)_canvas.transform;
            MakeCard(root, new Vector2(0f, 0f), new Vector2(Margin, Margin), 250f, "MOVE", "W A S D", null);
            _special = MakeCard(root, new Vector2(1f, 0f), new Vector2(-Margin, Margin), 360f, "SPECIAL",
                                "RIGHT CLICK <size=60%>OR SPACE</size>", ChogTheme.Gold);
            _attack = MakeCard(root, new Vector2(1f, 0f), new Vector2(-Margin - 360f - Gap, Margin), 270f, "ATTACK",
                               "LEFT CLICK", ChogTheme.Cheek);
        }

        // A dark card on a corner: the action in small letters over the keys in large ones, and,
        // when given a colour, a bar under them.
        private static Card MakeCard(RectTransform root, Vector2 corner, Vector2 position, float width,
                                     string action, string keys, Color? bar)
        {
            var frame = new GameObject("Card " + action, typeof(RectTransform), typeof(Image));
            var area = (RectTransform)frame.transform;
            area.SetParent(root, false);
            area.anchorMin = area.anchorMax = area.pivot = corner;
            area.anchoredPosition = position;
            area.sizeDelta = new Vector2(width, CardHeight);
            var image = frame.GetComponent<Image>();
            ChogTheme.AsCard(image);
            image.raycastTarget = false;

            var card = new Card { Frame = image };
            card.Action = Text(area, "Action", new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.94f), 24f, false);
            card.Action.text = action;
            card.Action.color = Color.white;
            var big = Text(area, "Keys", new Vector2(0.06f, bar.HasValue ? 0.2f : 0.08f), new Vector2(0.94f, 0.66f), 44f, true);
            big.text = keys;
            big.color = ChogTheme.Text;

            if (bar.HasValue)
            {
                var track = Box(area, "Bar", new Color(1f, 1f, 1f, 0.12f));
                track.anchorMin = new Vector2(0.06f, 0.09f);
                track.anchorMax = new Vector2(0.94f, 0.17f);
                track.offsetMin = track.offsetMax = Vector2.zero;
                card.Fill = Box(track, "Fill", bar.Value);
                card.Fill.anchorMin = Vector2.zero;
                card.Fill.anchorMax = new Vector2(0f, 1f);
                card.Fill.offsetMin = card.Fill.offsetMax = Vector2.zero;
            }
            return card;
        }

        private static TextMeshProUGUI Text(RectTransform parent, string name, Vector2 from, Vector2 to, float size, bool title)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var area = text.rectTransform;
            area.SetParent(parent, false);
            area.anchorMin = from;
            area.anchorMax = to;
            area.offsetMin = area.offsetMax = Vector2.zero;
            if (title)
                ChogTheme.AsTitle(text);
            else
                ChogTheme.AsBody(text);
            text.raycastTarget = false;
            text.richText = true;
            text.alignment = TextAlignmentOptions.Left;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = size;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static RectTransform Box(RectTransform parent, string name, Color colour)
        {
            var box = new GameObject(name, typeof(RectTransform), typeof(Image));
            var area = (RectTransform)box.transform;
            area.SetParent(parent, false);
            var image = box.GetComponent<Image>();
            image.sprite = ChogSprites.Rounded(6, 0, Color.clear, Color.white);
            image.type = Image.Type.Sliced;
            image.color = colour;
            image.raycastTarget = false;
            return area;
        }
    }
}
