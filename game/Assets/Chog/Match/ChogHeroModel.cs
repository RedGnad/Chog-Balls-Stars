using System.Collections.Generic;
using Chog.Diagnostics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The 3D hero of a Genesis: the Chog of tools/blender (head H3, its crown of leaves under the hat,
    /// the hat, the eyes, the mouth and the clothing of that Genesis), put together from the library of
    /// parts in Resources/ChogParts.bytes (tools/blender/export_chog_parts.py). Each of the four parts
    /// the game moves (Body, Head, armLeft, armRight) is one mesh with its colours in its vertices,
    /// drawn with one material shared by every hero (Resources/ChogHero.shader): four draw calls a
    /// hero. A recruit (a player without a Genesis) wears the same Chog bare, in grey.
    /// The 100 Genesis on another base (Skull, Radioactive, 1:1) are not in the library yet and keep
    /// the earlier look. On a private page, "hero3d=0" shows the earlier look for every hero.
    /// </summary>
    public static class ChogHeroModel
    {
        public const string LibraryResource = "ChogParts";
        public const string ShaderResource = "ChogHero";
        public const string QueryParameter = "hero3d";
        public const string ModelName = "ChogHeroModel";

        /// <summary>The number under which the library keeps the bare grey Chog of a recruit.</summary>
        public const int Recruit = 0;

        /// <summary>
        /// How much larger than it was made the model is drawn, over the scale of the class model it
        /// replaces: the Chog of tools/blender is 1.1 units tall with its crown; drawn so, it stands
        /// about as tall as the earlier body with the picture over it.
        /// </summary>
        public const float Scale = 2.2f;

        /// <summary>Height of the top of the crown over the feet, in units of the model.</summary>
        public const float ModelHeight = 1.15f;

        private static ChogPartsLibrary s_library;
        private static bool s_loaded;
        private static bool? s_off;
        private static Material s_material;

        /// <summary>The library, read once; null when the project has none or it cannot be read.</summary>
        public static ChogPartsLibrary Library
        {
            get
            {
                if (s_loaded)
                    return s_library;
                s_loaded = true;
                var file = Resources.Load<TextAsset>(LibraryResource);
                if (file == null)
                    return null;
                try
                {
                    s_library = ChogPartsLibrary.Read(file.bytes);
                    Debug.Log($"CHOG_HERO3D library: {s_library.Heroes.Count} heroes, {s_library.Pieces.Length} parts");
                }
                catch (System.Exception problem)
                {
                    Debug.LogWarning("CHOG_HERO3D the library of hero parts cannot be read: " + problem.Message);
                }
                Resources.UnloadAsset(file);
                return s_library;
            }
        }

        /// <summary>Whether a private test page asked for the earlier look ("hero3d=0").</summary>
        public static bool Off
        {
            get
            {
                s_off ??= LocalGenesis.TestEntriesAllowed(Application.absoluteURL, Application.isEditor)
                          && PerformanceSampling.TryGetQueryValue(Application.absoluteURL, QueryParameter, out var value)
                          && value == "0";
                return s_off.Value;
            }
        }

        /// <summary>Whether this Genesis (by the number shown in its name) has its 3D hero.</summary>
        public static bool Has(int metadataId)
        {
            return !Off && Library != null && Library.TryGetHero(metadataId, out _);
        }

        /// <summary>
        /// Puts the 3D hero of a Genesis (or the grey Chog of a recruit, with Recruit) in place of the
        /// model under "character" and returns it, moved by ChogBodyAnimator; null, with the character
        /// left as it is, when that hero is not in the library or the character has no model to replace.
        /// </summary>
        public static Transform Wear(Transform character, int metadataId)
        {
            if (character == null || !Has(metadataId))
                return null;

            foreach (var part in character.GetComponentsInChildren<Transform>(true))
                if (part.name == ModelName)
                    return part;

            // The model of the class (or the earlier Chog body): the parent of the body the animator moves.
            var old = character.GetComponentInChildren<ChogBodyAnimator>(true);
            if (old == null || old.transform.parent == null)
                return null;

            var oldModel = old.transform.parent;
            foreach (var drawn in oldModel.GetComponentsInChildren<Renderer>(true))
                drawn.enabled = false;
            Object.Destroy(old);

            var model = Build(metadataId, oldModel.parent);
            if (model == null)
                return null;
            model.localPosition = oldModel.localPosition;
            model.localRotation = oldModel.localRotation;
            model.localScale = oldModel.localScale * Scale;
            if (ChogBodyAnimator.Attach(model) == null)
                Debug.LogWarning("CHOG_HERO3D the hero has no parts named Body, armLeft and armRight: it will not move");
            return model;
        }

        /// <summary>
        /// The 3D hero of a Genesis under "parent", standing on its origin and looking along +Z, at the
        /// size it was made; null when it is not in the library.
        /// </summary>
        public static Transform Build(int metadataId, Transform parent)
        {
            var library = Library;
            if (library == null || !library.TryGetHero(metadataId, out _))
                return null;

            var root = new GameObject(ModelName).transform;
            root.SetParent(parent, false);
            var owned = root.gameObject.AddComponent<OwnedMeshes>();
            var made = new Dictionary<string, Transform>();
            // A part's parent comes before it in the library: the body first.
            foreach (var rest in library.Rest)
            {
                var part = new GameObject(rest.Name, typeof(MeshFilter), typeof(MeshRenderer));
                var holder = string.IsNullOrEmpty(rest.Parent) || !made.TryGetValue(rest.Parent, out var found) ? root : found;
                part.transform.SetParent(holder, false);
                part.transform.localPosition = new Vector3(rest.X, rest.Y, rest.Z);
                var mesh = MeshOf(library, metadataId, rest.Name);
                owned.Meshes.Add(mesh);
                part.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = part.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Material;
                renderer.receiveShadows = false;
                made[rest.Name] = part.transform;
            }

            return root;
        }

        /// <summary>The one material every hero is drawn with.</summary>
        public static Material Material
        {
            get
            {
                if (s_material != null)
                    return s_material;
                var shader = Resources.Load<Shader>(ShaderResource);
                if (shader == null || !shader.isSupported)
                {
                    Debug.LogWarning("CHOG_HERO3D the shader Chog/Hero is missing or not supported: an unlit colour is used");
                    shader = Shader.Find("Sprites/Default");
                }
                s_material = new Material(shader) { name = "ChogHero" };
                return s_material;
            }
        }

        // The mesh of one of the four parts of a hero: every piece it wears on that part, its colours
        // (skin and face its own) in the vertices. Each hero owns its meshes (OwnedMeshes).
        private static Mesh MeshOf(ChogPartsLibrary library, int metadataId, string restName)
        {
            library.TryGetHero(metadataId, out var hero);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color32>();
            var triangles = new List<int>();
            foreach (var index in hero.Pieces)
            {
                var piece = library.Pieces[index];
                if (piece.On != restName)
                    continue;
                var start = vertices.Count;
                for (var v = 0; v < piece.VertexCount; v++)
                {
                    vertices.Add(new Vector3(piece.Positions[v * 3], piece.Positions[v * 3 + 1], piece.Positions[v * 3 + 2]));
                    normals.Add(new Vector3(piece.Normals[v * 3], piece.Normals[v * 3 + 1], piece.Normals[v * 3 + 2]) / 127f);
                    library.ColourOf(piece.Materials[v], hero.Skin, out var r, out var g, out var b, out var a);
                    colours.Add(new Color(r, g, b, a));
                }
                foreach (var corner in piece.Indices)
                    triangles.Add(start + corner);
            }

            var made = new Mesh { name = $"ChogHero{metadataId}_{restName}" };
            if (vertices.Count > ushort.MaxValue)
                made.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            made.SetVertices(vertices);
            made.SetNormals(normals);
            made.SetColors(colours);
            made.SetTriangles(triangles, 0);
            made.RecalculateBounds();
            made.UploadMeshData(true);
            return made;
        }

        /// <summary>The meshes made for one hero, destroyed with it.</summary>
        private sealed class OwnedMeshes : MonoBehaviour
        {
            public readonly List<Mesh> Meshes = new List<Mesh>();

            private void OnDestroy()
            {
                foreach (var mesh in Meshes)
                    if (mesh != null)
                        Destroy(mesh);
            }
        }
    }
}
