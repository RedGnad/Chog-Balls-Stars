using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using Chog.Heroes;

[DataContract]
internal sealed class FixtureManifest
{
    [DataMember(Name = "chainId", IsRequired = true)] public int ChainId;
    [DataMember(Name = "collection", IsRequired = true)] public string Collection;
    [DataMember(Name = "fixtures", IsRequired = true)] public FixtureData[] Fixtures;
}

// Line of research/evidence/genesis-metadata.jsonl (metadata ids, not token ids).
[DataContract]
internal sealed class MetadataRecord
{
    [DataMember(Name = "metadata_id", IsRequired = true)] public int MetadataId;
    [DataMember(Name = "raw_sha256", IsRequired = true)] public string Hash;
    [DataMember(Name = "raw_json", IsRequired = true)] public string Json;
}

// research/evidence/genesis-bindings-*.json written by research/scripts/scan_genesis_bindings.py.
[DataContract]
internal sealed class BindingFile
{
    [DataMember(Name = "chain_id", IsRequired = true)] public int ChainId;
    [DataMember(Name = "contract", IsRequired = true)] public string Collection;
    [DataMember(Name = "block_number", IsRequired = true)] public long BlockNumber;
    [DataMember(Name = "tokens", IsRequired = true)] public BindingData[] Tokens;
}

[DataContract]
internal sealed class BindingData
{
    [DataMember(Name = "token_id", IsRequired = true)] public long TokenId;
    [DataMember(Name = "token_uri", IsRequired = true)] public string TokenUri;
    [DataMember(Name = "metadata_id", IsRequired = true)] public int MetadataId;
}

[DataContract]
internal sealed class CoverageReport
{
    [DataMember(Name = "rulesetVersion", Order = 1)] public string RulesetVersion;
    [DataMember(Name = "catalogHash", Order = 2)] public string CatalogHash;
    [DataMember(Name = "bindingBlock", Order = 3)] public long BindingBlock;
    [DataMember(Name = "tokens", Order = 4)] public int Tokens;
    [DataMember(Name = "metadataAvailable", Order = 5)] public int MetadataAvailable;
    [DataMember(Name = "resolved", Order = 6)] public int Resolved;
    [DataMember(Name = "rejected", Order = 7)] public int Rejected;
    [DataMember(Name = "roles", Order = 8)] public Dictionary<string, int> Roles = new Dictionary<string, int>();
    [DataMember(Name = "shapes", Order = 9)] public Dictionary<string, int> Shapes = new Dictionary<string, int>();
    [DataMember(Name = "effects", Order = 10)] public Dictionary<string, int> Effects = new Dictionary<string, int>();
    [DataMember(Name = "expressions", Order = 11)] public Dictionary<string, int> Expressions = new Dictionary<string, int>();
    [DataMember(Name = "distinctKits", Order = 12)] public int DistinctKits;
    [DataMember(Name = "rejections", Order = 13)] public Dictionary<string, int> Rejections = new Dictionary<string, int>();
}

// content/genesis-roster.json: every Genesis the catalog resolves, as positions in the catalog id tables.
// The game imports it into a simulation asset, so field names are a contract with that importer.
[DataContract]
internal sealed class RosterFile
{
    [DataMember(Name = "rulesetVersion", Order = 1)] public string RulesetVersion;
    [DataMember(Name = "catalogHash", Order = 2)] public string CatalogHash;
    [DataMember(Name = "chainId", Order = 3)] public int ChainId;
    [DataMember(Name = "collection", Order = 4)] public string Collection;
    [DataMember(Name = "bindingBlock", Order = 5)] public long BindingBlock;
    [DataMember(Name = "collectionSize", Order = 6)] public int CollectionSize;
    [DataMember(Name = "roles", Order = 7)] public string[] Roles;
    [DataMember(Name = "shapes", Order = 8)] public string[] Shapes;
    [DataMember(Name = "effects", Order = 9)] public string[] Effects;
    [DataMember(Name = "expressions", Order = 10)] public string[] Expressions;
    [DataMember(Name = "tiers", Order = 11)] public string[] Tiers;
    // Trait values as written in the metadata, for display: a hero stores a position in each
    // table. Position 0 is the empty string and stands for an absent trait.
    [DataMember(Name = "bodies", Order = 12)] public string[] Bodies;
    [DataMember(Name = "eyes", Order = 13)] public string[] Eyes;
    [DataMember(Name = "auras", Order = 14)] public string[] Auras;
    [DataMember(Name = "skins", Order = 15)] public string[] Skins;
    [DataMember(Name = "heroes", Order = 16)] public RosterHero[] Heroes;
}

