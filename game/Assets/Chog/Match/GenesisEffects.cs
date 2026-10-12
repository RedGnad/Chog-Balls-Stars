using System.Collections.Generic;
using CartoonFX;
using Chog.Diagnostics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The one place where the game spawns a visual effect for a Genesis attack. It keeps their
    /// cost bounded on a phone: no real-time light, no camera shake, fewer particles on mobile,
    /// and a cap on how many effects live at once. "?fx=0" in the page URL turns them all off,
    /// to measure what they cost on a device.
    /// </summary>
    public static class GenesisEffects
    {
        public const string SwitchParameter = "fx";

        // Beyond this many live effects a new one is skipped: the fight is already unreadable.
        private const int MaxLive = 10;
        private const int MaxParticlesOnMobile = 30;

        private static readonly List<GameObject> Live = new List<GameObject>();
        private static int s_spawned;
        private static int s_skipped;

        public static bool Enabled { get; private set; } = true;

        /// <summary>One line for the performance overlay.</summary>
        public static string Status
        {
            get
            {
                Prune();
                return "fx " + (Enabled ? "on" : "off") + "  live " + Live.Count + "  spawned " + s_spawned + "  skipped " + s_skipped;
            }
        }

        public static int LiveCount
        {
            get
            {
                Prune();
                return Live.Count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialise()
        {
            Enabled = !(PerformanceSampling.TryGetQueryInt(Application.absoluteURL, SwitchParameter, out var value) && value == 0);

            // A real-time light per explosion relights the whole arena around it, and a shaking
            // camera hides the fight: neither is worth its cost here.
            CFXR_Effect.GlobalDisableLights = true;
            CFXR_Effect.GlobalDisableCameraShake = true;

            PerformanceHud.StatusLines.Add(() => Status);
        }

        /// <summary>
        /// Spawns an effect under "parent", or in the scene at "position" when there is no parent.
        /// Returns null when effects are off, the prefab is missing or too many are live already.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float scale, Color tint)
        {
            if (!Enabled || prefab == null)
                return null;

            Prune();
            if (Live.Count >= MaxLive)
            {
                s_skipped++;
                return null;
            }

            var effect = parent == null
                ? Object.Instantiate(prefab, position, Quaternion.identity)
                : Object.Instantiate(prefab, parent);
            if (parent != null)
            {
                effect.transform.localPosition = position;
                effect.transform.localRotation = Quaternion.identity;
            }

            effect.transform.localScale = Vector3.one * scale;

            foreach (var light in effect.GetComponentsInChildren<Light>(true))
                light.enabled = false;

            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                if (tint.a > 0f)
                    main.startColor = tint;
                if (Application.isMobilePlatform && main.maxParticles > MaxParticlesOnMobile)
                    main.maxParticles = MaxParticlesOnMobile;
            }

            Live.Add(effect);
            s_spawned++;
            return effect;
        }

        private static void Prune()
        {
            for (var i = Live.Count - 1; i >= 0; i--)
                if (Live[i] == null)
                    Live.RemoveAt(i);
        }
    }
}
