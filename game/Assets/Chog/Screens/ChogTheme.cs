using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The look of the game's screens, after the official Chog identity read on 15 October 2026
    /// (research/chog-visual-identity-2026-10-15.md): the night purple of chogathon.chog.xyz, the cheek
    /// pink and the cream of the mascot, Koulen in capitals for titles, buttons and numbers, Convergence
    /// for sentences, pill buttons with a hard dark shadow under them. Every screen asks here for its
    /// colours, fonts and shapes, so the whole game changes in one place.
    /// </summary>
    public static class ChogTheme
    {
        public static readonly Color Night = Hex("0E0B1A");
        public static readonly Color NightHigh = Hex("1E1640");
        public static readonly Color Card = Hex("161228");
        public static readonly Color CardRaised = Hex("221A42");
        public static readonly Color Line = new Color(169f / 255f, 155f / 255f, 252f / 255f, 0.35f);
        public static readonly Color Purple = Hex("836EF9");
        public static readonly Color PurpleSoft = Hex("A99BFC");
        public static readonly Color Cheek = Hex("D83E7E");
        public static readonly Color Cream = Hex("FDF7D3");
        public static readonly Color Text = Hex("F4F1FE");
        public static readonly Color Muted = new Color(244f / 255f, 241f / 255f, 254f / 255f, 0.65f);
        public static readonly Color Ok = Hex("5CE68A");
        public static readonly Color Gold = Hex("F59E0B");
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);

        /// <summary>The kinds of buttons: the one thing to do, another thing, a quiet way back.</summary>
        public enum ButtonKind { Primary, Secondary, Quiet }

        private const string TitleFontResource = "Fonts/Koulen SDF";
        private const string BodyFontResource = "Fonts/Convergence SDF";
        private const string TitleFileResource = "Fonts/Koulen-Regular";
        private const string BodyFileResource = "Fonts/Convergence-Regular";
        private static TMP_FontAsset s_title, s_body;
        private static Font s_legacyTitle, s_legacyBody;
        private static bool s_loaded;

        /// <summary>Koulen: titles, buttons, numbers. Null when the project has not made it.</summary>
        public static TMP_FontAsset TitleFont
        {
            get { Load(); return s_title; }
        }

        /// <summary>Convergence: sentences. Null when the project has not made it.</summary>
        public static TMP_FontAsset BodyFont
        {
            get { Load(); return s_body; }
        }

        private static void Load()
        {
            if (s_loaded)
                return;
            s_loaded = true;
            s_title = Resources.Load<TMP_FontAsset>(TitleFontResource);
            s_body = Resources.Load<TMP_FontAsset>(BodyFontResource);
            // The sample's menu writes some labels with the older text component, which takes the font file.
            s_legacyTitle = Resources.Load<Font>(TitleFileResource);
            s_legacyBody = Resources.Load<Font>(BodyFileResource);
            if (s_title == null || s_body == null)
                Debug.LogWarning("CHOG_THEME the fonts of the theme are missing: the sample's font stays");
        }

        /// <summary>Writes a title, a button or a number in Koulen, in capitals.</summary>
        public static void AsTitle(TMP_Text text)
        {
            if (text == null)
                return;
            if (TitleFont != null)
                text.font = TitleFont;
            text.fontStyle &= ~FontStyles.Bold;
            text.fontStyle |= FontStyles.UpperCase;
            text.characterSpacing = 2f;
        }

        /// <summary>The same for a label of the older text component.</summary>
        public static void AsTitle(UnityEngine.UI.Text text)
        {
            Load();
            if (text == null || s_legacyTitle == null)
                return;
            text.font = s_legacyTitle;
            text.fontStyle = FontStyle.Normal;
            if (text.text != null)
                text.text = text.text.ToUpperInvariant();
        }

        /// <summary>Writes a sentence in Convergence.</summary>
        public static void AsBody(TMP_Text text)
        {
            if (text == null)
                return;
            if (BodyFont != null)
                text.font = BodyFont;
            text.fontStyle &= ~(FontStyles.Bold | FontStyles.UpperCase);
        }

        /// <summary>The fill of a quiet button: a muted purple that white letters read on.</summary>
        public static readonly Color Quiet = Hex("3B2F72");

        /// <summary>A pill button of that kind: its fill, a hard shadow under it, its label in Koulen.</summary>
        public static void Style(Button button, ButtonKind kind)
        {
            if (button == null)
                return;
            Shape(button);
            var image = ImageOf(button);
            if (image != null)
                image.color = Fill(kind);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                label.color = Color.white;
        }

        /// <summary>
        /// The shape of a pill button, its colour left as it is: the rounded fill, a hard shadow under
        /// it, the way it darkens when pressed, its label in Koulen.
        /// </summary>
        public static void Shape(Button button)
        {
            if (button == null)
                return;
            var image = ImageOf(button);
            if (image != null)
            {
                image.sprite = ChogSprites.Rounded(28, 0, Color.clear, Color.white);
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
                var shadow = image.GetComponent<Shadow>();
                if (shadow == null)
                    shadow = image.gameObject.AddComponent<Shadow>();
                shadow.effectColor = Shadow;
                shadow.effectDistance = new Vector2(5f, -6f);
                shadow.useGraphicAlpha = true;
            }

            var colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colours.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
            colours.selectedColor = Color.white;
            colours.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.6f);
            colours.colorMultiplier = 1f;
            button.colors = colours;

            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                AsTitle(label);
        }

        /// <summary>
        /// A card: a dark rounded panel with a thin lavender line around it. Its colours are in the
        /// sprite: the Image is left white.
        /// </summary>
        public static void AsCard(Image image, bool raised = false)
        {
            if (image == null)
                return;
            var fill = raised ? CardRaised : new Color(Card.r, Card.g, Card.b, 0.95f);
            image.sprite = ChogSprites.Rounded(24, 3, new Color(Line.r, Line.g, Line.b, 0.6f), fill);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        /// <summary>
        /// What draws a button: a child image named Background (the sample's buttons draw their fill
        /// there, under a transparent image they tint), else the graphic it tints, else its own image.
        /// </summary>
        public static Image ImageOf(Button button)
        {
            if (button == null)
                return null;
            var background = button.transform.Find("Background");
            if (background != null && background.GetComponent<Image>() is Image fill)
                return fill;
            if (button.targetGraphic is Image target)
                return target;
            return button.GetComponent<Image>();
        }

        public static Color Fill(ButtonKind kind)
        {
            switch (kind)
            {
                case ButtonKind.Primary: return Cheek;
                case ButtonKind.Secondary: return Purple;
                default: return Quiet;
            }
        }

        public static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var colour);
            return colour;
        }
    }
}
