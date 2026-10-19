using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chog.Match
{
    /// <summary>
    /// The colours the arena's materials can take (applied by Chog.Screens.ChogArenaSkin), by the
    /// material's name in the sample's pack, as hex: "Chog night", the default since 16 October 2026,
    /// and "Chog day", to compare in a match until the owner picks one. Read without the engine, so
    /// the page option is tested outside Unity. "ambient" is the flat light of the scene when the
    /// light of the match (ChogMatchLook) is off.
    /// </summary>
    public static class ArenaPalettes
    {
        public const string Night = "night";
        public const string Day = "day";
        public const string Ambient = "ambient";

        /// <summary>The palette a page without the option shows.</summary>
        public const string DefaultName = Night;

        private static readonly Dictionary<string, string> NightColours = new Dictionary<string, string>
        {
            // The floor is a deep berry purple. It keeps the heroes readable while the paths look like
            // part of a Chog arena rather than the sample's blue dungeon.
            { "WoodDark", "2A1A46" },
            // The surrounding stone stays several values darker, so it frames the playable lane.
            { "Stone", "5B3A6C" },
            { "StoneDark", "281534" },
            // The cover blocks are candy pink with violet sides. The two materials preserve the sample's
            // readable cover volumes while moving their identity toward the approved arena board.
            { "Metal", "F2A8E7" },
            { "BrownDark", "A763A8" },
            // Bushes are blue-green, distinct from each team's red and blue control language.
            { "Green", "25B6B3" },
            // The chest: gold and cheek pink.
            { "Gold", "F59E0B" },
            { "Red", "D83E7E" },
            { Ambient, "77528B" },
        };

        // A daylight arena in the colours of arena brawlers: a grass floor, pale lavender walls with
        // a deeper violet below, Monad purple frames around warm wooden crates, deep green bushes.
        private static readonly Dictionary<string, string> DayColours = new Dictionary<string, string>
        {
            { "WoodDark", "79A942" },
            { "Stone", "CFC6E4" },
            { "StoneDark", "7D6FB0" },
            { "Metal", "836EF9" },
            { "BrownDark", "E39A55" },
            { "Green", "2F8F5E" },
            { "Gold", "F59E0B" },
            { "Red", "D83E7E" },
            { Ambient, "CABAF4" },
        };

        // The light of each palette (MatchLookTuning, the option "looktune=" laid over it), chosen in
        // matches on 17 October 2026: the night keeps the brightness the owner saw in it, with a
        // little warmth; the day takes more of the key's warmth.
        private const string NightTuning = "exposure:1.12,slope:1.12,offset:0.12,arenashade:875D91,warmth:0.42";
        private const string DayTuning = "exposure:1.05,slope:1.1,offset:0.15,arenashade:8A7FB8,warmth:0.6";

        /// <summary>The palette an option names: "day" or "night" anywhere in it, the default otherwise.</summary>
        public static string NameOf(string option)
        {
            var palette = DefaultName;
            foreach (var item in (option ?? "").Split(','))
            {
                var name = item.Trim().ToLowerInvariant();
                if (name == Day || name == Night)
                    palette = name;
            }
            return palette;
        }

        /// <summary>The light a palette is drawn with, as the pairs of the option "looktune=".</summary>
        public static string TuningOf(string palette) => palette == Day ? DayTuning : NightTuning;

        /// <summary>Short names of the option for the materials, by what they are in the arena.</summary>
        public static readonly Dictionary<string, string> Roles = new Dictionary<string, string>
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

        /// <summary>
        /// The colours (material name, or "ambient", to six hex digits in capitals) a page's "arena="
        /// option asks for: an item without ":" names a palette ("day", "night"), the default
        /// otherwise; "role:hex" pairs, a role being a short name of Roles, a material name or
        /// "ambient", are laid over it. Anything not understood is left out. Whether the option named
        /// "ambient" itself is told apart (ambientGiven), since the light of the match then keeps it.
        /// </summary>
        public static Dictionary<string, string> Resolve(string option, out bool ambientGiven)
        {
            ambientGiven = false;
            var items = (option ?? "").Split(',');
            var palette = NameOf(option);
            var result = new Dictionary<string, string>(palette == Day ? DayColours : NightColours);
            foreach (var item in items)
            {
                var parts = item.Split(':');
                if (parts.Length != 2 || !TryHex(parts[1], out var hex))
                    continue;
                var role = parts[0].Trim();
                var key = Roles.TryGetValue(role.ToLowerInvariant(), out var material) ? material
                    : string.Equals(role, Ambient, StringComparison.OrdinalIgnoreCase) ? Ambient : role;
                result[key] = hex;
                if (key == Ambient)
                    ambientGiven = true;
            }
            return result;
        }

        /// <summary>The value of the "arena=" option in a page address, or "" without one.</summary>
        public static string OptionOf(string url)
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

        /// <summary>Six hex digits, with or without "#", in capitals.</summary>
        public static bool TryHex(string text, out string hex)
        {
            hex = (text ?? "").Trim().TrimStart('#').ToUpperInvariant();
            return hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
        }

        /// <summary>The red, green and blue of six hex digits, from 0 to 1.</summary>
        public static (float R, float G, float B) Rgb(string hex)
        {
            var rgb = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return (((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        }
    }
}
