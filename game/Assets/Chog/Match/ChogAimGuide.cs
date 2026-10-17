using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The aim of a lobbed throw before it leaves. The owner found a lob "impossible to aim" (17
    /// October): a mouse throws on the press, and the sample showed the arc only while a button was
    /// held, so after the throw. As a thrower's aim in Brawl Stars shows the curved path and the
    /// point of impact, this draws dots along the very arc the throw will fly (ChogAttackLook's
    /// LobHeight and LobRise, from the height of the hands), the ring where it lands (the size of
    /// the ring the throw then leaves on the floor), and a faint circle of the farthest reach around
    /// the hero. White when the throw is ready, red when the hero lacks the energy. One object for
    /// the local hero, three draw calls, the dots moving slowly toward the landing.
    /// </summary>
    public sealed class ChogAimGuide : MonoBehaviour
    {
        private const string ObjectName = "ChogAimGuide";
        private const int Dots = 12;
        private const int DotSides = 10;
        private const float DotRadius = 0.12f;
        // The dots run along the arc at this many dot spacings a second.
        private const float DotsPerSecond = 1.6f;
        // ChogAttackLook.Height: a throw leaves and lands at the height of the hands of a hero.
        public const float HandHeight = 0.9f;
        // ChogAttackLook.LandingRadius: the ring a throw leaves where it will land.
        private const float LandingRadius = 1.1f;
        private const float ReachWidth = 0.07f;
        // On the floor, whose planks stand above y = 0 (ChogTeamRing.Lift, ChogAttackLook).
        private const float OnFloor = 0.18f;

        private static readonly Color Ready = new Color(1f, 1f, 1f, 0.9f);
        // ChogTheme Danger (#E5484D).
        private static readonly Color Blocked = new Color(0.898f, 0.282f, 0.302f, 0.9f);

        private static ChogAimGuide s_guide;
        private static Material s_material;
        private static readonly Dictionary<bool, Mesh> s_landings = new Dictionary<bool, Mesh>();
        private static readonly Dictionary<(int, bool), Mesh> s_reaches = new Dictionary<(int, bool), Mesh>();

        private Mesh _dots;
        private Transform _landing;
        private Transform _reach;
        private MeshFilter _landingMesh;
        private MeshFilter _reachMesh;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colours = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        /// <summary>
        /// Shows the aim of a lob thrown from "hero" (on the floor) to "landing" (on the floor, already
        /// held within "reach"), "ready" when the hero has the energy for it. False when this device
        /// cannot draw it, and the caller keeps its own preview.
        /// </summary>
        public static bool ShowLob(Vector3 hero, Vector3 landing, float reach, bool ready)
        {
            if (s_guide == null && !Create())
                return false;
            s_guide.gameObject.SetActive(true);
            s_guide.Draw(hero, landing, reach, ready);
            return true;
        }

        public static void Hide()
        {
            if (s_guide != null && s_guide.gameObject.activeSelf)
                s_guide.gameObject.SetActive(false);
        }

        /// <summary>
        /// The point at "done" (0 thrown, 1 landed) of the arc a lob flies from "hero" to "landing",
        /// both on the floor: the path of ChogAttackLook's thrown object.
        /// </summary>
        public static Vector3 ArcPoint(Vector3 hero, Vector3 landing, float done)
        {
            var flat = new Vector3(landing.x - hero.x, 0f, landing.z - hero.z);
            var height = ChogAttackLook.LobHeight(flat.magnitude);
            var point = new Vector3(hero.x, hero.y, hero.z) + flat * Mathf.Clamp01(done);
            point.y = hero.y + HandHeight + ChogAttackLook.LobRise(height, done);
            return point;
        }

        /// <summary>Where along the arc (0 to 1) dot "index" stands "seconds" after the aim showed.</summary>
        public static float DotPlace(int index, float seconds)
        {
            var shift = seconds * DotsPerSecond - Mathf.Floor(seconds * DotsPerSecond);
            // From just off the hands to just above the ring: none hides in the hero or the ring.
            return Mathf.Lerp(0.06f, 0.97f, (index + shift) / Dots);
        }

        private static bool Create()
        {
            if (s_material == null)
            {
                var shader = Resources.Load<Shader>(ChogTeamRing.ShaderResource);
                if (shader == null || !shader.isSupported)
                    return false;
                s_material = new Material(shader) { name = "ChogAimGuide" };
            }
            var host = new GameObject(ObjectName);
            s_guide = host.AddComponent<ChogAimGuide>();
            s_guide.Build();
            return true;
        }

        private void Build()
        {
            _dots = new Mesh { name = "ChogAimDots" };
            _dots.MarkDynamic();
            Drawn("Dots", transform).sharedMesh = _dots;
            _landingMesh = Drawn("Landing", transform);
            _landing = _landingMesh.transform;
            _reachMesh = Drawn("Reach", transform);
            _reach = _reachMesh.transform;
        }

        private static MeshFilter Drawn(string name, Transform parent)
        {
            var drawn = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            drawn.transform.SetParent(parent, false);
            var renderer = drawn.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = s_material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return drawn.GetComponent<MeshFilter>();
        }

        private void Draw(Vector3 hero, Vector3 landing, float reach, bool ready)
        {
            var colour = ready ? Ready : Blocked;
            hero.y = 0f;
            landing.y = 0f;

            if (!s_landings.TryGetValue(ready, out var ring))
                s_landings[ready] = ring = GroundMeshes.Build("ChogAimLanding", LandingRadius, colour, false, true);
            _landingMesh.sharedMesh = ring;
            _landing.position = new Vector3(landing.x, OnFloor, landing.z);

            // The farthest reach, by tenths of a unit so a few meshes serve every attack.
            var tenths = Mathf.Max(1, Mathf.RoundToInt(reach * 10f));
            if (!s_reaches.TryGetValue((tenths, ready), out var circle))
                s_reaches[(tenths, ready)] = circle = Circle(tenths / 10f, colour);
            _reachMesh.sharedMesh = circle;
            _reach.position = new Vector3(hero.x, OnFloor, hero.z);

            DrawDots(hero, landing, colour);
        }

        // The dots face the camera, each a disc with a soft edge, all in one mesh in world space.
        private void DrawDots(Vector3 hero, Vector3 landing, Color colour)
        {
            var view = Camera.main != null ? Camera.main.transform : null;
            var right = view != null ? view.right : Vector3.right;
            var up = view != null ? view.up : Vector3.forward;
            var edge = new Color(colour.r, colour.g, colour.b, 0f);
            _vertices.Clear();
            _colours.Clear();
            _triangles.Clear();
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            for (var i = 0; i < Dots; i++)
            {
                var done = DotPlace(i, Time.time);
                var centre = ArcPoint(hero, landing, done);
                // A dot fades in off the hands and out above the ring.
                var fade = Mathf.Clamp01(Mathf.Min(done - 0.04f, 0.99f - done) * 12f);
                var solid = new Color(colour.r, colour.g, colour.b, colour.a * fade);
                var start = _vertices.Count;
                _vertices.Add(centre);
                _colours.Add(solid);
                for (var s = 0; s < DotSides; s++)
                {
                    var angle = s * Mathf.PI * 2f / DotSides;
                    var offset = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    _vertices.Add(centre + offset * (DotRadius * 0.7f));
                    _colours.Add(solid);
                    _vertices.Add(centre + offset * DotRadius);
                    _colours.Add(edge);
                }
                for (var s = 0; s < DotSides; s++)
                {
                    var inner = start + 1 + s * 2;
                    var nextInner = start + 1 + (s + 1) % DotSides * 2;
                    _triangles.Add(start);
                    _triangles.Add(inner);
                    _triangles.Add(nextInner);
                    _triangles.Add(inner);
                    _triangles.Add(inner + 1);
                    _triangles.Add(nextInner + 1);
                    _triangles.Add(inner);
                    _triangles.Add(nextInner + 1);
                    _triangles.Add(nextInner);
                }
            }
            _dots.Clear();
            _dots.SetVertices(_vertices);
            _dots.SetColors(_colours);
            _dots.SetTriangles(_triangles, 0);
            _dots.RecalculateBounds();
        }

        // A thin circle of radius "radius" on the floor, faint, with soft edges.
        private static Mesh Circle(float radius, Color colour)
        {
            const int segments = 72;
            var faint = new Color(colour.r, colour.g, colour.b, 0.32f);
            var clear = new Color(colour.r, colour.g, colour.b, 0f);
            var radii = new[] { radius - ReachWidth, radius - ReachWidth * 0.4f, radius + ReachWidth * 0.4f, radius + ReachWidth };
            var shades = new[] { clear, faint, faint, clear };
            var vertices = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();
            for (var s = 0; s < segments; s++)
            {
                var angle = s * Mathf.PI * 2f / segments;
                for (var r = 0; r < radii.Length; r++)
                {
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radii[r], 0f, Mathf.Sin(angle) * radii[r]));
                    colours.Add(shades[r]);
                }
            }
            for (var s = 0; s < segments; s++)
            {
                var here = s * radii.Length;
                var next = (s + 1) % segments * radii.Length;
                for (var r = 0; r < radii.Length - 1; r++)
                {
                    triangles.Add(here + r);
                    triangles.Add(next + r);
                    triangles.Add(here + r + 1);
                    triangles.Add(here + r + 1);
                    triangles.Add(next + r);
                    triangles.Add(next + r + 1);
                }
            }
            var mesh = new Mesh { name = "ChogAimReach" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private void OnDestroy()
        {
            if (s_guide == this)
                s_guide = null;
            if (_dots != null)
                Destroy(_dots);
        }
    }
}
