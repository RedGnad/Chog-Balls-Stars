using System.Reflection;
using Chog.Match;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// Dresses the sample's menu in the game's own look (ChogTheme): the night purple background with
    /// two halos of colour behind every screen, the sample's buttons as pink or purple pills, its texts in
    /// Koulen and Convergence, its logos away. On the main screen, the hero the player will enter with
    /// stands in 3D on a pedestal at the left (ChogHeroStage), its Genesis or the grey recruit, with its
    /// name under it, and the play buttons go under the hero. The screens keep their behaviour: only
    /// their look and their place change.
    /// </summary>
    public sealed class ChogMenuSkin : MonoBehaviour
    {
        private const string BackgroundName = "ChogBackground";
        private const string StageName = "ChogHeroStageArea";

        // The left column of the main screen: the hero, its name, then the play buttons.
        // Between the tagline of the title and the play buttons, as the sample lays them out.
        // The home screen of an arena game: the hero large at the left, the wallet at the top right,
        // the mode and a large PLAY at the bottom right, the other buttons small at the bottom left.
        private static readonly Vector2 StageMin = new Vector2(0.03f, 0.19f);
        private static readonly Vector2 StageMax = new Vector2(0.41f, 0.71f);
        // PLAY and, under it, PLAY WITH FRIENDS, placed one by one (the sample's column lays them out from its top).
        private static readonly Vector2 PlayMin = new Vector2(0.62f, 0.135f);
        private static readonly Vector2 PlayMax = new Vector2(0.97f, 0.285f);
        private static readonly Vector2 PartyMin = new Vector2(0.62f, 0.04f);
        private static readonly Vector2 PartyMax = new Vector2(0.97f, 0.115f);

        private ChogHeroStage _stage;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _line;
        private int _named = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Attach();
            Attach();
        }

        private static void Attach()
        {
            var menu = FindAnyObjectByType<QuantumMenuUIController>(FindObjectsInactive.Include);
            if (menu == null || menu.GetComponent<ChogMenuSkin>() != null)
                return;
            menu.gameObject.AddComponent<ChogMenuSkin>();
        }

        private void Start()
        {
            Dress(transform);
            var main = FindAnyObjectByType<QuantumMenuUIMain>(FindObjectsInactive.Include);
            if (main != null)
                ArrangeMain(main);
        }

        // The screens that cover the whole window (not the pop-ups, not the one over the match).
        private static readonly string[] FullScreens = { "MainMenu", "PartyMenu", "Settings", "Loading", "Scenes" };

        // Everything of the sample's menu that has a look of its own: backgrounds, buttons, texts.
        private static void Dress(Transform root)
        {
            if ((Application.absoluteURL ?? "").Contains("uitree=1"))
                LogTree(root, 0);

            foreach (var part in root.GetComponentsInChildren<Transform>(true))
                if (part.name == "ProductLogo" || part.name == "PhotonLogo")
                    part.gameObject.SetActive(false);

            // The sample's own backdrops go transparent (they still catch the taps they caught), and
            // every full screen gets the night of the game behind it.
            foreach (Transform child in root)
                if (child.name.Contains("Background"))
                    ClearOpaque(child, child as RectTransform);
            foreach (var screen in root.GetComponentsInChildren<QuantumMenuUIScreen>(true))
            {
                if (System.Array.FindIndex(FullScreens, name => screen.name.Contains(name)) < 0)
                    continue;
                ClearOpaque(screen.transform, screen.transform as RectTransform);
                if (screen.transform.Find(BackgroundName) == null)
                    AddBackground(screen.transform);
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                // Our own panels are already in the theme.
                if (IsOurs(button.transform))
                    continue;
                var label = Label(button);
                if (label.Length == 0)
                {
                    // An icon (the settings wheel): its own drawing, in cream.
                    foreach (var icon in button.GetComponentsInChildren<Image>(true))
                        if (icon.color.a > 0.05f)
                            icon.color = ChogTheme.Cream;
                    continue;
                }
                ChogTheme.Style(button, KindOf(label));
            }

            // Large texts are titles; what a player types keeps its own letters.
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (IsOurs(text.transform) || text.GetComponentInParent<Button>(true) != null)
                    continue;
                if (text.fontSize >= 48f)
                    ChogTheme.AsTitle(text);
                else
                    ChogTheme.AsBody(text);
            }
            foreach (var text in root.GetComponentsInChildren<Text>(true))
                if (!IsOurs(text.transform) && text.GetComponentInParent<InputField>(true) == null)
                    ChogTheme.AsTitle(text);
        }

        // Opaque images that cover nearly all of "area": made transparent, so what is behind shows.
        private static void ClearOpaque(Transform under, RectTransform area)
        {
            if (area == null)
                return;
            var whole = area.rect.size;
            foreach (var image in under.GetComponentsInChildren<Image>(true))
            {
                if (IsOurs(image.transform) || image.GetComponent<Button>() != null || image.color.a < 0.95f)
                    continue;
                var size = image.rectTransform.rect.size;
                if (size.x >= whole.x * 0.9f && size.y >= whole.y * 0.9f)
                {
                    var colour = image.color;
                    colour.a = 0f;
                    image.color = colour;
                }
            }
        }

        // What the menu is made of, written to the console with "?uitree=1": names, whether each is on,
        // and the colour and size of its image.
        private static void LogTree(Transform part, int depth)
        {
            if (depth > 4)
                return;
            var image = part.GetComponent<Image>();
            var rect = part as RectTransform;
            Debug.Log("CHOG_UITREE " + new string(' ', depth * 2) + part.name + (part.gameObject.activeSelf ? "" : " (off)")
                      + (image != null ? " image " + ColorUtility.ToHtmlStringRGBA(image.color) + (image.sprite != null ? " " + image.sprite.name : "") : "")
                      + (rect != null ? " size " + rect.rect.size : ""));
            foreach (Transform child in part)
                LogTree(child, depth + 1);
        }

        // The night of chogathon.chog.xyz, lighter towards the top, with a purple and a pink halo.
        private static void AddBackground(Transform screen)
        {
            var background = new GameObject(BackgroundName, typeof(RectTransform)).AddComponent<Image>();
            Stretch(background.rectTransform, screen, Vector2.zero, Vector2.one);
            background.rectTransform.SetAsFirstSibling();
            background.sprite = ChogSprites.Gradient(ChogTheme.Night, ChogTheme.NightHigh);
            background.raycastTarget = false;

            Halo(background.transform, new Vector2(-0.08f, 0.35f), new Vector2(0.55f, 1.15f), new Color(0.51f, 0.43f, 0.98f, 0.30f));
            Halo(background.transform, new Vector2(0.55f, -0.35f), new Vector2(1.15f, 0.55f), new Color(0.93f, 0.28f, 0.60f, 0.18f));
        }

        private static void Halo(Transform parent, Vector2 min, Vector2 max, Color colour)
        {
            var halo = new GameObject("Halo", typeof(RectTransform)).AddComponent<Image>();
            Stretch(halo.rectTransform, parent, min, max);
            halo.sprite = ChogSprites.Disc(128, 1f);
            halo.color = colour;
            halo.raycastTarget = false;
        }

        // The main screen: the hero at the left over the play buttons.
        private void ArrangeMain(QuantumMenuUIMain main)
        {
            var play = Field(main, "_playButton");
            var party = Field(main, "_partyButton");
            var column = play != null ? play.transform.parent as RectTransform : null;
            if (column != null)
            {
                // The column covers the screen and lays nothing out: each button is placed in screen shares.
                foreach (var layout in column.GetComponents<LayoutGroup>())
                    layout.enabled = false;
                foreach (var fitter in column.GetComponents<ContentSizeFitter>())
                    fitter.enabled = false;
                column.anchorMin = Vector2.zero;
                column.anchorMax = Vector2.one;
                column.offsetMin = Vector2.zero;
                column.offsetMax = Vector2.zero;
                column.pivot = new Vector2(0.5f, 0.5f);
                Place(play, PlayMin, PlayMax);
                Place(party, PartyMin, PartyMax);
            }
            if (play != null)
            {
                ChogTheme.Style(play, ChogTheme.ButtonKind.Primary);
                Relabel(play, "PLAY", 72f);
            }
            if (party != null)
            {
                ChogTheme.Style(party, ChogTheme.ButtonKind.Quiet);
                Relabel(party, "PLAY WITH FRIENDS", 34f);
            }

            if (main.transform.Find(StageName) != null)
                return;
            var area = new GameObject(StageName, typeof(RectTransform)).GetComponent<RectTransform>();
            Stretch(area, main.transform, StageMin, StageMax);
            // Over the night of the screen, under everything else.
            var night = main.transform.Find(BackgroundName);
            area.SetSiblingIndex(night != null ? night.GetSiblingIndex() + 1 : 0);

            // A soft light under the hero, then the hero, then its name.
            var glow = new GameObject("Glow", typeof(RectTransform)).AddComponent<Image>();
            Stretch(glow.rectTransform, area, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.42f));
            glow.sprite = ChogSprites.Disc(128, 1f);
            glow.color = new Color(0.51f, 0.43f, 0.98f, 0.35f);
            glow.raycastTarget = false;

            var heroArea = new GameObject("Hero", typeof(RectTransform)).GetComponent<RectTransform>();
            Stretch(heroArea, area, new Vector2(0f, 0.10f), Vector2.one);
            _stage = ChogHeroStage.Create(heroArea);

            _name = Label(area, new Vector2(-0.05f, 0.02f), new Vector2(1.05f, 0.14f), 56f, ChogTheme.Cream);
            // Under the name, what its basic attack throws and which trait gives it.
            _line = Label(area, new Vector2(-0.10f, -0.07f), new Vector2(1.10f, 0.03f), 30f, ChogTheme.PurpleSoft);
        }

        private void Update()
        {
            if (_stage == null)
                return;
            var played = WalletPanel.PlayedMetadataId;
            var shown = played > 0 && ChogHeroModel.Has(played) ? played : ChogHeroModel.Recruit;
            _stage.Show(shown);
            if (_named != played)
            {
                _named = played;
                _name.text = played > 0 ? "CHOG #" + played : "RECRUIT";
                _line.text = ChogHeroModel.AttackLine(played > 0 ? played : ChogHeroModel.Recruit);
            }
        }

        private static void Place(Button button, Vector2 min, Vector2 max)
        {
            if (button == null)
                return;
            var area = (RectTransform)button.transform;
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            area.pivot = new Vector2(0.5f, 0.5f);
            foreach (var element in button.GetComponents<LayoutElement>())
                element.ignoreLayout = true;
        }

        // The sample's labels sit to the right of an icon this game does not show: centred over the whole button.
        private static void Relabel(Button button, string text, float largest)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = largest;
            var area = label.rectTransform;
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(12f, 4f);
            area.offsetMax = new Vector2(-12f, -4f);
        }

        private static TextMeshProUGUI Label(Transform parent, Vector2 min, Vector2 max, float largest, Color colour)
        {
            var text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            Stretch(text.rectTransform, parent, min, max);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = largest;
            text.raycastTarget = false;
            ChogTheme.AsTitle(text);
            text.color = colour;
            return text;
        }

        private static bool IsOurs(Transform part)
        {
            for (var at = part; at != null; at = at.parent)
                if (at.name.StartsWith("Chog"))
                    return true;
            return false;
        }

        private static string Label(Button button)
        {
            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                return tmp.text.Trim().ToUpperInvariant();
            var legacy = button.GetComponentInChildren<Text>(true);
            return legacy != null && !string.IsNullOrWhiteSpace(legacy.text) ? legacy.text.Trim().ToUpperInvariant() : "";
        }

        private static ChogTheme.ButtonKind KindOf(string label)
        {
            switch (label)
            {
                case "QUICK PLAY":
                case "PLAY":
                case "START":
                case "CREATE":
                case "JOIN":
                    return ChogTheme.ButtonKind.Primary;
                case "PARTY MENU":
                    return ChogTheme.ButtonKind.Secondary;
                default:
                    return ChogTheme.ButtonKind.Quiet;
            }
        }

        private static Button Field(QuantumMenuUIMain main, string name)
        {
            var field = typeof(QuantumMenuUIMain).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(main) as Button;
        }

        private static void Stretch(RectTransform area, Transform parent, Vector2 min, Vector2 max)
        {
            area.SetParent(parent, false);
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
        }
    }
}
