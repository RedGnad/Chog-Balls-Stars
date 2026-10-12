using Quantum;
using TMPro;
using UnityEngine;

namespace Chog.Screens
{
    /// <summary>
    /// The note that tells a player without a Genesis, on the preparation screen, what they play
    /// and how to play a Genesis instead. It sits above the class buttons, which a recruit keeps.
    /// </summary>
    public static class RecruitNote
    {
        private const string ObjectName = "RecruitNote";

        // In units of the preparation screen, like the hero card.
        private const float Width = 900f;
        private const float Height = 84f;
        private const float Margin = 6f;

        private const string TitleColor = "#FFD27A";

        public static string Describe()
        {
            return "<b><color=" + TitleColor + ">RECRUIT</color></b>  <size=70%>" + (100 - Recruit.HealthPercent) +
                   "% less health, no Genesis attack.\nTo play a Chog Genesis: connect the wallet that holds it, or open a link its holder lent you.</size>";
        }

        /// <summary>
        /// Shows the note just above "top", the highest class button, as its sibling. "template"
        /// is any text of the screen: the note copies its font from it.
        /// </summary>
        public static void Show(RectTransform top, TextMeshProUGUI template)
        {
            var parent = (RectTransform)top.parent;
            if (parent.Find(ObjectName) != null)
                return;

            var text = Object.Instantiate(template, parent);
            text.name = ObjectName;
            text.raycastTarget = false;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.Bottom;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 40f;
            text.color = Color.white;
            text.text = Describe();

            var area = text.rectTransform;
            area.anchorMin = top.anchorMin;
            area.anchorMax = top.anchorMax;
            area.pivot = new Vector2(0.5f, 0f);
            area.sizeDelta = new Vector2(Width, Height);
            area.anchoredPosition = new Vector2(top.anchoredPosition.x,
                top.anchoredPosition.y + top.rect.height * (1f - top.pivot.y) + Margin);
            text.gameObject.SetActive(true);
        }
    }
}
