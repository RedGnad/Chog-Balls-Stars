using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chog.Heroes
{
    /// <summary>Pure, immutable-after-load catalogue; no wallet, engine, network or combat state.</summary>
    public sealed class HeroCatalog
    {
        private readonly CatalogData data;
        private readonly Dictionary<string, RoleData> roles;
        private readonly HashSet<string> shapes;
        private readonly HashSet<string> effects;
        private readonly HashSet<string> expressions;
        private readonly TraitTable bodyTable;
        private readonly TraitTable eyeTable;
        private readonly TraitTable auraTable;
        private readonly Dictionary<string, TierData> tiers;
        private readonly Dictionary<string, UniqueHeroData> uniqueHeroes;
        public string Version { get { return data.RulesetVersion; } }
        public string Hash { get; private set; }
        public int ChainId { get { return data.ChainId; } }
        public string Collection { get { return data.Collection; } }

        // Id tables in catalog order. A roster stores positions in these lists, so the order is part of the ruleset.
        public IReadOnlyList<string> RoleIds { get { return Array.AsReadOnly(Array.ConvertAll(data.Roles, role => role.Id)); } }
        public IReadOnlyList<string> ShapeIds { get { return Array.AsReadOnly(data.Shapes); } }
        public IReadOnlyList<string> EffectIds { get { return Array.AsReadOnly(data.Effects); } }
        public IReadOnlyList<string> ExpressionIds { get { return Array.AsReadOnly(data.Expressions); } }
        public IReadOnlyList<string> TierNames { get { return Array.AsReadOnly(Array.ConvertAll(data.Tiers, tier => tier.Tier)); } }

        private HeroCatalog(string json)
        {
            data = JsonData.Read<CatalogData>(json);
            Require(data != null && data.SchemaVersion == 2, "Unsupported catalog schema.");
            Require(!String.IsNullOrWhiteSpace(data.RulesetVersion), "Ruleset version is required.");
            Require(data.ChainId > 0 && GenesisSnapshot.IsAddress(data.Collection), "Invalid collection identity.");
            Require(data.MetadataRoot != null && data.MetadataRoot.StartsWith("ipfs://", StringComparison.Ordinal)
                    && data.MetadataRoot.EndsWith("/", StringComparison.Ordinal), "Invalid metadata root.");
            roles = new Dictionary<string, RoleData>(StringComparer.Ordinal);
            Require(data.Roles != null && data.Roles.Length > 0, "Roles are required.");
            foreach (var role in data.Roles)
            {
                Require(role != null && !String.IsNullOrWhiteSpace(role.Id), "Invalid role.");
                Require(!String.IsNullOrWhiteSpace(role.BasicAttackId) && !String.IsNullOrWhiteSpace(role.ClassAbilityId),
                        "Role must provide attack and class ability IDs.");
                Require(!roles.ContainsKey(role.Id), "Duplicate role: " + role.Id);
                roles.Add(role.Id, role);
            }
            shapes = ReadIds(data.Shapes, "shape");
            effects = ReadIds(data.Effects, "effect");
            expressions = ReadIds(data.Expressions, "expression");
            bodyTable = new TraitTable(data.BodyRules, new HashSet<string>(roles.Keys), "Body");
            eyeTable = new TraitTable(data.EyeRules, shapes, "Eyes");
            auraTable = new TraitTable(data.AuraRules, effects, "Aura");
            tiers = new Dictionary<string, TierData>(StringComparer.Ordinal);
            Require(data.Tiers != null, "Tiers are required.");
            foreach (var tier in data.Tiers)
            {
                Require(tier != null && !String.IsNullOrWhiteSpace(tier.Tier) && !String.IsNullOrWhiteSpace(tier.Base),
                        "Invalid tier definition.");
                Require(expressions.Contains(tier.ExpressionId), "Unknown tier expression.");
                Require(!tiers.ContainsKey(tier.Tier), "Duplicate tier: " + tier.Tier);
                tiers.Add(tier.Tier, tier);
            }
            uniqueHeroes = new Dictionary<string, UniqueHeroData>(StringComparer.Ordinal);
            Require(data.UniqueHeroes != null, "Unique hero table is required.");
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            var metadataIds = new HashSet<int>();
            foreach (var hero in data.UniqueHeroes)
            {
                Require(hero != null && GenesisSnapshot.IsTokenId(hero.TokenId) && hero.MetadataId > 0
                        && !String.IsNullOrWhiteSpace(hero.Id), "Invalid unique hero identity.");
                Require(roles.ContainsKey(hero.RoleId) && shapes.Contains(hero.ShapeId) && effects.Contains(hero.EffectId)
                        && expressions.Contains(hero.ExpressionId), "Unresolved unique hero references.");
                Require(!uniqueHeroes.ContainsKey(hero.TokenId) && uniqueIds.Add(hero.Id)
                        && metadataIds.Add(hero.MetadataId), "Duplicate unique hero.");
                uniqueHeroes.Add(hero.TokenId, hero);
            }
            Hash = JsonData.Hash(json);
        }

        public static HeroCatalog Load(string json) { return new HeroCatalog(json); }

        public HeroDefinition Resolve(GenesisSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (snapshot.ChainId != data.ChainId || !String.Equals(snapshot.Collection, data.Collection, StringComparison.OrdinalIgnoreCase))
                throw new HeroCatalogException("WRONG_COLLECTION", "This catalog only resolves its configured chain and collection.");
            if (!String.Equals(snapshot.MetadataUri, data.MetadataRoot + snapshot.MetadataId.ToString(CultureInfo.InvariantCulture) + ".json", StringComparison.Ordinal))
                throw new HeroCatalogException("METADATA_BINDING_MISMATCH", "Metadata ID and trusted token URI disagree.");
            var metadata = JsonData.Read<MetadataData>(snapshot.MetadataJson);
            if (metadata == null || metadata.Attributes == null || String.IsNullOrWhiteSpace(metadata.Name))
                throw new HeroCatalogException("INVALID_METADATA", "Metadata name and attributes are required.");
            var traits = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var attribute in metadata.Attributes)
            {
                if (attribute == null || String.IsNullOrWhiteSpace(attribute.Type) || String.IsNullOrWhiteSpace(attribute.Value))
                    throw new HeroCatalogException("INVALID_TRAIT", "Each trait must have a name and a non-empty string value.");
                if (traits.ContainsKey(attribute.Type))
                    throw new HeroCatalogException("DUPLICATE_TRAIT", "Duplicate trait: " + attribute.Type);
                traits.Add(attribute.Type, attribute.Value);
            }
            var tierName = Trait(traits, "Tier");
            TierData tier;
            if (tierName == null || !tiers.TryGetValue(tierName, out tier))
                throw new HeroCatalogException("UNSUPPORTED_TIER", "Unknown or absent Tier.");
            var baseName = Trait(traits, "Base");
            if (baseName != tier.Base)
                throw new HeroCatalogException("INCOMPATIBLE_LINEAGE", "Base and Tier do not match an authored rule.");
            var trace = new List<string> { "Base=" + baseName + "; Tier=" + tierName + " -> " + tier.ExpressionId };
            if (baseName == "1:1")
            {
                UniqueHeroData unique;
                if (!uniqueHeroes.TryGetValue(snapshot.TokenId, out unique) || unique.MetadataId != snapshot.MetadataId)
                    throw new HeroCatalogException("UNMAPPED_UNIQUE", "This 1:1 needs its own authored hero entry.");
                trace.Add("Genesis " + snapshot.TokenId + " / metadata " + snapshot.MetadataId + " -> " + unique.Id);
                return Definition(snapshot, metadata, roles[unique.RoleId], unique.ShapeId, unique.EffectId,
                                  unique.ExpressionId, tierName, unique.Id, trace);
            }
            var roleId = bodyTable.Match(traits, trace);
            var shape = eyeTable.Match(traits, trace);
            var effect = auraTable.Match(traits, trace);
            return Definition(snapshot, metadata, roles[roleId], shape, effect, tier.ExpressionId, tierName, null, trace);
        }

        private HeroDefinition Definition(GenesisSnapshot snapshot, MetadataData metadata, RoleData role,
                                          string shape, string effect, string expression, string tier, string unique,
                                          List<string> trace)
        {
            return new HeroDefinition(snapshot, metadata.Name, Version, Hash, role, shape, effect, expression, tier, unique, trace);
        }

        /// <summary>Explicit value-to-result table of one trait; an unlisted value is always rejected.</summary>
        private sealed class TraitTable
        {
            private readonly string name;
            private readonly Dictionary<string, string> results = new Dictionary<string, string>(StringComparer.Ordinal);
            private string absentResult;
            private bool hasAbsentRule;

            public TraitTable(TraitRuleGroup[] groups, HashSet<string> allowedResults, string traitName)
            {
                name = traitName;
                Require(groups != null && groups.Length > 0, "Missing " + traitName + " rules.");
                foreach (var group in groups)
                {
                    Require(group != null && group.ResultId != null && allowedResults.Contains(group.ResultId),
                            "Invalid " + traitName + " result reference.");
                    Require(group.Values != null && group.Values.Length > 0, "Empty " + traitName + " rule group.");
                    foreach (var value in group.Values)
                    {
                        if (value == null)
                        {
                            Require(!hasAbsentRule, "Duplicate " + traitName + " rule.");
                            hasAbsentRule = true;
                            absentResult = group.ResultId;
                            continue;
                        }
                        Require(value.Length > 0 && !results.ContainsKey(value), "Duplicate " + traitName + " rule.");
                        results.Add(value, group.ResultId);
                    }
                }
            }

            public string Match(Dictionary<string, string> traits, List<string> trace)
            {
                string value = Trait(traits, name);
                string result;
                if (value == null)
                {
                    if (!hasAbsentRule) throw Unmapped(null);
                    result = absentResult;
                }
                else if (!results.TryGetValue(value, out result))
                {
                    throw Unmapped(value);
                }
                trace.Add(name + "=" + (value ?? "<absent>") + " -> " + result);
                return result;
            }

            private HeroCatalogException Unmapped(string value)
            {
                return new HeroCatalogException("UNMAPPED_TRAIT", "No authored rule for " + name + "=" + (value ?? "<absent>") + ".");
            }
        }

        private static string Trait(Dictionary<string, string> traits, string name)
        {
            string value;
            return traits.TryGetValue(name, out value) ? value : null;
        }

        private static HashSet<string> ReadIds(string[] values, string label)
        {
            Require(values != null && values.Length > 0, "Missing " + label + " IDs.");
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                Require(!String.IsNullOrWhiteSpace(value) && result.Add(value), "Invalid or duplicate " + label + ".");
            return result;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new HeroCatalogException("INVALID_CATALOG", message);
        }
    }
}
