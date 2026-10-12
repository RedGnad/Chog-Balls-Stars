using System;

namespace Chog.Presentation
{
    /// <summary>
    /// First-run graphics quality per device class. Engine-independent so it can be tested
    /// outside Unity. Levels are looked up by name in the project's quality list.
    /// </summary>
    public static class QualityDefaults
    {
        // Desktop browsers have GPU headroom for shadows and per-pixel lights.
        public const string DesktopLevel = "Good";

        // Measured on a Helio G85 class phone: main-thread work already fills a 30 fps frame
        // at the two cheapest levels, so mobile starts without shadows.
        public const string MobileLevel = "Fast";

        /// <summary>
        /// Index of the default level for the device class, or -1 when the list is empty.
        /// Falls back to the nearest existing index when the named level is missing.
        /// </summary>
        public static int Choose(bool isMobile, string[] levelNames)
        {
            if (levelNames == null || levelNames.Length == 0)
                return -1;

            var wanted = isMobile ? MobileLevel : DesktopLevel;
            for (var i = 0; i < levelNames.Length; i++)
            {
                if (string.Equals(levelNames[i], wanted, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            var fallback = isMobile ? 1 : 3;
            return Math.Min(fallback, levelNames.Length - 1);
        }
    }
}
