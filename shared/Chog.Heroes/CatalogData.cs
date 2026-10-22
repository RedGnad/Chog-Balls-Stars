using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Chog.Heroes
{
    // These DTO fields are assigned by DataContractJsonSerializer through reflection.
#pragma warning disable CS0649
    [DataContract]
    internal sealed class CatalogData
    {
        [DataMember(Name = "schemaVersion", IsRequired = true)] public int SchemaVersion;
        [DataMember(Name = "rulesetVersion", IsRequired = true)] public string RulesetVersion;
        [DataMember(Name = "chainId", IsRequired = true)] public int ChainId;
        [DataMember(Name = "collection", IsRequired = true)] public string Collection;
        [DataMember(Name = "metadataRoot", IsRequired = true)] public string MetadataRoot;
        [DataMember(Name = "roles", IsRequired = true)] public RoleData[] Roles;
        [DataMember(Name = "shapes", IsRequired = true)] public string[] Shapes;
        [DataMember(Name = "effects", IsRequired = true)] public string[] Effects;
        [DataMember(Name = "expressions", IsRequired = true)] public string[] Expressions;
        // One visible trait decides one thing: Body the role, Eyes the signature shape,
        // Aura the signature effect. Other traits are cosmetic for this ruleset.
        [DataMember(Name = "bodyRules", IsRequired = true)] public TraitRuleGroup[] BodyRules;
        [DataMember(Name = "eyeRules", IsRequired = true)] public TraitRuleGroup[] EyeRules;
        [DataMember(Name = "auraRules", IsRequired = true)] public TraitRuleGroup[] AuraRules;
        [DataMember(Name = "tiers", IsRequired = true)] public TierData[] Tiers;
        [DataMember(Name = "uniqueHeroes", IsRequired = true)] public UniqueHeroData[] UniqueHeroes;
    }

    [DataContract]
    internal sealed class RoleData
    {
        [DataMember(Name = "id", IsRequired = true)] public string Id;
        [DataMember(Name = "basicAttackId", IsRequired = true)] public string BasicAttackId;
        [DataMember(Name = "classAbilityId", IsRequired = true)] public string ClassAbilityId;
    }

    [DataContract]
    internal sealed class TraitRuleGroup
    {
        [DataMember(Name = "resultId", IsRequired = true)] public string ResultId;
        // A null entry denotes a missing trait; the literal string "None" is a different value.
        [DataMember(Name = "values", IsRequired = true)] public string[] Values;
    }

    [DataContract]
    internal sealed class TierData
    {
        [DataMember(Name = "tier", IsRequired = true)] public string Tier;
        [DataMember(Name = "base", IsRequired = true)] public string Base;
        [DataMember(Name = "expressionId", IsRequired = true)] public string ExpressionId;
        // A shape every Genesis of this tier fires whatever its Eyes (22 October 2026: Radioactive Genesis fire a
        // radioactive burst); absent for a tier whose shape comes from the Eyes.
        [DataMember(Name = "shapeId", IsRequired = false)] public string ShapeId;
    }

    [DataContract]
    internal sealed class UniqueHeroData
    {
        [DataMember(Name = "tokenId", IsRequired = true)] public string TokenId;
        [DataMember(Name = "metadataId", IsRequired = true)] public int MetadataId;
        [DataMember(Name = "roleId", IsRequired = true)] public string RoleId;
        [DataMember(Name = "shapeId", IsRequired = true)] public string ShapeId;
        [DataMember(Name = "effectId", IsRequired = true)] public string EffectId;
        [DataMember(Name = "expressionId", IsRequired = true)] public string ExpressionId;
        [DataMember(Name = "id", IsRequired = true)] public string Id;
    }

    [DataContract]
    internal sealed class MetadataData
    {
        [DataMember(Name = "name", IsRequired = true)] public string Name;
        [DataMember(Name = "attributes", IsRequired = true)] public AttributeData[] Attributes;
    }

    [DataContract]
    internal sealed class AttributeData
    {
        [DataMember(Name = "trait_type", IsRequired = true)] public string Type;
        [DataMember(Name = "value", IsRequired = true)] public string Value;
    }

#pragma warning restore CS0649

    internal static class JsonData
    {
        public static T Read<T>(string json)
        {
            if (String.IsNullOrWhiteSpace(json) || json.Length > 262144)
                throw new HeroCatalogException("INVALID_JSON", "Empty or oversized JSON.");
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
            }
            catch (Exception error) when (error is SerializationException || error is ArgumentException)
            {
                throw new HeroCatalogException("INVALID_JSON", "Invalid or incomplete JSON: " + error.Message);
            }
        }

        public static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", "").ToLowerInvariant();
        }
    }

    public sealed class HeroCatalogException : Exception
    {
        public string Code { get; private set; }
        public HeroCatalogException(string code, string message) : base(message) { Code = code; }
    }
}
