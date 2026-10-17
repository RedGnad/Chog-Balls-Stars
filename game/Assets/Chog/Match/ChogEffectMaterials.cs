using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chog.Match
{
    /// <summary>
    /// The materials the 3D specials are drawn with (GenesisSpecial3D), made once from
    /// Resources/ChogEffect.shader: "Solid", opaque and written to depth as the heroes are (eyeballs,
    /// the tear, the laser's tubes, with their ink outline); "Glow", see-through and drawn after the
    /// opaque scene (the fan of light, the puddle, the front of a stare, the glow at the eyes), faded
    /// one renderer at a time; and "Floor", what lies on the arena's floor under them (Chog/Ground, as the
    /// shadows and rings of ChogTeamRing). When the shader is missing or the device cannot draw it, the
    /// specials keep their flat look (GenesisShape, GenesisBeam); on a private test page "special3d=0"
    /// asks for that flat look, to compare the two on one build.
    /// </summary>
    public static class ChogEffectMaterials
    {
        public const string ShaderResource = "ChogEffect";
        public const string QueryParameter = "special3d";

        private static readonly int FadeId = Shader.PropertyToID("_Fade");
        private static readonly int PictureId = Shader.PropertyToID("_Picture");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int CullId = Shader.PropertyToID("_Cull");

        private static Material s_solid;
        private static Material s_glow;
        private static Material s_floor;
        private static Material s_light;
        private static bool s_tried;
        private static bool? s_off;
        private static MaterialPropertyBlock s_block;

        /// <summary>Whether the specials are drawn in 3D on this page.</summary>
        public static bool Available
        {
            get
            {
                Make();
                return !Off && s_solid != null && s_glow != null && s_floor != null;
            }
        }

        /// <summary>Whether a private test page asked for the flat look of before ("special3d=0").</summary>
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

        public static Material Solid
        {
            get
            {
                Make();
                return s_solid;
            }
        }

        public static Material Glow
        {
            get
            {
                Make();
                return s_glow;
            }
        }

        public static Material Floor
        {
            get
            {
                Make();
                return s_floor;
            }
        }

        /// <summary>
        /// "Light", added to what is behind it as a light would (the flashes, rings, columns and bolts of
        /// ChogLight and ChogLightning): drawn after the see-through things, never written to depth, both sides.
        /// </summary>
        public static Material Light
        {
            get
            {
                Make();
                return s_light;
            }
        }

        /// <summary>
        /// How visible a glow drawn by this renderer is, from 0 (gone) to 1, and the picture a ball it draws
        /// wears (an eyeball's eye; null for none).
        /// </summary>
        public static void Apply(Renderer renderer, float fade, Texture picture)
        {
            if (renderer == null)
                return;
            s_block ??= new MaterialPropertyBlock();
            s_block.Clear();
            s_block.SetFloat(FadeId, Mathf.Clamp01(fade));
            if (picture != null)
                s_block.SetTexture(PictureId, picture);
            renderer.SetPropertyBlock(s_block);
        }

        private static void Make()
        {
            if (s_tried)
                return;
            s_tried = true;

            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("CHOG_SPECIAL3D the shader Chog/Effect is missing or not supported: the specials stay flat");
                return;
            }

            s_solid = new Material(shader) { name = "ChogEffectSolid", renderQueue = (int)RenderQueue.Geometry };
            s_solid.SetFloat(SrcBlendId, (float)BlendMode.One);
            s_solid.SetFloat(DstBlendId, (float)BlendMode.Zero);
            s_solid.SetFloat(ZWriteId, 1f);
            s_solid.SetFloat(CullId, (float)CullMode.Off);

            s_glow = new Material(shader) { name = "ChogEffectGlow", renderQueue = (int)RenderQueue.Transparent };
            s_glow.SetOverrideTag("RenderType", "Transparent");
            s_glow.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            s_glow.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            s_glow.SetFloat(ZWriteId, 0f);
            s_glow.SetFloat(CullId, (float)CullMode.Back);

            s_light = new Material(shader) { name = "ChogEffectLight", renderQueue = (int)RenderQueue.Transparent + 20 };
            s_light.SetOverrideTag("RenderType", "Transparent");
            s_light.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            s_light.SetFloat(DstBlendId, (float)BlendMode.One);
            s_light.SetFloat(ZWriteId, 0f);
            s_light.SetFloat(CullId, (float)CullMode.Off);

            var ground = Resources.Load<Shader>(ChogTeamRing.ShaderResource);
            if (ground != null && ground.isSupported)
                s_floor = new Material(ground) { name = "ChogEffectFloor" };
            else
                Debug.LogWarning("CHOG_SPECIAL3D the shader Chog/Ground is missing or not supported: the specials stay flat");
        }
    }
}
