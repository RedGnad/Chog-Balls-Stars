using UnityEngine;

namespace Chog.Match
{
    /// <summary>Who took the damage a number shows, from the local player's side.</summary>
    public enum ChogDamageSide
    {
        LocalHero,
        Ally,
        Enemy
    }

    /// <summary>
    /// How a damage number pops (TwinStickShooter.FloatingCombatText draws them): it springs from 0.6 to 1.3
    /// of its size and settles at 1 in a tenth of a second, its colour says who was hit (red for the local
    /// hero, orange for its team, a warm white for the other team), its size says how much of the target's
    /// health the blow took, and the blows on one target within a fifth of a second add up in one number
    /// instead of a pile. Plain numbers, tested without an engine.
    /// </summary>
    public static class ChogDamageNumbers
    {
        public const float PopSeconds = 0.1f;
        public const float PopFrom = 0.6f;
        public const float PopPeak = 1.3f;
        /// <summary>Blows on one target closer than this add up in one number.</summary>
        public const float MergeSeconds = 0.2f;
        public const float SmallestSize = 0.85f;
        public const float LargestSize = 1.6f;

        public static readonly Color LocalHeroColour = new Color(1f, 0.27f, 0.25f);
        public static readonly Color AllyColour = new Color(1f, 0.62f, 0.2f);
        public static readonly Color EnemyColour = new Color(1f, 0.96f, 0.82f);

        /// <summary>The size of a number "seconds" after it popped: 0.6, up to 1.3 at the middle, 1 from PopSeconds on.</summary>
        public static float Pop(float seconds)
        {
            if (seconds <= 0f)
                return PopFrom;
            if (seconds >= PopSeconds)
                return 1f;
            var half = PopSeconds * 0.5f;
            if (seconds < half)
            {
                // Out fast, slowing at the top.
                var t = seconds / half;
                return Mathf.Lerp(PopFrom, PopPeak, 1f - (1f - t) * (1f - t));
            }
            var back = (seconds - half) / half;
            return Mathf.Lerp(PopPeak, 1f, back * back * (3f - 2f * back));
        }

        /// <summary>The size of a number for a blow that took that share (0 to 1) of the target's health.</summary>
        public static float Size(float share) => Mathf.Clamp(SmallestSize + 2f * Mathf.Max(0f, share), SmallestSize, LargestSize);

        /// <summary>Whether a blow at "now" adds to the number of the blow at "last" on the same target.</summary>
        public static bool Merges(float last, float now) => last >= 0f && now >= last && now - last <= MergeSeconds + 1e-4f;

        public static Color Colour(ChogDamageSide side)
        {
            switch (side)
            {
                case ChogDamageSide.LocalHero: return LocalHeroColour;
                case ChogDamageSide.Ally: return AllyColour;
                default: return EnemyColour;
            }
        }

        /// <summary>The side of a target of team "targetTeam" for a player of "localTeam" (below 0 for a spectator).</summary>
        public static ChogDamageSide SideOf(bool isLocalHero, int targetTeam, int localTeam)
        {
            if (isLocalHero)
                return ChogDamageSide.LocalHero;
            return localTeam >= 0 && targetTeam == localTeam ? ChogDamageSide.Ally : ChogDamageSide.Enemy;
        }
    }
}
