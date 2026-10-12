using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// Puts the title of the game where the sample menu shows the logo of its engine, and hides
    /// that logo and its large emblem. The menu itself is still the sample's.
    /// </summary>
    public static class MenuTitle
    {
        private const string ObjectName = "ChogMenuTitle";
        private const string LogoSprites = "QuantumMenuLogo";

        // A working title: the game has no name yet. To be replaced by the one the team chooses.
        public const string Title = "CHOG GENESIS";
        public const string Tagline = "3v3 arena. Your Genesis is your hero.";

        // The label of the sample's scene chooser, which this game, with one arena, does not offer.
        private const string SceneChoiceLabel = "SELECT SCENE";

        // The menu switches that button on and off itself, so it is shrunk to nothing instead.
        private static void HideSceneChoice(Transform menuRoot)
        {
            // The sample writes its labels with either kind of text component.
            foreach (var label in menuRoot.GetComponentsInChildren<TMP_Text>(true))
                ShrinkButtonOf(label, label.text);
            foreach (var label in menuRoot.GetComponentsInChildren<Text>(true))
                ShrinkButtonOf(label, label.text);
        }

        private static void ShrinkButtonOf(Component label, string text)
        {
            if (!string.Equals((text ?? "").Trim(), SceneChoiceLabel, System.StringComparison.OrdinalIgnoreCase))
                return;

            var button = label.GetComponentInParent<Button>(true);
            if (button != null)
                button.transform.localScale = Vector3.zero;
        }

        /// <summary>Safe to call again: the title is added once, the logo stays hidden.</summary>
        public static void Apply(Transform menuRoot)
        {
            Image logo = null;
            foreach (var image in menuRoot.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || !image.sprite.name.StartsWith(LogoSprites))
                    continue;

                image.enabled = false;
                // The wordmark is the one the title replaces; the others are emblems.
                if (logo == null || image.sprite.name.Length < logo.sprite.name.Length)
                    logo = image;
            }

            HideSceneChoice(menuRoot);

            if (logo == null || logo.transform.parent.Find(ObjectName) != null)
                return;

            var title = new GameObject(ObjectName, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var area = title.rectTransform;
            var place = logo.rectTransform;
            area.SetParent(place.parent, false);
            area.SetSiblingIndex(place.GetSiblingIndex());
            area.anchorMin = place.anchorMin;
            area.anchorMax = place.anchorMax;
            area.pivot = place.pivot;
            // A little higher and taller than the logo was: the sample writes the region under it.
            area.anchoredPosition = place.anchoredPosition + new Vector2(0f, place.sizeDelta.y * 0.45f);
            area.sizeDelta = new Vector2(place.sizeDelta.x * 1.7f, place.sizeDelta.y * 1.25f);

            title.raycastTarget = false;
            title.richText = true;
            title.alignment = TextAlignmentOptions.Left;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 14f;
            title.fontSizeMax = 120f;
            title.color = Color.white;
            title.text = "<b>" + Title + "</b>\n<size=42%><color=#B9C4E6>" + Tagline + "</color></size>";
        }
    }
}
