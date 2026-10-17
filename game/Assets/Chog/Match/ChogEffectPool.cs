using System.Collections.Generic;
using Chog.Diagnostics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Where the short effects of a fight (ChogImpact, GenesisShape, GenesisBeam, ChogSignatureFlash) wait
    /// between two uses instead of being made and destroyed at each hit: a phone at about 32 frames a second
    /// has no time for the garbage of a new object per hit. Every pooled effect is a child of one object of
    /// the active scene, so it goes away with the scene as the effects did before; a free list then holds
    /// destroyed entries, which are skipped.
    /// </summary>
    public static class ChogEffectPool
    {
        private static Transform s_root;
        private static int s_made;
        private static int s_reused;

        /// <summary>The parent of every pooled effect, at the origin of the world, made on first use.</summary>
        public static Transform Root
        {
            get
            {
                if (s_root == null)
                    s_root = new GameObject("Chog Effect Pool").transform;
                return s_root;
            }
        }

        /// <summary>One line for the performance overlay.</summary>
        public static string Status => "pool made " + s_made + "  reused " + s_reused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialise()
        {
            s_made = 0;
            s_reused = 0;
            PerformanceHud.StatusLines.Add(() => Status);
        }

        /// <summary>A free effect from "free", made active, or null when there is none: the caller then makes one.</summary>
        public static T Take<T>(Stack<T> free) where T : Component
        {
            while (free.Count > 0)
            {
                var kept = free.Pop();
                if (kept == null)
                    continue;
                kept.gameObject.SetActive(true);
                s_reused++;
                return kept;
            }

            return null;
        }

        /// <summary>A new object under the root, counted as made.</summary>
        public static GameObject Make(string name, params System.Type[] components)
        {
            var made = new GameObject(name, components);
            made.transform.SetParent(Root, false);
            s_made++;
            return made;
        }

        /// <summary>The effect is over: hidden and kept for the next one.</summary>
        public static void Give<T>(Stack<T> free, T effect) where T : Component
        {
            if (effect == null)
                return;
            effect.gameObject.SetActive(false);
            if (effect.transform.parent != Root)
                effect.transform.SetParent(Root, false);
            free.Push(effect);
        }
    }
}
