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
            // Both arenas (Gameplay, GameplaySiege) have the same materials; the menu has none of them,
            // but is left alone anyway.
            if (!ArenaScenes.IsArena(scene.name))
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
                if (material.HasProperty("_ChogFloorPattern"))
                    material.SetFloat("_ChogFloorPattern", 0f);
            }

            ChogArenaRendererReport.WriteIfAsked(scene, roots);

            // The sample floor and wall geometry remain the source of every collider and baked map entry.
            // Their renderers are replaced as complete visual systems, so a floor treatment never leaks onto
            // the boundary and a wall treatment never turns a ground mesh into a dark strip.
            ChogArenaFloorSkin.Apply(roots, shader, palette == ArenaPalettes.Night && !floorOverride ? FloorTile() : null);
            ChogArenaBoundarySkin.Apply(roots);

            // The Photon sample's cover collision stays exactly where it is. Its repeated cage mesh is
            // replaced only at runtime by three batched candy-block surfaces, built from the original
            // renderer bounds. There are no new colliders, triggers or map objects.
            ChogArenaCoverSkin.Apply(roots, shader);
            var paletteAmbient = ColourOf(colours[ArenaPalettes.Ambient]);
            ChogMatchLook.LightArena(roots, paletteAmbient, ambientGiven ? paletteAmbient : (Color?)null);
            ChogMatchPost.Attach(roots);
        }

        /// <summary>
        /// Puts a skin made for a scene into that scene. A new object goes to the active scene, which can be the arena of the
        /// match before while the next one loads: its skins then went with it when it was unloaded (ArenaScenes), and the
        /// siege showed its sample crates and floor (22 October 2026, the owner, a siege after another match).
        /// </summary>
        internal static void KeepIn(GameObject made, GameObject[] roots)
        {
            if (made != null && roots != null && roots.Length > 0 && roots[0] != null && roots[0].scene.IsValid() && made.scene != roots[0].scene)
                SceneManager.MoveGameObjectToScene(made, roots[0].scene);
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
    /// Visual-only ground treatment. The source floor still owns its map and collision. A single textured
    /// surface covers its repeated brick renderers so the painted slab albedo remains continuous at game scale.
    /// </summary>
    internal static class ChogArenaFloorSkin
    {
        private const string ObjectName = "ChogArenaFloorSkin";
        private static Material s_ground;

        public static void Apply(GameObject[] roots, Shader arenaShader, Texture2D floorTile)
        {
            if (roots == null || HasObject(roots, ObjectName))
                return;

            var floor = FindChild(roots, "Level_1", "Floor", "InteriorFloor");
            if (floor == null)
                return;

            var renderers = floor.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            foreach (var renderer in renderers)
                renderer.enabled = false;

            var skin = new GameObject(ObjectName, typeof(MeshFilter), typeof(MeshRenderer));
            ChogArenaSkin.KeepIn(skin, roots);
            skin.GetComponent<MeshFilter>().sharedMesh = FloorMesh(bounds);
            var shader = arenaShader != null && arenaShader.isSupported ? arenaShader : Shader.Find("Standard");
            if (s_ground == null)
                s_ground = new Material(shader) { name = "ChogPaintedFloor", color = Color.white };
            if (s_ground.HasProperty("_ChogFloorPattern"))
            {
                s_ground.SetFloat("_ChogFloorPattern", floorTile != null ? 1f : 0f);
                if (floorTile != null)
                    s_ground.SetTexture("_ChogFloorTex", floorTile);
            }
            else if (floorTile != null)
            {
                s_ground.mainTexture = floorTile;
                s_ground.mainTextureScale = new Vector2(0.055f, 0.055f);
            }
            skin.GetComponent<MeshRenderer>().sharedMaterial = s_ground;
        }

        private static Mesh FloorMesh(Bounds bounds)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var y = bounds.max.y + 0.012f;
            AddQuad(vertices, normals, triangles, bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, y);

            var mesh = new Mesh { name = "ChogArenaPaintedFloor" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
                                    float x0, float z0, float x1, float z1, float y)
        {
            var start = vertices.Count;
            vertices.Add(new Vector3(x0, y, z0));
            vertices.Add(new Vector3(x1, y, z0));
            vertices.Add(new Vector3(x1, y, z1));
            vertices.Add(new Vector3(x0, y, z1));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.up);
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }

        private static Transform FindChild(GameObject[] roots, params string[] path)
        {
            foreach (var root in roots)
            {
                if (root.name != path[0])
                    continue;
                var cursor = root.transform;
                for (var i = 1; cursor != null && i < path.Length; i++)
                    cursor = cursor.Find(path[i]);
                if (cursor != null)
                    return cursor;
            }
            return null;
        }

        private static bool HasObject(GameObject[] roots, string name)
        {
            foreach (var root in roots)
                if (root.name == name || root.transform.Find(name) != null)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Visual-only boundary treatment. The sample's wall mesh and its collider layout stay intact, while two
    /// dedicated materials separate the dark face from its lighter top. It deliberately avoids the arena-wide
    /// contact shade that turned the near wall into a broken horizontal band.
    /// </summary>
    internal static class ChogArenaBoundarySkin
    {
        private const string DetailName = "ChogArenaNearWallStones";
        private static Material s_face;
        private static Material s_top;
        private static Material s_grout;
        private static Material s_stone;

        public static void Apply(GameObject[] roots)
        {
            if (roots == null)
                return;
            var nearBounds = new Bounds();
            var hasNearBounds = false;
            foreach (var root in roots)
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!UnderWalls(renderer.transform))
                    continue;
                if (renderer.bounds.size.y > 1f && IsNearCenterWall(renderer.transform))
                {
                    if (hasNearBounds)
                        nearBounds.Encapsulate(renderer.bounds);
                    else
                    {
                        nearBounds = renderer.bounds;
                        hasNearBounds = true;
                    }
                }
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                    continue;
                if (s_face == null)
                    s_face = WallMaterial("ChogBoundaryWallFace", materials[0], new Color(0.20f, 0.11f, 0.29f, 1f));
                if (s_top == null)
                    s_top = WallMaterial("ChogBoundaryWallTop", materials.Length > 1 ? materials[1] : materials[0],
                        new Color(0.36f, 0.23f, 0.47f, 1f));
                var replacement = new Material[materials.Length];
                for (var i = 0; i < replacement.Length; i++)
                    replacement[i] = i == 0 ? s_face : s_top;
                renderer.sharedMaterials = replacement;
            }
            if (hasNearBounds)
                DetailNearWall(roots, nearBounds);
        }

        private static void DetailNearWall(GameObject[] roots, Bounds bounds)
        {
            foreach (var root in roots)
                if (root.name == DetailName)
                    return;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var grout = new List<int>();
            var stones = new List<int>();
            var topY = bounds.max.y + 0.014f;
            var faceZ = bounds.min.z - 0.014f;
            AddWallTop(vertices, normals, uvs, grout, bounds.min.x, bounds.max.x,
                bounds.min.z, bounds.max.z, topY, 0f);
            AddWallFront(vertices, normals, uvs, grout, bounds.min.x, bounds.max.x,
                bounds.min.y, bounds.max.y, faceZ, 0f);

            const int columns = 14;
            const int courses = 2;
            var width = bounds.size.x / columns;
            var height = bounds.size.y / courses;
            for (var column = 0; column < columns; column++)
            {
                var x0 = bounds.min.x + column * width + 0.065f;
                var x1 = bounds.min.x + (column + 1) * width - 0.065f;
                var shift = column % 3 * 0.13f;
                AddWallTop(vertices, normals, uvs, stones, x0, x1, bounds.min.z + 0.065f,
                    bounds.max.z - 0.065f, topY + 0.006f, shift);
                for (var course = 0; course < courses; course++)
                {
                    var y0 = bounds.min.y + course * height + 0.065f;
                    var y1 = bounds.min.y + (course + 1) * height - 0.065f;
                    AddWallFront(vertices, normals, uvs, stones, x0, x1, y0, y1,
                        faceZ - 0.006f, shift + course * 0.17f);
                }
            }

            var mesh = new Mesh { name = "ChogArenaNearWallStoneMesh", subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(grout, 0);
            mesh.SetTriangles(stones, 1);
            mesh.RecalculateBounds();
            var detail = new GameObject(DetailName, typeof(MeshFilter), typeof(MeshRenderer));
            ChogArenaSkin.KeepIn(detail, roots);
            detail.GetComponent<MeshFilter>().sharedMesh = mesh;
            if (s_grout == null)
            {
                s_grout = WallMaterial("ChogBoundaryGrout", s_face, new Color(0.11f, 0.055f, 0.18f, 1f));
                s_grout.mainTexture = null;
            }
            if (s_stone == null)
            {
                s_stone = WallMaterial("ChogBoundaryStone", s_top, new Color(0.73f, 0.58f, 0.79f, 1f));
                s_stone.mainTexture = Resources.Load<Texture2D>("ArenaFloorSlabs");
            }
            detail.GetComponent<MeshRenderer>().sharedMaterials = new[] { s_grout, s_stone };
        }

        private static void AddWallTop(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
                                       List<int> triangles, float x0, float x1, float z0, float z1, float y, float shift)
        {
            var start = vertices.Count;
            vertices.Add(new Vector3(x0, y, z0));
            vertices.Add(new Vector3(x1, y, z0));
            vertices.Add(new Vector3(x1, y, z1));
            vertices.Add(new Vector3(x0, y, z1));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.06f + shift, 0.08f));
            uvs.Add(new Vector2(0.86f + shift, 0.08f));
            uvs.Add(new Vector2(0.86f + shift, 0.88f));
            uvs.Add(new Vector2(0.06f + shift, 0.88f));
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }

        private static void AddWallFront(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
                                         List<int> triangles, float x0, float x1, float y0, float y1, float z, float shift)
        {
            var start = vertices.Count;
            vertices.Add(new Vector3(x0, y0, z));
            vertices.Add(new Vector3(x1, y0, z));
            vertices.Add(new Vector3(x1, y1, z));
            vertices.Add(new Vector3(x0, y1, z));
            for (var i = 0; i < 4; i++)
                normals.Add(Vector3.back);
            uvs.Add(new Vector2(0.06f + shift, 0.08f));
            uvs.Add(new Vector2(0.86f + shift, 0.08f));
            uvs.Add(new Vector2(0.86f + shift, 0.88f));
            uvs.Add(new Vector2(0.06f + shift, 0.88f));
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }

        private static Material WallMaterial(string name, Material source, Color colour)
        {
            var material = new Material(source) { name = name, color = colour, enableInstancing = true };
            material.shader = Shader.Find("Standard");
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.03f);
            return material;
        }

        private static bool UnderWalls(Transform transform)
        {
            for (var cursor = transform.parent; cursor != null; cursor = cursor.parent)
                if (cursor.name == "Walls")
                    return true;
            return false;
        }

        private static bool IsNearCenterWall(Transform transform)
        {
            for (var cursor = transform.parent; cursor != null; cursor = cursor.parent)
                if (cursor.name == "CenterWall")
                    return cursor.position.z < 0f;
            return false;
        }
    }

    /// <summary>
    /// A development-only scene report, enabled by `?arenadiag=1`, that groups the source sample renderers by
    /// hierarchy branch, mesh and material. It is captured before visual cover replacement so the presentation
    /// layer can be mapped to the actual Photon sample without touching its geometry or simulation.
    /// </summary>
    internal static class ChogArenaRendererReport
    {
        private sealed class Group
        {
            public int Count;
            public Bounds Bounds;
            public bool HasBounds;
        }

        private static bool s_written;

        public static void WriteIfAsked(Scene scene, GameObject[] roots)
        {
            if (s_written || Application.absoluteURL.IndexOf("arenadiag=1", System.StringComparison.OrdinalIgnoreCase) < 0)
                return;
            s_written = true;
            var groups = new Dictionary<string, Group>();
            foreach (var root in roots)
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : renderer.GetType().Name;
                var key = BranchOf(renderer.transform) + " | " + mesh + " | " + MaterialsOf(renderer);
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new Group();
                    groups.Add(key, group);
                }
                group.Count++;
                if (group.HasBounds)
                    group.Bounds.Encapsulate(renderer.bounds);
                else
                {
                    group.Bounds = renderer.bounds;
                    group.HasBounds = true;
                }
            }

            var keys = new List<string>(groups.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            Debug.Log("CHOG_ARENA_RENDERERS scene=" + scene.name + " groups=" + keys.Count);
            foreach (var key in keys)
            {
                var group = groups[key];
                var bounds = group.Bounds;
                Debug.Log("CHOG_ARENA_RENDERER " + key + " | count=" + group.Count + " | centre=" + Vector(bounds.center) +
                    " | size=" + Vector(bounds.size));
            }
        }

        private static string BranchOf(Transform transform)
        {
            var cursor = transform;
            while (cursor.parent != null)
            {
                if (cursor.parent.name == "Level_1")
                    return "Level_1/" + cursor.name;
                cursor = cursor.parent;
            }
            return transform.root.name;
        }

        private static string MaterialsOf(Renderer renderer)
        {
            var names = renderer.sharedMaterials;
            var text = "";
            for (var i = 0; i < names.Length; i++)
            {
                if (i > 0)
                    text += ";";
                text += names[i] == null ? "None" : ChogArenaSkin.NameOf(names[i]);
            }
            return text;
        }

        private static string Vector(Vector3 value) =>
            value.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
            value.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
            value.z.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
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
        private static Texture2D s_paint;

        public static void Apply(GameObject[] roots, Shader arenaShader)
        {
            if (roots == null || HasSkin(roots))
                return;

            var covers = new List<Bounds>();
            var originals = new List<Renderer>();
            foreach (var root in roots)
            foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsCrate(candidate.name))
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
            ChogArenaSkin.KeepIn(skin, roots);
            var mesh = CandyMesh(covers);
            skin.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = skin.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[]
            {
                MaterialOf("ChogCandyCoverOutline", arenaShader, new Color(0.15f, 0.05f, 0.24f, 1f), ref s_outline),
                MaterialOf("ChogCandyCoverSide", arenaShader, new Color(0.64f, 0.20f, 0.58f, 1f), ref s_side),
                MaterialOf("ChogCandyCoverTop", arenaShader, Color.white, ref s_top),
            };
            if (s_paint == null)
                s_paint = Resources.Load<Texture2D>("ArenaCoverPaint");
            if (s_paint != null)
                s_top.mainTexture = s_paint;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        /// <summary>
        /// A crate of a map: "Crate", or "Crate -1,4" as the siege's map names them with their cell (SiegeMapBuilder). The siege's crates
        /// lost their candy skin when its map was built again on 22 October 2026 with those names (the owner: "even in siege the crates
        /// are pink without the texture"); the coins map keeps its sample crates, named otherwise, in their own look.
        /// </summary>
        internal static bool IsCrate(string name)
        {
            if (name == "Crate")
                return true;
            if (name == null || !name.StartsWith("Crate ", System.StringComparison.Ordinal))
                return false;
            var cell = name.Substring("Crate ".Length).Split(',');
            return cell.Length == 2 && int.TryParse(cell[0], out _) && int.TryParse(cell[1], out _);
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
            saved = new Material(shader) { name = name, color = colour, enableInstancing = true };
            if (saved.HasProperty("_ChogFloorPattern"))
                saved.SetFloat("_ChogFloorPattern", 0f);
            if (saved.HasProperty("_Glossiness"))
                saved.SetFloat("_Glossiness", 0.03f);
            return saved;
        }

        private static Mesh CandyMesh(List<Bounds> covers)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var outline = new List<int>();
            var side = new List<int>();
            var top = new List<int>();
            foreach (var cover in covers)
            {
                // The dark layer is a low outline and a small frame beneath the top. It cannot be a full
                // outer cube: that would occlude the pink surfaces behind it from the match camera.
                AddBox(vertices, normals, uvs, outline, cover.center - Vector3.up * (cover.size.y * 0.46f),
                    new Vector3(cover.size.x * 1.07f, cover.size.y * 0.12f, cover.size.z * 1.07f));
                AddBox(vertices, normals, uvs, outline, cover.center + Vector3.up * (cover.size.y * 0.39f),
                    new Vector3(cover.size.x * 0.90f, cover.size.y * 0.12f, cover.size.z * 0.90f));
                AddBox(vertices, normals, uvs, side, cover.center,
                    new Vector3(cover.size.x * 0.94f, cover.size.y * 0.94f, cover.size.z * 0.94f));
                AddBox(vertices, normals, uvs, top, cover.center + Vector3.up * (cover.size.y * 0.46f),
                    new Vector3(cover.size.x * 0.78f, cover.size.y * 0.12f, cover.size.z * 0.78f));
            }

            var mesh = new Mesh { name = "ChogArenaCandyCoverMesh", subMeshCount = 3 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(outline, 0);
            mesh.SetTriangles(side, 1);
            mesh.SetTriangles(top, 2);
            mesh.RecalculateBounds();
            return mesh;
        }

        // A low-poly rounded rectangle: the chamfered corners remove the source pack's rigid cage read,
        // while flat faces keep the arena material's top and side lighting clean at phone scale.
        private static void AddBox(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
                                   List<int> triangles, Vector3 center, Vector3 size)
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
            uvs.Add(new Vector2(0.5f, 0.5f));
            var topStart = vertices.Count;
            for (var i = 0; i < outline.Length; i++)
            {
                vertices.Add(new Vector3(center.x + outline[i].x, high, center.z + outline[i].y));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f + outline[i].x / size.x, 0.5f + outline[i].y / size.z));
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
                AddFace(vertices, normals, uvs, triangles, new Vector3(center.x + a.x, low, center.z + a.y),
                    new Vector3(center.x + b.x, low, center.z + b.y), new Vector3(center.x + b.x, high, center.z + b.y),
                    new Vector3(center.x + a.x, high, center.z + a.y), normal);
            }
        }

        private static void AddFace(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles,
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
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }
    }
}
