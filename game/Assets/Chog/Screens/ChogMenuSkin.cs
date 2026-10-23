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
    /// two halos of colour behind every screen, the sample's buttons as chunky buttons, its texts in the
    /// game's fonts, its logos away. The home screen's own layout is ChogHome's. The screens keep their
    /// behaviour: only their look changes.
    /// </summary>
    public sealed class ChogMenuSkin : MonoBehaviour
    {
        private const string BackgroundName = "ChogBackground";
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

            DressParty(root);
        }

        // FRIENDS opens the sample's party menu: create a private room, or join one with its code. It was the one
        // screen left in the sample's blue, with the region and build name of its developers in its header
        // (23 October 2026, the product walk). Same parts and same behaviour, in the look of our cards, with words a
        // player reads at once.
        private static void DressParty(Transform root)
        {
            Transform party = null;
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
                if (part.name.Contains("ViewPartyMenu"))
                    party = part;
            if (party == null)
                return;
            var header = party.Find("MenuHeader");
            if (header != null)
            {
                if (header.Find("Background") is Transform bar && bar.GetComponent<Image>() is Image fill)
                {
                    fill.sprite = ChogSprites.Rounded(12, 3, ChogTheme.Ink, ChogTheme.Card);
                    fill.type = Image.Type.Sliced;
                    fill.color = Color.white;
                }
                header.Find("VersionLabel")?.gameObject.SetActive(false);
                Words(header.Find("HeaderLabel"), "PLAY WITH FRIENDS", ChogTheme.Cream);
            }
            PartyCard(party.Find("MenuCards/CreateCard"), "CREATE A ROOM", "Start a private match, then share its code with your friends.");
            PartyCard(party.Find("MenuCards/JoinCard"), "JOIN A ROOM", "Type the code a friend gave you.");
        }

        private static void PartyCard(Transform card, string heading, string words)
        {
            if (card == null)
                return;
            ChogTheme.AsCard(card.Find("Background")?.GetComponent<Image>(), raised: true);
            var title = card.Find("CardHeader") as RectTransform;
            if (title != null)
                title.sizeDelta = new Vector2(560f, Mathf.Max(title.sizeDelta.y, 56f));
            Words(title, heading, ChogTheme.Hex("FFE27A"));
            var text = card.Find("CardText") as RectTransform;
            if (text != null)
                text.sizeDelta = new Vector2(text.sizeDelta.x, Mathf.Max(text.sizeDelta.y, 64f));
            Words(text, words, ChogTheme.Text);
            if (card.Find("CodeInputButton/Background")?.GetComponent<Image>() is Image field)
            {
                field.sprite = ChogSprites.Rounded(14, 3, ChogTheme.PurpleSoft, ChogTheme.Night);
                field.type = Image.Type.Sliced;
                field.color = Color.white;
            }
        }

        // The words of a text of the sample, whichever kind of text it is.
        private static void Words(Transform part, string words, Color colour)
        {
            if (part == null)
                return;
            if (part.GetComponent<TMP_Text>() is TMP_Text text)
            {
                text.text = words;
                text.color = colour;
                text.textWrappingMode = TextWrappingModes.Normal;
            }
            else if (part.GetComponent<Text>() is Text legacy)
            {
                legacy.text = words;
                legacy.color = colour;
                legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
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
        internal static void LogTree(Transform part, int depth)
        {
            if (depth > 6)
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
