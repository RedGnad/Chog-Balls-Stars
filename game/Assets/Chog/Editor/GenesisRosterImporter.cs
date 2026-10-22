using System;
using System.IO;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEngine;

namespace Chog.Editor
{
    /// <summary>
    /// Imports content/genesis-roster.json (written by "tools/hero-catalog/run.py roster") into
    /// the simulation asset a match reads, and points the menu's match config at it.
    /// </summary>
    public static class GenesisRosterImporter
    {
        // Field names are the JSON keys of the roster file.
        [Serializable]
        private sealed class RosterFile
        {
            public string rulesetVersion;
            public string catalogHash;
            public string[] roles;
            public string[] shapes;
            public string[] effects;
            public string[] expressions;
            public string[] tiers;
            public string[] bodies;
            public string[] eyes;
            public string[] auras;
            public string[] skins;
            public RosterHero[] heroes;
        }

        [Serializable]
        private sealed class RosterHero
        {
            public int tokenId;
            public int metadataId;
            public string name;
            public int role;
            public int shape;
            public int effect;
            public int expression;
            public int tier;
            public int body;
            public int eye;
            public int aura;
            public int skin;
        }

        // content/genesis-skin-colors.json, written by tools/genesis-art/derive_trait_colors.py.
        [Serializable]
        private sealed class TraitColorFile
        {
            public TraitColor[] colors;
        }

        [Serializable]
        private sealed class TraitColor
        {
            public string value;
            public string hex;
        }

        private const string AssetPath = "Assets/QuantumUser/Resources/GenesisRoster.asset";
        private const string MenuScenePath = "Assets/QuantumUser/Resources/TSS-QuantumMenuSceneInfo.asset";
        private const string Characters = "Assets/QuantumUser/Resources/Character_Playable_";

        // Character each role plays, and the sentence the hero card shows for it. A role absent
        // from this table has no character yet, and its Genesis stay out of matches.
        private static readonly string[][] RoleCharacters =
        {
            new[] { "brute", Characters + "KnightEntityPrototype.qprototype", "tough, fights up close" },
            new[] { "ranger", Characters + "ArcherEntityPrototype.qprototype", "fires fast from a distance" },
            new[] { "controller", Characters + "SpellcasterEntityPrototype.qprototype", "lobs zones that hold ground" }
        };

        private static string SourcePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../content/genesis-roster.json"));

        [MenuItem("Chog/Genesis/Import Roster")]
        private static void ImportFromMenu() => Debug.Log("CHOG roster: " + Import());

