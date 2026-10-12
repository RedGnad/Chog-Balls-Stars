using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Chog.Heroes
{
    [DataContract]
    public sealed class HeroDefinition
    {
        [DataMember(Name = "identityKey")] public string IdentityKey { get; private set; }
        [DataMember(Name = "displayName")] public string DisplayName { get; private set; }
        [DataMember(Name = "metadataId")] public int MetadataId { get; private set; }
        [DataMember(Name = "metadataHash")] public string MetadataHash { get; private set; }
        [DataMember(Name = "rulesetVersion")] public string RulesetVersion { get; private set; }
        [DataMember(Name = "catalogHash")] public string CatalogHash { get; private set; }
        [DataMember(Name = "archetypeId")] public string ArchetypeId { get; private set; }
        [DataMember(Name = "basicAttackId")] public string BasicAttackId { get; private set; }
        [DataMember(Name = "classAbilityId")] public string ClassAbilityId { get; private set; }
        [DataMember(Name = "signatureShapeId")] public string SignatureShapeId { get; private set; }
        [DataMember(Name = "signatureEffectId")] public string SignatureEffectId { get; private set; }
        [DataMember(Name = "tierExpressionId")] public string TierExpressionId { get; private set; }
        [DataMember(Name = "tier")] public string Tier { get; private set; }
        [DataMember(Name = "uniqueId")] public string UniqueId { get; private set; }
        [DataMember(Name = "explanation")] private string[] explanation;
        public IReadOnlyList<string> Explanation { get { return Array.AsReadOnly(explanation); } }

        internal HeroDefinition(GenesisSnapshot snapshot, string name, string version, string hash,
                                RoleData role, string shape, string effect, string expression, string tier,
                                string uniqueId, List<string> trace)
        {
            IdentityKey = snapshot.IdentityKey;
            DisplayName = name;
            MetadataId = snapshot.MetadataId;
            MetadataHash = snapshot.MetadataHash;
            RulesetVersion = version;
            CatalogHash = hash;
            ArchetypeId = role.Id;
            BasicAttackId = role.BasicAttackId;
            ClassAbilityId = role.ClassAbilityId;
            SignatureShapeId = shape;
            SignatureEffectId = effect;
            TierExpressionId = expression;
            Tier = tier;
            UniqueId = uniqueId;
            explanation = trace.ToArray();
        }
    }
}
