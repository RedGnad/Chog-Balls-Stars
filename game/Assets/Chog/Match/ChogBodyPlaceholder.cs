using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// A stand-in body made of four plain parts, named and arranged the way ChogBodyAnimator
    /// expects of the Chog body: "Body", "Head", "armLeft" and "armRight", looking along +Z,
    /// feet on the ground at the origin, each arm turning about its shoulder. It only exists to
    /// try the animation and the place of the body before the real model is there.
    /// </summary>
    public static class ChogBodyPlaceholder
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        // About the height of the class models of the sample.
        private const float ShoulderHeight = 0.62f;
        private const float ShoulderHalfWidth = 0.36f;

        /// <summary>Builds the stand-in under "parent", drawn with "material" in one colour.</summary>
        public static GameObject Build(Transform parent, Material material, Color color)
        {
            var root = new GameObject("Chog Body Stand-in");
            root.transform.SetParent(parent, false);

            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            Part(PrimitiveType.Sphere, "Torso", body, new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.72f, 0.55f), material, color);

            // The head and the arms stand beside the body on purpose, as in a model exported
            // part by part: the animator puts them under the body itself.
            var head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.localPosition = new Vector3(0f, 0.98f, 0f);
            Part(PrimitiveType.Sphere, "Skull", head, Vector3.zero, Vector3.one * 0.52f, material, color);

            Arm("armLeft", root.transform, -ShoulderHalfWidth, material, color);
            Arm("armRight", root.transform, ShoulderHalfWidth, material, color);
            return root;
        }

        // The origin of an arm is its shoulder; what is drawn hangs under it.
        private static void Arm(string name, Transform root, float side, Material material, Color color)
        {
            var arm = new GameObject(name).transform;
            arm.SetParent(root, false);
            arm.localPosition = new Vector3(side, ShoulderHeight, 0f);
            Part(PrimitiveType.Capsule, "Limb", arm, new Vector3(0f, -0.2f, 0f), new Vector3(0.17f, 0.22f, 0.17f), material, color);
        }

        private static void Part(PrimitiveType shape, string name, Transform parent, Vector3 position, Vector3 scale,
                                 Material material, Color color)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;

            var drawn = part.GetComponent<MeshRenderer>();
            if (material != null)
                drawn.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor(ColorProperty, color);
            block.SetColor(BaseColorProperty, color);
            drawn.SetPropertyBlock(block);
        }
    }
}
