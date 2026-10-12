using UnityEditor;
using UnityEngine;

namespace Chog.Editor
{
    // Toggles the runtime performance overlay for Play Mode. Builds use "?perf=1" in the URL.
    public static class DiagnosticsMenu
    {
        private const string MenuPath = "Chog/Diagnostics/Performance HUD In Play Mode";

        // Protocol string: must equal Chog.Diagnostics.PerformanceHud.PlayerPrefsKey.
        private const string PerformanceHudKey = "chog.perf";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            var enabled = PlayerPrefs.GetInt(PerformanceHudKey, 0) == 1;
            PlayerPrefs.SetInt(PerformanceHudKey, enabled ? 0 : 1);
            PlayerPrefs.Save();
        }

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            Menu.SetChecked(MenuPath, PlayerPrefs.GetInt(PerformanceHudKey, 0) == 1);
            return true;
        }
    }
}
