using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// What stands under every hero and structure of a match: a soft round shadow, so that a hero
    /// stays on the floor on a phone (which draws no real shadow), and a ring in the colour of its
    /// side, the convention of arena brawlers: green for the player, blue for the player's team, red
    /// for the other team. The sample drew a faded circle there (its TeamIndicator): that circle is
    /// hidden and the ring is put in its place, so that it goes away with it when an enemy hides in
    /// a bush. One small mesh per side (shadow and ring together), one material for all: a draw call
    /// each. Teams are read from the Quantum frame as StructureViews reads them; without a local
    /// player (a spectator) team 0 is drawn as the player's. Off with "?matchlook=-ring".
    /// </summary>
    public sealed class ChogTeamRing : MonoBehaviour
    {
        public const string ShaderResource = "ChogGround";

        /// <summary>The colours of the sides, set by the screens from ChogTheme (its roles Confirm, Ally, Enemy).</summary>
        public static Color Self = new Color(0.30f, 0.85f, 0.48f);
        public static Color Ally = new Color(0.24f, 0.53f, 0.91f);
        public static Color Enemy = new Color(0.79f, 0.22f, 0.36f);

        // The child of a hero's view that holds the sample's faded circle, shown or hidden with the hero.
        private const string SampleIndicator = "TeamIndicator";
        private const string RingName = "ChogTeamRing";
        // Above the floor, whose planks stand above y = 0: just under the height of the sample's circle (0.2).
        private const float Lift = 0.18f;
        // Sizes in world units: a hero is about 1.1 wide; the tower and the core stand on wider bases.
        private const float HeroRadius = 0.86f;
        private const float StructureRadius = 1.35f;

        private enum Side { Self, Ally, Enemy }

        private sealed class Ground
        {
            public GameObject Shown;
            public GameObject Ring;
            public Side Side;
        }

        private readonly Dictionary<EntityRef, Ground> _heroes = new Dictionary<EntityRef, Ground>();
        private readonly Dictionary<EntityRef, Ground> _structures = new Dictionary<EntityRef, Ground>();
        private readonly HashSet<EntityRef> _seen = new HashSet<EntityRef>();
        private readonly List<EntityRef> _gone = new List<EntityRef>();
        private readonly Dictionary<(Side, bool), Mesh> _meshes = new Dictionary<(Side, bool), Mesh>();
        private Material _material;
        private QuantumEntityViewUpdater _views;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!ChogMatchLook.Has(MatchLookParts.Part.Ring))
                return;
            var host = new GameObject("Chog Team Rings");
            DontDestroyOnLoad(host);
            host.AddComponent<ChogTeamRing>();
        }

        private void LateUpdate()
        {
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var frame = game != null && game.Frames != null ? game.Frames.Predicted : null;
            if (frame == null)
            {
                Clear(_heroes);
                Clear(_structures);
                return;
            }

            if (_views == null)
                _views = FindFirstObjectByType<QuantumEntityViewUpdater>();
            if (_views == null || MaterialOf() == null)
                return;

            var localTeam = LocalTeam(game, frame);
            var ours = localTeam < 0 ? 0 : localTeam;

            _seen.Clear();
            var heroes = frame.Filter<Character, TeamInfo>();
            while (heroes.Next(out var entity, out _, out var team))
            {
                var view = _views.GetView(entity);
                if (view == null)
                    continue;
                _seen.Add(entity);
                var side = IsLocal(game, frame, entity) ? Side.Self : team.Index == ours ? Side.Ally : Side.Enemy;
                if (!_heroes.TryGetValue(entity, out var ground) || ground.Shown == null || ground.Side != side)
                {
                    if (ground != null && ground.Shown != null)
                        Destroy(ground.Shown);
                    ground = UnderHero(view.transform, side);
                    _heroes[entity] = ground;
                }

                var dead = frame.TryGet<Health>(entity, out var health) && health.IsDead;
                if (ground.Shown.activeSelf == dead)
                    ground.Shown.SetActive(!dead);
            }
            Forget(_heroes);

            _seen.Clear();
            var structures = frame.Filter<Structure, Transform2D, TeamInfo, Health>();
            while (structures.Next(out var entity, out _, out var place, out var team, out var health))
            {
                _seen.Add(entity);
                var side = team.Index == ours ? Side.Ally : Side.Enemy;
                if (!_structures.TryGetValue(entity, out var ground) || ground.Shown == null)
                {
                    ground = UnderStructure(side);
                    _structures[entity] = ground;
                }
                ground.Shown.transform.position = place.Position.ToUnityVector3() + Vector3.up * Lift;
                // A ruin keeps its shadow and loses its colour.
                if (ground.Ring.activeSelf == health.IsDead)
                    ground.Ring.SetActive(!health.IsDead);
            }
            Forget(_structures);
        }

        private Ground UnderHero(Transform view, Side side)
        {
            // The sample's circle is hidden but its object is kept: it is what the sample shows and
            // hides with the hero, and the ring, under it, follows.
            Transform holder = view;
            foreach (var child in view.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != SampleIndicator)
                    continue;
                holder = child;
                foreach (var circle in child.GetComponents<SpriteRenderer>())
                    circle.enabled = false;
                break;
            }

            var shown = new GameObject(RingName);
            shown.transform.SetParent(holder, false);
            var height = holder != view ? holder.position.y - 0.02f : view.position.y + Lift;
            // The circle is turned to lie on the floor; the ring is laid out on the floor already.
            shown.transform.SetPositionAndRotation(new Vector3(view.position.x, height, view.position.z),
                Quaternion.Euler(0f, view.rotation.eulerAngles.y, 0f));
            var scale = holder.lossyScale;
            shown.transform.localScale = new Vector3(1f / Safe(scale.x), 1f / Safe(scale.y), 1f / Safe(scale.z));
            Draw(shown, MeshOf(side, false));
            return new Ground { Shown = shown, Ring = shown, Side = side };
        }

        private Ground UnderStructure(Side side)
        {
            var shown = new GameObject(RingName + " Structure");
            shown.transform.SetParent(transform, false);
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(shown.transform, false);
            Draw(shadow, ShadowMesh());
            var ring = new GameObject("Ring");
            ring.transform.SetParent(shown.transform, false);
            Draw(ring, MeshOf(side, true));
            return new Ground { Shown = shown, Ring = ring, Side = side };
        }

        private void Draw(GameObject target, Mesh mesh)
        {
            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            var drawn = target.AddComponent<MeshRenderer>();
            drawn.sharedMaterial = _material;
            drawn.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            drawn.receiveShadows = false;
            drawn.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            drawn.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private Material MaterialOf()
        {
            if (_material != null)
                return _material;
            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("CHOG_RING the shader Chog/Ground is missing or not supported: no ring is drawn");
                enabled = false;
                return null;
            }
            _material = new Material(shader) { name = "ChogGround" };
            return _material;
        }

        private Mesh _shadowOnly;

        private Mesh ShadowMesh()
        {
            if (_shadowOnly == null)
                _shadowOnly = GroundMeshes.Build("ChogGroundShadow", StructureRadius, Color.clear, true, false);
            return _shadowOnly;
        }

        // A hero's mesh holds its shadow and its ring; a structure's ring is apart from its shadow,
        // so that a ruin keeps the shadow.
        private Mesh MeshOf(Side side, bool structure)
        {
            if (_meshes.TryGetValue((side, structure), out var mesh))
                return mesh;
            var colour = side == Side.Self ? Self : side == Side.Ally ? Ally : Enemy;
            mesh = GroundMeshes.Build("ChogGround" + side + (structure ? "Structure" : ""),
                structure ? StructureRadius : HeroRadius, colour, !structure, true);
            _meshes[(side, structure)] = mesh;
            return mesh;
        }

        private static float Safe(float value) => Mathf.Abs(value) < 1e-4f ? 1f : value;

        private static bool IsLocal(QuantumGame game, Frame frame, EntityRef entity)
        {
            return frame.TryGet<PlayerLink>(entity, out var link) && game.PlayerIsLocal(link.PlayerRef);
        }

        private static int LocalTeam(QuantumGame game, Frame frame)
        {
            var players = frame.Filter<PlayerLink, TeamInfo>();
            while (players.Next(out _, out var link, out var team))
                if (game.PlayerIsLocal(link.PlayerRef))
                    return team.Index;
            return -1;
        }

        private void Forget(Dictionary<EntityRef, Ground> grounds)
        {
            _gone.Clear();
            foreach (var entry in grounds)
                if (!_seen.Contains(entry.Key))
                    _gone.Add(entry.Key);
            foreach (var entity in _gone)
            {
                if (grounds[entity].Shown != null)
                    Destroy(grounds[entity].Shown);
                grounds.Remove(entity);
            }
        }

        private static void Clear(Dictionary<EntityRef, Ground> grounds)
        {
            foreach (var ground in grounds.Values)
                if (ground.Shown != null)
                    Destroy(ground.Shown);
            grounds.Clear();
        }
    }

    /// <summary>The meshes of ChogTeamRing, flat on the floor (the plane y = 0), coloured by their vertices.</summary>
    public static class GroundMeshes
    {
        private const int Segments = 40;

        // The shadow: a dark violet, densest in the middle, gone at its edge.
        private static readonly Color ShadowColour = new Color(0.07f, 0.03f, 0.14f, 0.42f);

        /// <summary>
        /// A mesh of radius "radius" (the outer edge of the ring): a soft shadow, a faint wash of the
        /// side's colour inside the ring, and the ring itself with soft edges, each as asked.
        /// </summary>
        public static Mesh Build(string name, float radius, Color colour, bool withShadow, bool withRing)
        {
            var vertices = new List<Vector3>();
            var colours = new List<Color>();
            var triangles = new List<int>();

            if (withShadow)
            {
                // Three circles: the centre, two thirds out, the edge (just inside the ring).
                Disc(vertices, colours, triangles, radius * 0.8f, ShadowColour,
                    new Color(ShadowColour.r, ShadowColour.g, ShadowColour.b, ShadowColour.a * 0.7f),
                    new Color(ShadowColour.r, ShadowColour.g, ShadowColour.b, 0f));
            }

            if (withRing)
            {
                var wash = new Color(colour.r, colour.g, colour.b, 0.14f);
                var clear = new Color(colour.r, colour.g, colour.b, 0f);
                var full = new Color(colour.r, colour.g, colour.b, 0.92f);
                var inner = radius * 0.80f;
                var feather = radius * 0.06f;
                // The wash fills the ring, fading toward the middle.
                Band(vertices, colours, triangles, radius * 0.45f, new Color(colour.r, colour.g, colour.b, 0f), inner - feather, wash);
                Band(vertices, colours, triangles, inner - feather, wash, inner, full);
                Band(vertices, colours, triangles, inner, full, radius - feather, full);
                Band(vertices, colours, triangles, radius - feather, full, radius, clear);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static void Disc(List<Vector3> vertices, List<Color> colours, List<int> triangles, float radius,
            Color centre, Color middle, Color edge)
        {
            var start = vertices.Count;
            vertices.Add(Vector3.zero);
            colours.Add(centre);
            for (var i = 0; i < Segments; i++)
            {
                vertices.Add(Point(i, radius * 0.66f));
                colours.Add(middle);
            }
            for (var i = 0; i < Segments; i++)
            {
                var a = start + 1 + i;
                var b = start + 1 + (i + 1) % Segments;
                triangles.Add(start);
                triangles.Add(b);
                triangles.Add(a);
            }
            Band(vertices, colours, triangles, radius * 0.66f, middle, radius, edge);
        }

        private static void Band(List<Vector3> vertices, List<Color> colours, List<int> triangles,
            float innerRadius, Color innerColour, float outerRadius, Color outerColour)
        {
            var start = vertices.Count;
            for (var i = 0; i < Segments; i++)
            {
                vertices.Add(Point(i, innerRadius));
                colours.Add(innerColour);
                vertices.Add(Point(i, outerRadius));
                colours.Add(outerColour);
            }
            for (var i = 0; i < Segments; i++)
            {
                var a = start + i * 2;
                var b = start + i * 2 + 1;
                var c = start + (i + 1) % Segments * 2;
                var d = start + (i + 1) % Segments * 2 + 1;
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        private static Vector3 Point(int index, float radius)
        {
            var angle = index * Mathf.PI * 2f / Segments;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }
    }
}
