using System.Runtime.InteropServices;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Short vibrations of an Android phone on the blows that matter to the player (the local hero hit, its
    /// fall, a structure that falls), through the browser (ChogHaptics.jslib, navigator.vibrate). Other
    /// devices have none. The player can turn them off; the choice is kept on the device and they are on
    /// unless turned off.
    /// </summary>
    public static class ChogHaptics
    {
        /// <summary>The key of the setting on the device (PlayerPrefs): 1 on, 0 off.</summary>
        public const string SettingKey = "chog.haptics";

        public const int HitMilliseconds = 20;
        public const int FallMilliseconds = 60;
        public const int StructureMilliseconds = 40;
        // Two vibrations closer than this are one: a burst of hits would only buzz.
        private const float Spacing = 0.08f;

        private static int s_enabled = -1;
        private static float s_last = -1f;

        /// <summary>Whether the player wants vibrations, kept on the device; on by default.</summary>
        public static bool Enabled
        {
            get
            {
                if (s_enabled < 0)
                    s_enabled = PlayerPrefs.GetInt(SettingKey, 1) != 0 ? 1 : 0;
                return s_enabled == 1;
            }
            set
            {
                s_enabled = value ? 1 : 0;
                PlayerPrefs.SetInt(SettingKey, s_enabled);
                PlayerPrefs.Save();
            }
        }

        /// <summary>A vibration of that many milliseconds, unless turned off or one just played.</summary>
        public static void Pulse(int milliseconds)
        {
            if (!Enabled || milliseconds <= 0)
                return;
            var now = Time.unscaledTime;
            if (s_last >= 0f && now - s_last < Spacing)
                return;
            s_last = now;
#if UNITY_WEBGL && !UNITY_EDITOR
            ChogVibrate(milliseconds);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ChogVibrate(int milliseconds);
#endif
    }
}
