using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EMatchMode = Quantum.EMatchMode;
using QuantumMenuSceneInfo = Quantum.QuantumMenuSceneInfo;

namespace Chog.Screens
{
    /// <summary>
    /// The mode a player plays, chosen on the menu with the button beside QUICK PLAY: SIEGE (a tower and a
    /// core for each team, no coin, three minutes; MatchModes in the simulation) or COINS (the sample's
    /// Coin Grab, no structure). The choice is kept by the browser. It goes into the config of the match
    /// the player starts (the menu copies its scene's RuntimeConfig when a room is made), and after the
    /// build in the name the page gives Photon (BuildIdentity), so a player only meets players who chose
    /// the same mode; the game services read the build before that name's last "-" (server/src/modes.js).
    /// An address with "?mode=coins" or "?mode=siege" chooses for the player, as a tap would: a link can
    /// bring friends into the same mode.
    /// </summary>
    public sealed class MatchModeChoice : MonoBehaviour
    {
        private const string ButtonName = "ChogModeButton";
        private const string Key = "chog.match.mode";
        public const string QueryParameter = "mode";

        private static EMatchMode? s_chosen;

        public static EMatchMode Chosen
        {
            get
            {
                if (s_chosen == null)
                {
                    var kept = EMatchMode.Siege;
                    try
                    {
                        kept = PlayerPrefs.GetString(Key, "siege") == "coins" ? EMatchMode.Coins : EMatchMode.Siege;
                    }
                    catch (System.Exception)
                    {
                        // A browser that keeps nothing plays the siege.
                    }
                    s_chosen = kept;
                }
                return s_chosen.Value;
            }
        }

        /// <summary>What follows the build in the name the page gives Photon: "-siege" or "-coins".</summary>
        public static string Suffix => SuffixOf(Chosen);

        public static string SuffixOf(EMatchMode mode) => mode == EMatchMode.Coins ? "-coins" : "-siege";

        public static string LabelOf(EMatchMode mode) => mode == EMatchMode.Coins ? "COINS" : "SIEGE";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            var asked = QueryValue(Application.absoluteURL, QueryParameter);
            if (asked == "coins")
                Choose(EMatchMode.Coins);
            else if (asked == "siege")
                Choose(EMatchMode.Siege);
            Apply();
            SceneManager.sceneLoaded += (scene, mode) => Attach();
            Attach();
        }

        /// <summary>The value of one option of an address, or null.</summary>
        public static string QueryValue(string url, string name)
        {
            int start = (url ?? "").IndexOf('?');
            if (start < 0)
                return null;
            foreach (var part in url.Substring(start + 1).Split('&', '#'))
            {
                int equals = part.IndexOf('=');
                if (equals > 0 && part.Substring(0, equals) == name)
                    return System.Uri.UnescapeDataString(part.Substring(equals + 1));
            }
            return null;
        }

        /// <summary>Writes the chosen mode into the configs the menu starts matches with.</summary>
        private static void Apply()
        {
            // In the Editor these assets are the project files themselves: leave them untouched.
            if (Application.isEditor)
                return;
            foreach (var scene in Resources.LoadAll<QuantumMenuSceneInfo>(""))
                scene.RuntimeConfig.Mode = Chosen;
        }

        private static void Choose(EMatchMode mode)
        {
            s_chosen = mode;
            try
            {
                PlayerPrefs.SetString(Key, mode == EMatchMode.Coins ? "coins" : "siege");
                PlayerPrefs.Save();
            }
            catch (System.Exception)
            {
                // Not kept for the next visit; this visit still plays it.
            }
            Apply();
            Debug.Log("CHOG_MODE chosen " + LabelOf(mode));
        }

        private static void Attach()
        {
            var main = FindAnyObjectByType<QuantumMenuUIMain>(FindObjectsInactive.Include);
            if (main == null || main.transform.Find(ButtonName) != null)
                return;

            // Beside PLAY, at the bottom right of the home screen.
            var area = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)area.transform;
            rect.SetParent(main.transform, false);
            rect.anchorMin = new Vector2(0.44f, 0.135f);
            rect.anchorMax = new Vector2(0.60f, 0.285f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var button = area.GetComponent<Button>();
            button.targetGraphic = area.GetComponent<Image>();

            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var labelArea = label.rectTransform;
            labelArea.SetParent(rect, false);
            labelArea.anchorMin = Vector2.zero;
            labelArea.anchorMax = Vector2.one;
            labelArea.offsetMin = new Vector2(4f, 2f);
            labelArea.offsetMax = new Vector2(-4f, -2f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 44f;
            label.raycastTarget = false;
            ChogTheme.Style(button, ChogTheme.ButtonKind.Quiet);

            void Show() => label.text = "<size=50%>MODE, TAP TO CHANGE</size>\n" + LabelOf(Chosen);
            Show();
            button.onClick.AddListener(() =>
            {
                Choose(Chosen == EMatchMode.Siege ? EMatchMode.Coins : EMatchMode.Siege);
                Show();
            });
        }
    }
}
