using System;
using System.Globalization;

namespace Chog.Match
{
    /// <summary>
    /// The numbers of the look of a match, given to the shaders as globals by ChogMatchLook
    /// (ChogHero.shader, ChogArena.shader): the defaults chosen in matches on 17 October 2026, and a
    /// page option to try others without a build, "?looktune=exposure:1.2,arenashade:8f86c0,rim:0.4"
    /// (a name, then a number or six hex digits for a colour). Read without the engine (tested
    /// outside Unity). Colours are red, green, blue from 0 to 1.
    /// </summary>
    public sealed class MatchLookTuning
    {
        public const string QueryParameter = "looktune";

        // The key light, as the camera sees it (view space: x right, y up, z toward the viewer): from
        // the upper left and in front, as the matcaps of arena brawlers are lit. The team lights are
        // turned to come from there for their own camera, so shadows agree with the shading.
        public float LightX = -0.4f;
        public float LightY = 0.55f;
        public float LightZ = 0.73f;

        /// <summary>How much of the key's warmth (#FFE8C2) lit colours take, 0 none, 1 all.</summary>
        public float Warmth = 0.35f;

        // Heroes: white where lit, this lavender in the crescent toward the lower right of the silhouette.
        public (float R, float G, float B) HeroShade = Rgb("D4C2E6");
        /// <summary>Where the crescent starts (0 to 1 of the light) and how soft its edge is.</summary>
        public float HeroRampCentre = 0.5f;
        public float HeroRampWidth = 0.14f;
        public (float R, float G, float B) RimColour = Rgb("FFF6E0");
        public float Rim = 0.35f;
        public (float R, float G, float B) Outline = Rgb("21162F");

        // The arena: tops lit, faces turned away from the key toward this violet; "slope" and
        // "offset" set how fast (light = saturate(cosine * slope + offset)).
        // (Those of the night, ArenaPalettes; each palette gives its own when the arena is painted.)
        public (float R, float G, float B) ArenaShade = Rgb("7A72B0");
        public float Slope = 1.1f;
        public float Offset = 0.15f;
        public float Exposure = 1f;
        /// <summary>What a full shadow multiplies a colour by: about 75%, toward violet.</summary>
        public (float R, float G, float B) ShadowTone = Rgb("BDB6DE");
        /// <summary>The shade at the foot of walls and crates, and up to what height.</summary>
        public float Foot = 0.3f;
        public float FootHeight = 0.7f;

        // The post-processing of a computer (ChogMatchPost): the glow, from what luminance, and the grading.
        public float Bloom = 0.35f;
        public float BloomThreshold = 0.8f;
        public float Contrast = 1.06f;
        public float Saturation = 1.1f;
        public float Vignette = 0.28f;

        /// <summary>The defaults with the pairs of a page's option laid over them; anything not understood is left out.</summary>
        public static MatchLookTuning Parse(string option)
        {
            var tuning = new MatchLookTuning();
            if (string.IsNullOrWhiteSpace(option))
                return tuning;
            foreach (var pair in Uri.UnescapeDataString(option).Split(','))
            {
                var parts = pair.Split(':');
                if (parts.Length != 2)
                    continue;
                var name = parts[0].Trim().ToLowerInvariant();
                var text = parts[1].Trim();
                if (TryColour(text, out var colour))
                {
                    switch (name)
                    {
                        case "heroshade": tuning.HeroShade = colour; continue;
                        case "rimcolour": tuning.RimColour = colour; continue;
                        case "outline": tuning.Outline = colour; continue;
                        case "arenashade": tuning.ArenaShade = colour; continue;
                        case "shadow": tuning.ShadowTone = colour; continue;
                    }
                }
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    continue;
                switch (name)
                {
                    case "lightx": tuning.LightX = number; break;
                    case "lighty": tuning.LightY = number; break;
                    case "lightz": tuning.LightZ = number; break;
                    case "warmth": tuning.Warmth = Clamp(number, 0f, 1f); break;
                    case "ramp": tuning.HeroRampCentre = Clamp(number, 0f, 1.2f); break;
                    case "rampwidth": tuning.HeroRampWidth = Clamp(number, 0.01f, 1f); break;
                    case "rim": tuning.Rim = Clamp(number, 0f, 1f); break;
                    case "slope": tuning.Slope = Clamp(number, 0f, 4f); break;
                    case "offset": tuning.Offset = Clamp(number, -2f, 2f); break;
                    case "exposure": tuning.Exposure = Clamp(number, 0.3f, 3f); break;
                    case "foot": tuning.Foot = Clamp(number, 0f, 1f); break;
                    case "footheight": tuning.FootHeight = Clamp(number, 0.05f, 5f); break;
                    case "bloom": tuning.Bloom = Clamp(number, 0f, 2f); break;
                    case "threshold": tuning.BloomThreshold = Clamp(number, 0f, 1f); break;
                    case "contrast": tuning.Contrast = Clamp(number, 0.5f, 2f); break;
                    case "saturation": tuning.Saturation = Clamp(number, 0f, 2f); break;
                    case "vignette": tuning.Vignette = Clamp(number, 0f, 1f); break;
                }
            }
            return tuning;
        }

        /// <summary>The key's direction in view space, of length 1 (the default when the three are zero).</summary>
        public (float X, float Y, float Z) LightDirection()
        {
            var length = (float)Math.Sqrt(LightX * LightX + LightY * LightY + LightZ * LightZ);
            if (length < 1e-4f)
                return new MatchLookTuning().LightDirection();
            return (LightX / length, LightY / length, LightZ / length);
        }

        private static float Clamp(float value, float low, float high) => value < low ? low : value > high ? high : value;

        private static bool TryColour(string text, out (float R, float G, float B) colour)
        {
            colour = default;
            if (text.Length != 6 || !ArenaPalettes.TryHex(text, out var hex))
                return false;
            colour = ArenaPalettes.Rgb(hex);
            return true;
        }

        private static (float R, float G, float B) Rgb(string hex) => ArenaPalettes.Rgb(hex);
    }
}
