using System;
using System.Globalization;
using System.Reflection;
using Chog.Match;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace Chog.Screens
{
    /// <summary>
    /// The settings of the game, over the home screen (the wheel at its top right): the music and the
    /// sounds in steps of a tenth, the camera shake and the vibration on or off. Each is kept on the
    /// device by the class that uses it (ChogSounds, ChogCameraShake, ChogHaptics). MORE opens the
    /// sample's own settings (graphics, region).
    /// </summary>
    public sealed class ChogSettings : MonoBehaviour
    {
        private const string ObjectName = "ChogSettings";
        private const float Step = 0.1f;

        private QuantumMenuUIMain _main;
        private RectTransform _musicBar;
        private TextMeshProUGUI _musicValue;
        private RectTransform _soundBar;
        private TextMeshProUGUI _soundValue;
        private Button _shake;
        private Button _haptics;

        /// <summary>Opens the settings over the main screen of the menu.</summary>
        public static void Open(Transform main)
        {
            var existing = main.Find(ObjectName);
            var settings = existing != null ? existing.GetComponent<ChogSettings>() : null;
            if (settings == null)
            {
                var dim = ChogUi.Dim(ObjectName, main, 0.84f);
                settings = dim.gameObject.AddComponent<ChogSettings>();
                settings.Build(main.GetComponent<QuantumMenuUIMain>());
                dim.gameObject.AddComponent<Button>().onClick.AddListener(settings.Close);
            }
            settings.gameObject.SetActive(true);
            settings.transform.SetAsLastSibling();
            settings.Refresh();
        }

        private void Close() => gameObject.SetActive(false);

        private void Build(QuantumMenuUIMain main)
        {
            _main = main;
            var card = ChogUi.Card("Card", transform, new Vector2(0.27f, 0.12f), new Vector2(0.73f, 0.88f), raised: true).transform;
            // The card takes its own taps: only a tap beside it closes the settings.
            card.GetComponent<Image>().raycastTarget = true;
            ChogUi.Title("Title", card, new Vector2(0.06f, 0.85f), new Vector2(0.80f, 0.97f), 64f, TextAlignmentOptions.Left).text = "SETTINGS";
            var close = ChogUi.Button("Close", card, new Vector2(0.84f, 0.865f), new Vector2(0.95f, 0.965f), ChogTheme.ButtonKind.Danger, "", 30f,
                ChogIcons.Icon.Close);
            close.onClick.AddListener(Close);

            Volume(card, 0, "MUSIC", ChogIcons.Icon.Music, () => ChogSounds.MusicVolume, value => ChogSounds.MusicVolume = value,
                out _musicBar, out _musicValue);
            Volume(card, 1, "SOUNDS", ChogIcons.Icon.Sound, () => ChogSounds.SoundVolume, value => ChogSounds.SoundVolume = value,
                out _soundBar, out _soundValue);
            _shake = Toggle(card, 2, "CAMERA SHAKE", ChogIcons.Icon.Shake, () => ChogCameraShake.Enabled = !ChogCameraShake.Enabled);
            _haptics = Toggle(card, 3, "VIBRATION", ChogIcons.Icon.Shake, () => ChogHaptics.Enabled = !ChogHaptics.Enabled);
            ChogUi.Body("Phones", card, new Vector2(0.06f, 0.255f), new Vector2(0.94f, 0.30f), 22f, TextAlignmentOptions.Left,
                ChogTheme.PurpleSoft).text = "Vibration works on Android phones.";

            var more = ChogUi.Button("More", card, new Vector2(0.06f, 0.05f), new Vector2(0.50f, 0.18f), ChogTheme.ButtonKind.Quiet, "MORE", 40f);
            more.onClick.AddListener(() =>
            {
                Close();
                var found = typeof(QuantumMenuUIMain).GetMethod("OnSettingsButtonPressed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                found?.Invoke(_main, null);
            });
            var done = ChogUi.Button("Done", card, new Vector2(0.53f, 0.05f), new Vector2(0.94f, 0.18f), ChogTheme.ButtonKind.Confirm, "DONE", 40f);
            done.onClick.AddListener(Close);
        }

        // A row, from the top of the card: its pictogram and name at the left.
        private static RectTransform Row(Transform card, int position, string name, ChogIcons.Icon icon)
        {
            var top = 0.82f - position * 0.13f;
            var row = ChogUi.Area(name, card, new Vector2(0.06f, top - 0.11f), new Vector2(0.94f, top));
            var tile = ChogUi.Pinned("Tile", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(66f, 66f));
            ChogUi.Plate("Plate", tile, Vector2.zero, Vector2.one, ChogTheme.Purple, 12);
            ChogUi.Icon("Icon", tile, icon, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f));
            ChogUi.Title("Name", row, new Vector2(0.15f, 0.1f), new Vector2(0.52f, 0.9f), 36f, TextAlignmentOptions.Left).text = name;
            return row;
        }

        private void Volume(Transform card, int position, string name, ChogIcons.Icon icon, Func<float> read, Action<float> write,
                            out RectTransform bar, out TextMeshProUGUI value)
        {
            var row = Row(card, position, name, icon);
            var less = ChogUi.Button("Less", row, new Vector2(0.52f, 0.12f), new Vector2(0.61f, 0.88f), ChogTheme.ButtonKind.Quiet, "-", 44f);
            bar = ChogUi.Bar("Level", row, new Vector2(0.63f, 0.32f), new Vector2(0.86f, 0.68f), ChogTheme.Action);
            var more = ChogUi.Button("More", row, new Vector2(0.88f, 0.12f), new Vector2(0.97f, 0.88f), ChogTheme.ButtonKind.Quiet, "+", 44f);
            value = ChogUi.Number("Value", row, new Vector2(0.63f, 0.70f), new Vector2(0.86f, 1.05f), 20f, TextAlignmentOptions.Center,
                ChogTheme.PurpleSoft, false);
            less.onClick.AddListener(() =>
            {
                write(Mathf.Round(Mathf.Clamp01(read() - Step) * 10f) / 10f);
                Refresh();
            });
            more.onClick.AddListener(() =>
            {
                write(Mathf.Round(Mathf.Clamp01(read() + Step) * 10f) / 10f);
                Refresh();
            });
        }

        private Button Toggle(Transform card, int position, string name, ChogIcons.Icon icon, Action flip)
        {
            var row = Row(card, position, name, icon);
            var button = ChogUi.Button("Switch", row, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.88f), ChogTheme.ButtonKind.Confirm, "ON", 36f);
            button.onClick.AddListener(() =>
            {
                flip();
                Refresh();
            });
            return button;
        }

        private void Refresh()
        {
            if (_musicBar == null)
                return;
            ShowVolume(_musicBar, _musicValue, ChogSounds.MusicVolume);
            ShowVolume(_soundBar, _soundValue, ChogSounds.SoundVolume);
            ShowSwitch(_shake, ChogCameraShake.Enabled);
            ShowSwitch(_haptics, ChogHaptics.Enabled);
        }

        private static void ShowVolume(RectTransform bar, TextMeshProUGUI value, float volume)
        {
            ChogUi.SetBar(bar, volume);
            value.text = Mathf.RoundToInt(volume * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static void ShowSwitch(Button button, bool on)
        {
            ChogTheme.SetButtonFill(button, on ? ChogTheme.Confirm : ChogTheme.Quiet);
            ChogUi.LabelOf(button).text = on ? "ON" : "OFF";
        }
    }
}
