using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Quantum;
using Quantum.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using QuantumMap = Quantum.Map;

namespace Chog.Editor
{
    /// <summary>
    /// Builds the siege's own map from its layout (content/maps/siege-three-lanes.txt): the scene
    /// Assets/QuantumUser/Scenes/GameplaySiege.unity, a copy of the sample's Gameplay.unity whose blocks,
    /// bushes, points and cover points are those of the layout, and the Quantum map
    /// QuantumUser/Resources/SiegeMap with its navmesh for the bots. The coins keep Map1 and Gameplay.unity,
    /// which this never opens or saves.
    ///
    /// Run it from the menu (Chog > Maps > Build siege map) or with the word "siege-map" in the build request
    /// file (BuildRequestWatcher). It refuses while a loaded scene has unsaved changes, and puts the scenes
    /// that were open back afterwards. What it does, in order, and why
    /// (research/evidence/siege-map-layouts-2026-10-17/README.md, section 7):
    /// 1. creates SiegeMap.asset with Map1's physics settings, and the scene as a copy of Gameplay.unity when
    ///    they do not exist yet (a copy is not opened, so nothing is baked);
    /// 2. opens the scene and points its QuantumMapData at SiegeMap before anything else: saving the copy while
    ///    it still points at Map1 would bake the siege's layout into the coins' map (the scene save bakes);
    /// 3. replaces the sample's blocks and bushes (Tilemap_Level1, Tiles_HidingTest) with Crate and
    ///    InvisibleProp instances at the cells of the layout, placed as the sample's own instances stand;
    /// 4. moves the spawn, objective and escape points, and writes one cover point beside each block side
    ///    that faces free ground, with the cover point asset of its direction (the sample's generators
    ///    have none set, and the bots read that asset);
    /// 5. bakes the map twice: the bushes' neighbours, written by HidingPointsMapBaker during a bake,
    ///    reach the map on the second;
    /// 6. bakes the bots' navmesh ("NavMesh", the name the AI asks for) from temporary geometry: the floor,
    ///    the blocks and the two cores and towers as obstacles (the navmesh does not know the structures,
    ///    which are placed by code), with Unity's agent of radius 0.25 as Map1's; the geometry is removed after;
    /// 7. saves, registers the new assets in the Quantum database, adds the scene to the build, and writes
    ///    .artifacts/unity/siege-map/: the exported simulation assets (to play matches on the map outside
    ///    Unity), the navmesh's triangles, a summary and the simulation stamp a build would carry.
    /// The asset InvisibilityGroups, rewritten by every bake of a map and read by no simulation code, is put
    /// back as it was, so the coins' assets stay byte for byte what they were.
    /// </summary>
    public static class SiegeMapBuilder
    {
        public const string LayoutFile = "content/maps/siege-three-lanes.txt";
        public const string SourceScenePath = "Assets/QuantumUser/Scenes/Gameplay.unity";
        public const string SiegeScenePath = "Assets/QuantumUser/Scenes/GameplaySiege.unity";
        public const string SiegeMapAssetPath = "Assets/QuantumUser/Resources/SiegeMap.asset";
        public const string CoinsMapAssetPath = "Assets/QuantumUser/Resources/Map1.asset";
        private const string CratePrefab = "Assets/TwinStickShooter/Prefabs/Props/Crate.prefab";
        private const string BushPrefab = "Assets/TwinStickShooter/Prefabs/Props/InvisibleProp.prefab";
        private const string CoverPointPrefab = "Assets/QuantumUser/Resources/CoverPoint.prefab";
        private const string CoverPointAssets = "Assets/QuantumUser/Resources/CoverPointAsset_";
        private const string InvisibilityGroupsPath = "Assets/QuantumUser/Resources/InvisibilityGroups.asset";
        private const string LayoutRootName = "Layout_Siege";
        private const string NavMeshObjectName = "NavMesh";

        // The arena of the sample: 21 x 33 cells of one unit, the walls' inner faces at x +-10.525 and
        // y +-16.525; cover points only inside x +-11, y +-17 (the bounds of the sample's CoverPointsManager).
        private const int HalfWidth = 10;
        private const int HalfHeight = 16;
        private const float FloorHalfWidth = 10.5f;
        private const float FloorHalfHeight = 16.5f;

        // Where the simulation places the structures (Structures.cs) and how wide they are.
        private static readonly (Vector2 at, float radius)[] Structures =
        {
            (new Vector2(0f, -12.5f), 1.5f), (new Vector2(0f, 12.5f), 1.5f),
            (new Vector2(0.5f, -8.5f), 0.6f), (new Vector2(-0.5f, 8.5f), 0.6f),
        };

