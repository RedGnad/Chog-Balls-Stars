using Chog.Match;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The score of a match at the top of the screen, as the validated mockup "Hud" draws it: the
    /// player's team in a blue pill at the left, the other in a red one at the right, each with the
    /// pictogram of what is counted (a tower for the damage of a siege, a coin for coins), and the clock
    /// between them on a dark plate, red in its last thirty seconds, with what the score counts under
    /// it. It reads the last predicted frame each frame; the sample's coin bars and the siege's own
    /// clock step aside while it is shown.
    /// </summary>
    public sealed class ChogScoreboard : MonoBehaviour
    {
        private const string ObjectName = "ChogScoreboard";
        private const float ShortSeconds = 30f;

        private static readonly Vector2 PillSize = new Vector2(210f, 74f);
        private static readonly Vector2 ClockSize = new Vector2(220f, 84f);

        private TextMeshProUGUI _ours;
        private TextMeshProUGUI _theirs;
        private Image _oursIcon;
        private Image _theirsIcon;
        private TextMeshProUGUI _clock;
        private TextMeshProUGUI _caption;
        private GameObject _clockPlate;
        private TMP_Text _coinTimer;
        private bool _siegeShown = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Attach(scene);
            Attach(SceneManager.GetActiveScene());
        }

        private static void Attach(Scene scene)
        {
            if (!scene.IsValid() || GameObject.Find(ObjectName) != null)
                return;
            Transform coins = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var part in root.GetComponentsInChildren<Transform>(true))
                    if (part.name == "CoinGrabUI")
                        coins = part;
            if (coins == null)
                return;

            var host = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(host, scene);
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            host.AddComponent<ChogScoreboard>().Build(coins);
        }

        private void Build(Transform coins)
        {
            // The sample's coin bars: this board shows the score instead. Its countdown text keeps
            // deciding when the coins' clock runs, and is no longer drawn.
            foreach (var part in coins.GetComponentsInChildren<Transform>(true))
            {
                if (part.name.StartsWith("CollectiblesTeamLeft"))
                    part.gameObject.SetActive(false);
                if (part.name == "Timer")
                    _coinTimer = part.GetComponentInChildren<TMP_Text>(true);
            }
            if (_coinTimer != null)
                _coinTimer.enabled = false;
            StructureViews.ClockDrawnElsewhere = true;

            var top = new Vector2(0.5f, 1f);
            var ours = ChogUi.Pinned("Ours", transform, top, new Vector2(1f, 1f), new Vector2(-ClockSize.x * 0.5f - 12f, -14f), PillSize);
            ChogUi.Plate("Plate", ours, Vector2.zero, Vector2.one, ChogTheme.Ally, 18).gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -5f);
            _oursIcon = ChogUi.Icon("Icon", ours, ChogIcons.Icon.Tower, new Vector2(0.05f, 0.14f), new Vector2(0.36f, 0.86f));
            _ours = ChogUi.Title("Score", ours, new Vector2(0.36f, 0.08f), new Vector2(0.95f, 0.94f), 54f);

            _clockPlate = ChogUi.Pinned("Clock", transform, top, new Vector2(0.5f, 1f), new Vector2(0f, -10f), ClockSize).gameObject;
            ChogUi.Plate("Plate", _clockPlate.transform, Vector2.zero, Vector2.one, ChogTheme.Ink, 16);
            _clock = ChogUi.Title("Time", _clockPlate.transform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.96f), 62f,
                TextAlignmentOptions.Center, ChogTheme.Cream);
            _caption = ChogUi.Number("Caption", transform, Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Center, Color.white);
            ChogUi.Pin(_caption.rectTransform, top, new Vector2(0.5f, 1f), new Vector2(0f, -10f - ClockSize.y - 4f), new Vector2(380f, 30f));

            var theirs = ChogUi.Pinned("Theirs", transform, top, new Vector2(0f, 1f), new Vector2(ClockSize.x * 0.5f + 12f, -14f), PillSize);
            ChogUi.Plate("Plate", theirs, Vector2.zero, Vector2.one, ChogTheme.Enemy, 18).gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -5f);
            _theirs = ChogUi.Title("Score", theirs, new Vector2(0.05f, 0.08f), new Vector2(0.64f, 0.94f), 54f);
            _theirsIcon = ChogUi.Icon("Icon", theirs, ChogIcons.Icon.Tower, new Vector2(0.64f, 0.14f), new Vector2(0.95f, 0.86f));
            gameObject.SetActive(true);
        }

        private void OnDestroy() => StructureViews.ClockDrawnElsewhere = false;

        private unsafe void Update()
        {
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var frame = game != null && game.Frames != null ? game.Frames.Predicted : null;
            var playing = frame != null && frame.Global->State == GameState.Playing;
            // Nothing to count during the team preparation, nor once the end screen is up.
            var shown = playing && !ChogResultScreen.IsShown;
            foreach (Transform child in transform)
                if (child.gameObject != _clockPlate && child.gameObject.activeSelf != shown)
                    child.gameObject.SetActive(shown);
            if (!shown)
                _clockPlate.SetActive(false);
            if (!shown)
                return;

            var siege = MatchModes.IsSiege(frame);
            if (siege != _siegeShown)
            {
                _siegeShown = siege;
                var icon = ChogIcons.Get(siege ? ChogIcons.Icon.Tower : ChogIcons.Icon.Coin);
                _oursIcon.sprite = icon;
                _theirsIcon.sprite = icon;
            }

            var teams = frame.ResolveList(frame.Global->TeamsData);
            var mine = LocalTeam(game, frame) == 1 ? 1 : 0;
            if (teams.Count >= 2)
            {
                _ours.text = teams[mine].Score.ToString();
                _theirs.text = teams[1 - mine].Score.ToString();
            }

            // A siege has a clock from its start; coins only while a team holds the countdown.
            var running = siege || (_coinTimer != null && _coinTimer.gameObject.activeInHierarchy);
            if (_clockPlate.activeSelf != running)
                _clockPlate.SetActive(running);
            var left = siege ? MatchModes.SiegeTimeLeft(frame).AsFloat : frame.Global->MatchTimer.AsFloat;
            _clock.text = StructureViews.ClockText(left);
            _clock.color = left <= ShortSeconds ? ChogTheme.Hex("FF7A7F") : ChogTheme.Cream;
            _caption.text = siege ? "DAMAGE TO TOWERS" : running ? "COINS, THE CLOCK RUNS" : "COINS";
        }

        // The team of the local player's character; 0 for a spectator.
        private static int LocalTeam(QuantumGame game, Frame frame)
        {
            var characters = frame.Filter<PlayerLink, TeamInfo>();
            while (characters.Next(out _, out var link, out var team))
                if (link.PlayerRef != PlayerRef.None && game.PlayerIsLocal(link.PlayerRef))
                    return team.Index;
            return 0;
        }
    }
}
