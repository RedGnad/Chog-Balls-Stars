using System.Collections.Generic;
using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// One screen to look at bodies moved by ChogBodyAnimator, without a match: each model in a
    /// row, standing, running, attacking, winning and losing. It shows the three class models of
    /// the sample, a stand-in made of four plain parts, and the Chog body when the project has
    /// one (a model saved as "ChogBody" in a Resources folder). Opened with the page option
    /// "?bodies=1"; it covers the menu.
    /// </summary>
    public sealed class ChogBodyViewer : MonoBehaviour
    {
        public const string QueryParameter = "bodies";

        /// <summary>Name, in a Resources folder, of the model that is the body of every Genesis.</summary>
        public const string BodyResource = "ChogBody";

        private static readonly string[] ClassPrefabs =
            { "Character_Playable_Knight", "Character_Playable_Archer", "Character_Playable_Spellcaster" };
        private static readonly string[] States = { "stands", "runs", "attacks", "wins", "loses" };

        private const float CellWidth = 2.6f;
        private const float CellHeight = 2.5f;
        private const float AttackEverySeconds = 0.9f;

        // Far from any scene, so the camera of the viewer sees nothing else.
        private static readonly Vector3 Origin = new Vector3(0f, 6000f, 0f);
        private static readonly Color Background = new Color(0.16f, 0.14f, 0.2f);

        private RenderTexture _picture;
        private readonly List<(ChogBodyAnimator Body, int State)> _shown = new List<(ChogBodyAnimator, int)>();
        private float _nextAttack;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Open()
        {
            if (!PerformanceSampling.HasQueryFlag(Application.absoluteURL, QueryParameter))
                return;

            var host = new GameObject("Chog Body Viewer");
            DontDestroyOnLoad(host);
            host.AddComponent<ChogBodyViewer>();
        }

        /// <summary>
        /// A copy of the model inside a character of the sample, without its scripts and without
        /// its animation component: only the parts and what draws them. Null when there is none.
        /// </summary>
        public static GameObject ModelOf(string characterPrefab, Transform parent)
        {
            var character = Resources.Load<GameObject>(characterPrefab);
            var animated = character == null ? null : character.GetComponentInChildren<Animator>(true);
            if (animated == null)
                return null;

            // Copied under a parent that is switched off, so that no script of the copy wakes up
            // before it is removed.
            var holder = new GameObject(characterPrefab + " holder");
            holder.SetActive(false);
            holder.transform.SetParent(parent, false);
            var model = Instantiate(animated.gameObject, holder.transform);
            // A script another one requires cannot go first: a second pass takes what the first left.
            for (var pass = 0; pass < 3; pass++)
                foreach (var behaviour in model.GetComponentsInChildren<Behaviour>(true))
                    DestroyImmediate(behaviour);
            model.transform.SetParent(parent, false);
            Destroy(holder);
            return model;
        }

        private void Start()
        {
            var root = new GameObject("Viewer Content").transform;
            root.SetParent(transform, false);
            root.position = Origin;

            // What is shown, row by row: a name and a way to make one copy of the model.
            var rows = new List<(string Name, System.Func<Transform, GameObject> Make)>();
            Material plain = null;
            foreach (var prefab in ClassPrefabs)
            {
                var name = prefab.Replace("Character_Playable_", "");
                rows.Add((name, parent => ModelOf(prefab, parent)));
                if (plain == null)
                {
                    var character = Resources.Load<GameObject>(prefab);
                    var drawn = character == null ? null : character.GetComponentInChildren<MeshRenderer>(true);
                    plain = drawn == null ? null : drawn.sharedMaterial;
                }
            }

            var chog = Resources.Load<GameObject>(BodyResource);
            if (chog != null)
                rows.Add(("Chog body", parent => Instantiate(chog, parent)));
            rows.Add(("four plain parts", parent => ChogBodyPlaceholder.Build(parent, plain, new Color(0.62f, 0.45f, 0.9f))));

            var width = States.Length * CellWidth;
            var height = Mathf.Max(rows.Count * CellHeight, width * 9f / 16f);

            _picture = new RenderTexture(1920, 1080, 24);
            var view = new GameObject("Viewer Camera").AddComponent<Camera>();
            view.transform.SetParent(root, false);
            // From the front and a little above, as flat as a drawing board.
            view.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            view.transform.localPosition = view.transform.localRotation * new Vector3(0f, 0f, -30f);
            view.orthographic = true;
            view.orthographicSize = height * 0.5f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Background;
            view.nearClipPlane = 1f;
            view.farClipPlane = 80f;
            view.targetTexture = _picture;

            var sun = new GameObject("Viewer Light").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.localRotation = Quaternion.Euler(40f, 25f, 0f);
            sun.intensity = 1.2f;

            var canvasHost = new GameObject("Viewer Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasHost.transform.SetParent(transform, false);
            var canvas = canvasHost.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var shown = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            Stretch(shown.rectTransform, canvasHost.transform, Vector2.zero, Vector2.one);
            shown.texture = _picture;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var column = 0; column < States.Length; column++)
                Label(canvasHost.transform, font, States[column], (column + 0.1f) / States.Length, 0.94f, 0.8f / States.Length, 0.05f);

            for (var row = 0; row < rows.Count; row++)
            {
                var top = 1f - (row + 0.2f) * CellHeight / height;
                Label(canvasHost.transform, font, rows[row].Name, 0.005f, top - 0.04f, 0.12f, 0.04f);
                for (var column = 0; column < States.Length; column++)
                {
                    var model = rows[row].Make(root);
                    if (model == null)
                        continue;

                    // Turned to face the camera, feet near the bottom of its cell.
                    model.transform.localPosition = new Vector3(-width * 0.5f + (column + 0.5f) * CellWidth,
                        height * 0.5f - (row + 0.85f) * CellHeight, 0f);
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    var body = ChogBodyAnimator.Attach(model.transform);
                    if (body == null)
                    {
                        Debug.LogWarning("CHOG_BODIES " + rows[row].Name + " has no Body, ArmLeft and ArmRight parts");
                        continue;
                    }

                    _shown.Add((body, column));
                    if (column == 1)
                        body.Speed = ChogBodyAnimator.FullRunSpeed;
                    else if (column == 3)
                        body.Cheer();
                    else if (column == 4)
                        body.Defeated();
                }
            }

            Debug.Log("CHOG_BODIES " + rows.Count + " models, " + _shown.Count + " bodies moved");
        }

        private void Update()
        {
            if (Time.time < _nextAttack)
                return;

            _nextAttack = Time.time + AttackEverySeconds;
            foreach (var shown in _shown)
                if (shown.State == 2 && shown.Body != null)
                    shown.Body.Attack();
        }

        private void OnDestroy()
        {
            if (_picture != null)
                _picture.Release();
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
            label.resizeTextMaxSize = 26;
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
