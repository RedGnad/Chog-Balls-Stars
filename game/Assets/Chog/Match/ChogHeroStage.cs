using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// A 3D hero shown from close inside a screen: the hero of a Genesis (ChogHeroModel) standing on a
    /// round pedestal, turning slowly, lit as in the match, drawn by its own camera into a picture with
    /// a transparent background that a RawImage lays over the screen. Used by the menu and by the end
    /// of a match. Each stage stands far from the arena and from the other stages, so no camera sees
    /// anything else.
    /// </summary>
    public sealed class ChogHeroStage : MonoBehaviour
    {
        private const float TurnDegreesPerSecond = 22f;
        private const float FieldOfView = 24f;
        private const float Elevation = 10f;
        private const float StartTurn = 200f;   // three quarter, from the front left, as the pictures
        private const float PedestalRadius = 0.62f;
        private const float PedestalHeight = 0.08f;

        // A layer no scene uses: the camera and the light of a stage see only what stands on it, so the
        // light of a stage never lights the arena.
        public const int Layer = 30;

        private static int s_stages;

        private RawImage _shown;
        private RenderTexture _picture;
        private Camera _camera;
        private Transform _stand;
        private Transform _hero;
        private ChogBodyAnimator _body;
        private int _showing = -1;
        private bool _turns = true;

        /// <summary>The number of the Genesis shown (ChogHeroModel.Recruit for the grey Chog), -1 for none.</summary>
        public int Showing => _showing;

        /// <summary>Lays a stage over "area" (a RectTransform of a canvas) and returns it.</summary>
        public static ChogHeroStage Create(RectTransform area)
        {
            var shown = new GameObject("ChogHeroStage", typeof(RectTransform)).AddComponent<RawImage>();
            var rect = shown.rectTransform;
            rect.SetParent(area, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            shown.raycastTarget = false;
            shown.color = Color.white;
            var stage = shown.gameObject.AddComponent<ChogHeroStage>();
            stage._shown = shown;
            stage.Build(s_stages++);
            return stage;
        }

        private void Build(int index)
        {
            // The world of the stage, at the root of the scene so that no layout moves it.
            var world = new GameObject("ChogHeroStage World " + index).transform;
            world.position = new Vector3(0f, 9000f, 1000f * index);
            _stand = world;

            var pedestal = new GameObject("Pedestal", typeof(MeshFilter), typeof(MeshRenderer));
            pedestal.transform.SetParent(world, false);
            pedestal.GetComponent<MeshFilter>().sharedMesh = PedestalMesh();
            pedestal.GetComponent<MeshRenderer>().sharedMaterial = ChogHeroModel.Material;

            _camera = new GameObject("ChogHeroStage Camera").AddComponent<Camera>();
            _camera.transform.SetParent(world, false);
            _camera.fieldOfView = FieldOfView;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 40f;
            _camera.depth = -50f;
            _camera.cullingMask = 1 << Layer;

            var sun = new GameObject("ChogHeroStage Light").AddComponent<Light>();
            sun.transform.SetParent(world, false);
            sun.type = LightType.Directional;
            sun.transform.localRotation = Quaternion.Euler(45f, 160f, 0f);
            sun.intensity = 1f;
            sun.shadows = LightShadows.None;
            sun.cullingMask = 1 << Layer;
            pedestal.layer = Layer;
        }

        /// <summary>Shows the hero of a Genesis (or the grey Chog); hides the stage when it has none.</summary>
        public void Show(int metadataId)
        {
            if (metadataId == _showing)
                return;
            _showing = metadataId;
            if (_hero != null)
                Destroy(_hero.gameObject);
            _hero = ChogHeroModel.Has(metadataId) ? ChogHeroModel.Build(metadataId, _stand) : null;
            _shown.enabled = _hero != null;
            if (_hero == null)
                return;
            foreach (var part in _hero.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = Layer;
            _hero.localPosition = new Vector3(0f, PedestalHeight, 0f);
            _hero.localRotation = Quaternion.Euler(0f, StartTurn, 0f);
            _body = ChogBodyAnimator.Attach(_hero);
        }

        /// <summary>The hero cheers (a win) or slumps (a loss); stops turning to face the viewer.</summary>
        public void Mood(bool won)
        {
            if (_body == null)
                return;
            _turns = false;
            _hero.localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (won)
                _body.Cheer();
            else
                _body.Defeated();
        }

        private void LateUpdate()
        {
            if (_hero == null || _shown == null)
                return;

            // The picture follows the size the stage takes on screen.
            var size = Vector2Int.Max(Vector2Int.one * 64, Vector2Int.RoundToInt(PixelSize(_shown.rectTransform)));
            if (_picture == null || _picture.width != size.x || _picture.height != size.y)
            {
                if (_picture != null)
                    _picture.Release();
                _picture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                _camera.targetTexture = _picture;
                _shown.texture = _picture;
            }

            // Framed on the whole hero and its pedestal, whatever the shape of the stage.
            var height = ChogHeroModel.ModelHeight + 0.55f;
            var aspect = size.x / (float)size.y;
            var fit = Mathf.Max(height, (PedestalRadius * 2.4f) / aspect);
            var distance = fit * 0.5f / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            var turn = Quaternion.Euler(Elevation, 0f, 0f);
            _camera.transform.localRotation = turn;
            _camera.transform.localPosition = new Vector3(0f, height * 0.36f, 0f) - turn * Vector3.forward * distance;

            if (_turns)
                _hero.Rotate(0f, TurnDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }

        // The camera draws only while the stage is on screen: a hidden menu costs nothing in a match.
        private void OnEnable()
        {
            if (_stand != null)
                _stand.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_stand != null)
                _stand.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_picture != null)
                _picture.Release();
            if (_stand != null)
                Destroy(_stand.gameObject);
        }

        private static Vector2 PixelSize(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new Vector2(Mathf.Abs(high.x - low.x), Mathf.Abs(high.y - low.y));
        }

        private static Mesh s_pedestal;

        // A low round pedestal in the colours of the game: a lilac top, a darker side, the dark outline
        // ring around its top edge, in the vertex colours ChogHero.shader reads (lit, alpha 1).
        private static Mesh PedestalMesh()
        {
            if (s_pedestal != null)
                return s_pedestal;

            const int sides = 48;
            var top = new Color(0.55f, 0.47f, 0.86f, 1f);
            var side = new Color(0.33f, 0.26f, 0.60f, 1f);
            var line = new Color(0.07f, 0.04f, 0.10f, 0.6f);   // unlit
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();

            vertices.Add(new Vector3(0f, 0f, 0f));
            normals.Add(Vector3.up);
            colours.Add(top);
            for (var i = 0; i <= sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var at = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices.Add(at * (PedestalRadius - 0.03f));
                normals.Add(Vector3.up);
                colours.Add(top);
            }
            for (var i = 0; i < sides; i++)
                triangles.AddRange(new[] { 0, i + 2, i + 1 });

            // The line around the top, then the side down to the floor.
            AddBand(vertices, normals, colours, triangles, sides, PedestalRadius - 0.03f, 0f, PedestalRadius, 0f, line, true);
            AddBand(vertices, normals, colours, triangles, sides, PedestalRadius, 0f, PedestalRadius, -PedestalHeight, side, false);

            s_pedestal = new Mesh { name = "ChogHeroStage Pedestal" };
            s_pedestal.SetVertices(vertices);
            s_pedestal.SetNormals(normals);
            s_pedestal.SetColors(colours);
            s_pedestal.SetTriangles(triangles, 0);
            s_pedestal.RecalculateBounds();
            return s_pedestal;
        }

        private static void AddBand(List<Vector3> vertices, List<Vector3> normals, List<Color> colours, List<int> triangles,
                                    int sides, float innerRadius, float innerY, float outerRadius, float outerY, Color colour, bool flat)
        {
            var start = vertices.Count;
            for (var i = 0; i <= sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var at = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var normal = flat ? Vector3.up : at;
                vertices.Add(at * innerRadius + Vector3.up * innerY);
                vertices.Add(at * outerRadius + Vector3.up * outerY);
                normals.Add(normal);
                normals.Add(normal);
                colours.Add(colour);
                colours.Add(colour);
            }
            for (var i = 0; i < sides; i++)
            {
                var a = start + i * 2;
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
        }
    }
}