        private sealed class Layout
        {
            public readonly HashSet<Vector2Int> Blocks = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Bushes = new HashSet<Vector2Int>();
            public readonly List<(int team, Vector2 at)> Spawns = new List<(int, Vector2)>();
            public readonly List<Vector2> Objectives = new List<Vector2>();
            public readonly List<(int team, Vector2 at)> Escapes = new List<(int, Vector2)>();
        }

        [MenuItem("Chog/Maps/Build siege map")]
        private static void BuildFromMenu()
        {
            Debug.Log("CHOG siege map: " + Build());
        }

        /// <summary>Builds everything; returns a one-line summary, or throws with the reason it stopped.</summary>
        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode is active.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var open = SceneManager.GetSceneAt(i);
                if (open.isDirty)
                    throw new InvalidOperationException("The open scene " + (open.path.Length > 0 ? open.path : open.name) +
                                                        " has unsaved changes: save or discard them first.");
            }

            var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var layout = ReadLayout(Path.Combine(repository, LayoutFile));
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var groups = AssetDatabase.LoadAssetAtPath<InvisibilityGroups>(InvisibilityGroupsPath);
            var groupsBefore = CopyOf(groups.Groups);
            var autoSync = Physics.autoSyncTransforms;
            Physics.autoSyncTransforms = true;
            try
            {
                var map = EnsureMapAsset();
                EnsureScene();
                var report = BuildScene(layout, map, repository);
                AddToBuild();
                return report;
            }
            finally
            {
                groups.Groups = groupsBefore;
                Physics.autoSyncTransforms = autoSync;
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static Layout ReadLayout(string path)
        {
            var layout = new Layout();
            var rows = 0;
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                    continue;
                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "row":
                        var y = int.Parse(parts[1], CultureInfo.InvariantCulture);
                        var cells = parts[2];
                        if (cells.Length != 2 * HalfWidth + 1 || Math.Abs(y) > HalfHeight)
                            throw new InvalidDataException("Row " + y + " of " + path + " is not a row of the arena");
                        for (var i = 0; i < cells.Length; i++)
                        {
                            var cell = new Vector2Int(i - HalfWidth, y);
                            if (cells[i] == '#')
                                layout.Blocks.Add(cell);
                            else if (cells[i] == 'b')
                                layout.Bushes.Add(cell);
                            else if (cells[i] != '.')
                                throw new InvalidDataException("Unknown cell '" + cells[i] + "' in row " + y);
                        }
                        rows++;
                        break;
                    case "spawn":
                        layout.Spawns.Add((int.Parse(parts[1], CultureInfo.InvariantCulture), Point(parts[2], parts[3])));
                        break;
                    case "objective":
                        layout.Objectives.Add(Point(parts[1], parts[2]));
                        break;
                    case "escape":
                        layout.Escapes.Add((int.Parse(parts[1], CultureInfo.InvariantCulture), Point(parts[2], parts[3])));
                        break;
                    default:
                        throw new InvalidDataException("Unknown line in " + path + ": " + line);
                }
            }

