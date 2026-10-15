using System.Text;
using Chog.Match;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The card that explains a Genesis on the preparation screen: its image, then for the
    /// outfit, the eyes, the aura and the rarity, what the trait is and what it gives in play. The sentences
    /// come from the roster asset, where they are written next to the numbers they describe.
    /// </summary>
    public static class GenesisHeroCard
    {
        private const string ObjectName = "GenesisHeroCard";
        private const string TextName = "Text";

        // In units of the preparation screen: the card fits between the two team columns.
        private const float Width = 470f;
        private const float Height = 390f;
        private const float PortraitSize = 96f;
        private const float Margin = 8f;

        private static readonly Color PanelColor = ChogTheme.Card;
        private const string TraitColor = "#A9A9B8";
        private const string CareerColor = "#FFD27A";

        // The dash, U+2014, between the number and the name of a 1:1 hero.
        private static readonly string LongDash = " " + (char)0x2014 + " ";

        /// <summary>
        /// Shows the card as a sibling of "like", centred on "anchoredPosition" in the same
        /// anchors. "template" is any text of the screen: the card copies its font from it.
        /// </summary>
        public static void Show(RectTransform like, Vector2 anchoredPosition, TextMeshProUGUI template,
                                GenesisRoster roster, GenesisRosterEntry hero)
        {
            var parent = (RectTransform)like.parent;
            var card = parent.Find(ObjectName) as RectTransform;
            TextMeshProUGUI text;
            if (card == null)
            {
                var panel = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
                card = (RectTransform)panel.transform;
                card.SetParent(parent, false);
                var background = panel.GetComponent<Image>();
                background.color = PanelColor;
                ChogTheme.AsCard(background);
                background.raycastTarget = false;

                text = Object.Instantiate(template, card);
                text.name = TextName;
                text.raycastTarget = false;
                text.richText = true;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.alignment = TextAlignmentOptions.Top;
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = 30f;
                text.color = Color.white;

                var area = text.rectTransform;
                area.anchorMin = new Vector2(0f, 0f);
                area.anchorMax = new Vector2(1f, 1f);
                area.pivot = new Vector2(0.5f, 0.5f);
                area.offsetMin = new Vector2(Margin, Margin);
                area.offsetMax = new Vector2(-Margin, -(PortraitSize + 2f * Margin));
            }
            else
            {
                text = card.Find(TextName).GetComponent<TextMeshProUGUI>();
            }

            card.anchorMin = like.anchorMin;
            card.anchorMax = like.anchorMax;
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = anchoredPosition;
            card.sizeDelta = new Vector2(Width, Height);
            card.gameObject.SetActive(true);

            GenesisPortrait.Show(card, hero.MetadataId, PortraitSize, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -Margin));
            text.text = Describe(roster, hero);

            // The career is public: it is read for any Genesis, and written on the card when it arrives.
            s_shown = text;
            s_roster = roster;
            s_hero = hero;
            if (!s_listening)
            {
                s_listening = true;
                GenesisCareers.Changed += () =>
                {
                    if (s_shown != null)
                        s_shown.text = Describe(s_roster, s_hero);
                };
            }

            if (!GenesisCareers.TryGet(hero.TokenId, out _))
                GenesisCareers.Read(new[] { hero.TokenId });
        }

        // The card on screen, to rewrite when its career arrives.
        private static TextMeshProUGUI s_shown;
        private static GenesisRoster s_roster;
        private static GenesisRosterEntry s_hero;
        private static bool s_listening;

        /// <summary>The name of a Genesis as screens write it.</summary>
        public static string DisplayName(GenesisRosterEntry hero) => CleanName(hero.Name);

        /// <summary>A Genesis name without the long dash of the 1:1 heroes, which is not in every font.</summary>
        public static string CleanName(string name) => (name ?? "").Replace(LongDash, " ");

        /// <summary>The text of the card, with TextMesh Pro tags for sizes and colours.</summary>
        public static string Describe(GenesisRoster roster, GenesisRosterEntry hero)
        {
            var text = new StringBuilder("<b>").Append(DisplayName(hero)).Append("</b>");
            if (GenesisCareers.TryGet(hero.TokenId, out var career))
            {
                text.Append("\n<size=62%><color=").Append(CareerColor).Append('>').Append(GenesisCareers.DescribeAll(career))
                    .Append("</color></size>");
            }

            // The card describes the form of the signature this player's Genesis enters with.
            var form = GenesisForms.PlayedBy(hero.TokenId);
            var lines = roster.CardLines(hero, form);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var gives = line.Gives.ToUpperInvariant();

                // The effect is written in the colour its attacks and hits are drawn with.
                var isEffect = line.Trait == GenesisRoster.EffectTrait;
                var color = isEffect && roster.EffectColors != null && hero.Effect < roster.EffectColors.Length
                    ? roster.EffectColors[hero.Effect]
                    : Color.clear;
                if (color.a > 0f)
                    gives = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + gives + "</color>";

                text.Append("\n<size=68%><color=").Append(TraitColor).Append('>')
                    .Append(line.Trait.ToUpperInvariant()).Append(" : ").Append(line.Value).Append("</color></size>")
                    .Append("\n<b>").Append(gives).Append("</b>  <size=72%>").Append(line.Summary).Append("</size>");
            }

            // What mastery still has to unlock: the reason to play this Genesis again.
            if (form == 0 && roster.HasSecondForm(hero.Shape) && !GenesisForms.IsUnlockedFor(hero.TokenId))
            {
                text.Append("\n<size=62%><color=").Append(CareerColor).Append(">SECOND FORM AT MASTERY ")
                    .Append(GenesisForms.UnlockLevel).Append(" : ").Append(roster.SecondFormNames[hero.Shape].ToUpperInvariant())
                    .Append("</color></size>");
            }

            return text.ToString();
        }
    }
}
