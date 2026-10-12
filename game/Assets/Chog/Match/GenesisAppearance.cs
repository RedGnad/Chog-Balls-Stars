using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>How a Genesis hero is laid out for the camera, in world units.</summary>
    public struct GenesisLook
    {
        /// <summary>Diameter of the image that is the head.</summary>
        public float FaceSize;
        /// <summary>How far up the screen the image is raised from the head of the body.</summary>
        public float FaceLift;
        /// <summary>How much larger than the model was made the body is drawn.</summary>
        public float BodyScale;

        /// <summary>
        /// Test option "look=face size,face lift,body scale" (for example "look=1.1,0.3,1.4"),
        /// honoured where test entries are: it tries a layout without a new build.
        /// </summary>
        public const string QueryParameter = "look";

        // Chosen on 10 October 2026 from headless captures of six layouts: the image stays large
        // enough to tell one Genesis from another, and the whole body shows under it.
        public static readonly GenesisLook Default = new GenesisLook { FaceSize = 1.2f, FaceLift = 0.55f, BodyScale = 1.55f };

        /// <summary>The layout a page address asks for where test entries are honoured, else the default one.</summary>
        public static GenesisLook For(string url, bool isEditor)
        {
            return LocalGenesis.TestEntriesAllowed(url, isEditor)
                   && Chog.Diagnostics.PerformanceSampling.TryGetQueryValue(url, QueryParameter, out var value)
                   && TryParse(value, out var look)
                ? look
                : Default;
        }

        /// <summary>
        /// Reads "face size,face lift,body scale": three numbers with a point as the decimal
        /// mark, the sizes within what can be drawn (0.3 to 3) and the lift from -2 to 2.
        /// </summary>
        public static bool TryParse(string value, out GenesisLook look)
        {
            look = Default;
            var parts = (value ?? string.Empty).Split(',');
            if (parts.Length != 3)
                return false;

            var numbers = new float[3];
            for (var index = 0; index < 3; index++)
                if (!float.TryParse(parts[index], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out numbers[index]))
                    return false;

            if (numbers[0] < 0.3f || numbers[0] > 3f || numbers[1] < -2f || numbers[1] > 2f || numbers[2] < 0.3f || numbers[2] > 3f)
                return false;

            look = new GenesisLook { FaceSize = numbers[0], FaceLift = numbers[1], BodyScale = numbers[2] };
            return true;
        }
    }

    /// <summary>
    /// Makes a hero look like its Genesis with what every token already has: its real image
    /// becomes the head of the character, always turned towards the camera, and its body takes
    /// the colour of its Skin trait. The camera looks almost straight down, so the image is kept
    /// to the size of a head and the body is drawn larger: shoulders, arms and weapon show around
    /// the image, and what the body does can be read. It works on any character whose model has
    /// a part named "Head", so the sample bodies and a future Chog body are dressed the same way.
    /// </summary>
    public sealed class GenesisAppearance : MonoBehaviour
    {
        private const string FaceName = "GenesisFace";
        private const string HeadName = "Head";

        // How far the face is pulled towards the camera so that shoulders and arms never cut into it.
        private const float TowardsCamera = 0.9f;
        // Half the width of the head of the sample's models, which their name and bars were placed above.
        private const float SampleHeadRadius = 0.5f;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static Mesh s_quad;
        private static GenesisLook? s_look;

        private Transform _camera;
        private float _lift;

        /// <summary>The layout in use: the default one, or the one a test page asks for.</summary>
        public static GenesisLook Look
        {
            get
            {
                s_look ??= GenesisLook.For(Application.absoluteURL, Application.isEditor);
                return s_look.Value;
            }
        }

        /// <summary>
        /// How far the name and the bars of a Genesis hero move up so that they stay above its
        /// image, in units of a canvas of that height: the image stands higher on the screen
        /// than the head of the model they were placed above.
        /// </summary>
        public static float LabelRaise(Camera camera, Vector3 at, float canvasHeight)
        {
            if (camera == null || !GenesisEffects.Enabled)
                return 0f;

            var look = Look;
            var higher = Mathf.Max(0f, look.FaceLift + look.FaceSize * 0.5f - SampleHeadRadius);
            var raised = camera.WorldToViewportPoint(at + camera.transform.up * higher).y - camera.WorldToViewportPoint(at).y;
            return raised * canvasHeight;
        }

        /// <summary>
        /// Dresses the character under "character" as that Genesis. Calling it again for the same
        /// Genesis does nothing.
        /// </summary>
        public static void Apply(Transform character, GenesisRoster roster, GenesisRosterEntry hero)
        {
            if (character == null || roster == null || !GenesisEffects.Enabled)
                return;

            // The Chog body, when the project has one, takes the place of the class model first:
            // the image and the colour then dress that body.
            var body = ChogBody.Wear(character);
            var head = FindPart(body != null ? body : character, HeadName);
            var anchor = head != null ? head : character;
            if (anchor.Find(FaceName) != null)
                return;

            var model = ModelOf(body, character);
            TintBody(model, roster, hero);
            if (roster.FaceMaterial == null)
                return;

            var look = Look;
            if (model != null)
                model.localScale *= look.BodyScale;

            var face = new GameObject(FaceName, typeof(MeshFilter), typeof(MeshRenderer), typeof(GenesisAppearance));
            face.GetComponent<GenesisAppearance>()._lift = look.FaceLift;
            face.transform.SetParent(anchor, false);
            face.transform.position = anchor.position;
            var scale = anchor.lossyScale.x == 0f ? 1f : anchor.lossyScale.x;
            face.transform.localScale = Vector3.one * (look.FaceSize / scale);
            face.GetComponent<MeshFilter>().sharedMesh = Quad();

            var renderer = face.GetComponent<MeshRenderer>();
            renderer.material = new Material(roster.FaceMaterial);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;

            GenesisArt.Request(hero.MetadataId, texture =>
            {
                // The character may be gone by the time the image arrives.
                if (renderer == null)
                    return;

                renderer.material.mainTexture = texture;
                renderer.enabled = true;

                // The image is the head now: the model's own head, hair and helmet step aside.
                if (head != null)
                    foreach (var part in head.GetComponentsInChildren<Renderer>(true))
                        if (part != renderer)
                            part.enabled = false;
            });
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                var main = Camera.main;
                if (main == null)
                    return;
                _camera = main.transform;
            }

            // A flat image only reads when it faces the viewer, wherever the hero turns. It is
            // raised up the screen, not up in the world: seen from above that is what leaves
            // the shoulders in view.
            transform.rotation = _camera.rotation;
            transform.position = transform.parent.position + _camera.up * _lift - _camera.forward * TowardsCamera;
        }

        // The model of a hero: what holds the body that is moved by code, the Chog body when
        // one is worn, else the class model of the sample. Null when the character has no such
        // body. Never the character itself: what else hangs under it (its rings, its bars, the
        // capsule shown around a hero that cannot be hurt) is neither coloured nor enlarged.
        private static Transform ModelOf(Transform chogBody, Transform character)
        {
            if (chogBody != null)
                return chogBody;

            var moved = character.GetComponentInChildren<ChogBodyAnimator>(true);
            if (moved == null)
                return null;

            return moved.transform.parent == character ? moved.transform : moved.transform.parent;
        }

        private void OnDestroy()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
                Destroy(renderer.material);
        }

        // The whole model takes the colour of the Skin trait (its own colour for a 1:1), through a
        // property block so that no material is copied; without a known colour the model stays as it is.
        // Only the model: on 10 October the user saw "a big capsule" on heroes, the sample's
        // see-through sign of a hero that cannot be hurt, painted solid by this colour.
        private static void TintBody(Transform model, GenesisRoster roster, GenesisRosterEntry hero)
        {
            roster.LookOf(hero, out _, out var skin);
            if (model == null || skin.a <= 0f)
                return;

            var block = new MaterialPropertyBlock();
            foreach (var part in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                // One colour for each material of a part: the skin, and on the Chog body the
                // pink of the mittens and the dark line around them (ChogBody.PartColour).
                var materials = part.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var colour = ChogBody.PartColour(materials[i] != null ? materials[i].name : "", skin);
                    part.GetPropertyBlock(block, i);
                    // The two names a material's main colour goes by, so any body takes the tint.
                    block.SetColor(ColorProperty, colour);
                    block.SetColor(BaseColorProperty, colour);
                    part.SetPropertyBlock(block, i);
                }
            }
        }

        // The sample names its parts after the model ("character_knightHead"); a part simply
        // named "Head" matches too.
        private static Transform FindPart(Transform root, string name)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
                if (part.name.EndsWith(name, System.StringComparison.OrdinalIgnoreCase))
                    return part;
            return null;
        }

        private static Mesh Quad()
        {
            if (s_quad != null)
                return s_quad;

            s_quad = new Mesh { name = "GenesisFaceQuad" };
            s_quad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            s_quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            s_quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            s_quad.RecalculateBounds();
            return s_quad;
        }
    }
}
