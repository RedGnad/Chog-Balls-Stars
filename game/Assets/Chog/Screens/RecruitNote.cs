using Quantum;
using TMPro;
using UnityEngine;

namespace Chog.Screens
{
    /// <summary>
    /// The note that tells a player without a Genesis, on the preparation screen, what they play
    /// and how to play a Genesis instead. It takes the place of the class buttons, which a recruit
    /// does not get: a recruit plays the one character the menu shows.
    /// </summary>
    public static class RecruitNote
    {
        private const string ObjectName = "RecruitNote";

        // In units of the preparation screen, like the hero card: the column between the two teams,
        // so the note never runs over their names (it did at 900 wide).
        private const float Width = 470f;
        private const float Height = 210f;

        private const string TitleColor = "#FFD27A";

        public static string Describe()
        {
            // A wallet that holds a Genesis but did not sign in plays a recruit: it is told why, not left to wonder (the audit,
            // 23 October 2026).
            if (Chog.Match.PlayerWallet.State == Chog.Match.WalletState.Ready && Chog.Match.PlayerWallet.Tokens.Length > 0
                && Chog.Match.PlayerWallet.SignIn != Chog.Match.SignInState.SignedIn)
                return "<b><color=" + TitleColor + ">RECRUIT MODE</color></b>\n<size=75%>Sign in on the home screen to play your Genesis.</size>";
            // Two lines (23 October 2026, the owner: light words; a recruit plays for practice, rank and stakes need a Genesis; named RECRUIT MODE).
            return "<b><color=" + TitleColor + ">RECRUIT MODE</color></b>\n<size=75%>Play a Genesis to rank and bet.</size>";
        }

        /// <summary>
        /// Shows the note centred on "center", where the class buttons were, as a sibling of
        /// "button", one of them. "template" is any text of the screen: the note copies its font.
        /// </summary>
        public static void Show(RectTransform button, Vector2 center, TextMeshProUGUI template)
        {
            var parent = (RectTransform)button.parent;
            if (parent.Find(ObjectName) != null)
                return;

            var text = Object.Instantiate(template, parent);
            text.name = ObjectName;
            text.raycastTarget = false;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 34f;
            text.color = Color.white;
            text.text = Describe();

            var area = text.rectTransform;
            area.anchorMin = button.anchorMin;
            area.anchorMax = button.anchorMax;
            area.pivot = new Vector2(0.5f, 0.5f);
            area.sizeDelta = new Vector2(Width, Height);
            area.anchoredPosition = center;
            text.gameObject.SetActive(true);
        }
    }
}
