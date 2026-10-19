using System.Collections.Generic;
using Chog.Match;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Screens
{
    /// <summary>
    /// Paints and lights the arena in the colours of the game. The sample's dungeon (planks, stone
    /// walls, crates, the plants heroes hide in, a chest) is made of a few shared materials of one
    /// flat colour each, named by the pack they come from; each takes a colour of a palette of
    /// Chog.Match.ArenaPalettes ("Chog night" by default, "Chog day" to compare) and, since
    /// 17 October 2026, the shader Chog/Arena (a diffuse matcap, a soft violet shadow), with the
    /// light of the match set by Chog.Match.ChogMatchLook. The shapes, the colliders and the
    /// simulation are the sample's.
    /// A page option tries other colours without a build: "arena=day", or
    /// "arena=floor:2b2257,plant:34c38f" (a role or a material name, then a colour in hex) laid over
    /// the palette named first ("arena=day,floor:6fa03c"). "?matchlook=0" shows the look of before.
    /// </summary>
    public static class ChogArenaSkin
    {
        /// <summary>The scene of the arena; the menu has none of these materials, but is left alone anyway.</summary>
        public const string ArenaScene = "Gameplay";

        public const string ArenaShaderResource = "ChogArena";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Paint(scene);
            Paint(SceneManager.GetActiveScene());
        }

        private static void Paint(Scene scene)
        {
            if (!scene.IsValid())
                return;
            if (scene.name != ArenaScene)
            {
                ChogMatchLook.LeaveArena();
                return;
            }

            // The rings under the heroes take the colours of the screens' roles.
            ChogTeamRing.Self = ChogTheme.Confirm;
            ChogTeamRing.Ally = ChogTheme.Ally;
            ChogTeamRing.Enemy = ChogTheme.Enemy;

            var option = ArenaPalettes.OptionOf(Application.absoluteURL);
            var palette = ArenaPalettes.NameOf(option);
            var floorOverride = option.IndexOf("floor:", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                option.IndexOf("WoodDark:", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var colours = ArenaPalettes.Resolve(option, out var ambientGiven);
            ChogMatchLook.TuneArena(ArenaPalettes.TuningOf(palette));
            var shader = ChogMatchLook.Has(MatchLookParts.Part.Shade) ? ArenaShader() : null;
            var roots = scene.GetRootGameObjects();
            var painted = new HashSet<Material>();
            foreach (var root in roots)
            foreach (var drawn in root.GetComponentsInChildren<Renderer>(true))
            foreach (var material in drawn.sharedMaterials)
            {
                if (material == null || !painted.Add(material) || !material.HasProperty("_Color"))
                    continue;
                var materialName = NameOf(material);
                var named = colours.TryGetValue(materialName, out var hex);
                if (named)
                    material.color = ColourOf(hex);
                // Every opaque material of the arena's own shapes drawn with Unity's standard shader takes
                // the matcap, painted or not, so that nothing in the arena keeps the light of before.
                if (shader != null && material.shader != null && material.shader.name == "Standard" && drawn is MeshRenderer
                    && material.renderQueue <= (int)UnityEngine.Rendering.RenderQueue.GeometryLast)
                    material.shader = shader;
                if (materialName == "WoodDark" && material.HasProperty("_ChogFloorPattern"))
                {
                    var floorTile = palette == ArenaPalettes.Night && !floorOverride ? FloorTile() : null;
                    material.SetFloat("_ChogFloorPattern", floorTile != null ? 1f : 0f);
                    if (floorTile != null)
                    {
                        material.SetTexture("_ChogFloorTex", floorTile);
                        material.color = Color.white;
                    }
                }
            }

            // The Photon sample's cover collision stays exactly where it is. Its repeated cage mesh is
            // replaced only at runtime by three batched candy-block surfaces, built from the original
            // renderer bounds. There are no new colliders, triggers or map objects.
            ChogArenaCoverSkin.Apply(roots, shader);
            var paletteAmbient = ColourOf(colours[ArenaPalettes.Ambient]);
            ChogMatchLook.LightArena(roots, paletteAmbient, ambientGiven ? paletteAmbient : (Color?)null);
            ChogMatchPost.Attach(roots);
        }

        private static Shader s_arenaShader;
        private static Texture2D s_floorTile;

        // The sample floor has no usable surface UVs. The arena shader projects the approved-direction
        // albedo in world space; no map geometry or collider is changed.
        private static Texture2D FloorTile()
        {
            if (s_floorTile == null)
                s_floorTile = Resources.Load<Texture2D>("ArenaFloorSlabs");
            return s_floorTile;
        }

        private static Shader ArenaShader()
        {
            if (s_arenaShader != null)
                return s_arenaShader;
            var shader = Resources.Load<Shader>(ArenaShaderResource);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("CHOG_ARENA the shader Chog/Arena is missing or not supported: the arena keeps Unity's standard shader");
                return null;
            }
            s_arenaShader = shader;
            return shader;
        }

        private static Color ColourOf(string hex)
        {
            var (r, g, b) = ArenaPalettes.Rgb(hex);
            return new Color(r, g, b);
        }

        /// <summary>The name a material has in the pack: a copy made at run time ends in " (Instance)".</summary>
        internal static string NameOf(Material material)
        {
            var name = material.name;
            const string copy = " (Instance)";
            while (name.EndsWith(copy, System.StringComparison.Ordinal))
                name = name.Substring(0, name.Length - copy.Length);
            return name;
        }
    }

    /// <summary>
    /// Visual-only cover treatment for the sample Crate prefabs. The 86 Quantum blockers remain the
    /// source of position and collision. One batched mesh replaces their visible dungeon cages with a
    /// dark outline, berry sides and a pink inset top, following the map direction without changing play.
    /// </summary>
    internal static class ChogArenaCoverSkin
    {
        private const string ObjectName = "ChogArenaCandyCoverSkin";
        private static Material s_outline;
        private static Material s_side;
        private static Material s_top;

        public static void Apply(GameObject[] roots, Shader arenaShader)
        {
            if (roots == null || HasSkin(roots))
                return;

            var covers = new List<Bounds>();
            var originals = new List<Renderer>();
            foreach (var root in roots)
            foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != "Crate")
                    continue;
                var renderers = candidate.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    continue;
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                if (bounds.size.x < 0.1f || bounds.size.z < 0.1f)
                    continue;
                covers.Add(bounds);
                originals.AddRange(renderers);
            }
            if (covers.Count == 0)
                return;

            foreach (var original in originals)
                original.enabled = false;

            var skin = new GameObject(ObjectName, typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = CandyMesh(covers);
            skin.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = skin.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[]
            {
                MaterialOf("ChogCandyCoverOutline", arenaShader, new Color(0.15f, 0.05f, 0.24f, 1f), ref s_outline),
                MaterialOf("ChogCandyCoverSide", arenaShader, new Color(0.64f, 0.20f, 0.58f, 1f), ref s_side),
                MaterialOf("ChogCandyCoverTop", arenaShader, new Color(0.96f, 0.54f, 0.85f, 1f), ref s_top),
            };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static bool HasSkin(GameObject[] roots)
        {
            foreach (var root in roots)
                if (root.name == ObjectName || root.transform.Find(ObjectName) != null)
                    return true;
            return false;
        }

        private static Material MaterialOf(string name, Shader arenaShader, Color colour, ref Material saved)
        {
            if (saved != null)
                return saved;
            var shader = arenaShader != null && arenaShader.isSupported ? arenaShader : Shader.Find("Standard");
            saved = new Material(shader) { name = name, color = colour };
            if (saved.HasProperty("_ChogFloorPattern"))
                saved.SetFloat("_ChogFloorPattern", 0f);
            return saved;
        }

        private static Mesh CandyMesh(List<Bounds> covers)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var outline = new List<int>();
            var side = new List<int>();
            var top = new List<int>();
            foreach (var cover in covers)
            {
                // The dark layer is a low outline and a small frame beneath the top. It cannot be a full
                // outer cube: that would occlude the pink surfaces behind it from the match camera.
                AddBox(vertices, normals, outline, cover.center - Vector3.up * (cover.size.y * 0.46f),
                    new Vector3(cover.size.x * 1.07f, cover.size.y * 0.12f, cover.size.z * 1.07f));
                AddBox(vertices, normals, outline, cover.center + Vector3.up * (cover.size.y * 0.39f),
                    new Vector3(cover.size.x * 0.90f, cover.size.y * 0.12f, cover.size.z * 0.90f));
                AddBox(vertices, normals, side, cover.center,
                    new Vector3(cover.size.x * 0.94f, cover.size.y * 0.94f, cover.size.z * 0.94f));
                AddBox(vertices, normals, top, cover.center + Vector3.up * (cover.size.y * 0.46f),
                    new Vector3(cover.size.x * 0.78f, cover.size.y * 0.12f, cover.size.z * 0.78f));
            }

            var mesh = new Mesh { name = "ChogArenaCandyCoverMesh", subMeshCount = 3 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(outline, 0);
            mesh.SetTriangles(side, 1);
            mesh.SetTriangles(top, 2);
            mesh.RecalculateBounds();
            return mesh;
        }

        // A low-poly rounded rectangle: the chamfered corners remove the source pack's rigid cage read,
        // while flat faces keep the arena material's top and side lighting clean at phone scale.
        private static void AddBox(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 center, Vector3 size)
        {
            var half = size * 0.5f;
            var corner = Mathf.Min(half.x, half.z) * 0.22f;
            var outline = new[]
            {
                new Vector2(-half.x + corner, half.z), new Vector2(half.x - corner, half.z),
                new Vector2(half.x, half.z - corner), new Vector2(half.x, -half.z + corner),
                new Vector2(half.x - corner, -half.z), new Vector2(-half.x + corner, -half.z),
                new Vector2(-half.x, -half.z + corner), new Vector2(-half.x, half.z - corner),
            };
            var low = center.y - half.y;
            var high = center.y + half.y;
            var topCenter = vertices.Count;
            vertices.Add(new Vector3(center.x, high, center.z));
            normals.Add(Vector3.up);
            var topStart = vertices.Count;
            for (var i = 0; i < outline.Length; i++)
            {
                vertices.Add(new Vector3(center.x + outline[i].x, high, center.z + outline[i].y));
                normals.Add(Vector3.up);
            }
            for (var i = 0; i < outline.Length; i++)
            {
                triangles.Add(topCenter);
                triangles.Add(topStart + i);
                triangles.Add(topStart + (i + 1) % outline.Length);
            }
            for (var i = 0; i < outline.Length; i++)
            {
                var next = (i + 1) % outline.Length;
                var a = outline[i];
                var b = outline[next];
                var normal = new Vector3(a.x + b.x, 0f, a.y + b.y).normalized;
                AddFace(vertices, normals, triangles, new Vector3(center.x + a.x, low, center.z + a.y),
                    new Vector3(center.x + b.x, low, center.z + b.y), new Vector3(center.x + b.x, high, center.z + b.y),
                    new Vector3(center.x + a.x, high, center.z + a.y), normal);
            }
        }

        private static void AddFace(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
                                    Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            var first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }
    }
}
