namespace Quantum
{
    public static class GenesisLoadoutSelection
    {
        public static unsafe void Apply(Frame frame, EntityRef character, GenesisCombatLoadout loadout)
        {
            if (loadout == null || !loadout.HasCombatReferences)
                throw new System.ArgumentException("Missing Genesis combat references.", nameof(loadout));
            var attacks = frame.Unsafe.GetPointer<CharacterAttacks>(character);
            attacks->BasicSkillData = loadout.BasicSkill;
            attacks->SpecialSkillData = loadout.SignatureSkill;
        }

        public static bool IsConsistent(RuntimeConfig config, RuntimePlayer player, GenesisCombatLoadout loadout)
        {
            return config != null && player != null && loadout != null
                && player.GenesisLoadout.Id == loadout.Guid
                && loadout.HasCombatReferences
                && !string.IsNullOrEmpty(player.GenesisIdentityKey)
                && player.GenesisMetadataId > 0
                && IsHash(player.GenesisMetadataHash)
                && IsHash(config.GenesisCatalogHash)
                && string.Equals(config.GenesisCatalogHash, player.GenesisCatalogHash, System.StringComparison.Ordinal)
                && string.Equals(config.GenesisCatalogHash, loadout.CatalogHash, System.StringComparison.Ordinal);
        }

        private static bool IsHash(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
    }
}
