using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The look of the game's screens. Since 17 October 2026 it follows the Supercell-inspired direction
    /// the owner chose (docs/DESIGN-BRIEF.md): Lilita One with a thick dark outline and a hard shadow for
    /// titles and buttons, Nunito for sentences and numbers, Koulen kept for the logo; chunky buttons with
    /// a near-black outline, a light band on top and a darker lip below, that sink when pressed; the
    /// colour roles of Supercell with the Chog colours (gold for the main action, green to confirm,
    /// purple for the rest, red to close, pink kept for $CHOG). Every screen asks here for its colours,
    /// fonts and shapes, so the whole game changes in one place.
    /// </summary>
    public static class ChogTheme
    {
        public static readonly Color Night = Hex("0E0B1A");
        public static readonly Color NightHigh = Hex("1E1640");
        public static readonly Color Card = Hex("161228");
        public static readonly Color CardRaised = Hex("2C2264");
        /// <summary>The fill of a card that lies on the night (AsCard).</summary>
        public static readonly Color CardFill = Hex("211A4C");
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
        /// <summary>The near-black of every outline and hard shadow.</summary>
        public static readonly Color Ink = Hex("140F24");
        /// <summary>The main action (PLAY, PLAY AGAIN).</summary>
        public static readonly Color Action = Hex("F7A81B");
        /// <summary>To confirm, buy, claim.</summary>
        public static readonly Color Confirm = Hex("4CD97A");
        /// <summary>To close, cancel, leave.</summary>
        public static readonly Color Danger = Hex("E5484D");
        /// <summary>Your team, and the way home.</summary>
        public static readonly Color Ally = Hex("3E86E8");
        /// <summary>The other team.</summary>
        public static readonly Color Enemy = Hex("C9375D");

        /// <summary>
        /// The kinds of buttons: the one thing to do (gold), another thing (purple), a quiet way back,
        /// confirming (green), closing (red), and anything paid in $CHOG (pink).
        /// </summary>
        public enum ButtonKind { Primary, Secondary, Quiet, Confirm, Danger, Chog }

        private const string TitleFontResource = "Fonts/Lilita One SDF";
        private const string TitleMaterialResource = "Fonts/Lilita One SDF Outlined";
        private const string BodyFontResource = "Fonts/Nunito ExtraBold SDF";
        private const string NumberFontResource = "Fonts/Nunito Black SDF";
        private const string NumberMaterialResource = "Fonts/Nunito Black SDF Outlined";
        private const string LogoFontResource = "Fonts/Koulen SDF";
        private const string LogoMaterialResource = "Fonts/Koulen SDF Outlined";
        // Before the build has made the new fonts, the earlier ones.
        private const string OldTitleFontResource = "Fonts/Koulen SDF";
        private const string OldBodyFontResource = "Fonts/Convergence SDF";
        private const string TitleFileResource = "Fonts/LilitaOne-Regular";
        private const string BodyFileResource = "Fonts/Nunito-ExtraBold";
        private static TMP_FontAsset s_title, s_body, s_number, s_logo;
        private static Material s_titleOutlined, s_numberOutlined, s_logoOutlined;
        private static Font s_legacyTitle, s_legacyBody;
        private static bool s_loaded;

        /// <summary>Lilita One: titles and buttons. Null when the project has made none of the fonts.</summary>
        public static TMP_FontAsset TitleFont
        {
            get { Load(); return s_title; }
        }

        /// <summary>Nunito ExtraBold: sentences. Null when the project has made none of the fonts.</summary>
        public static TMP_FontAsset BodyFont
        {
            get { Load(); return s_body; }
        }

        private static void Load()
        {
            if (s_loaded)
                return;
            s_loaded = true;
            s_title = Resources.Load<TMP_FontAsset>(TitleFontResource) ?? Resources.Load<TMP_FontAsset>(OldTitleFontResource);
            s_body = Resources.Load<TMP_FontAsset>(BodyFontResource) ?? Resources.Load<TMP_FontAsset>(OldBodyFontResource);
            s_number = Resources.Load<TMP_FontAsset>(NumberFontResource) ?? s_body;
            s_logo = Resources.Load<TMP_FontAsset>(LogoFontResource) ?? s_title;
            s_titleOutlined = Resources.Load<Material>(TitleMaterialResource);
            s_numberOutlined = Resources.Load<Material>(NumberMaterialResource);
            s_logoOutlined = Resources.Load<Material>(LogoMaterialResource);
            // The sample's menu writes some labels with the older text component, which takes the font file.
            s_legacyTitle = Resources.Load<Font>(TitleFileResource);
            s_legacyBody = Resources.Load<Font>(BodyFileResource);
            if (s_title == null || s_body == null)
                Debug.LogWarning("CHOG_THEME the fonts of the theme are missing: the sample's font stays");
        }

        /// <summary>
        /// Writes a title, a button or a banner in Lilita One, in capitals, with the thick dark outline and
        /// the hard shadow ("outlined" false: plain, for small labels on a light ground).
        /// </summary>
        public static void AsTitle(TMP_Text text, bool outlined = true)
        {
            if (text == null)
                return;
            if (TitleFont != null)
                text.font = TitleFont;
            if (outlined && s_titleOutlined != null && text.font == s_title)
                text.fontSharedMaterial = s_titleOutlined;
            text.fontStyle &= ~FontStyles.Bold;
            text.fontStyle |= FontStyles.UpperCase;
            text.characterSpacing = 1f;
        }

        /// <summary>Writes a number (a score, a balance, a count) in Nunito Black, outlined.</summary>
        public static void AsNumber(TMP_Text text, bool outlined = true)
        {
            Load();
            if (text == null || s_number == null)
                return;
            text.font = s_number;
            if (outlined && s_numberOutlined != null)
                text.fontSharedMaterial = s_numberOutlined;
            text.fontStyle &= ~(FontStyles.Bold | FontStyles.UpperCase);
            text.characterSpacing = 0f;
        }

        /// <summary>Writes the game's name in Koulen, outlined, as on chog.xyz.</summary>
        public static void AsLogo(TMP_Text text)
        {
            Load();
            if (text == null || s_logo == null)
                return;
            text.font = s_logo;
            if (s_logoOutlined != null)
                text.fontSharedMaterial = s_logoOutlined;
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

        /// <summary>Writes a sentence in Nunito ExtraBold.</summary>
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

        /// <summary>A button with the shared face, side, outline and Lilita One label.</summary>
        public static void Style(Button button, ButtonKind kind)
        {
            if (button == null)
                return;
            Shape(button);
            Paint(button, kind);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                label.color = Color.white;
        }

        /// <summary>Apply the shared button surface to a named colour role.</summary>
        public static void Paint(Button button, ButtonKind kind)
        {
            SetButtonFill(button, Fill(kind));
        }

        /// <summary>Keep the home action entry point on the same surface as every other button.</summary>
        public static void PaintRelief(Button button, ButtonKind kind)
        {
            var image = ImageOf(button);
            if (image == null)
                return;
            var baseName = button.name + "ReliefBase";
            var oldBase = image.rectTransform.parent.Find(baseName);
            if (oldBase != null)
                Object.Destroy(oldBase.gameObject);
            Paint(button, kind);
        }

        /// <summary>Change a button's colour without tinting its ink border.</summary>
        public static void SetButtonFill(Button button, Color fill)
        {
            var image = ImageOf(button);
            if (image == null)
                return;
            image.sprite = ChogSprites.Chunky(fill, Color.Lerp(fill, Ink, 0.28f));
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = ButtonScale;
            image.color = Color.white;
        }

        private const float ButtonScale = 0.82f;

        /// <summary>
        /// The common button shape keeps the current fill, including buttons supplied by the sample.
        /// </summary>
        public static void Shape(Button button)
        {
            if (button == null)
                return;
            var image = ImageOf(button);
            if (image != null)
            {
                SetButtonFill(button, image.color);
                var shadow = image.GetComponent<Shadow>();
                if (shadow == null)
                    shadow = image.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(Ink.r, Ink.g, Ink.b, 0.60f);
                shadow.effectDistance = new Vector2(0f, -7f);
                shadow.useGraphicAlpha = true;
                shadow.enabled = false;
                var shine = image.transform.Find("ChogShine");
                if (shine != null)
                    shine.gameObject.SetActive(false);
            }

            var press = button.GetComponent<ChogPress>();
            if (press != null)
                press.enabled = false;

            if (button.GetComponent<ChogHover>() == null)
                button.gameObject.AddComponent<ChogHover>();

            var colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colours.pressedColor = new Color(0.86f, 0.86f, 0.90f, 1f);
            colours.selectedColor = Color.white;
            colours.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.6f);
            colours.colorMultiplier = 1f;
            button.colors = colours;

            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                AsTitle(label);
        }

        /// <summary>
        /// A card (docs/DESIGN-BRIEF.md): a deep purple rounded panel outlined in ink like the buttons, with
        /// a hard dark shadow under it; raised, a lighter purple for what lies over another card. Its
        /// colours are in the sprite: the Image is left white.
        /// </summary>
        public static void AsCard(Image image, bool raised = false)
        {
            if (image == null)
                return;
            var fill = raised ? CardRaised : new Color(CardFill.r, CardFill.g, CardFill.b, 0.96f);
            image.sprite = ChogSprites.Rounded(24, 3, Ink, fill);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.8f;
            image.color = Color.white;
            var shadow = image.GetComponent<Shadow>();
            if (shadow == null)
                shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.04f, 0.02f, 0.09f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.useGraphicAlpha = true;
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
                case ButtonKind.Primary: return Action;
                case ButtonKind.Secondary: return Purple;
                case ButtonKind.Confirm: return Confirm;
                case ButtonKind.Danger: return Danger;
                case ButtonKind.Chog: return Cheek;
                default: return Quiet;
            }
        }

        /// <summary>
        /// The class of a hero as this game names it (the role of its Genesis), for a character of the
        /// sample: the knight plays the brute, the archer the ranger, the spellcaster the controller.
        /// </summary>
        public static string ClassName(string sampleName)
        {
            switch ((sampleName ?? "").Trim().ToUpperInvariant())
            {
                case "KNIGHT": return "BRUTE";
                case "ARCHER": return "RANGER";
                case "SPELLCASTER": return "CONTROLLER";
                default: return sampleName;
            }
        }

        public static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var colour);
            return colour;
        }
    }
}
