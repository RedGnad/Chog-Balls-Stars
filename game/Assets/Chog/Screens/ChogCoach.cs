using Chog.Match;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace Chog.Screens
{
    /// <summary>
    /// The coach of a player's first match (decided by the owner on 17 October; mockup "Hud"): three
    /// bubbles in turn, each gone once the player did what it says. 1/3 move, 2/3 throw the basic
    /// attack, 3/3 what wins the mode (bring their tower down, or hold the coins). The words follow
    /// the controls in use, keys and mouse or the sticks. Seen once per browser; a tap on a bubble
    /// skips the coach; a page tested by script ("perf=1" or "noguide=1") never shows it.
    /// </summary>
    public sealed class ChogCoach : MonoBehaviour
    {
        private const string ObjectName = "ChogCoach";
        private const string SeenKey = "chog.coach.seen";
        private const float MoveDistance = 2.5f;
        private const float LastStepSeconds = 7f;

        private RectTransform _bubble;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _text;
        private int _step = -1;
        private bool _hasStart;
        private Vector2 _start;
        private float _energy = -1f;
        private float _lastShownAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Attach(scene);
            Attach(SceneManager.GetActiveScene());
        }

        private static void Attach(Scene scene)
        {
            var url = Application.absoluteURL ?? "";
            if (!scene.IsValid() || url.Contains("perf=1") || url.Contains("noguide=1") || PlayerPrefs.GetInt(SeenKey, 0) == 1
                || GameObject.Find(ObjectName) != null)
                return;
            // Only the arena scene has the coin and siege screens.
            var arena = false;
            foreach (var root in scene.GetRootGameObjects())
                arena |= root.GetComponentInChildren<TwinStickShooter.CoinGrabUI>(true) != null;
            if (!arena)
                return;

            var host = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(host, scene);
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            host.AddComponent<ChogCoach>().Build();
        }

        private void Build()
        {
            _bubble = ChogUi.Pinned("Bubble", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 132f));
            var plate = ChogUi.Plate("Plate", _bubble, Vector2.zero, Vector2.one, ChogTheme.Cream, 18);
            plate.raycastTarget = true;
            plate.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -6f);
            plate.gameObject.AddComponent<Button>().onClick.AddListener(Finish);
            _title = ChogUi.Title("Title", _bubble, new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.94f), 40f, TextAlignmentOptions.Center, ChogTheme.Ink);
            _title.fontSharedMaterial = _title.font != null ? _title.font.material : _title.fontSharedMaterial;
            _text = ChogUi.Body("Text", _bubble, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.48f), 26f, TextAlignmentOptions.Center,
                ChogTheme.Hex("3B2F72"));
            _bubble.gameObject.SetActive(false);
        }

        // Seen: the coach does not come back in this browser.
        private void Finish()
        {
            try
            {
                PlayerPrefs.SetInt(SeenKey, 1);
                PlayerPrefs.Save();
            }
            catch (System.Exception)
            {
                // Not kept: the coach may show again in the next match.
            }
            Destroy(gameObject);
        }

        private unsafe void Update()
        {
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var frame = game != null && game.Frames != null ? game.Frames.Predicted : null;
            var playing = frame != null && frame.Global->State == GameState.Playing && !ChogResultScreen.IsShown;
            var hero = playing ? LocalHero(frame, game) : EntityRef.None;
            _bubble.gameObject.SetActive(hero != EntityRef.None && _step >= 0);
            if (hero == EntityRef.None)
                return;
            if (_step < 0)
                Show(0, frame);

            var place = frame.Get<Transform2D>(hero).Position;
            var at = new Vector2(place.X.AsFloat, place.Y.AsFloat);
            switch (_step)
            {
                case 0:
                    if (!_hasStart)
                    {
                        _start = at;
                        _hasStart = true;
                    }
                    if (Vector2.Distance(_start, at) > MoveDistance)
                        Show(1, frame);
                    break;
                case 1:
                    // The basic attack spends energy: a drop of it is a throw.
                    var energy = AttributesHelper.Has(frame, hero, EAttributeType.Energy)
                        ? AttributesHelper.GetCurrentValue(frame, hero, EAttributeType.Energy).AsFloat : 0f;
                    if (_energy >= 0f && energy < _energy - 0.01f)
                        Show(2, frame);
                    _energy = energy;
                    break;
                default:
                    if (Time.unscaledTime - _lastShownAt > LastStepSeconds)
                        Finish();
                    break;
            }
        }

        private void Show(int step, Frame frame)
        {
            _step = step;
            _lastShownAt = Time.unscaledTime;
            var keys = ControlModeWatcher.Mode == ControlMode.Desktop;
            switch (step)
            {
                case 0:
                    _title.text = "1/3  MOVE";
                    _text.text = keys ? "W A S D to move. The mouse aims." : "The left stick moves your hero.";
                    break;
                case 1:
                    _title.text = "2/3  ATTACK";
                    _text.text = keys ? "Left click throws your basic attack, from what your Genesis holds or wears."
                        : "Drag the pink stick to aim, let go to throw your basic attack.";
                    break;
                default:
                    var siege = MatchModes.IsSiege(frame);
                    _title.text = siege ? "3/3  BRING THEIR TOWER DOWN" : "3/3  HOLD THE COINS";
                    _text.text = (siege ? "Hit their tower and core. " : "Pick up coins, keep them from the other team. ")
                                 + (keys ? "Right click or Space fires your special once it is charged." : "The gold stick fires your special once it is charged.")
                                 + " Tap to close.";
                    break;
            }
        }

        private static EntityRef LocalHero(Frame frame, QuantumGame game)
        {
            var heroes = frame.Filter<PlayerLink, Transform2D>();
            while (heroes.Next(out var entity, out var link, out _))
                if (link.PlayerRef != PlayerRef.None && game.PlayerIsLocal(link.PlayerRef))
                    return entity;
            return EntityRef.None;
        }
    }
}