[DataContract]
internal sealed class RosterHero
{
    [DataMember(Name = "tokenId", Order = 1)] public int TokenId;
    [DataMember(Name = "metadataId", Order = 2)] public int MetadataId;
    [DataMember(Name = "name", Order = 3)] public string Name;
    [DataMember(Name = "role", Order = 4)] public int Role;
    [DataMember(Name = "shape", Order = 5)] public int Shape;
    [DataMember(Name = "effect", Order = 6)] public int Effect;
    [DataMember(Name = "expression", Order = 7)] public int Expression;
    [DataMember(Name = "tier", Order = 8)] public int Tier;
    [DataMember(Name = "uniqueId", Order = 9)] public string UniqueId;
    [DataMember(Name = "body", Order = 10)] public int Body;
    [DataMember(Name = "eye", Order = 11)] public int Eye;
    [DataMember(Name = "aura", Order = 12)] public int Aura;
    [DataMember(Name = "skin", Order = 13)] public int Skin;
}

// The attributes of one metadata file, read here only to show the trait values on the hero card.
[DataContract]
internal sealed class TraitList
{
    [DataMember(Name = "attributes")] public TraitValue[] Attributes;
}

[DataContract]
internal sealed class TraitValue
{
    [DataMember(Name = "trait_type")] public string Type;
    [DataMember(Name = "value")] public string Value;
}

[DataContract]
internal sealed class FixtureData
{
    [DataMember(Name = "tokenId", IsRequired = true)] public string TokenId;
    [DataMember(Name = "metadataId", IsRequired = true)] public int MetadataId;
    [DataMember(Name = "metadataUri", IsRequired = true)] public string MetadataUri;
    [DataMember(Name = "file", IsRequired = true)] public string File;
    [DataMember(Name = "sha256", IsRequired = true)] public string Hash;
}

internal static class Program
{
    private static string catalogJson;
    private static HeroCatalog catalog;
    private static FixtureManifest manifest;
    private static string fixtureDirectory;
    private static int passed;
    private static int failed;

