using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// CANCEL, in the red of closing, on the team preparation screen at its bottom right: leaves the room the way the
    /// end of a match does (ChogResultScreen.LeaveMatch). The sample's only way out of that screen was
    /// the settings wheel, which players did not find. Escape does the same on a computer.
    /// </summary>
    public static class PreparationBack
    {
        private const string ObjectName = "ChogPreparationBack";

        /// <summary>Adds the button to the preparation screen "screen" once; "template" is any text of it, for its font.</summary>
        public static void Show(Transform screen, TextMeshProUGUI template)
        {
            if (screen == null || screen.Find(ObjectName) != null)
                return;

            var area = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)area.transform;
            rect.SetParent(screen, false);
            rect.anchorMin = new Vector2(0.80f, 0.04f);
            rect.anchorMax = new Vector2(0.97f, 0.13f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var button = area.GetComponent<Button>();
            button.targetGraphic = area.GetComponent<Image>();

            var label = Object.Instantiate(template, rect);
            label.name = "Label";
            label.raycastTarget = false;
            label.text = "CANCEL";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 36f;
            var labelArea = label.rectTransform;
            labelArea.anchorMin = Vector2.zero;
            labelArea.anchorMax = Vector2.one;
            labelArea.offsetMin = new Vector2(6f, 2f);
            labelArea.offsetMax = new Vector2(-6f, -2f);
            labelArea.pivot = new Vector2(0.5f, 0.5f);
            label.gameObject.SetActive(true);
            ChogTheme.Style(button, ChogTheme.ButtonKind.Danger);

            var keys = area.AddComponent<EscapeKey>();
            button.onClick.AddListener(keys.Leave);
        }

        private sealed class EscapeKey : MonoBehaviour
        {
            private bool _leaving;

            private void Update()
            {
                var keys = UnityEngine.InputSystem.Keyboard.current;
                if (keys != null && keys.escapeKey.wasPressedThisFrame)
                    Leave();
            }

            public async void Leave()
            {
                if (_leaving)
                    return;
                _leaving = true;
                await ChogResultScreen.LeaveMatch(false);
                _leaving = false;
            }
        }
    }
}
