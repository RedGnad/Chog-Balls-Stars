using System;
using System.IO;
using System.Linq;
using Chog.Arena;
using Fusion;
using Fusion.Editor;
using Fusion.Photon.Realtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Editor
{
    public static class FusionProjectSetup
    {
        public const string Boot = "Assets/Chog/Scenes/Boot.unity";
        public const string Arena = "Assets/Chog/Scenes/Arena.unity";
        [Serializable] private sealed class Settings { public string appIdFusion; }

        [MenuItem("Chog/Qualification/Préparer la scène Fusion")]
        public static void Configure()
        {
            var root = Directory.GetParent(Application.dataPath).Parent.FullName;
            var settings = JsonUtility.FromJson<Settings>(File.ReadAllText(Path.Combine(root, "config/photon-fusion.json")));
            if (!Guid.TryParse(settings.appIdFusion, out _)) throw new InvalidDataException("Invalid Fusion AppID.");
            var app = PhotonAppSettings.Global;
            app.AppSettings.AppIdFusion = settings.appIdFusion;
            app.AppSettings.AppVersion = "chog-p0-fusion-1";
            app.AppSettings.FixedRegion = "eu";
            EditorUtility.SetDirty(app);
            var config = NetworkProjectConfig.Global;
            config.AllowClientServerModesInWebGL = true;
            config.PeerMode = NetworkProjectConfig.PeerModes.Single;
            config.Simulation.PlayerCount = 6;
            NetworkProjectConfigUtilities.SaveGlobalConfig(config);
            PlayerSettings.companyName = "RedGnad";
            PlayerSettings.productName = "Chog Arena";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.runInBackground = true;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            playerSettings.FindProperty("activeInputHandler").intValue = 1;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            Directory.CreateDirectory("Assets/Chog/Scenes");
            Directory.CreateDirectory("Assets/Chog/Resources/Chog");
            AssetDatabase.Refresh();
            MakePrefabs();
            if (!File.Exists(Arena)) MakeArena();
            if (!File.Exists(Boot)) MakeBoot();
            PrepareArcherSlice();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Boot, true), new EditorBuildSettingsScene(Arena, true) };
            AssetDatabase.SaveAssets();
            NetworkProjectConfigUtilities.RebuildPrefabTable();
            EditorSceneManager.OpenScene(Boot);
            Debug.Log("CHOG_FUSION_SETUP_OK: AppID configured, Server/Client WebGL enabled, two scenes and two network prefabs prepared.");
        }

        private static void MakePrefabs()
        {
            string heroPath = "Assets/Chog/Resources/Chog/Hero.prefab";
            if (!File.Exists(heroPath))
            {
                var hero = new GameObject("Hero", typeof(NetworkObject), typeof(NetworkCharacterController), typeof(ArenaHero));
                var controller = hero.GetComponent<CharacterController>();
                controller.height = 2; controller.center = Vector3.up; controller.radius = .45f;
                var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "VisualRoot"; visual.transform.SetParent(hero.transform, false); visual.transform.localPosition = Vector3.up;
                UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                PrefabUtility.SaveAsPrefabAsset(hero, heroPath); UnityEngine.Object.DestroyImmediate(hero);
            }
            string projectilePath = "Assets/Chog/Resources/Chog/Projectile.prefab";
            if (!File.Exists(projectilePath))
            {
                var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere); projectile.name = "Projectile";
                projectile.transform.localScale = Vector3.one * .24f;
                UnityEngine.Object.DestroyImmediate(projectile.GetComponent<Collider>());
                projectile.AddComponent<NetworkObject>(); projectile.AddComponent<NetworkTransform>(); projectile.AddComponent<ArenaProjectile>();
                PrefabUtility.SaveAsPrefabAsset(projectile, projectilePath); UnityEngine.Object.DestroyImmediate(projectile);
            }
        }

        [MenuItem("Chog/Qualification/Préparer l'archer Fusion")]
        public static void PrepareArcherSlice()
        {
            const string art = "Assets/Chog/SampleArt/";
            string heroPath = "Assets/Chog/Resources/Chog/Hero.prefab";
            if (!File.Exists(heroPath)) MakePrefabs();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(art + "character_barbarian.fbx");
            var crossbow = AssetDatabase.LoadAssetAtPath<GameObject>(art + "crossbow_rare.fbx");
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(art + "Character Animator.controller");
            if (model == null || crossbow == null || controller == null)
                throw new FileNotFoundException("Archer sample art is missing. Restore the selected KayKit assets with their .meta files.");

            var root = PrefabUtility.LoadPrefabContents(heroPath);
            try
            {
                var old = root.transform.Find("VisualRoot");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var visual = new GameObject("VisualRoot").transform;
                visual.SetParent(root.transform, false);
                var gfx = new GameObject("GFX", typeof(Animator)).transform;
                gfx.SetParent(visual, false);
                gfx.GetComponent<Animator>().runtimeAnimatorController = controller;
                var animated = Child(gfx, "KayKit Animated Character");
                var body = Child(animated, "Body");
                var head = Child(body, "Head");
                var left = Child(body, "armLeft");
                var leftHand = Child(left, "handSlotLeft");
                var right = Child(body, "armRight");
                var hand = Child(right, "handSlotRight");
                // Keep the KayKit hierarchy and bind poses used by the sample animation clips.
                head.localPosition = new Vector3(0, .7115979f, 0);
                head.localRotation = new Quaternion(.044547986f, 0, 0, .9990072f);
                left.localPosition = new Vector3(-.14f, .69159794f, 0);
                left.localRotation = new Quaternion(.68586344f, -.17202237f, .6858629f, .17202252f);
                right.localPosition = new Vector3(.14f, .69159794f, 0);
                right.localRotation = new Quaternion(.70510703f, .28591824f, .6467236f, -.053135417f);
                leftHand.localPosition = new Vector3(0, .5509223f, 0);
                leftHand.localRotation = new Quaternion(.6066169f, .60661674f, -.36334014f, .36334005f);
                hand.localPosition = new Vector3(0, .55092233f, 0);
                hand.localRotation = new Quaternion(.30050635f, .37659088f, -.67987657f, .55285f);
                MeshPart(model, body, "character_barbarianBody");
                var headMesh = MeshPart(model, head, "character_barbarianHead");
                headMesh.localPosition = new Vector3(0, -.007855552f, .00070234394f);
                headMesh.localRotation = new Quaternion(-.04454767f, 0, 0, .9990073f);
                var hat = MeshPart(model, headMesh, "character_barbarianHat");
                hat.localPosition = new Vector3(0, .70082545f, 0);
                var leftMesh = MeshPart(model, left, "character_barbarianArmLeft");
                leftMesh.localPosition = new Vector3(0, .08139634f, -.02907208f);
                leftMesh.localRotation = new Quaternion(-.6858636f, .17202206f, -.6858628f, .17202275f);
                var rightMesh = MeshPart(model, right, "character_barbarianArmRight");
                rightMesh.localPosition = new Vector3(-.019413197f, .07844923f, .030649947f);
                rightMesh.localRotation = new Quaternion(-.7051073f, -.2859185f, -.64672333f, -.053135324f);
                var weapon = (GameObject)PrefabUtility.InstantiatePrefab(crossbow, hand);
                weapon.transform.localPosition = new Vector3(.021f, -.193f, .093f);
                weapon.transform.localRotation = new Quaternion(-.7937766f, -.06955674f, -.02831456f, .6035553f);

                var view = root.GetComponent<ArenaHeroView>();
                if (view == null) view = root.AddComponent<ArenaHeroView>();
                view.VolleySound = AssetDatabase.LoadAssetAtPath<AudioClip>(art + "metal_11.ogg");
                view.SpecialSound = AssetDatabase.LoadAssetAtPath<AudioClip>(art + "pot_01.ogg");
                view.ImpactSound = AssetDatabase.LoadAssetAtPath<AudioClip>(art + "explosion.ogg");
                PrefabUtility.SaveAsPrefabAsset(root, heroPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            const string zonePath = "Assets/Chog/Resources/Chog/Zone.prefab";
            if (!File.Exists(zonePath))
            {
                var zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                zone.name = "Zone";
                zone.transform.localScale = new Vector3(ArcherRules.SpecialRadius * 2, .04f, ArcherRules.SpecialRadius * 2);
                UnityEngine.Object.DestroyImmediate(zone.GetComponent<Collider>());
                zone.AddComponent<NetworkObject>();
                zone.AddComponent<NetworkTransform>();
                zone.AddComponent<ArenaZone>();
                PrefabUtility.SaveAsPrefabAsset(zone, zonePath);
                UnityEngine.Object.DestroyImmediate(zone);
            }

            var scene = EditorSceneManager.OpenScene(Arena);
            var camera = GameObject.FindWithTag("MainCamera");
            if (camera == null) throw new InvalidDataException("Arena scene has no MainCamera.");
            if (camera.GetComponent<ArenaCameraFollow>() == null) camera.AddComponent<ArenaCameraFollow>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 25, -7.5f), Quaternion.Euler(75, 0, 0));
            camera.GetComponent<Camera>().orthographic = false;
            camera.GetComponent<Camera>().fieldOfView = 35;
            StyleArena();
            EditorSceneManager.SaveScene(scene);
            NetworkProjectConfigUtilities.RebuildPrefabTable();
            EditorSceneManager.OpenScene(Boot);
            AssetDatabase.SaveAssets();
            Debug.Log("CHOG_ARCHER_PREPARED: art, Animator, camera, zone and Archer views are ready without Quantum components.");
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Transform MeshPart(GameObject model, Transform parent, string meshName)
        {
            var source = model.GetComponentsInChildren<MeshFilter>(true)
                .FirstOrDefault(filter => filter.sharedMesh != null && filter.sharedMesh.name == meshName);
            if (source == null) throw new InvalidDataException("Missing Archer mesh " + meshName);
            var child = Child(parent, meshName);
            child.gameObject.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            var renderer = child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            return child;
        }

        private static void StyleArena()
        {
            const string folder = "Assets/Chog/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Chog", "Materials");
            var floor = Material(folder + "/Floor.mat", new Color(.115f, .18f, .235f));
            var wall = Material(folder + "/Wall.mat", new Color(.075f, .105f, .145f));
            var accent = Material(folder + "/Lane.mat", new Color(.25f, .39f, .46f));
            foreach (var name in new[] { "Ground", "West", "East", "North", "South" })
            {
                var obj = GameObject.Find(name);
                if (obj != null) obj.GetComponent<Renderer>().sharedMaterial = name == "Ground" ? floor : wall;
            }
            var lane = GameObject.Find("Lane line");
            if (lane == null)
            {
                lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lane.name = "Lane line";
                lane.transform.SetPositionAndRotation(new Vector3(0, .015f, 0), Quaternion.identity);
                lane.transform.localScale = new Vector3(.08f, .015f, 16);
                UnityEngine.Object.DestroyImmediate(lane.GetComponent<Collider>());
            }
            lane.GetComponent<Renderer>().sharedMaterial = accent;
            var camera = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera != null) camera.backgroundColor = new Color(.055f, .08f, .115f);
        }

        private static Material Material(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void MakeBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Chog Arena", typeof(ArenaSession), typeof(ArenaControls), typeof(ArenaHud));
            EditorSceneManager.SaveScene(scene, Boot);
        }

        private static void MakeArena()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box("Ground", new Vector3(0, -.2f, 0), new Vector3(24, .4f, 18));
            Box("West", new Vector3(-12, 1, 0), new Vector3(.5f, 2, 18));
            Box("East", new Vector3(12, 1, 0), new Vector3(.5f, 2, 18));
            Box("North", new Vector3(0, 1, 9), new Vector3(24, 2, .5f));
            Box("South", new Vector3(0, 1, -9), new Vector3(24, 2, .5f));
            var camera = new GameObject("Camera", typeof(Camera)); camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0, 17, -12), Quaternion.Euler(55, 0, 0));
            camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().orthographicSize = 11;
            var light = new GameObject("Light", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0); light.GetComponent<Light>().intensity = 1.2f;
            EditorSceneManager.SaveScene(scene, Arena);
        }

        private static void Box(string name, Vector3 position, Vector3 scale)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = scale;
        }
    }
}