        /// <summary>Rewrites the roster asset from the JSON file and returns a one-line summary.</summary>
        public static string Import()
        {
            var file = JsonUtility.FromJson<RosterFile>(File.ReadAllText(SourcePath));
            if (file == null || file.roles == null || file.heroes == null || file.heroes.Length == 0)
                throw new InvalidOperationException("The roster file has no heroes: " + SourcePath);

            var roster = AssetDatabase.LoadAssetAtPath<GenesisRoster>(AssetPath);
            if (roster == null)
            {
                roster = ScriptableObject.CreateInstance<GenesisRoster>();
                AssetDatabase.CreateAsset(roster, AssetPath);
            }

            roster.RulesetVersion = file.rulesetVersion;
            roster.CatalogHash = file.catalogHash;
            roster.RoleIds = file.roles;
            roster.ShapeIds = file.shapes;
            roster.EffectIds = file.effects;
            roster.ExpressionIds = file.expressions;
            roster.TierNames = file.tiers;

            var playableRoles = 0;
            roster.RolePrototypes = new AssetRef<EntityPrototype>[file.roles.Length];
            roster.RoleSummaries = new string[file.roles.Length];
            for (var i = 0; i < file.roles.Length; i++)
            {
                var path = CharacterOf(file.roles[i], out var summary);
                if (path == null)
                    continue;

                roster.RoleSummaries[i] = summary;

                var prototype = AssetDatabase.LoadAssetAtPath<EntityPrototype>(path);
                if (prototype == null || !prototype.Guid.IsValid)
                    throw new InvalidOperationException("Character not found for role " + file.roles[i] + ": " + path);

                roster.RolePrototypes[i] = new AssetRef<EntityPrototype> { Id = prototype.Guid };
                playableRoles++;
            }

            var signatures = GenesisSignatureImporter.Import(file.shapes, file.effects, file.expressions);
            roster.SignatureSkills = signatures.Skills;
            roster.EffectIcons = signatures.Icons;
            roster.EffectColors = signatures.Colors;
            roster.ShapeMaterial = GenesisSignatureImporter.UnlitMaterial("GenesisShape");
            roster.ShapeSummaries = signatures.ShapeSummaries;
            roster.EffectSummaries = signatures.EffectSummaries;
            roster.DeliverySummaries = signatures.DeliverySummaries;
            roster.SecondFormNames = signatures.SecondFormNames;
            roster.SecondFormSummaries = signatures.SecondFormSummaries;
            roster.BodyValues = file.bodies ?? new string[0];
            roster.EyeValues = file.eyes ?? new string[0];
            roster.AuraValues = file.auras ?? new string[0];
            roster.EyeLooks = GenesisSignatureImporter.EyeLooksFor(roster.EyeValues);
            roster.SkinValues = file.skins ?? new string[0];
            roster.SkinColors = SkinColorsFor(roster.SkinValues);
            roster.FaceMaterial = GenesisSignatureImporter.UnlitMaterial("GenesisFace");
            roster.UniqueLooks = GenesisSignatureImporter.UniqueLooks();
            roster.RadioactiveLook = GenesisSignatureImporter.RadioactiveLook();
            roster.RadioactiveSkin = GenesisSignatureImporter.RadioactiveSkin;
            foreach (var unique in roster.UniqueLooks)
                if (Array.FindIndex(file.heroes, hero => hero.tokenId == unique.TokenId) < 0)
                    throw new InvalidOperationException("A look is authored for token " + unique.TokenId + ", which is not in the roster.");

            roster.Entries = new GenesisRosterEntry[file.heroes.Length];
            for (var i = 0; i < file.heroes.Length; i++)
            {
                var hero = file.heroes[i];
                if (i > 0 && hero.tokenId <= file.heroes[i - 1].tokenId)
                    throw new InvalidOperationException("Roster heroes must be sorted by distinct token id.");

                roster.Entries[i] = new GenesisRosterEntry
                {
                    TokenId = hero.tokenId,
                    MetadataId = hero.metadataId,
                    Name = hero.name,
                    Role = Position(hero.role, file.roles),
                    Shape = Position(hero.shape, file.shapes),
                    Effect = Position(hero.effect, file.effects),
                    Expression = Position(hero.expression, file.expressions),
                    Tier = Position(hero.tier, file.tiers),
                    Body = Position(hero.body, roster.BodyValues),
                    Eye = Position(hero.eye, roster.EyeValues),
                    Aura = Position(hero.aura, roster.AuraValues),
                    Skin = Position(hero.skin, roster.SkinValues)
                };
            }

            EditorUtility.SetDirty(roster);
            WriteCardText(roster);

            var menuScene = AssetDatabase.LoadAssetAtPath<QuantumMenuSceneInfo>(MenuScenePath);
            if (menuScene == null)
                throw new InvalidOperationException("Menu scene config not found: " + MenuScenePath);

            var rosterGuid = QuantumUnityDBUtilities.GetExpectedAssetGuid(roster, out _);
            if (menuScene.RuntimeConfig.GenesisRoster.Id != rosterGuid)
            {
                menuScene.RuntimeConfig.GenesisRoster = new AssetRef<GenesisRoster> { Id = rosterGuid };
                EditorUtility.SetDirty(menuScene);
            }

            AssetDatabase.SaveAssets();

            // Quantum registers a new asset in its database on a delayed Editor call. A build
            // requested right after the import must not depend on that timing: a match would
            // start without the roster and silently fall back to the sample cast.
            var reference = new AssetRef<GenesisRoster> { Id = rosterGuid };
            var wasRegistered = IsRegistered(reference);
            if (!wasRegistered)
            {
                QuantumUnityDBUtilities.RefreshGlobalDB(true);
                if (!IsRegistered(reference))
                    throw new InvalidOperationException("The roster is not in the Quantum asset database: " + rosterGuid);
            }

            return roster.Entries.Length + " heroes, " + playableRoles + " of " + file.roles.Length +
                   " roles with a character, " + signatures.Authored + " signatures authored for " +
                   file.shapes.Length * file.effects.Length + " pairs of shape and effect, " + signatures.AuthoredSecondForms +
                   " second forms, ruleset " + file.rulesetVersion + ", asset " + rosterGuid +
                   (wasRegistered ? "." : ", registered by a forced database refresh.");
        }

