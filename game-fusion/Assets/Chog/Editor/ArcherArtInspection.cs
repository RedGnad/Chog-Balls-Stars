using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Chog.Editor
{
    public static class ArcherArtInspection
    {
        [MenuItem("Chog/Qualification/Inspecter le modèle Archer")]
        public static void Inspect()
        {
            const string path = "Assets/Chog/SampleArt/character_barbarian.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new System.IO.FileNotFoundException(path);
            foreach (var material in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string _, out long id);
                Debug.Log($"CHOG_ARCHER_MATERIAL {material.name} {id}");
            }
            Walk(model.transform, "");
            Debug.Log("CHOG_ARCHER_INSPECTION_OK");
        }

        private static void Walk(Transform node, string prefix)
        {
            var mesh = node.GetComponent<MeshFilter>()?.sharedMesh;
            var materials = node.GetComponent<Renderer>()?.sharedMaterials;
            string ids = materials == null ? "" : string.Join(",", materials.Select(material =>
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string _, out long id);
                return material.name + ":" + id;
            }));
            Debug.Log($"CHOG_ARCHER_NODE {prefix}/{node.name} pos={node.localPosition} rot={node.localRotation} mesh={mesh?.name} submeshes={mesh?.subMeshCount} materials={ids}");
            foreach (Transform child in node) Walk(child, prefix + "/" + node.name);
        }
    }
}