            if (rows != 2 * HalfHeight + 1)
                throw new InvalidDataException(path + " has " + rows + " rows instead of " + (2 * HalfHeight + 1));
            foreach (var cell in layout.Blocks.Concat(layout.Bushes))
            {
                var turned = new Vector2Int(-cell.x, -cell.y);
                if (layout.Blocks.Contains(cell) != layout.Blocks.Contains(turned) || layout.Bushes.Contains(cell) != layout.Bushes.Contains(turned))
                    throw new InvalidDataException("The layout does not turn onto itself at " + cell);
            }
            return layout;
        }

        private static Vector2 Point(string x, string y)
        {
            return new Vector2(float.Parse(x, CultureInfo.InvariantCulture), float.Parse(y, CultureInfo.InvariantCulture));
        }

        private static QuantumMap EnsureMapAsset()
        {
            var coins = AssetDatabase.LoadAssetAtPath<QuantumMap>(CoinsMapAssetPath);
            if (coins == null)
                throw new InvalidOperationException("The coins' map is missing: " + CoinsMapAssetPath);

            var map = AssetDatabase.LoadAssetAtPath<QuantumMap>(SiegeMapAssetPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<QuantumMap>();
                // The physics settings of the sample's map: same arena, same world.
                map.WorldSize = coins.WorldSize;
                map.BucketsCount = coins.BucketsCount;
                map.BucketsSubdivisions = coins.BucketsSubdivisions;
                map.BucketingAxis = coins.BucketingAxis;
                map.SortingAxis = coins.SortingAxis;
                map.SceneMeshCellSize = coins.SceneMeshCellSize;
                map.TriangleMeshCellSize = coins.TriangleMeshCellSize;
                map.GridSizeX = coins.GridSizeX;
                map.GridSizeY = coins.GridSizeY;
                map.GridNodeSize = coins.GridNodeSize;
                AssetDatabase.CreateAsset(map, SiegeMapAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(SiegeMapAssetPath, ImportAssetOptions.ForceSynchronousImport);
                map = AssetDatabase.LoadAssetAtPath<QuantumMap>(SiegeMapAssetPath);
            }

            var guid = QuantumUnityDBUtilities.GetExpectedAssetGuid(map, out _);
            if (guid == coins.Guid || !guid.IsValid)
                throw new InvalidOperationException("The siege's map has no Quantum id of its own (" + guid + ")");
            if (!QuantumUnityDB.TryGetGlobalAssetEditorInstance(new AssetRef<QuantumMap>(guid), out QuantumMap registered) || registered == null)
            {
                QuantumUnityDBUtilities.RefreshGlobalDB(true);
                if (!QuantumUnityDB.TryGetGlobalAssetEditorInstance(new AssetRef<QuantumMap>(guid), out registered) || registered == null)
                    throw new InvalidOperationException("SiegeMap is not in the Quantum asset database yet (" + guid + "): run the builder again");
            }
            return map;
        }

        private static void EnsureScene()
        {
            if (File.Exists(Path.Combine(Application.dataPath, "..", SiegeScenePath)))
                return;
            // A copy is not opened: nothing is baked into Map1, which the copy still names until step 2.
            if (!AssetDatabase.CopyAsset(SourceScenePath, SiegeScenePath))
                throw new InvalidOperationException("Could not copy " + SourceScenePath + " to " + SiegeScenePath);
        }

        private static string BuildScene(Layout layout, QuantumMap map, string repository)
        {
            var coins = AssetDatabase.LoadAssetAtPath<QuantumMap>(CoinsMapAssetPath);
            var scene = EditorSceneManager.OpenScene(SiegeScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var mapData = roots.SelectMany(r => r.GetComponentsInChildren<QuantumMapData>(true)).Single();

            // First, before anything can save the scene: this scene is the siege's map, never Map1.
            mapData.AssetRef = new AssetRef<QuantumMap>(map.Guid);
            if (mapData.AssetRef.Id == coins.Guid)
                throw new InvalidOperationException("The siege scene still points at Map1");
            EditorUtility.SetDirty(mapData);

            foreach (var runner in roots.SelectMany(r => r.GetComponentsInChildren<QuantumRunnerLocalDebug>(true)))
            {
                // Played straight from the Editor, this scene is a siege on its own map.
                runner.RuntimeConfig.Map = new AssetRef<QuantumMap>(map.Guid);
                runner.RuntimeConfig.Mode = EMatchMode.Siege;
                EditorUtility.SetDirty(runner);
            }

            var level = FindAll(scene, "Level_1").Single();
            // The sample's tilemap on the first run, the layout this builder placed on a later one.
            var placed = FindAll(scene, "Tilemap_Level1").Concat(FindAll(scene, LayoutRootName)).ToList();
            var staticLayer = UnityEngine.LayerMask.NameToLayer("Static");
            var crate = Template(placed, collider => collider.gameObject.layer == staticLayer ? collider.transform.parent : null, "Crate");
            var bush = Template(placed, collider => collider.gameObject.CompareTag("InvisibilityProp") ? collider.transform : null, "InvisibleProp");

            foreach (var old in FindAll(scene, "Tilemap_Level1").Concat(FindAll(scene, "Tiles_HidingTest")).Concat(FindAll(scene, LayoutRootName)).ToList())
                UnityEngine.Object.DestroyImmediate(old);

            var root = new GameObject(LayoutRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetParent(level.transform, false);
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CratePrefab);
            var bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BushPrefab);
            foreach (var cell in Sorted(layout.Blocks))
                Place(cratePrefab, crate, cell, root.transform, scene, "Crate");
            foreach (var cell in Sorted(layout.Bushes))
                Place(bushPrefab, bush, cell, root.transform, scene, "InvisibleProp");

            var chest = FindAll(scene, "Chest").SingleOrDefault();
            var chestNote = "no chest";
            if (chest != null && layout.Blocks.Contains(Vector2Int.zero))
            {
                // The block of the very centre stands where the sample's decorative chest stood: a crate there
                // would swallow it. The chest goes, the block stays a crate like every other.
                chestNote = "the chest at the centre removed for the centre block (its bounds " + BoundsOf(chest) + ")";
                UnityEngine.Object.DestroyImmediate(chest);
            }

            var points = MovePoints(scene, layout);
            var cover = WriteCoverPoints(scene, layout);
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);

            // Twice: the bushes' neighbours reach the map on the second bake.
            for (var bake = 0; bake < 2; bake++)
                QuantumEditorAutoBaker.BakeMap(mapData, QuantumMapDataBakeFlags.BakeMapData | QuantumMapDataBakeFlags.GenerateAssetDB);
            var navmeshes = BakeNavMesh(mapData, layout);

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("The siege scene could not be saved");
            AssetDatabase.SaveAssets();
            QuantumUnityDBUtilities.RefreshGlobalDB(true);

            var baked = mapData.GetAsset(true);
            var counts = baked.StaticColliders2D.GroupBy(c => c.StaticData.Name).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.Key + " " + g.Count());
            var coinsColliders = coins.StaticColliders2D.Length;
            if (coinsColliders != 424)
                throw new InvalidOperationException("Map1 has " + coinsColliders + " static colliders instead of 424: it was baked again");
            if (baked.Scene != Path.GetFileNameWithoutExtension(SiegeScenePath) || !navmeshes.Any(n => n.Name == NavMeshObjectName))
                throw new InvalidOperationException("SiegeMap names the scene " + baked.Scene + " and has no navmesh named " + NavMeshObjectName);

            var output = Path.Combine(repository, ".artifacts", "unity", "siege-map");
            Directory.CreateDirectory(output);
            var database = Path.Combine(output, "quantum-db.json");
            QualificationBuild.ExportSimulationAssets(database);
            var stamp = QualificationBuild.SimulationStamp(database);
            WriteNavMesh(navmeshes.First(n => n.Name == NavMeshObjectName), Path.Combine(output, "navmesh.json"));

            var summary = "SiegeMap " + map.Guid + ", scene " + baked.Scene + "; static colliders " + baked.StaticColliders2D.Length +
                          " (" + string.Join(", ", counts) + "), map entities " + baked.MapEntities.Length + "; " + points + "; " +
                          cover + " cover points; " + chestNote + "; navmesh " + string.Join(", ", navmeshes.Select(n => n.Name + " " +
                          n.Triangles.Length + " triangles")) + "; Map1 still " + coinsColliders + " static colliders; simulation stamp " +
                          stamp + "; exported to " + database;
            File.WriteAllText(Path.Combine(output, "summary.txt"), summary + "\n", new UTF8Encoding(false));
            return summary;
        }

        // How the sample's own instances of a prefab stand in the scene: their height, rotation and scale, so
        // that the new ones look the same.
        private sealed class Pose
        {
            public float Height;
            public Quaternion Rotation = Quaternion.identity;
            public Vector3 Scale = Vector3.one;
        }

        private static Pose Template(IEnumerable<GameObject> placed, Func<QuantumStaticBoxCollider2D, Transform> rootOf, string name)
        {
            foreach (var collider in placed.SelectMany(p => p.GetComponentsInChildren<QuantumStaticBoxCollider2D>(false)))
            {
                var root = rootOf(collider);
                if (root != null)
                    return new Pose { Height = root.position.y, Rotation = root.rotation, Scale = root.lossyScale };
            }
            throw new InvalidOperationException("No " + name + " found in the scene to copy how it stands");
        }

        private static void Place(GameObject prefab, Pose pose, Vector2Int cell, Transform parent, Scene scene, string name)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name + " " + cell.x + "," + cell.y;
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(new Vector3(cell.x, pose.Height, cell.y), pose.Rotation);
            instance.transform.localScale = pose.Scale;
            // The collider decides where the block is for the simulation: put it on the cell's centre exactly.
            var collider = instance.GetComponentInChildren<QuantumStaticBoxCollider2D>(true);
            var off = collider.transform.position - instance.transform.position;
            instance.transform.position = new Vector3(cell.x - off.x, pose.Height, cell.y - off.z);
        }

        private static string MovePoints(Scene scene, Layout layout)
        {
            var roots = scene.GetRootGameObjects();
            var spawns = roots.SelectMany(r => r.GetComponentsInChildren<QPrototypeSpawnPoint>(true)).ToList();
            var objectives = roots.SelectMany(r => r.GetComponentsInChildren<QPrototypeObjectivePoint>(true)).ToList();
            var escapes = roots.SelectMany(r => r.GetComponentsInChildren<QPrototypeEscapeRoute>(true)).ToList();
            for (var team = 0; team < 2; team++)
            {
                var owned = team;
                Assign(spawns.Where(s => s.Prototype.Team == owned).Select(s => s.transform),
                    layout.Spawns.Where(s => s.team == owned).Select(s => s.at), "spawn points of team " + team);
                Assign(escapes.Where(s => s.Prototype.TeamId == owned).Select(s => s.transform),
                    layout.Escapes.Where(s => s.team == owned).Select(s => s.at), "escape points of team " + team);
            }
            Assign(objectives.Select(o => o.transform), layout.Objectives, "objective points");
            return spawns.Count + " spawn points, " + objectives.Count + " objective points, " + escapes.Count + " escape points";
        }

        // Each point keeps its own object and height; the n-th from the left (then from the bottom) takes the
        // n-th position of the layout in that same order.
        private static void Assign(IEnumerable<Transform> points, IEnumerable<Vector2> positions, string what)
        {
            var sorted = points.OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
            var targets = positions.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
            if (sorted.Count != targets.Count)
                throw new InvalidOperationException("The scene has " + sorted.Count + " " + what + ", the layout " + targets.Count);
            for (var i = 0; i < sorted.Count; i++)
                sorted[i].position = new Vector3(targets[i].x, sorted[i].position.y, targets[i].y);
        }

        // One cover point beside each side of a block that faces free ground inside the arena, as the sample's
        // generators made them (CoverPointsGenerator: north, south, west, east, the first one kept where two
        // meet), with the asset of its direction, whose normal the bots read (FindCoverSpot).
        private static int WriteCoverPoints(Scene scene, Layout layout)
        {
            var manager = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                .Single(b => b != null && b.GetType().Name == "CoverPointsManager").transform;
            for (var i = manager.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(manager.GetChild(i).gameObject);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoverPointPrefab);
            var sides = new (Vector2Int step, string asset, float yaw)[]
            {
                (Vector2Int.up, "North", 0f), (Vector2Int.down, "South", 180f), (Vector2Int.left, "West", 270f), (Vector2Int.right, "East", 90f),
            };
            var assets = sides.ToDictionary(s => s.asset, s => AssetDatabase.LoadAssetAtPath<Quantum.CoverPoint>(CoverPointAssets + s.asset + ".asset"));
            if (assets.Values.Any(a => a == null))
                throw new InvalidOperationException("A cover point asset is missing under " + CoverPointAssets);

            var used = new HashSet<Vector2Int>();
            foreach (var block in Sorted(layout.Blocks))
            foreach (var side in sides)
            {
                var at = block + side.step;
                if (Math.Abs(at.x) > HalfWidth || Math.Abs(at.y) > HalfHeight || layout.Blocks.Contains(at) || !used.Add(at))
                    continue;
                var point = UnityEngine.Object.Instantiate(prefab);
                point.name = prefab.name + "(Clone)";
                SceneManager.MoveGameObjectToScene(point, scene);
                point.transform.SetParent(manager, false);
                point.transform.SetPositionAndRotation(new Vector3(at.x, 0f, at.y), Quaternion.Euler(0f, side.yaw, 0f));
                point.GetComponent<QuantumStaticCircleCollider2D>().Settings.Asset = assets[side.asset];
            }
            return used.Count;
        }

        private static List<Quantum.NavMesh> BakeNavMesh(QuantumMapData mapData, Layout layout)
        {
            var holder = mapData.GetComponentsInChildren<QuantumMapNavMeshUnity>(true).Single(n => n.name == NavMeshObjectName);
            var surfaceObject = new GameObject("SiegeNavMeshGeometry (temporary)");
            SceneManager.MoveGameObjectToScene(surfaceObject, mapData.gameObject.scene);
            surfaceObject.transform.SetParent(holder.transform, false);
            try
            {
                var surface = surfaceObject.AddComponent<NavMeshSurface>();
                surface.agentTypeID = 0; // Unity's Humanoid agent of this project: radius 0.25, as Map1's navmesh.
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = ~0;
                surface.defaultArea = 0;

                var floor = new GameObject("Floor");
                floor.transform.SetParent(surfaceObject.transform, false);
                var ground = floor.AddComponent<BoxCollider>();
                ground.center = new Vector3(0f, -0.5f, 0f);
                ground.size = new Vector3(2f * FloorHalfWidth, 1f, 2f * FloorHalfHeight);

                // Everything that stops a hero: not walkable, so the navmesh keeps away from it by the agent's radius.
                var obstacles = new GameObject("Obstacles");
                obstacles.transform.SetParent(surfaceObject.transform, false);
                var modifier = obstacles.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = 1; // Not Walkable
                modifier.applyToChildren = true;
                foreach (var cell in Sorted(layout.Blocks))
                {
                    var box = new GameObject("Block").AddComponent<BoxCollider>();
                    box.transform.SetParent(obstacles.transform, false);
                    box.transform.position = new Vector3(cell.x, 1f, cell.y);
                    box.size = new Vector3(1f, 2f, 1f);
                }
                foreach (var (at, radius) in Structures)
                {
                    var pillar = new GameObject("Structure").AddComponent<CapsuleCollider>();
                    pillar.transform.SetParent(obstacles.transform, false);
                    pillar.transform.position = new Vector3(at.x, 1.5f, at.y);
                    pillar.direction = 1;
                    pillar.radius = radius;
                    pillar.height = 3f;
                }

                holder.NavMeshSurfaces = new[] { surfaceObject };
                Physics.SyncTransforms();
                QuantumMapNavMeshUnityEditor.BakeUnityNavmesh(holder.gameObject);
                QuantumMapNavMeshUnityEditor.UpdateDefaultMinAgentRadius();
                var navmeshes = QuantumMapDataBaker.BakeNavMeshes(mapData, true).ToList();
                QuantumMapNavMeshUnityEditor.ClearUnityNavmesh(holder.gameObject);
                EditorUtility.SetDirty(mapData);
                EditorUtility.SetDirty(mapData.GetAsset(true));
                return navmeshes;
            }
            finally
            {
                // Nothing of it stays: a later bake from Quantum's menu must not find a surface to import.
                holder.NavMeshSurfaces = new GameObject[0];
                UnityEngine.Object.DestroyImmediate(surfaceObject);
                EditorUtility.SetDirty(holder);
            }
        }

        private static void WriteNavMesh(Quantum.NavMesh navmesh, string path)
        {
            var text = new StringBuilder();
            text.Append("{\"name\":\"").Append(navmesh.Name).Append("\",\"vertices\":[");
            for (var i = 0; i < navmesh.Vertices.Length; i++)
            {
                var point = navmesh.Vertices[i].Point;
                text.Append(i == 0 ? "" : ",").Append('[').Append(point.X.AsFloat.ToString("0.###", CultureInfo.InvariantCulture))
                    .Append(',').Append(point.Z.AsFloat.ToString("0.###", CultureInfo.InvariantCulture)).Append(']');
            }
            text.Append("],\"triangles\":[");
            for (var i = 0; i < navmesh.Triangles.Length; i++)
            {
                var triangle = navmesh.Triangles[i];
                text.Append(i == 0 ? "" : ",").Append('[').Append(triangle.Vertex0).Append(',').Append(triangle.Vertex1).Append(',')
                    .Append(triangle.Vertex2).Append(']');
            }
            text.Append("]}\n");
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        private static void AddToBuild()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == SiegeScenePath && s.enabled))
                return;
            scenes.RemoveAll(s => s.path == SiegeScenePath);
            var after = scenes.FindIndex(s => s.path == SourceScenePath);
            scenes.Insert(after < 0 ? scenes.Count : after + 1, new EditorBuildSettingsScene(SiegeScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static List<Group> CopyOf(List<Group> groups)
        {
            return groups?.Select(g => new Group { StaticCollidersIds = g.StaticCollidersIds == null ? null : new List<int>(g.StaticCollidersIds) }).ToList();
        }

        private static IEnumerable<Vector2Int> Sorted(IEnumerable<Vector2Int> cells)
        {
            return cells.OrderBy(c => c.y).ThenBy(c => c.x);
        }

        private static IEnumerable<GameObject> FindAll(Scene scene, string name)
        {
            return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == name).Select(t => t.gameObject);
        }

        private static string BoundsOf(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return "none";
            var bounds = renderers[0].bounds;
            foreach (var drawn in renderers)
                bounds.Encapsulate(drawn.bounds);
            return bounds.size.ToString("0.00");
        }
    }
}
