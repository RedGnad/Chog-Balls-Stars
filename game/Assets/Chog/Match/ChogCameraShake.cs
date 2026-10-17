using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// A light shake of the match camera, after "The art of screenshake" (Jan Willem Nijman) and Squirrel
    /// Eiserloh's "Juicing your cameras with math" (GDC 2016): blows add trauma, the camera moves by the
    /// square of the trauma along smooth noise, and the trauma decays on its own, so small hits barely
    /// move it and a fall shakes it for a moment. The camera's script adds Offset to the place it would
    /// have been without the shake, which stays its own. The player can turn it off; the choice is kept
    /// on the device and it is on unless turned off.
    /// </summary>
    public static class ChogCameraShake
    {
        /// <summary>The key of the setting on the device (PlayerPrefs): 1 on, 0 off.</summary>
        public const string SettingKey = "chog.camera-shake";

        /// <summary>What a hit on the local hero adds, and what its fall or a fallen structure adds.</summary>
        public const float LocalHit = 0.2f;
        public const float BigBlow = 0.5f;

        /// <summary>The trauma lost per second.</summary>
        public const float DecayPerSecond = 1.4f;
        /// <summary>The farthest the camera moves, in world units, at full trauma (a light shake).</summary>
        public const float MaxOffset = 0.45f;
        // How fast the noise the camera follows changes.
        private const float NoiseSpeed = 22f;

        private static float s_trauma;
        private static int s_enabled = -1;

        /// <summary>From 0 (still) to 1 (the most shake there is).</summary>
        public static float Trauma => s_trauma;

        /// <summary>Whether the player wants the shake, kept on the device; on by default.</summary>
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
                if (!value)
                    s_trauma = 0f;
            }
        }

        /// <summary>A blow: adds that much trauma, never beyond 1.</summary>
        public static void Add(float amount)
        {
            if (Enabled)
                s_trauma = AddTo(s_trauma, amount);
        }

        /// <summary>The trauma after a blow of "amount".</summary>
        public static float AddTo(float trauma, float amount) => Mathf.Clamp01(trauma + Mathf.Max(0f, amount));

        /// <summary>The trauma after "seconds" without a blow.</summary>
        public static float Decay(float trauma, float seconds) => Mathf.Max(0f, trauma - DecayPerSecond * seconds);

        /// <summary>How much the camera moves for that trauma, as a share of MaxOffset: its square.</summary>
        public static float Shake(float trauma) => trauma * trauma;

        /// <summary>
        /// Lets the trauma decay by "deltaTime" and gives how far the camera moves now, in its own axes
        /// (x right, y up), never more than MaxOffset; zero when the shake is off or at rest.
        /// </summary>
        public static Vector2 Step(float deltaTime, float time)
        {
            s_trauma = Decay(s_trauma, deltaTime);
            if (!Enabled || s_trauma <= 0f)
                return Vector2.zero;
            return Offset(s_trauma, time);
        }

        /// <summary>Where the camera is pushed for that trauma at that time: smooth noise, each axis -1 to 1.</summary>
        public static Vector2 Offset(float trauma, float time)
        {
            var amount = MaxOffset * Shake(trauma);
            return new Vector2(Noise(time * NoiseSpeed, 0), Noise(time * NoiseSpeed, 1)) * amount;
        }

        /// <summary>
        /// Smooth noise between -1 and 1: a random value at each whole "x", eased from one to the next
        /// ("channel" gives another series). Plain arithmetic, the same on every device.
        /// </summary>
        public static float Noise(float x, int channel)
        {
            var whole = Mathf.FloorToInt(x);
            var part = x - whole;
            var eased = part * part * (3f - 2f * part);
            return Mathf.Lerp(Random01(whole, channel), Random01(whole + 1, channel), eased) * 2f - 1f;
        }

        // A number from 0 to 1 that only depends on its two arguments (an integer hash).
        private static float Random01(int at, int channel)
        {
            unchecked
            {
                var hash = (uint)at * 374761393u + (uint)channel * 668265263u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>No shake left (a new match).</summary>
        public static void Clear() => s_trauma = 0f;
    }
}
