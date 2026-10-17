using System;
using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chog.Match
{
    /// <summary>
    /// The parts of the look of a match (17 October 2026), each of which can be turned off on a page
    /// to compare it with the look of before on the same build:
    /// <list type="bullet">
    /// <item>Shade: heroes and arena drawn with a diffuse matcap (ChogHero.shader, ChogArena.shader);</item>
    /// <item>Light: a warm key light, the same shadow for both teams, no fog, a lavender ambient;</item>
    /// <item>Ring: a soft shadow and a ring in the colour of its side under every hero and structure
    /// (ChogTeamRing), in place of the sample's faded circle;</item>
    /// <item>Post: a light grading, glow, vignette and edge smoothing on a computer (ChogMatchPost).</item>
    /// </list>
    /// "?matchlook=0" turns every part off, "?matchlook=shade,ring" keeps only those (naming "post"
    /// forces it at every quality level), "?matchlook=-post" turns off only that one.
    /// </summary>
    public static class ChogMatchLook
    {
        public const string QueryParameter = "matchlook";

        private static MatchLookParts.Part? s_parts;

        /// <summary>The parts this page draws, read once from its address.</summary>
        public static MatchLookParts.Part Parts
        {
            get
            {
                if (s_parts == null)
                {
                    PerformanceSampling.TryGetQueryValue(Application.absoluteURL, QueryParameter, out var value);
                    s_parts = MatchLookParts.Parse(value);
                }
                return s_parts.Value;
            }
        }

        public static bool Has(MatchLookParts.Part part) => (Parts & part) == part;

        /// <summary>The key light: a warm white (#FFE8C2).</summary>
        public static readonly Color Key = new Color(1f, 232f / 255f, 194f / 255f);

        public const float KeyIntensity = 1f;

        private static MatchLookTuning s_tuning;

        /// <summary>
        /// The numbers of the look: the defaults, then those of the arena's palette once it is
        /// painted (TuneArena), with the page's "looktune=" option over both.
        /// </summary>
        public static MatchLookTuning Tuning => s_tuning ?? (s_tuning = MatchLookTuning.Parse(PageTuning()));

        private static string PageTuning()
        {
            PerformanceSampling.TryGetQueryValue(Application.absoluteURL, MatchLookTuning.QueryParameter, out var value);
            return value ?? "";
        }

        /// <summary>Gives the shaders the light of the arena's palette (pairs of "looktune="), the page's option over it.</summary>
        public static void TuneArena(string paletteTuning)
        {
            s_tuning = MatchLookTuning.Parse(paletteTuning + "," + PageTuning());
            SetGlobals();
        }

        // The names of the sample's two team lights (GameEssentials.prefab): GameViewInverter turns on
        // the one of the local player's team, whose camera looks from the other end for team B.
        private const string TeamALight = "LightTeamA";
        private const string TeamBLight = "LightTeamB";

        /// <summary>
        /// Both team lights cast the same shadow at full strength: Chog/Arena turns a full shadow into
        /// 75% of the colour, so the strength is set where it is read, in the shader.
        /// </summary>
        public const float ShadowStrength = 1f;

        /// <summary>The flat light of what still uses Unity's own shaders: a lavender (#CABAF4).</summary>
        public static readonly Color Ambient = new Color(202f / 255f, 186f / 255f, 244f / 255f);

        private const string LookOldGlobal = "_ChogLookOld";
        private const string KeyGlobal = "_ChogKey";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SetGlobals()
        {
            var tuning = Tuning;
            var (lx, ly, lz) = tuning.LightDirection();
            Shader.SetGlobalFloat(LookOldGlobal, Has(MatchLookParts.Part.Shade) ? 0f : 1f);
            Shader.SetGlobalVector(KeyGlobal, Vector4.zero);
            Shader.SetGlobalVector("_ChogLightView", new Vector4(lx, ly, lz, 0f));
            Shader.SetGlobalVector("_ChogHeroShade", Of(tuning.HeroShade, 1f));
            Shader.SetGlobalVector("_ChogHeroRamp", new Vector4(tuning.HeroRampCentre, tuning.HeroRampWidth, 0f, 0f));
            Shader.SetGlobalVector("_ChogHeroRim", Of(tuning.RimColour, tuning.Rim));
            Shader.SetGlobalVector("_ChogHeroOutline", Of(tuning.Outline, 1f));
            Shader.SetGlobalVector("_ChogArenaShade", Of(tuning.ArenaShade, 1f));
            Shader.SetGlobalVector("_ChogArenaRamp", new Vector4(tuning.Slope, tuning.Offset, tuning.Exposure, 0f));
            Shader.SetGlobalVector("_ChogArenaShadow", Of(tuning.ShadowTone, 1f));
            Shader.SetGlobalVector("_ChogArenaFoot", new Vector4(tuning.Foot, tuning.FootHeight, 0f, 0f));
        }

        private static Vector4 Of((float R, float G, float B) colour, float w) => new Vector4(colour.R, colour.G, colour.B, w);

        /// <summary>
        /// The rotation of a team light whose camera looks down at "pitch" and turns by "yaw": it comes
        /// from where the shaders' key comes from as that camera sees it (view space z points to the
        /// viewer, a camera's own z away from it).
        /// </summary>
        public static Quaternion KeyRotation(float pitch, float yaw)
        {
            var (lx, ly, lz) = Tuning.LightDirection();
            var toLight = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(lx, ly, -lz);
            return Quaternion.LookRotation(-toLight);
        }

        /// <summary>
        /// Lights the arena scene once it is loaded (ChogArenaSkin calls it): both team lights take
        /// the key's colour and the same full shadow strength, the fog goes, and the matcap shaders
        /// are told the key's warmth. Leaves the scene as the sample made it when Light is off.
        /// "ambient" is the palette's flat light, used when the light rig is off.
        /// </summary>
        public static void LightArena(GameObject[] roots, Color paletteAmbient, Color? ambientOption)
        {
            if (!Has(MatchLookParts.Part.Light))
            {
                RenderSettings.ambientLight = ambientOption ?? paletteAmbient;
                return;
            }

            foreach (var root in roots)
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type != LightType.Directional)
                    continue;
                light.color = Key;
                light.intensity = KeyIntensity;
                light.shadowStrength = ShadowStrength;
                // Each team's light comes from the upper left of its own camera: the same shadows for
                // both teams, falling toward the viewer and to the right, as the shading says.
                if (light.name == TeamALight)
                    light.transform.rotation = KeyRotation(ChogCameraRig.Pitch, 0f);
                else if (light.name == TeamBLight)
                    light.transform.rotation = KeyRotation(ChogCameraRig.Pitch, 180f);
            }

            // A lower camera (ChogCameraRig) sees ground farther away than the shadows reached.
            if (QualitySettings.shadows != ShadowQuality.Disable && QualitySettings.shadowDistance < ChogCameraRig.ShadowReach)
                QualitySettings.shadowDistance = ChogCameraRig.ShadowReach;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientOption ?? Ambient;
            var warm = Color.Lerp(Color.white, Key, Tuning.Warmth);
            Shader.SetGlobalVector(KeyGlobal, new Vector4(warm.r, warm.g, warm.b, 1f));
        }

        /// <summary>Outside the arena (the menus) the heroes are drawn without the key's warmth.</summary>
        public static void LeaveArena() => Shader.SetGlobalVector(KeyGlobal, Vector4.zero);
    }

    /// <summary>The page option of ChogMatchLook, read without the engine (tested outside Unity).</summary>
    public static class MatchLookParts
    {
        [Flags]
        public enum Part
        {
            None = 0,
            Shade = 1,
            Light = 2,
            Ring = 4,
            /// <summary>Post-processing where the quality level allows it (a computer).</summary>
            Post = 8,
            /// <summary>Post-processing at any quality level: named on the page.</summary>
            PostForced = 16,
        }

        /// <summary>What a page without the option draws.</summary>
        public const Part Default = Part.Shade | Part.Light | Part.Ring | Part.Post;

        /// <summary>
        /// The parts named by the option: nothing given is the default; "0", "none" or "old" is the
        /// look of before; "1" or "all" is everything, post-processing forced; a list of names keeps
        /// those; a name preceded by "-" removes it from the default (or from the names before it).
        /// Names not understood are left out.
        /// </summary>
        public static Part Parse(string value)
        {
            if (value == null)
                return Default;
            var text = Uri.UnescapeDataString(value).Trim().ToLowerInvariant();
            if (text.Length == 0)
                return Default;
            if (text == "0" || text == "none" || text == "old")
                return Part.None;
            if (text == "1" || text == "all")
                return Default | Part.PostForced;

            var named = Part.None;
            var removed = Part.None;
            var anyNamed = false;
            foreach (var item in text.Split(','))
            {
                var name = item.Trim();
                var remove = name.StartsWith("-", StringComparison.Ordinal);
                if (remove)
                    name = name.Substring(1);
                var part = Of(name);
                if (part == Part.None)
                    continue;
                if (remove)
                    removed |= part == Part.Post ? Part.Post | Part.PostForced : part;
                else
                {
                    named |= part == Part.Post ? Part.Post | Part.PostForced : part;
                    anyNamed = true;
                }
            }

            return (anyNamed ? named : Default) & ~removed;
        }

        /// <summary>
        /// Whether a quality level draws the post-processing: the level named "from" and every level
        /// after it in the project's list (the list runs from the cheapest to the finest). False when
        /// that name is not in the list.
        /// </summary>
        public static bool PostAllowed(int level, string[] names, string from)
        {
            if (names == null)
                return false;
            var first = Array.FindIndex(names, name => string.Equals(name, from, StringComparison.OrdinalIgnoreCase));
            return first >= 0 && level >= first;
        }

        private static Part Of(string name)
        {
            switch (name)
            {
                case "shade": return Part.Shade;
                case "light": return Part.Light;
                case "ring": return Part.Ring;
                case "post": return Part.Post;
                default: return Part.None;
            }
        }
    }
}