        // content/genesis-card-text.json: the sentences of the hero card, for pages outside the game.
        // Field names are its JSON keys.
        [Serializable]
        private sealed class CardTextFile
        {
            public string rulesetVersion;
            public CardText[] roles;
            public CardText[] shapes;
            public CardText[] effects;
            public CardText[] deliveries;
            public int recruitHealthPercent;
        }

        [Serializable]
        private sealed class CardText
        {
            public string id;
            public string summary;
            // "#RRGGBB" for an effect that has a colour, else empty.
            public string color;
        }

        // The sentences of the hero card are written in the importers, next to the numbers they
        // describe. This copies them to a content file so a web page can show the same card.
        private static void WriteCardText(GenesisRoster roster)
        {
            var file = new CardTextFile
            {
                rulesetVersion = roster.RulesetVersion,
                roles = CardTexts(roster.RoleIds, roster.RoleSummaries, null),
                shapes = CardTexts(roster.ShapeIds, roster.ShapeSummaries, null),
                effects = CardTexts(roster.EffectIds, roster.EffectSummaries, roster.EffectColors),
                deliveries = CardTexts(roster.ExpressionIds, roster.DeliverySummaries, null),
                recruitHealthPercent = Recruit.HealthPercent
            };
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../content/genesis-card-text.json"));
            var json = JsonUtility.ToJson(file, true) + "\n";
            if (!File.Exists(path) || File.ReadAllText(path) != json)
                File.WriteAllText(path, json);
        }

        private static CardText[] CardTexts(string[] ids, string[] summaries, Color[] colors)
        {
            var texts = new CardText[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                texts[i] = new CardText
                {
                    id = ids[i],
                    summary = summaries != null && i < summaries.Length ? summaries[i] ?? "" : "",
                    color = colors != null && i < colors.Length && colors[i].a > 0f
                        ? "#" + ColorUtility.ToHtmlStringRGB(colors[i])
                        : ""
                };
            }

            return texts;
        }

        private static bool IsRegistered(AssetRef<GenesisRoster> reference)
        {
            return QuantumUnityDB.TryGetGlobalAsset(reference, out GenesisRoster registered) && registered != null;
        }

        // The colour of each Skin value, from the file derived from the portraits. A value the
        // file does not list stays transparent, and the hero then keeps its own colours.
        private static Color[] SkinColorsFor(string[] skinValues)
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../content/genesis-skin-colors.json"));
            var file = File.Exists(path) ? JsonUtility.FromJson<TraitColorFile>(File.ReadAllText(path)) : null;
            var colors = new Color[skinValues.Length];
            if (file == null || file.colors == null)
                return colors;

            for (var i = 0; i < skinValues.Length; i++)
                foreach (var entry in file.colors)
                    if (entry.value == skinValues[i] && ColorUtility.TryParseHtmlString(entry.hex, out var parsed))
                        colors[i] = parsed;
            return colors;
        }

        private static string CharacterOf(string role, out string summary)
        {
            foreach (var row in RoleCharacters)
            {
                if (row[0] == role)
                {
                    summary = row[2];
                    return row[1];
                }
            }

            summary = null;
            return null;
        }

        private static byte Position(int value, string[] table)
        {
            if (table == null || value < 0 || value >= table.Length || value > byte.MaxValue)
                throw new InvalidOperationException("Roster position " + value + " is outside its id table.");
            return (byte)value;
        }
    }
}
