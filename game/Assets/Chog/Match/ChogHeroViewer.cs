using System.Collections.Generic;
using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// One screen to look at the 3D heroes (ChogHeroModel) without a match: for each Genesis named in
    /// the page option "?heroes=1287,14,17" (or "?heroes=1" for a set chosen to show many traits), its
    /// hero from close, three quarter from the front, above the same hero seen from the angle of the
    /// game camera. "&spin=1" turns them slowly. It covers the menu.
    /// </summary>
    public sealed class ChogHeroViewer : MonoBehaviour
    {
        public const string QueryParameter = "heroes";

        // Shown by "?heroes=1": the owner's Genesis and heroes with many kinds of hats, eyes, mouths
        // and clothes (the Genesis pointed at during batches 5 and 6 among them).
        private static readonly int[] Default = { 1287, 14, 17, 1241, 43, 29, 62, 51 };

        private const float CellWidth = 1.5f;
        private const float RowHeight = 1.5f;
        private const float GameElevation = 73f;   // degrees over the horizon, as render_chog_traits.py
        private const float CloseElevation = 12f;
        private const float SpinDegreesPerSecond = 30f;

        // Far from any scene, so the cameras of the viewer see nothing else; one place for each row.
        private static readonly Vector3 CloseOrigin = new Vector3(0f, 7000f, 0f);
        private static readonly Vector3 GameOrigin = new Vector3(0f, 7000f, 400f);
        private static readonly Color Background = new Color(0.16f, 0.14f, 0.2f);

        private readonly List<Transform> _turned = new List<Transform>();
        private readonly List<(Transform Thing, ChogHeroModel.AttackStyle Style)> _thrown =
            new List<(Transform, ChogHeroModel.AttackStyle)>();
        // One picture for each row: on WebGL a camera that draws into a part of a picture clears
        // the whole of it, so two cameras cannot share one.
        private readonly List<RenderTexture> _pictures = new List<RenderTexture>();
        private bool _spin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Open()
        {
            if (!PerformanceSampling.TryGetQueryValue(Application.absoluteURL, QueryParameter, out _))
                return;

            var host = new GameObject("Chog Hero Viewer");
            DontDestroyOnLoad(host);
            host.AddComponent<ChogHeroViewer>();
        }

        /// <summary>The Genesis a page asks for: numbers separated by commas, or the default set for "1".</summary>
        public static int[] Asked(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "1")
                return Default;
            var asked = new List<int>();
            foreach (var word in value.Split(','))
                if (int.TryParse(word, out var number) && number >= 0)
                    asked.Add(number);
            return asked.Count > 0 ? asked.ToArray() : Default;
        }

        private void Start()
        {
            PerformanceSampling.TryGetQueryValue(Application.absoluteURL, QueryParameter, out var value);
            _spin = PerformanceSampling.HasQueryFlag(Application.absoluteURL, "spin");
            var numbers = Asked(value);
            var width = Mathf.Max(numbers.Length * CellWidth, 4f);

            var canvasHost = new GameObject("Viewer Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasHost.transform.SetParent(transform, false);
            var canvas = canvasHost.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            Row(CloseOrigin, numbers, width, CloseElevation, 200f, canvasHost.transform, new Vector2(0f, 0.5f), Vector2.one);
            Row(GameOrigin, numbers, width, GameElevation, 180f, canvasHost.transform, Vector2.zero, new Vector2(1f, 0.5f));

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var column = 0; column < numbers.Length; column++)
            {
                var said = numbers[column] == ChogHeroModel.Recruit ? "recruit" : "CHOG #" + numbers[column];
                if (!ChogHeroModel.Has(numbers[column]))
                    said += " (not modelled)";
                // Where the column stands across the picture, as the cameras of Row frame it.
                var middle = 0.5f + (-width * 0.5f + (column + 0.5f) * CellWidth) / (2f * HalfExtent(width));
                var across = CellWidth / (2f * HalfExtent(width));
                Label(canvasHost.transform, font, said, middle - across * 0.5f, 0.93f, across, 0.05f);
            }

            Label(canvasHost.transform, font, "game camera", 0.005f, 0.44f, 0.15f, 0.04f);
            Debug.Log("CHOG_HEROES " + numbers.Length + " heroes asked, " + _turned.Count / 2 + " built");
        }

        // One row of heroes, seen by its own camera into its own picture, shown over that part of the screen.
        private void Row(Vector3 origin, int[] numbers, float width, float elevation, float facing, Transform canvas,
                         Vector2 screenMin, Vector2 screenMax)
        {
            var root = new GameObject("Viewer Row").transform;
            root.SetParent(transform, false);
            root.position = origin;

            for (var column = 0; column < numbers.Length; column++)
            {
                var hero = ChogHeroModel.Build(numbers[column], root);
                if (hero == null)
                    continue;
                hero.localPosition = new Vector3(-width * 0.5f + (column + 0.5f) * CellWidth, 0f, 0f);
                hero.localRotation = Quaternion.Euler(0f, facing, 0f);
                ChogBodyAnimator.Attach(hero);
                _turned.Add(hero);

                // Beside the hero seen by the game camera: what its basic attack throws, turning as it does.
                if (elevation > 45f)
                {
                    var thrown = ChogHeroModel.BuildAttack(numbers[column], root, out var style, out _);
                    if (thrown != null)
                    {
                        // The hero is drawn at the size it was made here: its object too.
                        thrown.localScale = Vector3.one / ChogHeroModel.Scale;
                        thrown.localPosition = hero.localPosition + new Vector3(0.5f, 0.6f, -0.2f);
                        _thrown.Add((thrown, style));
                    }
                }
            }

            var view = new GameObject("Viewer Camera").AddComponent<Camera>();
            view.transform.SetParent(root, false);
            view.transform.localRotation = Quaternion.Euler(elevation, 0f, 0f);
            view.transform.localPosition = new Vector3(0f, 0.6f, 0f) + view.transform.localRotation * new Vector3(0f, 0f, -30f);
            view.orthographic = true;
            view.aspect = Aspect;
            view.orthographicSize = HalfExtent(width) / Aspect;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Background;
            view.nearClipPlane = 1f;
            view.farClipPlane = 80f;
            var picture = new RenderTexture(1920, 540, 24) { antiAliasing = 4 };
            _pictures.Add(picture);
            view.targetTexture = picture;
            var shown = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            Stretch(shown.rectTransform, canvas, screenMin, screenMax);
            shown.texture = picture;
            shown.raycastTarget = false;

            var sun = new GameObject("Viewer Light").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.localRotation = Quaternion.Euler(50f, 30f, 0f);
            sun.intensity = 1f;
        }

        // Each row is half of a 1920 x 1080 picture.
        private const float Aspect = 1920f / 540f;

        // Half the width a row's camera frames: all the heroes, and never less than a row's height.
        private static float HalfExtent(float width) => Mathf.Max(width * 0.5f + 0.2f, RowHeight * 0.5f * Aspect);

        private void Update()
        {
            foreach (var (thing, style) in _thrown)
            {
                if (thing == null)
                    continue;
                var turn = style == ChogHeroModel.AttackStyle.Spin ? new Vector3(0f, 180f, 0f)
                    : style == ChogHeroModel.AttackStyle.Tumble ? new Vector3(150f, 0f, 0f) : new Vector3(0f, 0f, 60f);
                thing.Rotate(turn * Time.deltaTime, Space.Self);
            }
            if (!_spin)
                return;
            foreach (var hero in _turned)
                if (hero != null)
                    hero.Rotate(0f, SpinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }

        private void OnDestroy()
        {
            foreach (var picture in _pictures)
                if (picture != null)
                    picture.Release();
        }

        private static void Label(Transform canvas, Font font, string text, float left, float bottom, float width, float height)
        {
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            Stretch(label.rectTransform, canvas, new Vector2(left, bottom), new Vector2(left + width, bottom + height));
            label.font = font;
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 24;
            label.raycastTarget = false;
        }

        private static void Stretch(RectTransform area, Transform parent, Vector2 min, Vector2 max)
        {
            area.SetParent(parent, false);
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
        }
    }
}
