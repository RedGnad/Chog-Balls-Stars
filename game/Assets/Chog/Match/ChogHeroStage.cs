using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// A 3D hero shown from close inside a screen: the hero of a Genesis (ChogHeroModel) standing on a
    /// painted, uneven pedestal, turning slowly, lit as in the match, drawn by its own camera into a picture with
    /// a transparent background that a RawImage lays over the screen. Used by the menu and by the end
    /// of a match. Each stage stands far from the arena and from the other stages, so no camera sees
    /// anything else.
    /// </summary>
    public sealed class ChogHeroStage : MonoBehaviour, IDragHandler
    {
        // Degrees the hero turns for a drag across the whole stage.
        private const float DragDegrees = 300f;

        // The hero sways about the three-quarter view, as a showcase does, instead of turning its back.
        private const float SwayDegrees = 18f;
        private const float SwaySeconds = 7f;
        private const float FieldOfView = 24f;
        private const float Elevation = 10f;
        private const float StartTurn = 222f;   // a stable three-quarter view, with enough motion to read depth
        private const float PedestalRadius = 0.62f;
        private const float PedestalHeight = 0.08f;

        // A deliberately uneven outline. A perfect 48-sided disc read as a generic UI coin in the home
        // capture; this still fits the hero's feet but gives the stage the hand-cut silhouette of the
        // approved direction.
        private static readonly float[] PedestalOutline =
        {
            1.00f, 1.02f, 0.99f, 1.02f, 0.98f, 1.01f, 1.03f, 0.99f,
            1.01f, 0.98f, 1.03f, 1.00f, 0.98f, 1.02f, 0.99f, 1.01f,
            1.03f, 0.98f, 1.01f, 0.99f, 1.03f, 0.99f, 1.02f, 1.00f,
        };

        // The pink contact paint is a real stage mesh under the feet, not a screen-space glow. Its
        // outline and fill are two simple opaque shapes, so it remains visible in the stage camera.
        private static readonly Vector2[] PaintOutline =
        {
            new Vector2(-0.52f, -0.05f), new Vector2(-0.41f, -0.23f), new Vector2(-0.20f, -0.30f),
            new Vector2(0.00f, -0.25f), new Vector2(0.17f, -0.34f), new Vector2(0.39f, -0.22f),
            new Vector2(0.52f, -0.04f), new Vector2(0.42f, 0.13f), new Vector2(0.22f, 0.23f),
            new Vector2(0.03f, 0.17f), new Vector2(-0.16f, 0.29f), new Vector2(-0.36f, 0.20f),
            new Vector2(-0.49f, 0.08f),
        };

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
        // Room left over the head, as a share of the hero's height: the arms of a cheer rise above it (22 October 2026, the
        // owner: the victory was cut at the top and the hands clipped by the edge of the picture).
        private float _headroom;
        // What dragging added to the hero's turn.
        private float _turned;

        /// <summary>Whether a finger or the mouse can turn the hero by dragging across the stage.</summary>
        public bool Draggable
        {
            set => _shown.raycastTarget = value;
        }

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
            // A result stage survives the gameplay scene unloading while the referee
            // completes the replay. OnDestroy removes it with its screen.
            DontDestroyOnLoad(world.gameObject);
            _stand = world;

            var pedestal = new GameObject("Pedestal", typeof(MeshFilter), typeof(MeshRenderer));
            pedestal.transform.SetParent(world, false);
            pedestal.GetComponent<MeshFilter>().sharedMesh = PedestalMesh();
            pedestal.GetComponent<MeshRenderer>().sharedMaterial = ChogHeroModel.Material;

            var paintOutline = new GameObject("Stage paint outline", typeof(MeshFilter), typeof(MeshRenderer));
            paintOutline.transform.SetParent(world, false);
            paintOutline.transform.localPosition = Vector3.up * 0.004f;
            paintOutline.GetComponent<MeshFilter>().sharedMesh = PaintMesh("ChogHeroStage Paint Outline", new Color(0.07f, 0.04f, 0.10f, 1f));
            paintOutline.GetComponent<MeshRenderer>().sharedMaterial = ChogHeroModel.Material;

            var paintFill = new GameObject("Stage paint", typeof(MeshFilter), typeof(MeshRenderer));
            paintFill.transform.SetParent(world, false);
            paintFill.transform.localPosition = Vector3.up * 0.007f;
            paintFill.transform.localScale = new Vector3(0.92f, 1f, 0.92f);
            paintFill.GetComponent<MeshFilter>().sharedMesh = PaintMesh("ChogHeroStage Paint", new Color(0.91f, 0.12f, 0.42f, 1f));
            paintFill.GetComponent<MeshRenderer>().sharedMaterial = ChogHeroModel.Material;

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
            paintOutline.layer = Layer;
            paintFill.layer = Layer;
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
            _headroom = won ? 0.5f : 0f;
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
            var extra = ChogHeroModel.ModelHeight * _headroom;
            var aspect = size.x / (float)size.y;
            var fit = Mathf.Max(height + extra, (PedestalRadius * 2.4f) / aspect);
            var distance = fit * 0.5f / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            var turn = Quaternion.Euler(Elevation, 0f, 0f);
            _camera.transform.localRotation = turn;
            // The bottom of the picture stays where it was; the headroom is added above.
            _camera.transform.localPosition = new Vector3(0f, height * 0.36f + extra * 0.5f, 0f) - turn * Vector3.forward * distance;

            if (_turns)
                _hero.localRotation = Quaternion.Euler(0f, StartTurn + _turned + SwayDegrees * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / SwaySeconds), 0f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var width = Mathf.Max(1f, PixelSize(_shown.rectTransform).x);
            _turned -= eventData.delta.x / width * DragDegrees;
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

        // A low uneven gold pedestal in the home screen's colours: light top, deeper side, a dark rim and
        // a pink contact mark behind the hero's feet. Vertex colours work with the same material as heroes.
        private static Mesh PedestalMesh()
        {
            if (s_pedestal != null)
                return s_pedestal;

            var top = new Color(0.98f, 0.72f, 0.22f, 1f);
            var side = new Color(0.72f, 0.40f, 0.02f, 1f);
            var line = new Color(0.07f, 0.04f, 0.10f, 0.6f);   // unlit
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();

            vertices.Add(new Vector3(0f, 0f, 0f));
            normals.Add(Vector3.up);
            colours.Add(top);
            for (var i = 0; i <= PedestalOutline.Length; i++)
            {
                var point = RingPoint(i, PedestalRadius - 0.03f, 0f);
                vertices.Add(point);
                normals.Add(Vector3.up);
                colours.Add(Color.Lerp(top, new Color(1f, 0.81f, 0.40f, 1f), i % 3 == 0 ? 0.18f : 0f));
            }
            for (var i = 0; i < PedestalOutline.Length; i++)
                triangles.AddRange(new[] { 0, i + 2, i + 1 });

            // The line around the top, then the side down to the floor.
            AddBand(vertices, normals, colours, triangles, PedestalRadius - 0.03f, 0f, PedestalRadius, 0f, line, true);
            AddBand(vertices, normals, colours, triangles, PedestalRadius, 0f, PedestalRadius, -PedestalHeight, side, false);

            s_pedestal = new Mesh { name = "ChogHeroStage Pedestal" };
            s_pedestal.SetVertices(vertices);
            s_pedestal.SetNormals(normals);
            s_pedestal.SetColors(colours);
            s_pedestal.SetTriangles(triangles, 0);
            s_pedestal.RecalculateBounds();
            return s_pedestal;
        }

        private static Mesh PaintMesh(string name, Color colour)
        {
            var vertices = new List<Vector3> { Vector3.zero };
            var normals = new List<Vector3> { Vector3.up };
            var colours = new List<Color> { colour };
            var triangles = new List<int>();
            for (var i = 0; i <= PaintOutline.Length; i++)
            {
                var point = PaintOutline[i % PaintOutline.Length];
                vertices.Add(new Vector3(point.x, 0f, point.y));
                normals.Add(Vector3.up);
                colours.Add(colour);
            }
            for (var i = 0; i < PaintOutline.Length; i++)
                triangles.AddRange(new[] { 0, i + 1, i + 2 });
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 RingPoint(int index, float radius, float y)
        {
            var count = PedestalOutline.Length;
            var angle = index % count * Mathf.PI * 2f / count;
            var outline = PedestalOutline[index % count];
            return new Vector3(Mathf.Cos(angle) * radius * outline, y, Mathf.Sin(angle) * radius * outline);
        }

        private static void AddBand(List<Vector3> vertices, List<Vector3> normals, List<Color> colours, List<int> triangles,
                                    float innerRadius, float innerY, float outerRadius, float outerY, Color colour, bool flat)
        {
            var start = vertices.Count;
            for (var i = 0; i <= PedestalOutline.Length; i++)
            {
                var inner = RingPoint(i, innerRadius, innerY);
                var outer = RingPoint(i, outerRadius, outerY);
                var normal = flat ? Vector3.up : new Vector3(outer.x, 0f, outer.z).normalized;
                vertices.Add(inner);
                vertices.Add(outer);
                normals.Add(normal);
                normals.Add(normal);
                colours.Add(colour);
                colours.Add(colour);
            }
            for (var i = 0; i < PedestalOutline.Length; i++)
            {
                var a = start + i * 2;
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
        }
    }
}
