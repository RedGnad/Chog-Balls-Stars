using Chog.Diagnostics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The place of the Chog body. When the project has a model saved as "ChogBody" in a
    /// Resources folder, every Genesis hero wears it instead of the model of its class, moved by
    /// ChogBodyAnimator and dressed by GenesisAppearance (the real image as its head, the Skin
    /// colour on its parts). The model is made of parts named Body, Head, armLeft and armRight,
    /// looks along +Z, stands on the origin, and each arm has its origin at the shoulder. Until
    /// the model exists nothing changes, except with the test option "body=standin" on a private
    /// page, which puts a stand-in of four plain parts in that place to try it.
    /// </summary>
    public static class ChogBody
    {
        public const string Resource = "ChogBody";
        public const string QueryParameter = "body";
        private const string StandInValue = "standin";
        private const string ModelName = "ChogBodyModel";

        // What a Chog wears whatever its Skin: on the official character the mittens and the feet
        // are pink, and the line drawn around each part is nearly black.
        public static readonly Color GloveColour = new Color(0.96f, 0.36f, 0.62f);
        public static readonly Color OutlineColour = new Color(0.07f, 0.04f, 0.10f);

        private static GameObject s_model;
        private static bool s_looked;

        /// <summary>
        /// The colour of one material of the body model, told by its name as the model is made
        /// (tools/blender): "ChogGlove" is the hands and the feet, "ChogOutline" the line around
        /// the parts; anything else, the skin itself included, takes the colour of the Skin.
        /// </summary>
        public static Color PartColour(string materialName, Color skin)
        {
            var name = materialName ?? "";
            if (name.StartsWith("ChogGlove", System.StringComparison.Ordinal))
                return GloveColour;
            if (name.StartsWith("ChogOutline", System.StringComparison.Ordinal))
                return OutlineColour;
            return skin;
        }

        /// <summary>Whether the page address asks for the stand-in body, where test entries are honoured.</summary>
        public static bool StandInAsked(string url, bool isEditor)
        {
            return LocalGenesis.TestEntriesAllowed(url, isEditor)
                && PerformanceSampling.TryGetQueryValue(url, QueryParameter, out var value) && value == StandInValue;
        }

        /// <summary>
        /// Puts the Chog body in place of the class model under "character" and returns it; null
        /// when there is no Chog body to wear, and the character is left as it is.
        /// </summary>
        public static Transform Wear(Transform character)
        {
            var worn = FindWorn(character);
            if (worn != null)
                return worn;

            if (!s_looked)
            {
                s_looked = true;
                s_model = Resources.Load<GameObject>(Resource);
            }

            var standIn = s_model == null && StandInAsked(Application.absoluteURL, Application.isEditor);
            if (s_model == null && !standIn)
                return null;

            // The class model: the parent of the body the animator moves. Without one there is
            // no known place to put the Chog body in.
            var old = character.GetComponentInChildren<ChogBodyAnimator>(true);
            if (old == null || old.transform.parent == null)
                return null;

            var oldModel = old.transform.parent;
            Material material = null;
            foreach (var drawn in oldModel.GetComponentsInChildren<Renderer>(true))
            {
                if (material == null)
                    material = drawn.sharedMaterial;
                drawn.enabled = false;
            }

            Object.Destroy(old);

            var model = standIn
                ? ChogBodyPlaceholder.Build(oldModel.parent, material, Color.white)
                : Object.Instantiate(s_model, oldModel.parent);
            model.name = ModelName;
            model.transform.localPosition = oldModel.localPosition;
            model.transform.localRotation = oldModel.localRotation;
            model.transform.localScale = Vector3.Scale(model.transform.localScale, oldModel.localScale);
            if (ChogBodyAnimator.Attach(model.transform) == null)
                Debug.LogWarning("CHOG_BODY the Chog body has no parts named Body, armLeft and armRight: it will not move");
            return model.transform;
        }

        private static Transform FindWorn(Transform character)
        {
            foreach (var part in character.GetComponentsInChildren<Transform>(true))
                if (part.name == ModelName)
                    return part;
            return null;
        }
    }
}