    public static int Main(string[] args)
    {
        if (args.Length == 4 && args[0] == "coverage")
        {
            try { return RunCoverage(args[1], args[2], args[3]); }
            catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
        if (args.Length == 4 && args[0] == "roster")
        {
            try { return RunRoster(args[1], args[2], args[3]); }
            catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
        if (args.Length < 3 || (args[0] != "inspect" && args[0] != "test"))
        {
            Console.Error.WriteLine("Usage: hero-catalog <inspect|test> <catalog.json> <fixture-manifest.json>");
            Console.Error.WriteLine("       hero-catalog <coverage|roster> <catalog.json> <metadata.jsonl> <bindings.json>");
            return 2;
        }
        try
        {
            catalogJson = File.ReadAllText(args[1]);
            catalog = HeroCatalog.Load(catalogJson);
            fixtureDirectory = Path.GetDirectoryName(Path.GetFullPath(args[2]));
            using (var stream = File.OpenRead(args[2]))
                manifest = (FixtureManifest)new DataContractJsonSerializer(typeof(FixtureManifest)).ReadObject(stream);
            if (args[0] == "test") return RunTests();
            var heroes = new List<HeroDefinition>();
            foreach (var fixture in manifest.Fixtures) heroes.Add(catalog.Resolve(Snapshot(fixture.TokenId)));
            new DataContractJsonSerializer(typeof(List<HeroDefinition>)).WriteObject(Console.OpenStandardOutput(), heroes);
            Console.WriteLine();
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error is HeroCatalogException
                ? ((HeroCatalogException)error).Code + ": " + error.Message : error.Message);
            return 1;
        }
    }

    private static GenesisSnapshot Snapshot(string tokenId, Func<string, string> edit = null,
                                            int? chain = null, string collection = null,
                                            int? metadataId = null, string uri = null,
                                            string overrideTokenId = null, bool retainHash = false)
    {
        var fixture = Array.Find(manifest.Fixtures, x => x.TokenId == tokenId);
        if (fixture == null) throw new Exception("Fixture not found: " + tokenId);
        var json = File.ReadAllText(Path.Combine(fixtureDirectory, fixture.File));
        if (edit != null) json = edit(json);
        return new GenesisSnapshot(chain ?? manifest.ChainId, collection ?? manifest.Collection,
            overrideTokenId ?? fixture.TokenId, metadataId ?? fixture.MetadataId,
            uri ?? fixture.MetadataUri, json, edit == null || retainHash ? fixture.Hash : Hash(json));
    }

    private static BindingFile ReadBindings(string bindingsPath)
    {
        using (var stream = File.OpenRead(bindingsPath))
            return (BindingFile)new DataContractJsonSerializer(typeof(BindingFile)).ReadObject(stream);
    }

    private static Dictionary<int, MetadataRecord> ReadMetadata(string metadataPath)
    {
        var metadata = new Dictionary<int, MetadataRecord>();
        var lineSerializer = new DataContractJsonSerializer(typeof(MetadataRecord));
        foreach (var line in File.ReadAllLines(metadataPath))
        {
            if (line.Trim().Length == 0) continue;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(line)))
            {
                var record = (MetadataRecord)lineSerializer.ReadObject(stream);
                metadata[record.MetadataId] = record;
            }
        }
        return metadata;
    }

    // Writes the playable cast: every scanned token the real resolver accepts, sorted by token id.
    // A token without recorded metadata, or with a trait the catalog does not map, is left out and
    // reported on stderr; it never receives a fallback hero.
    private static int RunRoster(string catalogPath, string metadataPath, string bindingsPath)
    {
        var heroes = HeroCatalog.Load(File.ReadAllText(catalogPath));
        var bindings = ReadBindings(bindingsPath);
        var metadata = ReadMetadata(metadataPath);
        var entries = new List<RosterHero>();
        var bodies = new List<string> { "" };
        var eyes = new List<string> { "" };
        var auras = new List<string> { "" };
        var skins = new List<string> { "" };
        var traitSerializer = new DataContractJsonSerializer(typeof(TraitList));
        int withoutMetadata = 0, rejected = 0;
        foreach (var token in bindings.Tokens)
        {
            MetadataRecord record;
            if (!metadata.TryGetValue(token.MetadataId, out record)) { withoutMetadata++; continue; }
            try
            {
                var hero = heroes.Resolve(new GenesisSnapshot(bindings.ChainId, bindings.Collection,
                    token.TokenId.ToString(System.Globalization.CultureInfo.InvariantCulture), token.MetadataId,
                    token.TokenUri, record.Json, record.Hash));
                entries.Add(new RosterHero {
                    TokenId = checked((int)token.TokenId), MetadataId = hero.MetadataId, Name = hero.DisplayName,
                    Role = Position(heroes.RoleIds, hero.ArchetypeId), Shape = Position(heroes.ShapeIds, hero.SignatureShapeId),
                    Effect = Position(heroes.EffectIds, hero.SignatureEffectId),
                    Expression = Position(heroes.ExpressionIds, hero.TierExpressionId),
                    Tier = Position(heroes.TierNames, hero.Tier), UniqueId = hero.UniqueId ?? "",
                    Body = TraitPosition(bodies, record.Json, traitSerializer, "Body"),
                    Eye = TraitPosition(eyes, record.Json, traitSerializer, "Eyes"),
                    Aura = TraitPosition(auras, record.Json, traitSerializer, "Aura"),
                    Skin = TraitPosition(skins, record.Json, traitSerializer, "Skin") });
            }
            catch (HeroCatalogException) { rejected++; }
        }
        entries.Sort((a, b) => a.TokenId.CompareTo(b.TokenId));
        var roster = new RosterFile { RulesetVersion = heroes.Version, CatalogHash = heroes.Hash, ChainId = heroes.ChainId,
            Collection = heroes.Collection, BindingBlock = bindings.BlockNumber, CollectionSize = bindings.Tokens.Length,
            Roles = ToArray(heroes.RoleIds), Shapes = ToArray(heroes.ShapeIds), Effects = ToArray(heroes.EffectIds),
            Expressions = ToArray(heroes.ExpressionIds), Tiers = ToArray(heroes.TierNames),
            Bodies = bodies.ToArray(), Eyes = eyes.ToArray(), Auras = auras.ToArray(), Skins = skins.ToArray(),
            Heroes = entries.ToArray() };
        new DataContractJsonSerializer(typeof(RosterFile)).WriteObject(Console.OpenStandardOutput(), roster);
        Console.WriteLine();
        Console.Error.WriteLine("Roster: " + entries.Count + " of " + bindings.Tokens.Length + " Genesis playable; "
            + withoutMetadata + " without recorded metadata, " + rejected + " rejected by the catalog (see coverage).");
        return 0;
    }

    // Position of the value of one trait in its display table, adding it on first sight.
    // An absent trait is position 0. Tables grow in token order, so the file is stable.
    private static int TraitPosition(List<string> table, string metadataJson, DataContractJsonSerializer serializer, string trait)
    {
        TraitList traits;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(metadataJson)))
            traits = (TraitList)serializer.ReadObject(stream);
        var value = "";
        if (traits != null && traits.Attributes != null)
            foreach (var attribute in traits.Attributes)
                if (attribute != null && attribute.Type == trait) value = attribute.Value ?? "";
        var position = table.IndexOf(value);
        if (position < 0) { table.Add(value); position = table.Count - 1; }
        return position;
    }

    private static int Position(IReadOnlyList<string> ids, string id)
    {
        for (var i = 0; i < ids.Count; i++) if (ids[i] == id) return i;
        throw new Exception("Resolved id is missing from the catalog tables: " + id);
    }

    private static string[] ToArray(IReadOnlyList<string> values)
    {
        var result = new string[values.Count];
        for (var i = 0; i < result.Length; i++) result[i] = values[i];
        return result;
    }

    // Resolves every scanned token whose metadata is recorded, with the real resolver.
    // A rejected token is counted under its reason; nothing receives a fallback hero.
    private static int RunCoverage(string catalogPath, string metadataPath, string bindingsPath)
    {
        var heroes = HeroCatalog.Load(File.ReadAllText(catalogPath));
        var bindings = ReadBindings(bindingsPath);
        var metadata = ReadMetadata(metadataPath);
        var report = new CoverageReport { RulesetVersion = heroes.Version, CatalogHash = heroes.Hash,
                                          BindingBlock = bindings.BlockNumber, Tokens = bindings.Tokens.Length };
        var kits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in bindings.Tokens)
        {
            MetadataRecord record;
            if (!metadata.TryGetValue(token.MetadataId, out record)) continue;
            report.MetadataAvailable++;
            try
            {
                var hero = heroes.Resolve(new GenesisSnapshot(bindings.ChainId, bindings.Collection,
                    token.TokenId.ToString(System.Globalization.CultureInfo.InvariantCulture), token.MetadataId,
                    token.TokenUri, record.Json, record.Hash));
                report.Resolved++;
                Count(report.Roles, hero.ArchetypeId); Count(report.Shapes, hero.SignatureShapeId);
                Count(report.Effects, hero.SignatureEffectId); Count(report.Expressions, hero.TierExpressionId);
                kits.Add(hero.ArchetypeId + "/" + hero.SignatureShapeId + "/" + hero.SignatureEffectId + "/" + hero.TierExpressionId
                         + "/" + (hero.UniqueId ?? ""));
            }
            catch (HeroCatalogException error)
            {
                report.Rejected++;
                Count(report.Rejections, error.Code + ": " + error.Message);
            }
        }
        report.DistinctKits = kits.Count;
        var settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
        new DataContractJsonSerializer(typeof(CoverageReport), settings).WriteObject(Console.OpenStandardOutput(), report);
        Console.WriteLine();
        return 0;
    }

    private static void Count(Dictionary<string, int> counts, string key)
    {
        int value;
        counts.TryGetValue(key, out value);
        counts[key] = value + 1;
    }

    private static int RunTests()
    {
        Test("canonical token ID stays separate from displayed CHOG number", () => {
            var h = catalog.Resolve(Snapshot("512"));
            Equal(h.IdentityKey, "143:" + manifest.Collection + ":512");
            Equal(h.DisplayName, "CHOG #4");
            Equal(h.MetadataId, 4);
        });
        Test("aura-absent Uncommon resolves to ranger, wave shape, raw effect, echo", () => {
            var h = catalog.Resolve(Snapshot("512"));
            Equal(h.ArchetypeId, "ranger"); Equal(h.SignatureShapeId, "wave");
            Equal(h.SignatureEffectId, "raw"); Equal(h.TierExpressionId, "echo");
            Require(Contains(h.Explanation, "Aura=<absent> -> raw"), "Missing trait explanation absent.");
        });
        Test("real Fire Common resolves to ranger, puddle shape, burn effect, direct", () => {
            var h = catalog.Resolve(Snapshot("973"));
            Equal(h.DisplayName, "CHOG #465"); Equal(h.ArchetypeId, "ranger");
            Equal(h.SignatureShapeId, "puddle"); Equal(h.SignatureEffectId, "burn"); Equal(h.TierExpressionId, "direct");
        });
        Test("the project owner's Genesis resolves from its real traits, body absent", () => {
            var h = catalog.Resolve(Snapshot("1795"));
            Equal(h.DisplayName, "CHOG #1287"); Equal(h.IdentityKey, "143:" + manifest.Collection + ":1795");
            Equal(h.ArchetypeId, "brute"); Equal(h.SignatureShapeId, "flash");
            Equal(h.SignatureEffectId, "purify"); Equal(h.TierExpressionId, "direct");
            Require(Contains(h.Explanation, "Body=<absent> -> brute") && Contains(h.Explanation, "Eyes=Anime Glass -> flash")
                    && Contains(h.Explanation, "Aura=Clean -> purify"), "Each deciding trait must be explained.");
        });
        Test("Blaze works with only Base and Tier using its verified token binding", () => {
            var h = catalog.Resolve(Snapshot("561"));
            Equal(h.UniqueId, "blaze"); Equal(h.ArchetypeId, "controller");
            Equal(h.SignatureShapeId, "nova"); Equal(h.SignatureEffectId, "burn"); Equal(h.TierExpressionId, "inversion");
        });
        Test("wrong chain rejected", () => Error("WRONG_COLLECTION", () => catalog.Resolve(Snapshot("512", chain: 10143))));
        Test("different collection rejected", () => Error("WRONG_COLLECTION", () => catalog.Resolve(Snapshot("512", collection: "0x0000000000000000000000000000000000000001"))));
        Test("address capitalization is normalized without changing identity", () => {
            var address = "0x" + manifest.Collection.Substring(2).ToUpperInvariant();
            Equal(catalog.Resolve(Snapshot("512", collection: address)).IdentityKey, catalog.Resolve(Snapshot("512")).IdentityKey);
        });
        Test("zero, negative, leading-zero and uint256-overflow IDs rejected", () => {
            foreach (var id in new[] { "0", "-1", "0512", "1e3", new String('9', 78) })
                Error("INVALID_IDENTITY", () => Snapshot("512", overrideTokenId: id));
        });
        Test("metadata number cannot silently replace the canonical token ID", () =>
            Error("METADATA_BINDING_MISMATCH", () => catalog.Resolve(Snapshot("512", metadataId: 512))));
        Test("metadata from a different root rejected", () =>
            Error("METADATA_BINDING_MISMATCH", () => catalog.Resolve(Snapshot("512", uri: "ipfs://different/4.json"))));
        Test("changed metadata rejected against the recorded hash", () =>
            Error("METADATA_HASH_MISMATCH", () => Snapshot("512", s => s.Replace("Uncommon", "Common"), retainHash: true)));
        Test("unmapped body is rejected instead of silently receiving a fallback", () =>
            Error("UNMAPPED_TRAIT", () => catalog.Resolve(Snapshot("512", s => s.Replace("Grey Shirt", "Unpublished Shirt")))));
        Test("unmapped aura is rejected even when eyes have a valid rule", () =>
            Error("UNMAPPED_TRAIT", () => catalog.Resolve(Snapshot("973", s => s.Replace("\"Fire\"", "\"Unknown Aura\"")))));
        Test("duplicate trait is rejected independently of list order", () =>
            Error("DUPLICATE_TRAIT", () => catalog.Resolve(Snapshot("512", s => AddTrait(s, "Tier", "Legendary")))));
        Test("rare tier plus Origin cannot stack incompatible lineage benefits", () =>
            Error("INCOMPATIBLE_LINEAGE", () => catalog.Resolve(Snapshot("512", s => s.Replace("Uncommon", "Rare")))));
        Test("unknown rarity rejected", () =>
            Error("UNSUPPORTED_TIER", () => catalog.Resolve(Snapshot("512", s => s.Replace("Uncommon", "Mythic")))));
        Test("another 1:1 cannot use Blaze's entry", () =>
            Error("UNMAPPED_UNIQUE", () => catalog.Resolve(Snapshot("561", overrideTokenId: "562"))));
        Test("missing body has an explicit authored role", () => {
            var h = catalog.Resolve(Snapshot("512", s => RemoveTrait(s, "Body")));
            Equal(h.ArchetypeId, "brute");
        });
        Test("literal None is not equivalent to a missing body", () =>
            Error("UNMAPPED_TRAIT", () => catalog.Resolve(Snapshot("512", s => s.Replace("Grey Shirt", "None")))));
        Test("Head is cosmetic in this ruleset and cannot change the role", () => {
            var h = catalog.Resolve(Snapshot("512", s => s.Replace("Choggie Beanie", "Wizard Hat")));
            Equal(h.ArchetypeId, "ranger"); Equal(h.ClassAbilityId, "ranger.dash");
        });
        Test("one trait decides one thing: a different Aura changes the effect only", () => {
            var h = catalog.Resolve(Snapshot("973", s => s.Replace("\"Fire\"", "\"Wind\"")));
            Equal(h.ArchetypeId, "ranger"); Equal(h.SignatureShapeId, "puddle");
            Equal(h.SignatureEffectId, "haste"); Equal(h.TierExpressionId, "direct");
        });
        Test("unknown cosmetic trait does not change the combat profile", () => {
            var h = catalog.Resolve(Snapshot("512", s => AddTrait(s, "Background2", "Test cosmetic")));
            Equal(h.ArchetypeId, "ranger"); Equal(h.SignatureShapeId, "wave");
            Equal(h.SignatureEffectId, "raw"); Equal(h.TierExpressionId, "echo");
        });
        Test("a value listed under two results fails at catalog load", () => Error("INVALID_CATALOG", () =>
            HeroCatalog.Load(catalogJson.Replace("\"Wind\"", "\"Smoke\""))));
        Test("duplicate mapping fails at catalog load", () => Error("INVALID_CATALOG", () =>
            HeroCatalog.Load(catalogJson.Replace("\"Black Trackset\"", "\"Grey Shirt\""))));
        Test("broken role reference fails at catalog load", () => Error("INVALID_CATALOG", () =>
            HeroCatalog.Load(catalogJson.Replace("\"resultId\": \"ranger\"", "\"resultId\": \"missing-role\""))));
        Test("missing required catalog field fails at load", () => Error("INVALID_JSON", () =>
            HeroCatalog.Load(catalogJson.Replace("\"bodyRules\"", "\"typoBodyRules\""))));
        Test("catalog version and content hash travel with each hero", () => {
            var h = catalog.Resolve(Snapshot("512"));
            Equal(h.RulesetVersion, "genesis-0.4.0"); Equal(h.CatalogHash, Hash(catalogJson));
        });
        Test("resolving another hero cannot mutate an earlier profile", () => {
            var before = catalog.Resolve(Snapshot("512")); catalog.Resolve(Snapshot("561"));
            Equal(before.ArchetypeId, "ranger"); Equal(before.TierExpressionId, "echo");
            Equal(before.SignatureShapeId, catalog.Resolve(Snapshot("512")).SignatureShapeId);
            Equal(before.SignatureEffectId, catalog.Resolve(Snapshot("512")).SignatureEffectId);
        });
        Test("changed rules remain isolated in a new catalog instance", () => {
            var next = HeroCatalog.Load(catalogJson.Replace("genesis-0.4.0", "genesis-next"));
            Require(catalog.Hash != next.Hash, "Version change must change manifest hash.");
            Equal(next.Version, "genesis-next");
            Equal(catalog.Resolve(Snapshot("512")).RulesetVersion, "genesis-0.4.0");
        });
        Test("an Epic Radioactive fires the radioactive burst (nova) whatever its Eyes", () => {
            // 22 October 2026: the tier, not the Eyes, gives a Radioactive Genesis its shape.
            var h = catalog.Resolve(Snapshot("512", json => json
                .Replace("\"Origin\"", "\"Radioactive\"").Replace("\"Uncommon\"", "\"Epic\"")));
            Equal(h.SignatureShapeId, "nova"); Equal(h.TierExpressionId, "persistent-zone");
            Equal(catalog.Resolve(Snapshot("512")).SignatureShapeId, "wave");
        });
        Console.WriteLine("RESULT " + passed + " passed; " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }

    // Synthetic edits below test rejection/precedence. They are never recorded as real NFT metadata.
    private static string AddTrait(string json, string type, string value)
    {
        int index = json.LastIndexOf(']');
        return json.Insert(index, ", {\"trait_type\":\"" + type + "\",\"value\":\"" + value + "\"}");
    }

    private static string RemoveTrait(string json, string type)
    {
        var pattern = "\\{\\s*\"trait_type\"\\s*:\\s*\"" + type + "\"\\s*,\\s*\"value\"\\s*:\\s*\"[^\"]*\"\\s*\\},?";
        return System.Text.RegularExpressions.Regex.Replace(json, pattern, "");
    }

    private static string Hash(string text)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
    }
    private static bool Contains(IReadOnlyList<string> lines, string value)
    { foreach (var line in lines) if (line == value) return true; return false; }
    private static void Equal<T>(T actual, T expected)
    { Require(EqualityComparer<T>.Default.Equals(actual, expected), "Expected " + expected + ", received " + actual + "."); }
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Error(string code, Action action)
    {
        try { action(); }
        catch (HeroCatalogException error) { Equal(error.Code, code); return; }
        throw new Exception("Expected rejection " + code + ".");
    }
    private static void Test(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
}
