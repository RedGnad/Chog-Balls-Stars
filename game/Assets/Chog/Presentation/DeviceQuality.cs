using UnityEngine;
using UnityEngine.Scripting;

namespace Chog.Presentation
{
    /// <summary>
    /// Applies the device-class quality level on first run. Once the menu has stored a level,
    /// whether chosen by the player in the settings screen or kept from an earlier run,
    /// that stored level wins and this class does nothing.
    /// </summary>
    public static class DeviceQuality
    {
        // Storage key owned by the Quantum sample menu settings: a data contract, not ours to rename.
        private const string MenuQualityKey = "Photon.Menu.QualityLevel";

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyFirstRunDefault()
        {
            if (PlayerPrefs.HasKey(MenuQualityKey))
                return;

            var level = QualityDefaults.Choose(Application.isMobilePlatform, QualitySettings.names);
            if (level >= 0 && level != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(level, true);
        }
    }
}
