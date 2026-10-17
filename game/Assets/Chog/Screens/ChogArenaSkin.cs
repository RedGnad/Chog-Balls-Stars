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
            var colours = ArenaPalettes.Resolve(option, out var ambientGiven);
            ChogMatchLook.TuneArena(ArenaPalettes.TuningOf(ArenaPalettes.NameOf(option)));
            var shader = ChogMatchLook.Has(MatchLookParts.Part.Shade) ? ArenaShader() : null;
            var roots = scene.GetRootGameObjects();
            var painted = new HashSet<Material>();
            foreach (var root in roots)
            foreach (var drawn in root.GetComponentsInChildren<Renderer>(true))
            foreach (var material in drawn.sharedMaterials)
            {
                if (material == null || !painted.Add(material) || !material.HasProperty("_Color"))
                    continue;
                var named = colours.TryGetValue(NameOf(material), out var hex);
                if (named)
                    material.color = ColourOf(hex);
                // Every opaque material of the arena's own shapes drawn with Unity's standard shader takes
                // the matcap, painted or not, so that nothing in the arena keeps the light of before.
                if (shader != null && material.shader != null && material.shader.name == "Standard" && drawn is MeshRenderer
                    && material.renderQueue <= (int)UnityEngine.Rendering.RenderQueue.GeometryLast)
                    material.shader = shader;
            }

            var paletteAmbient = ColourOf(colours[ArenaPalettes.Ambient]);
            ChogMatchLook.LightArena(roots, paletteAmbient, ambientGiven ? paletteAmbient : (Color?)null);
            ChogMatchPost.Attach(roots);
        }

        private static Shader s_arenaShader;

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
}
