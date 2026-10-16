using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Screens
{
    /// <summary>
    /// Paints the arena in the colours of the game. The sample's dungeon (planks, stone walls, crates,
    /// the plants heroes hide in, a chest) is made of a few shared materials of one flat colour each,
    /// named by the pack they come from; each takes a colour of the Chog night here. Only colours
    /// change: the shapes, the colliders and the simulation are the sample's, and the heroes, lit by
    /// their own shader, are not touched.
    /// A private option tries other colours on a page without a build:
    /// "arena=floor:2b2257,plant:34c38f" (a role or a material name, then a colour in hex).
    /// </summary>
    public static class ChogArenaSkin
    {
        /// <summary>The scene of the arena; the menu has none of these materials, but is left alone anyway.</summary>
        public const string ArenaScene = "Gameplay";

        /// <summary>The colour each material of the arena takes, by its name in the pack.</summary>
        internal static readonly Dictionary<string, Color> Colours = new Dictionary<string, Color>
        {
            // The planks of the floor: a deep indigo, darker than every hero so they stand out.
            { "WoodDark", ChogTheme.Hex("262052") },
            // The walls around the arena: the night, a little lighter on their faces.
            { "Stone", ChogTheme.Hex("2C2358") },
            { "StoneDark", ChogTheme.Hex("16112E") },
            // The crates: lavender frames around deep purple planks, lighter than the floor and darker
            // than the heroes (brighter crates were tried and drew the eye away from the fight).
            { "Metal", ChogTheme.Hex("A194F0") },
            { "BrownDark", ChogTheme.Hex("3F3192") },
            // The plants heroes hide in: a mint that no Chog wears.
            { "Green", ChogTheme.Hex("2BC48F") },
            // The chest: gold and cheek pink.
            { "Gold", ChogTheme.Gold },
            { "Red", ChogTheme.Cheek },
        };

        /// <summary>Short names of the option for the materials, by what they are in the arena.</summary>
        internal static readonly Dictionary<string, string> Roles = new Dictionary<string, string>
        {
            { "floor", "WoodDark" },
            { "wall", "Stone" },
            { "walldark", "StoneDark" },
            { "frame", "Metal" },
            { "crate", "BrownDark" },
            { "plant", "Green" },
            { "gold", "Gold" },
            { "red", "Red" },
        };

        /// <summary>The flat light of the arena, a violet in place of the sample's blue grey.</summary>
        internal static readonly Color Ambient = ChogTheme.Hex("6A62A0");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Paint(scene);
            Paint(SceneManager.GetActiveScene());
        }

        private static void Paint(Scene scene)
        {
            if (!scene.IsValid() || scene.name != ArenaScene)
                return;

            var colours = WithOption(Colours, Application.absoluteURL);
            var painted = new HashSet<Material>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var drawn in root.GetComponentsInChildren<Renderer>(true))
            foreach (var material in drawn.sharedMaterials)
            {
                if (material == null || !painted.Add(material) || !material.HasProperty("_Color"))
                    continue;
                if (colours.TryGetValue(NameOf(material), out var colour))
                    material.color = colour;
            }

            if (colours.TryGetValue("ambient", out var ambient))
                RenderSettings.ambientLight = ambient;
            else
                RenderSettings.ambientLight = Ambient;
        }

        /// <summary>The name a material has in the pack: a copy made at run time ends in " (Instance)".</summary>
        internal static string NameOf(Material material)
        {
            var name = material.name;
            const string copy = " (Instance)";
            while (name.EndsWith(copy, StringComparison.Ordinal))
                name = name.Substring(0, name.Length - copy.Length);
            return name;
        }

        /// <summary>
        /// The colours with those of the page's "arena=" option laid over them: "role:hex" pairs
        /// separated by commas, a role being a short name of Roles, a material name, or "ambient".
        /// Anything not understood is left out.
        /// </summary>
        internal static Dictionary<string, Color> WithOption(Dictionary<string, Color> colours, string url)
        {
            var result = new Dictionary<string, Color>(colours);
            var option = OptionOf(url);
            if (option.Length == 0)
                return result;

            foreach (var pair in option.Split(','))
            {
                var parts = pair.Split(':');
                if (parts.Length != 2 || !TryHex(parts[1], out var colour))
                    continue;
                var role = parts[0].Trim();
                result[Roles.TryGetValue(role.ToLowerInvariant(), out var material) ? material : role] = colour;
            }
            return result;
        }

        private static string OptionOf(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";
            var query = url.IndexOf('?');
            if (query < 0)
                return "";
            foreach (var field in url.Substring(query + 1).Split('&', '#'))
                if (field.StartsWith("arena=", StringComparison.Ordinal))
                    return Uri.UnescapeDataString(field.Substring("arena=".Length));
            return "";
        }

        private static bool TryHex(string text, out Color colour)
        {
            colour = default;
            text = text.Trim().TrimStart('#');
            if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                return false;
            colour = new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
            return true;
        }
    }
}
