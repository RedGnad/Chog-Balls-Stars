namespace Quantum
{
    // Immutable combat data. A loadout can be shared by Genesis with the same resolved genes.
    [System.Serializable]
    public class GenesisCombatLoadout : AssetObject
    {
        public string RulesetVersion;
        public string CatalogHash;
        public string ArchetypeId;
        public string BasicAttackId;
        public string ClassAbilityId;
        // Eyes decide the shape of the signature, Aura its effect (see content/hero-catalog.prototype.json).
        public string SignatureShapeId;
        public string SignatureEffectId;
        public string TierExpressionId;
        public string UniqueId;

        public AssetRef<EntityPrototype> CharacterPrototype;
        public AssetRef<SkillData> BasicSkill;
        public AssetRef<SkillData> SignatureSkill;

        public bool HasCombatReferences
        {
            get { return CharacterPrototype.IsValid && BasicSkill.IsValid && SignatureSkill.IsValid; }
        }
    }
}
