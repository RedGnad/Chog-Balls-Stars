namespace Chog.Arena
{
    // First Fusion tuning slice, traced to the Quantum Archer assets. Hit points and
    // damage are rescaled for the current arena; tick quantization is handled by Fusion.
    public static class ArcherRules
    {
        public const int MaxHealth = 100;
        public const float MaxEnergy = 3f;
        public const float MaxSpecial = 1f;
        public const float EnergyRegenPerSecond = 1.3f;
        public const float SpecialChargePerArrowHit = .15f;
        public const float SpecialChargePerZonePulse = .1f;
        public const int BasicCount = 7;
        public const float BasicInterval = .075f;
        public const float BasicProjectileSpeed = 10f;
        public const float BasicProjectileLife = .85f;
        public const int BasicDamage = 20;
        public const float SpecialRange = 8f;
        public const float SpecialProjectileSpeed = 6f;
        public const float SpecialRadius = 3f;
        public const float SpecialInterval = 1f;
        public const int SpecialPulses = 3;
        public const int SpecialDamage = 35;
        public const float SpecialSlowSeconds = 2f;
        public const float BasicAimLockSeconds = 1f;
        public const float SpecialAimLockSeconds = .5f;
    }
}
