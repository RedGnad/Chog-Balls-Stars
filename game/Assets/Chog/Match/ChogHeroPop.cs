using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// How a hero leaves and comes back (TwinStickShooter.CharacterView draws it): when it falls it swells
    /// for a tenth of a second, then bursts into pieces of its skin and face colours and is gone; when it
    /// comes back it grows from nothing past its size and settles (a Back ease), in a third of a second.
    /// Plain numbers, tested without an engine, and the colours of a hero read from the library of parts.
    /// </summary>
    public static class ChogHeroPop
    {
        public const float SwellSeconds = 0.1f;
        public const float SwellTo = 1.3f;
        public const float ComeBackSeconds = 0.35f;
        /// <summary>The pieces of a fall are this much larger than those of a hit, and fly this much faster.</summary>
        public const float BurstScale = 2f;
        public const float BurstSpeed = 1.6f;

        // The overshoot of the Back ease (Robert Penner's easing equations).
        private const float Overshoot = 1.70158f;

        /// <summary>
        /// The Back ease out: 0 at 0, past 1 (by about a tenth) near the end, exactly 1 at 1.
        /// </summary>
        public static float BackOut(float t)
        {
            t = Mathf.Clamp01(t);
            var u = t - 1f;
            return 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u;
        }

        /// <summary>The size of a falling hero "seconds" after its fall: from 1 up to SwellTo, eased out.</summary>
        public static float Swell(float seconds)
        {
            var t = Mathf.Clamp01(seconds / SwellSeconds);
            return Mathf.Lerp(1f, SwellTo, 1f - (1f - t) * (1f - t));
        }

        /// <summary>The size of a hero "seconds" after it came back.</summary>
        public static float ComeBack(float seconds) => seconds >= ComeBackSeconds ? 1f : Mathf.Max(0f, BackOut(seconds / ComeBackSeconds));

        /// <summary>
        /// The skin and face colours of the hero of that Genesis (ChogHeroModel.Recruit for a recruit), or
        /// false when the library does not know it.
        /// </summary>
        public static bool Colours(int metadataId, out Color skin, out Color face)
        {
            skin = Color.white;
            face = Color.white;
            var library = ChogHeroModel.Library;
            if (metadataId < 0 || library == null || !library.TryGetHero(metadataId, out var hero) || hero.Skin >= library.Skins.Length)
                return false;
            var colours = library.Skins[hero.Skin];
            skin = new Color(colours.SkinR, colours.SkinG, colours.SkinB);
            face = new Color(colours.FaceR, colours.FaceG, colours.FaceB);
            return true;
        }
    }
}
