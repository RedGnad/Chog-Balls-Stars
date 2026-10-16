using Quantum;
using TMPro;
using UnityEngine;

namespace Chog.Screens
{
    /// <summary>
    /// Writes under the special attack stick of the touch controls what the special of the local
    /// Genesis is: its shape, and its effect in the colour of the Aura. A recruit, whose special
    /// is the plain one of its class, gets no label.
    /// </summary>
    public sealed class SpecialButtonLabel : MonoBehaviour
    {
        // Name of the special stick in the sample's touch layout (MobileLayout.prefab).
        private const string StickName = "AimSpecialJoystick";
        private const string LabelName = "ChogSpecialLabel";
        private const float SearchEverySeconds = 0.5f;

        private float _nextSearch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Special Label");
            DontDestroyOnLoad(host);
            host.AddComponent<SpecialButtonLabel>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextSearch)
                return;

            _nextSearch = Time.unscaledTime + SearchEverySeconds;
            var runner = QuantumRunner.Default;
            if (runner == null || runner.Game == null || runner.Game.Frames == null || runner.Game.Frames.Verified == null)
                return;

            // The touch layout only exists, and is only active, on a touch device in a match.
            var stick = GameObject.Find(StickName);
            if (stick == null || stick.transform.Find(LabelName) != null)
                return;

            var text = Describe(runner.Game);
            if (text != null)
                Attach((RectTransform)stick.transform, text);
        }

        // "WAVE" over "CHILL" in the colour of the effect; null while the local hero is unknown
        // or is not a Genesis. Also written on the desktop panel (ChogDesktopControls).
        internal static string Describe(QuantumGame game)
        {
            var frame = game.Frames.Verified;
            var roster = GenesisCast.Roster(frame);
            if (roster == null)
                return null;

            var heroes = frame.Filter<PlayerLink, GenesisHero>();
            while (heroes.Next(out _, out var playerLink, out var hero))
            {
                if (!game.PlayerIsLocal(playerLink.PlayerRef))
                    continue;

                // The name of the form the hero plays: the shape itself, or its second form.
                var shape = roster.FormName(hero.Shape, hero.Form);
                var effect = hero.Effect < roster.EffectIds.Length ? roster.EffectIds[hero.Effect] : "";
                var color = roster.EffectColors != null && hero.Effect < roster.EffectColors.Length
                    ? roster.EffectColors[hero.Effect]
                    : Color.clear;
                var tinted = color.a > 0f
                    ? "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + effect.ToUpperInvariant() + "</color>"
                    : effect.ToUpperInvariant();
                return "<b>" + shape.ToUpperInvariant() + "</b>\n<size=80%>" + tinted + "</size>";
            }

            return null;
        }

        private static void Attach(RectTransform stick, string text)
        {
            var label = new GameObject(LabelName, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var area = label.rectTransform;
            area.SetParent(stick, false);
            // Just under the stick, centred on it.
            area.anchorMin = new Vector2(0.5f, 0f);
            area.anchorMax = new Vector2(0.5f, 0f);
            area.pivot = new Vector2(0.5f, 1f);
            area.anchoredPosition = new Vector2(0f, -6f);
            area.sizeDelta = new Vector2(Mathf.Max(stick.rect.width * 1.6f, 160f), Mathf.Max(stick.rect.height * 0.5f, 54f));

            label.raycastTarget = false;
            label.richText = true;
            label.alignment = TextAlignmentOptions.Top;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 40f;
            label.color = Color.white;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(20, 16, 30, 255);
            label.text = text;
        }
    }
}
