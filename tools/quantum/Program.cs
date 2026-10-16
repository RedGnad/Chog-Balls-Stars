using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Chog.Genesis;
using Chog.Heroes;
using Photon.Deterministic;
using Quantum;
using Quantum.Allocator;

internal static unsafe class Program
{
    private static string root;
    private static int passed;
    private static int failed;
    private static readonly List<string> results = new List<string>();

    private static int Main(string[] args)
    {
        root = args[0];
        var catalog = HeroCatalog.Load(File.ReadAllText(Path.Combine(root, "content/hero-catalog.prototype.json")));
        var common = Hero(catalog, "973");
        var uncommon = Hero(catalog, "512");
        var blaze = Hero(catalog, "561");
        var direct = Loadout(common, 1001, 5001);
        var echo = Loadout(uncommon, 1002, 5002);
        var available = new[] { direct, echo };
        var match = new RuntimeConfig { RequireGenesisLoadout = true, GenesisCatalogHash = common.CatalogHash };

        Test("canonical NFT identity is preserved while selecting a Quantum prototype", () => {
            var player = GenesisLoadoutBinder.Bind(common, available);
            Require(player.GenesisIdentityKey.EndsWith(":973"), "Canonical identity lost");
            Require(player.GenesisMetadataId == 465 && player.PlayerNickname == "CHOG #465", "Display metadata lost");
            Require(player.PlayerAvatar.Id == direct.CharacterPrototype.Id, "Wrong prototype");
            Require(player.GenesisLoadout.Id == direct.Guid, "Wrong loadout");
        });
        Test("real Common and Uncommon profiles select different signature asset references", () => {
            var a = GenesisLoadoutBinder.Bind(common, available);
            var b = GenesisLoadoutBinder.Bind(uncommon, available);
            Require(a.GenesisLoadout.Id != b.GenesisLoadout.Id, "Variants collapsed");
            Require(direct.SignatureSkill.Id != echo.SignatureSkill.Id, "Signatures collapsed");
        });
        Test("unimplemented Legendary profile has no fallback to a Common", () =>
            Rejected(() => GenesisLoadoutBinder.Bind(blaze, available)));
        Test("a hero cannot borrow a signature with another effect or another shape", () => {
            var wrongEffect = Loadout(common, 1003, 5003); wrongEffect.SignatureEffectId = "raw";
            Rejected(() => GenesisLoadoutBinder.Bind(common, new[] { wrongEffect }));
            var wrongShape = Loadout(common, 1004, 5004); wrongShape.SignatureShapeId = "beam";
            Rejected(() => GenesisLoadoutBinder.Bind(common, new[] { wrongShape }));
        });
        Test("same labels with a different catalog hash cannot bind", () => {
            var wrong = Loadout(common, 1003, 5003); wrong.CatalogHash = new string('a', 64);
            Rejected(() => GenesisLoadoutBinder.Bind(common, new[] { wrong }));
        });
        Test("same hash with a different ruleset version cannot bind", () => {
            var wrong = Loadout(common, 1003, 5003); wrong.RulesetVersion = "other";
            Rejected(() => GenesisLoadoutBinder.Bind(common, new[] { wrong }));
        });
        Test("missing skill references reject the loadout before entering a match", () => {
            var wrong = Loadout(common, 1003, 5003); wrong.SignatureSkill = default;
            Rejected(() => GenesisLoadoutBinder.Bind(common, new[] { wrong }));
        });
        Test("duplicate matching loadouts reject an ambiguous catalog", () =>
            Rejected(() => GenesisLoadoutBinder.Bind(common, new[] { direct, Loadout(common, 1003, 5003) })));
        Test("matching admission fields and match catalog pass consistency validation", () =>
            Require(GenesisLoadoutSelection.IsConsistent(match, GenesisLoadoutBinder.Bind(common, available), direct), "Rejected valid profile"));
        Test("a stale match catalog rejects an otherwise valid player", () => {
            var stale = new RuntimeConfig { GenesisCatalogHash = new string('b', 64) };
            Require(!GenesisLoadoutSelection.IsConsistent(stale, GenesisLoadoutBinder.Bind(common, available), direct), "Stale catalog accepted");
        });
        Test("swapping the player loadout reference is rejected", () => {
            var player = GenesisLoadoutBinder.Bind(common, available); player.GenesisLoadout.Id = echo.Guid;
            Require(!GenesisLoadoutSelection.IsConsistent(match, player, direct), "Swapped reference accepted");
        });
        Test("missing metadata fingerprint is rejected", () => {
            var player = GenesisLoadoutBinder.Bind(common, available); player.GenesisMetadataHash = "";
            Require(!GenesisLoadoutSelection.IsConsistent(match, player, direct), "Missing fingerprint accepted");
        });
        Test("binding another NFT cannot mutate an already selected player", () => {
            var a = GenesisLoadoutBinder.Bind(common, available);
            var b = GenesisLoadoutBinder.Bind(uncommon, available);
            b.PlayerNickname = "changed";
            Require(a.PlayerNickname == "CHOG #465" && a.GenesisLoadout.Id == direct.Guid, "Earlier player mutated");
        });
        Test("Genesis capabilities are applied independently in a real Quantum Frame", () => FrameTest(direct, echo));
        Test("bot tick intervals keep their real-time pace when the simulation rate changes", () => {
            Require(TickIntervals.FromAuthoringTicks(60, 15) == 15 && TickIntervals.FromAuthoringTicks(60, 1) == 1,
                "60 Hz must keep the authored tick counts");
            Require(TickIntervals.FromAuthoringTicks(30, 30) == 15 && TickIntervals.FromAuthoringTicks(30, 10) == 5,
                "30 Hz must halve the tick counts");
            Require(TickIntervals.FromAuthoringTicks(30, 15) == 8, "Half ticks round to the nearest tick");
            Require(TickIntervals.FromAuthoringTicks(30, 1) == 1 && TickIntervals.FromAuthoringTicks(20, 1) == 1,
                "An interval never falls under one tick");
        });

        Test("the roster finds a Genesis by its canonical token id, not by the number in its name", () => {
            var roster = Roster();
            var index = roster.IndexOf(1795);
            Require(index >= 0, "Token 1795 is missing from the roster");
            var hero = roster.Entries[index];
            Require(hero.MetadataId == 1287 && hero.Name == "CHOG #1287", "Wrong Genesis behind token 1795");
            Require(roster.RoleIds[hero.Role] == "brute" && roster.ShapeIds[hero.Shape] == "flash"
                && roster.EffectIds[hero.Effect] == "purify", "Token 1795 lost the kit its traits give it");
            Require(roster.IndexOf(0) == -1 && roster.IndexOf(int.MaxValue) == -1, "Unknown ids must not resolve");
            for (int i = 1; i < roster.Entries.Length; ++i)
                Require(roster.Entries[i - 1].TokenId < roster.Entries[i].TokenId, "Roster must be sorted by token id");
        });
        Test("a player enters as their Genesis and bots are other Genesis of the roster", () => CastTest(Roster()));
        Test("a role without a character keeps its Genesis out of the match", () => {
            var roster = Roster();
            var brute = Array.IndexOf(roster.RoleIds, "brute");
            roster.RolePrototypes[brute] = default;
            InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
                Require(!GenesisCast.TryGetPlayerHero(frame, new RuntimePlayer { GenesisTokenId = 1795 }, out _, out _),
                    "A Genesis without a character entered the match");
                for (int i = 0; i < 40; ++i) {
                    Require(GenesisCast.TryDrawBotHero(frame, out var bot, out var prototype), "No bot Genesis drawn");
                    Require(bot.Role != brute && prototype.IsValid, "A bot drew a role without a character");
                    GenesisCast.Tag(frame, frame.Create(), bot);
                }
            });
        });

        Test("bots named by the match config enter first, each once, then the draw is random again", () => {
            var roster = Roster();
            var config = MatchWith(roster);
            // 999999 is not a Genesis and is skipped.
            config.BotGenesisTokens = new[] { 181, 999999, 70, 181 };
            InFrame(new AssetObject[] { roster }, config, frame => {
                var drawn = new List<int>();
                for (int i = 0; i < 4; ++i) {
                    Require(GenesisCast.TryDrawBotHero(frame, out var bot, out var prototype) && prototype.IsValid, "No bot Genesis drawn");
                    GenesisCast.Tag(frame, frame.Create(), bot);
                    drawn.Add(bot.TokenId);
                }
                Require(drawn[0] == 181 && drawn[1] == 70, "The named bots did not enter first: " + string.Join(",", drawn));
                Require(drawn.Distinct().Count() == 4, "A Genesis entered twice: " + string.Join(",", drawn));
            });
        });

        Test("a player without a Genesis is a recruit with less health, in a Genesis match only", () => {
            var roster = Roster();
            EntityRef WithHealth(Frame frame, int health) {
                var entity = frame.Create();
                frame.Add<Attributes>(entity);
                var attributes = frame.Unsafe.GetPointer<Attributes>(entity);
                attributes->DataDictionary = frame.AllocateDictionary<EAttributeType, AttributeData>();
                frame.ResolveDictionary(attributes->DataDictionary)
                    .Add(EAttributeType.Health, new AttributeData { InitialValue = health, MaxValue = health });
                return entity;
            }
            InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
                var recruit = WithHealth(frame, 10000);
                frame.Add<PlayerLink>(recruit);
                Recruit.Apply(frame, recruit);
                var health = AttributesHelper.GetAttributeData(frame, recruit, EAttributeType.Health);
                Require(health.MaxValue == 8500 && health.InitialValue == 8500, "A recruit should have 85% of the health");
                Require(Recruit.Is(frame, recruit), "A player without a Genesis is a recruit");

                var hero = WithHealth(frame, 10000);
                frame.Add<PlayerLink>(hero);
                GenesisCast.Tag(frame, hero, roster.Entries[roster.IndexOf(1795)]);
                Require(!Recruit.Is(frame, hero), "A Genesis is not a recruit");
            });
            // Without a roster the match is the plain sample: nobody is weakened.
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                var plain = WithHealth(frame, 10000);
                frame.Add<PlayerLink>(plain);
                Recruit.Apply(frame, plain);
                Require(AttributesHelper.GetMaxValue(frame, plain, EAttributeType.Health) == 10000, "A plain match changed the health");
                Require(!Recruit.Is(frame, plain), "A plain match has no recruit");
            });
        });

        Test("the hero card names each trait of a Genesis and what it gives", () => {
            var roster = Roster();
            var mine = roster.CardLines(roster.Entries[roster.IndexOf(1795)]);
            Require(mine.Length == 4, "A card has four lines");
            Require(mine[3].Trait == "Rarity" && mine[3].Value == "Common" && mine[3].Gives == "direct"
                && mine[3].Summary == "about direct", "Wrong Rarity line");
            Require(mine[0].Trait == "Outfit" && mine[0].Value == "none" && mine[0].Gives == "brute",
                "Token 1795 has no Body trait, which gives the brute");
            Require(mine[1].Trait == "Eyes" && mine[1].Value == "Anime Glass" && mine[1].Gives == "flash"
                && mine[1].Summary == "about flash", "Wrong Eyes line");
            Require(mine[2].Trait == "Aura" && mine[2].Value == "Clean" && mine[2].Gives == "purify", "Wrong Aura line");

            // A 1:1 has no trait values: the card says its kit was made for it.
            var halo = roster.CardLines(roster.Entries[roster.IndexOf(1667)]);
            Require(halo[0].Value == "one of one" && halo[1].Value == "one of one" && halo[2].Value == "one of one",
                "A 1:1 must not read as a Genesis without traits");
            Require(halo[0].Gives == "controller" && halo[1].Gives == "nova" && halo[2].Gives == "mend", "Wrong kit for Halo");

            // A Common without Body, Eyes or Aura value keeps the plain wording.
            var plain = roster.Entries.First(e => e.Body == 0 && e.Aura == 0 && roster.TierNames[e.Tier] == "Common");
            Require(roster.CardLines(plain)[0].Value == "none", "An absent trait reads 'none'");
        });
        Test("a Genesis fires the signature of its Eyes and Aura, whatever class its Body gives", () => {
            var roster = Roster();
            var classSpecial = new AssetRef<SkillData> { Id = new AssetGuid(4002) };
            int Position(GenesisRosterEntry hero) =>
                (hero.Expression * roster.ShapeIds.Length + hero.Shape) * roster.EffectIds.Length + hero.Effect;
            InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
                var mine = roster.Entries[roster.IndexOf(1795)];
                Require(roster.ShapeIds[mine.Shape] == "flash" && roster.EffectIds[mine.Effect] == "purify",
                    "Token 1795 should carry the flash shape and the purify effect");
                var character = frame.Create();
                frame.Add<CharacterAttacks>(character);
                frame.Unsafe.GetPointer<CharacterAttacks>(character)->SpecialSkillData = classSpecial;
                GenesisCast.Tag(frame, character, mine);
                var mySpecial = frame.Get<CharacterAttacks>(character).SpecialSkillData.Id;
                Require(mySpecial == roster.SignatureSkills[Position(mine)].Id, "The special is not the signature of the Genesis");
                Require(GenesisCast.KitLabel(frame, character) == "brute / flash / purify", "Wrong kit label");

                AssetGuid SpecialOf(GenesisRosterEntry hero) {
                    var entity = frame.Create();
                    frame.Add<CharacterAttacks>(entity);
                    GenesisCast.Tag(frame, entity, hero);
                    return frame.Get<CharacterAttacks>(entity).SpecialSkillData.Id;
                }

                // Same class: other Eyes, or the same Eyes with another Aura, give another special.
                foreach (var other in new[] {
                    roster.Entries.First(e => e.Role == mine.Role && e.Shape != mine.Shape),
                    roster.Entries.First(e => e.Role == mine.Role && e.Shape == mine.Shape && e.Effect != mine.Effect) }) {
                    Require(SpecialOf(other) != mySpecial, "Different traits gave the same signature");
                }

                // The same Eyes and Aura with another Tier give another special too.
                bool Delivers(GenesisRosterEntry hero, string expression) => roster.ExpressionIds[hero.Expression] == expression;
                var echoed = roster.Entries.First(e => Delivers(e, "echo")
                    && roster.Entries.Any(d => Delivers(d, "direct") && d.Shape == e.Shape && d.Effect == e.Effect));
                var plain = roster.Entries.First(d => Delivers(d, "direct") && d.Shape == echoed.Shape && d.Effect == echoed.Effect);
                Require(SpecialOf(echoed) == roster.SignatureSkills[Position(echoed)].Id, "The echo special is not the echo signature");
                Require(SpecialOf(echoed) != SpecialOf(plain), "Another Tier gave the same signature");
            });
            // A delivery without a signature of its own plays as the direct one.
            InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
                var echo = roster.Entries.First(e => roster.ExpressionIds[e.Expression] == "echo");
                var direct = echo.Shape * roster.EffectIds.Length + echo.Effect;
                roster.SignatureSkills[Position(echo)] = default;
                var character = frame.Create();
                frame.Add<CharacterAttacks>(character);
                GenesisCast.Tag(frame, character, echo);
                Require(frame.Get<CharacterAttacks>(character).SpecialSkillData.Id == roster.SignatureSkills[direct].Id,
                    "A missing delivery did not fall back to the direct signature");
            });
            // A pair without an authored signature keeps the special of the class.
            roster.SignatureSkills[Position(roster.Entries[roster.IndexOf(1795)])] = default;
            InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
                var character = frame.Create();
                frame.Add<CharacterAttacks>(character);
                frame.Unsafe.GetPointer<CharacterAttacks>(character)->SpecialSkillData = classSpecial;
                GenesisCast.Tag(frame, character, roster.Entries[roster.IndexOf(1795)]);
                Require(frame.Get<CharacterAttacks>(character).SpecialSkillData.Id == classSpecial.Id,
                    "A missing signature replaced the class special");
            });
        });

        Test("a Genesis plays the second form of its signature only when its player equipped it", () => {
            var roster = Roster();
            int Position(GenesisRosterEntry hero, int form) =>
                ((form * roster.ExpressionIds.Length + hero.Expression) * roster.ShapeIds.Length + hero.Shape) * roster.EffectIds.Length + hero.Effect;
            var mine = roster.Entries[roster.IndexOf(1795)];
            Require(roster.SignatureSkills[Position(mine, 0)].Id != roster.SignatureSkills[Position(mine, 1)].Id, "Both forms are the same signature");
            InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
                EntityRef Enter(int form) {
                    var entity = frame.Create();
                    frame.Add<CharacterAttacks>(entity);
                    GenesisCast.Tag(frame, entity, mine, form);
                    return entity;
                }
                AssetGuid Special(EntityRef entity) => frame.Get<CharacterAttacks>(entity).SpecialSkillData.Id;

                var first = Enter(0);
                Require(Special(first) == roster.SignatureSkills[Position(mine, 0)].Id && frame.Get<GenesisHero>(first).Form == 0,
                    "The first form is not the plain signature");
                var second = Enter(1);
                Require(Special(second) == roster.SignatureSkills[Position(mine, 1)].Id && frame.Get<GenesisHero>(second).Form == 1,
                    "The second form was not equipped");
                // Anything that is not 1 plays the first form.
                var odd = Enter(7);
                Require(Special(odd) == Special(first) && frame.Get<GenesisHero>(odd).Form == 0, "An unknown form was played");

                Require(GenesisCast.KitLabel(frame, first) == "brute / flash / purify", "The first form changed the kit label");
                Require(GenesisCast.KitLabel(frame, second) == "brute / second flash / purify", "The kit label does not name the second form");
            });

            // The card describes the form played, and keeps the trait it comes from.
            var eyes = roster.CardLines(mine, 1)[1];
            Require(eyes.Gives == "second flash" && eyes.Summary == "about second flash" && eyes.Value == "Anime Glass",
                "The card does not describe the second form");
            Require(roster.CardLines(mine, 0)[1].Gives == "flash" && roster.CardLines(mine)[1].Gives == "flash", "The first form changed on the card");

            // A second form that was not authored plays as the first form of the same delivery.
            var echo = roster.Entries.First(e => roster.ExpressionIds[e.Expression] == "echo");
            roster.SignatureSkills[Position(echo, 1)] = default;
            InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
                var entity = frame.Create();
                frame.Add<CharacterAttacks>(entity);
                GenesisCast.Tag(frame, entity, echo, 1);
                Require(frame.Get<CharacterAttacks>(entity).SpecialSkillData.Id == roster.SignatureSkills[Position(echo, 0)].Id,
                    "A missing second form did not fall back to the first form of its delivery");
            });

            // A shape without a second form keeps its own name everywhere.
            roster.SecondFormNames[mine.Shape] = "";
            Require(!roster.HasSecondForm(mine.Shape) && roster.FormName(mine.Shape, 1) == "flash"
                && roster.CardLines(mine, 1)[1].Gives == "flash", "A shape without a second form was renamed");
        });

        // ---- The towers and the cores (Structure.qtn, Structures.cs) ----
        // No system runs in these frames, so what AttributesSystem does when a component is
        // added is done by hand: without it an attribute keeps no list of changes and nothing
        // can hurt it.
        void Ready(Frame frame, EntityRef entity) {
            var attributes = frame.ResolveDictionary(frame.Unsafe.GetPointer<Attributes>(entity)->DataDictionary).GetEnumerator();
            while (attributes.MoveNext()) attributes.ValuePtrUnsafe->Init(frame);
        }
        EntityRef HeroAt(Frame frame, int team, FPVector2 position) {
            var hero = frame.Create();
            frame.Add<Character>(hero);
            frame.Add(hero, Transform2D.Create(position, 0));
            frame.Add(hero, new TeamInfo { Index = team });
            frame.Add(hero, new Health());
            var attributes = frame.AllocateDictionary<EAttributeType, AttributeData>();
            attributes.Add(EAttributeType.Health, new AttributeData { InitialValue = 6000, MaxValue = 6000 });
            attributes.Add(EAttributeType.Power, new AttributeData { InitialValue = 1, MaxValue = 3 });
            attributes.Add(EAttributeType.Stun, new AttributeData { InitialValue = 0, MaxValue = 1 });
            frame.Add(hero, new Attributes { DataDictionary = attributes });
            Ready(frame, hero);
            return hero;
        }
        EntityRef StructureOf(Frame frame, int team, EStructureKind kind) {
            var all = frame.Filter<Structure, TeamInfo>();
            while (all.Next(out EntityRef entity, out Structure structure, out TeamInfo owner))
                if (owner.Index == team && structure.Kind == kind) return entity;
            throw new Exception("No " + kind + " for team " + team);
        }
        FP HealthOf(Frame frame, EntityRef entity) => AttributesHelper.GetAttributeData(frame, entity, EAttributeType.Health).CurrentValue;
        EntityRef Fire(Frame frame, EntityRef tower) => Structures.Shoot(frame, tower, frame.Unsafe.GetPointer<Structure>(tower),
            frame.Unsafe.GetPointer<Transform2D>(tower), frame.Unsafe.GetPointer<TeamInfo>(tower), frame.Unsafe.GetPointer<Health>(tower));

        Test("each team gets one core and one tower, once, on free ground of the map", () => {
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                Structures.Build(frame);
                Structures.Build(frame);
                Require(frame.ComponentCount<Structure>() == 4, "A match has two cores and two towers, built once");
                for (int team = 0; team < 2; team++) {
                    var core = StructureOf(frame, team, EStructureKind.Core);
                    var tower = StructureOf(frame, team, EStructureKind.Tower);
                    Require(frame.Get<Transform2D>(core).Position == Structures.CorePosition(team), "The core is not where the map has room for it");
                    Require(frame.Get<Transform2D>(tower).Position == Structures.TowerPosition(team), "The tower is not where the map has room for it");
                    Require(HealthOf(frame, core) == Structures.CoreHealth && HealthOf(frame, tower) == Structures.TowerHealth, "A structure does not start whole");
                    Require(frame.Get<Structure>(core).Range == 0 && frame.Get<Structure>(tower).Range == Structures.TowerRange, "Only a tower shoots");
                    Require(!frame.Has<Character>(core) && !frame.Has<Character>(tower), "A structure must not count as a hero");
                }
                // The map turns onto itself around its centre: what one team has, the other has opposite.
                Require(Structures.CorePosition(0) == -Structures.CorePosition(1) && Structures.TowerPosition(0) == -Structures.TowerPosition(1),
                    "The two bases are not opposite each other");
            });
        });

        Test("a tower hurts the nearest enemy hero within its range, once per interval, and nobody else", () => {
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                Structures.Build(frame);
                var tower = StructureOf(frame, 0, EStructureKind.Tower);
                var at = frame.Get<Transform2D>(tower).Position;
                var ally = HeroAt(frame, 0, at + new FPVector2(1, 0));
                var far = HeroAt(frame, 1, at + new FPVector2(0, Structures.TowerRange + 1));
                Require(Fire(frame, tower) == default, "A tower shot with no enemy in range");

                var near = HeroAt(frame, 1, at + new FPVector2(0, 2));
                var nearer = HeroAt(frame, 1, at + new FPVector2(1, 0));
                var fallen = HeroAt(frame, 1, at + new FPVector2(0, FP._0_50));
                frame.Unsafe.GetPointer<Health>(fallen)->IsDead = true;

                Require(Fire(frame, tower) == nearer, "The tower did not pick the nearest living enemy");
                Require(HealthOf(frame, nearer) == 6000 - Structures.TowerDamage, "The shot did not take the tower's damage");
                Require(HealthOf(frame, near) == 6000 && HealthOf(frame, ally) == 6000 && HealthOf(frame, far) == 6000 && HealthOf(frame, fallen) == 6000,
                    "A shot hurt someone it should not");
                Require(Fire(frame, tower) == default, "A tower shot twice within its interval");

                frame.Unsafe.GetPointer<Structure>(tower)->NextShotTime = 0;
                frame.Unsafe.GetPointer<Health>(tower)->IsDead = true;
                Require(Fire(frame, tower) == default, "A destroyed tower still shoots");
                Require(Fire(frame, StructureOf(frame, 0, EStructureKind.Core)) == default, "A core shot");
            });
        });

        Test("a bot is pushed out of the reach of an enemy tower, and of no other", () => {
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                Structures.Build(frame);
                var tower = StructureOf(frame, 0, EStructureKind.Tower);
                var at = frame.Get<Transform2D>(tower).Position;
                var inside = HeroAt(frame, 1, at + new FPVector2(0, 2));
                var push = Structures.KeepAway(frame, inside);
                Require(push.X == 0 && push.Y > 1, "An enemy inside the reach of a tower must be pushed straight away from it, harder than its own wish");
                var deeper = Structures.KeepAway(frame, HeroAt(frame, 1, at + new FPVector2(0, 1)));
                Require(deeper.Y > push.Y, "The push must grow towards the tower");
                Require(Structures.KeepAway(frame, HeroAt(frame, 1, at + new FPVector2(0, Structures.TowerRange + 1))) == default,
                    "Outside the reach of a tower nothing must push a bot");
                Require(Structures.KeepAway(frame, HeroAt(frame, 0, at + new FPVector2(0, 2))) == default, "A tower must not push its own team away");

                Require(Structures.KeepAway(frame, inside, tower) == default, "A bot that attacks a tower must be let under it");
                Require(Structures.KeepAway(frame, inside, StructureOf(frame, 0, EStructureKind.Core)) == default,
                    "A bot that attacks a core must be let under the tower of that team");
                Require(Structures.KeepAway(frame, inside, StructureOf(frame, 1, EStructureKind.Core)) == push,
                    "What a bot attacks elsewhere must not let it under this tower");

                frame.Unsafe.GetPointer<Health>(tower)->IsDead = true;
                Require(Structures.KeepAway(frame, inside) == default, "A destroyed tower must not keep anyone away");
            });
        });

        Test("a bot defends its structure under attack, and pushes the other team's with the numbers or behind a player", () => {
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                Structures.Build(frame);
                void Move(EntityRef hero, FPVector2 to) => frame.Unsafe.GetPointer<Transform2D>(hero)->Position = to;
                void Alive(EntityRef hero, bool alive) => frame.Unsafe.GetPointer<Health>(hero)->IsDead = !alive;
                EntityRef BotAt(int team, FPVector2 position) {
                    var hero = HeroAt(frame, team, position);
                    frame.Add(hero, new Bot { IsActive = true });
                    return hero;
                }

                var agent = BotAt(0, default);
                var mate = BotAt(0, new FPVector2(1, 0));
                var spare = BotAt(0, new FPVector2(2, 0));
                var first = BotAt(1, new FPVector2(8, 0));
                var second = BotAt(1, new FPVector2(-8, 0));
                var third = BotAt(1, new FPVector2(8, 3));
                var theirTower = StructureOf(frame, 1, EStructureKind.Tower);
                var theirCore = StructureOf(frame, 1, EStructureKind.Core);
                var ourTower = StructureOf(frame, 0, EStructureKind.Tower);

                Require(Structures.BotObjective(frame, agent) == default, "Three against three and nothing under attack: a bot has no duty");

                // The numbers: one of theirs is down.
                Alive(third, false);
                Require(Structures.BotObjective(frame, agent) == theirTower, "With more heroes alive a bot must push their tower");
                Move(first, new FPVector2(Structures.PushCalmDistance - 1, 0));
                Require(Structures.BotObjective(frame, agent) == default, "A bot with an enemy hero close to it must fight that hero, not push");
                Move(first, new FPVector2(8, 0));
                frame.Unsafe.GetPointer<Health>(theirTower)->IsDead = true;
                Require(Structures.Attackable(frame, 1) == theirCore && Structures.BotObjective(frame, agent) == theirCore,
                    "Once their tower fell, their core is what a bot pushes");
                frame.Unsafe.GetPointer<Health>(theirTower)->IsDead = false;
                Alive(third, true);

                // Behind a player: a hero of the team that no bot plays stands under their tower.
                var towerAt = frame.Get<Transform2D>(theirTower).Position;
                Move(mate, towerAt + new FPVector2(0, -2));
                Require(Structures.BotObjective(frame, agent) == default, "A bot under their tower must not draw the other bots there");
                // The player takes the place of a bot, so the teams stay three against three.
                Alive(spare, false);
                var player = HeroAt(frame, 0, towerAt + new FPVector2(0, -2));
                Require(Structures.BotObjective(frame, agent) == theirTower, "A player under their tower must be followed by the bots of the team");
                Require(Structures.BotObjective(frame, player) == default, "Nobody stands under their tower but that hero: it has no one to follow");
                Alive(player, false);
                Require(Structures.BotObjective(frame, agent) == default, "A defeated player leads no push");

                // Defence: our tower is hit, the nearest of their heroes around it is the one to stop.
                Structures.Hurt(frame, agent);
                Structures.Hurt(frame, ourTower);
                Require(Structures.BotObjective(frame, agent) == default, "An alarm with no enemy hero around the structure gives no duty");
                Move(second, frame.Get<Transform2D>(ourTower).Position + new FPVector2(3, 0));
                Require(Structures.BotObjective(frame, agent) == second, "A bot must go for the hero that attacks its team's tower");
                Require(Structures.BotObjective(frame, first) == ourTower,
                    "The alarm of a tower is not for the other team: with three heroes against two, it goes on pushing that tower");
                frame.Add(agent, new AIMemory { HealthStatus = EHealthStatus.Low });
                Require(Structures.BotObjective(frame, agent) == default, "A bot low on health must be left to run");
                frame.Unsafe.GetPointer<AIMemory>(agent)->HealthStatus = EHealthStatus.High;
                frame.Unsafe.GetPointer<Structure>(ourTower)->AlarmUntil = 0;
                Require(Structures.BotObjective(frame, agent) == default, "Once the alarm is over the bots go back to what they did");
            });
        });

        Test("an attack hurts a structure and does nothing else to it; without health it is destroyed, not defeated as a hero", () => {
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                Structures.Build(frame);
                var tower = StructureOf(frame, 1, EStructureKind.Tower);
                Ready(frame, tower);
                var attacker = HeroAt(frame, 0, default);
                Effect Of(EAttributeType type, FP value) => new Effect {
                    AttributeType = type, Value = value, Appliance = EModifierAppliance.OneTime, Operation = EModifierOperation.Subtract };

                EffectsHelper.OnApply(frame, attacker, tower, Of(EAttributeType.Stun, 2));
                EffectsHelper.OnApply(frame, attacker, tower, Of(EAttributeType.Speed, 1));
                Require(HealthOf(frame, tower) == Structures.TowerHealth, "An effect a tower has no attribute for changed it");
                EffectsHelper.OnApply(frame, attacker, tower, Of(EAttributeType.Health, 900));
                Require(HealthOf(frame, tower) == Structures.TowerHealth - 900, "An attack did not hurt the tower");

                // The last hit: the health system leaves a structure to its own end.
                EffectsHelper.OnApply(frame, attacker, tower, Of(EAttributeType.Health, Structures.TowerHealth));
                var filter = new HealthSystem.Filter {
                    Entity = tower, Attributes = frame.Unsafe.GetPointer<Attributes>(tower), Health = frame.Unsafe.GetPointer<Health>(tower) };
                new HealthSystem().Update(frame, ref filter);
                Require(frame.Get<Health>(tower).IsDead, "A tower without health still stands");
                Require(frame.Get<Transform2D>(tower).Position == Structures.TowerPosition(1), "A destroyed tower was moved away as a defeated hero is");
                Require(!frame.Get<PhysicsCollider2D>(tower).Enabled, "A destroyed tower still stops heroes and attacks");
                Require(Structures.WinnerByCore(frame) == -1, "A tower that falls must not end the match");
            });
        });

        Test("the first core that falls ends the match at once, for the other team", () => {
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                Structures.Build(frame);
                frame.Global->MatchTimer = 15;
                Require(Structures.WinnerByCore(frame) == -1 && Structures.MatchClock(frame) == 15,
                    "With both cores standing the clock of the match must be read as it is");

                Structures.Destroyed(frame, StructureOf(frame, 1, EStructureKind.Core));
                Require(frame.Global->DestroyedCore == 2 && Structures.WinnerByCore(frame) == 0, "Team 1 lost its core: team 0 wins");
                Require(Structures.MatchClock(frame) < 0, "A fallen core must make the match end now");

                Structures.Destroyed(frame, StructureOf(frame, 0, EStructureKind.Core));
                Require(Structures.WinnerByCore(frame) == 0, "A core that falls later must not change who won");
            });
        });

        RuntimeConfig Mode(EMatchMode mode) => new RuntimeConfig { Mode = mode };
        // The game manager's coin spawn, run with no prototype: it may only be run where it spawns nothing.
        int SiegeCoinsSpawned(Frame frame) {
            int before = frame.ComponentCount<Transform2D>();
            var context = default(AIContext);
            new SpawnWithRandomRadius { Amount = 1, Radius = 1 }.Execute(frame, default, ref context);
            return frame.ComponentCount<Transform2D>() - before;
        }

        Test("a match of coins builds no structure, a siege spawns no coin, and the earlier match has both", () => {
            InFrame(new AssetObject[0], Mode(EMatchMode.Coins), frame => {
                new StructureSystem().OnGameStart(frame);
                Require(frame.ComponentCount<Structure>() == 0, "A match of coins must have no tower and no core");
                Require(MatchModes.HasCoins(frame), "A match of coins must spawn its coins");
            });
            InFrame(new AssetObject[0], Mode(EMatchMode.Siege), frame => {
                new StructureSystem().OnGameStart(frame);
                Require(frame.ComponentCount<Structure>() == 4, "A siege has two towers and two cores");
                Require(!MatchModes.HasCoins(frame) && SiegeCoinsSpawned(frame) == 0, "A siege must spawn no coin");
            });
            // A config without a mode, as in every record made before the modes: towers, cores and coins.
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                new StructureSystem().OnGameStart(frame);
                Require(frame.RuntimeConfig.Mode == EMatchMode.TowersAndCoins, "A config without a mode must play the earlier match");
                Require(frame.ComponentCount<Structure>() == 4 && MatchModes.HasCoins(frame), "The earlier match has its structures and its coins");
            });
        });

        Test("in a siege the score is the damage done to the other team's structures, and the siege's own clock ends the match", () => {
            InFrame(new AssetObject[0], Mode(EMatchMode.Siege), frame => {
                new PlayerJoiningSystem().OnInit(frame);
                new StructureSystem().OnGameStart(frame);
                var teams = frame.ResolveList(frame.Global->TeamsData);
                var attacker = HeroAt(frame, 0, default);
                var tower = StructureOf(frame, 1, EStructureKind.Tower);
                Ready(frame, tower);
                Ready(frame, StructureOf(frame, 1, EStructureKind.Core));
                EffectsHelper.OnApply(frame, attacker, tower, new Effect {
                    AttributeType = EAttributeType.Health, Value = 950, Appliance = EModifierAppliance.OneTime, Operation = EModifierOperation.Subtract });
                Structures.Hurt(frame, tower);
                Require(teams[0].Score == 9 && teams[1].Score == 0, $"950 health taken from team 1 is 9 points for team 0, not {teams[0].Score} to {teams[1].Score}");

                frame.Global->MatchTimer = -5;
                Require(Structures.MatchClock(frame) > 0, "A siege must not end on the coin countdown's clock");
                var started = frame.Global->GameplayStartedAt;
                Require(started > 0 && MatchModes.SiegeTimeLeftAt(started, started + 60) == MatchModes.SiegeSeconds - 60,
                    "The siege's clock runs from the start of play");
                Require(MatchModes.SiegeTimeLeftAt(started, started + MatchModes.SiegeSeconds + 1) < 0, "A siege ends when its time runs out");
                Require(MatchModes.SiegeTimeLeftAt(0, 500) == MatchModes.SiegeSeconds, "Before play begins the siege keeps all its time");
                var context = default(AIContext);
                Require(new GetTeamScore { TeamId = 0 }.Execute(frame, default, ref context) == 0,
                    "The game manager must read no score in a siege, or its coin countdown would start");

                Structures.Destroyed(frame, StructureOf(frame, 1, EStructureKind.Core));
                Require(teams[0].Score == FPMath.FloorToInt((Structures.CoreHealth + 950) / MatchModes.HealthPerSiegePoint),
                    "A fallen core counts all its health");
                Require(Structures.MatchClock(frame) < 0 && Structures.WinnerByCore(frame) == 0, "A fallen core still ends a siege at once");
            });
            // The same damage in the earlier match leaves the coins as the score.
            InFrame(new AssetObject[0], new RuntimeConfig(), frame => {
                new PlayerJoiningSystem().OnInit(frame);
                new StructureSystem().OnGameStart(frame);
                var tower = StructureOf(frame, 1, EStructureKind.Tower);
                Ready(frame, tower);
                EffectsHelper.OnApply(frame, HeroAt(frame, 0, default), tower, new Effect {
                    AttributeType = EAttributeType.Health, Value = 950, Appliance = EModifierAppliance.OneTime, Operation = EModifierOperation.Subtract });
                Structures.Hurt(frame, tower);
                Require(frame.ResolveList(frame.Global->TeamsData)[0].Score == 0, "Damage to a structure must not score outside a siege");
                frame.Global->MatchTimer = 15;
                Require(Structures.MatchClock(frame) == 15, "Outside a siege the clock is the coin countdown's");
            });
        });

        var evidence = new {
            clientDate = "2026-10-08", recordedAtUtc = DateTime.UtcNow.ToString("O"),
            sdk = Environment.Version.ToString(), quantumAssembly = typeof(AssetObject).Assembly.FullName,
            passed, failed, tests = results,
            limitations = new[] {
                "Loadout IDs in this harness are test assets, not Unity-authored gameplay variants.",
                "No Unity import, full cast/damage sequence, network session or WebGL build exercised.",
                "Consistency checks do not authenticate a wallet or establish NFT ownership."
            }
        };
        File.WriteAllText(Path.Combine(root, ".artifacts/quantum-dotnet/test-results.json"), JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{passed} passed; {failed} failed.");
        return failed == 0 ? 0 : 1;
    }

    private static HeroDefinition Hero(HeroCatalog catalog, string tokenId)
    {
        var directory = Path.Combine(root, "tests/fixtures/genesis");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        var data = manifest.RootElement;
        foreach (var fixture in data.GetProperty("fixtures").EnumerateArray())
        {
            if (fixture.GetProperty("tokenId").GetString() != tokenId) continue;
            return catalog.Resolve(new GenesisSnapshot(data.GetProperty("chainId").GetInt32(),
                data.GetProperty("collection").GetString(), tokenId, fixture.GetProperty("metadataId").GetInt32(),
                fixture.GetProperty("metadataUri").GetString(),
                File.ReadAllText(Path.Combine(directory, fixture.GetProperty("file").GetString())),
                fixture.GetProperty("sha256").GetString()));
        }
        throw new InvalidOperationException("Fixture not found");
    }

    private static GenesisCombatLoadout Loadout(HeroDefinition hero, long id, long signature)
    {
        return new GenesisCombatLoadout {
            Identifier = new AssetObjectIdentifier { Guid = new AssetGuid(id), Path = "test/" + id },
            RulesetVersion = hero.RulesetVersion, CatalogHash = hero.CatalogHash,
            ArchetypeId = hero.ArchetypeId, BasicAttackId = hero.BasicAttackId, ClassAbilityId = hero.ClassAbilityId,
            SignatureShapeId = hero.SignatureShapeId, SignatureEffectId = hero.SignatureEffectId,
            TierExpressionId = hero.TierExpressionId, UniqueId = hero.UniqueId,
            CharacterPrototype = new AssetRef<EntityPrototype> { Id = new AssetGuid(2001) },
            BasicSkill = new AssetRef<SkillData> { Id = new AssetGuid(4001) },
            SignatureSkill = new AssetRef<SkillData> { Id = new AssetGuid(signature) }
        };
    }

    // The real content/genesis-roster.json, with test characters for its three first roles.
    private static GenesisRoster Roster()
    {
        using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "content/genesis-roster.json")));
        var data = file.RootElement;
        string[] Ids(string name) => data.GetProperty(name).EnumerateArray().Select(x => x.GetString()).ToArray();
        byte Position(JsonElement hero, string name) => checked((byte)hero.GetProperty(name).GetInt32());
        var roles = Ids("roles");
        return new GenesisRoster {
            Identifier = new AssetObjectIdentifier { Guid = new AssetGuid(7001), Path = "test/roster" },
            RulesetVersion = data.GetProperty("rulesetVersion").GetString(),
            CatalogHash = data.GetProperty("catalogHash").GetString(),
            RoleIds = roles, ShapeIds = Ids("shapes"), EffectIds = Ids("effects"),
            ExpressionIds = Ids("expressions"), TierNames = Ids("tiers"),
            RolePrototypes = roles.Select((_, i) => new AssetRef<EntityPrototype> { Id = new AssetGuid(2001 + i) }).ToArray(),
            SignatureSkills = Enumerable.Range(0, GenesisRoster.FormCount * Ids("expressions").Length * Ids("shapes").Length * Ids("effects").Length)
                .Select(i => new AssetRef<SkillData> { Id = new AssetGuid(6001 + i) }).ToArray(),
            SecondFormNames = Ids("shapes").Select(id => "second " + id).ToArray(),
            SecondFormSummaries = Ids("shapes").Select(id => "about second " + id).ToArray(),
            Entries = data.GetProperty("heroes").EnumerateArray().Select(hero => new GenesisRosterEntry {
                TokenId = hero.GetProperty("tokenId").GetInt32(), MetadataId = hero.GetProperty("metadataId").GetInt32(),
                Name = hero.GetProperty("name").GetString(), Role = Position(hero, "role"), Shape = Position(hero, "shape"),
                Effect = Position(hero, "effect"), Expression = Position(hero, "expression"), Tier = Position(hero, "tier"),
                Body = Position(hero, "body"), Eye = Position(hero, "eye"), Aura = Position(hero, "aura")
            }).ToArray(),
            BodyValues = Ids("bodies"), EyeValues = Ids("eyes"), AuraValues = Ids("auras"),
            // The sentences are authored in the Unity importer; any text shows they are carried.
            RoleSummaries = roles.Select(id => "about " + id).ToArray(),
            ShapeSummaries = Ids("shapes").Select(id => "about " + id).ToArray(),
            EffectSummaries = Ids("effects").Select(id => "about " + id).ToArray(),
            DeliverySummaries = Ids("expressions").Select(id => "about " + id).ToArray()
        };
    }

    private static RuntimeConfig MatchWith(GenesisRoster roster)
    {
        return new RuntimeConfig { GenesisRoster = new AssetRef<GenesisRoster> { Id = roster.Guid } };
    }

    private static void CastTest(GenesisRoster roster)
    {
        InFrame(new AssetObject[] { roster }, MatchWith(roster), frame => {
            Require(GenesisCast.TryGetPlayerHero(frame, new RuntimePlayer { GenesisTokenId = 1795 }, out var mine, out var prototype),
                "The declared Genesis was not accepted");
            Require(mine.MetadataId == 1287 && prototype.Id == roster.RolePrototypes[mine.Role].Id, "Wrong hero or class");
            Require(!GenesisCast.TryGetPlayerHero(frame, new RuntimePlayer(), out _, out _), "A player without Genesis got one");
            Require(!GenesisCast.TryGetPlayerHero(frame, new RuntimePlayer { GenesisTokenId = 999999 }, out _, out _),
                "A token outside the roster entered the match");

            var character = frame.Create();
            GenesisCast.Tag(frame, character, mine);
            Require(frame.Get<GenesisHero>(character).TokenId == 1795 && frame.Get<GenesisHero>(character).Role == mine.Role,
                "The character does not carry its Genesis");
            Require(GenesisCast.DisplayName(frame, character) == "CHOG #1287", "Wrong displayed name");
            Require(GenesisCast.DisplayName(frame, frame.Create()) == null, "A plain character got a Genesis name");

            var inMatch = new HashSet<int> { 1795 };
            for (int i = 0; i < 5; ++i) {
                Require(GenesisCast.TryDrawBotHero(frame, out var bot, out var botPrototype), "No bot Genesis drawn");
                Require(inMatch.Add(bot.TokenId), "A Genesis is in the match twice");
                Require(botPrototype.Id == roster.RolePrototypes[bot.Role].Id, "A bot plays another class than its Genesis");
                GenesisCast.Tag(frame, frame.Create(), bot);
            }
        });
        // Without a roster the match keeps the sample cast.
        InFrame(Array.Empty<AssetObject>(), new RuntimeConfig(), frame => {
            Require(!GenesisCast.TryGetPlayerHero(frame, new RuntimePlayer { GenesisTokenId = 1795 }, out _, out _)
                && !GenesisCast.TryDrawBotHero(frame, out _, out _), "A match without roster produced a Genesis");
        });
    }

    private static void FrameTest(GenesisCombatLoadout direct, GenesisCombatLoadout echo)
    {
        InFrame(Array.Empty<AssetObject>(), new RuntimeConfig(), frame => {
            var a = frame.Create(); var b = frame.Create();
            frame.Add<CharacterAttacks>(a); frame.Add<CharacterAttacks>(b);
            GenesisLoadoutSelection.Apply(frame, a, direct);
            GenesisLoadoutSelection.Apply(frame, b, echo);
            Require(frame.Get<CharacterAttacks>(a).SpecialSkillData.Id == direct.SignatureSkill.Id, "Direct skill not applied");
            Require(frame.Get<CharacterAttacks>(b).SpecialSkillData.Id == echo.SignatureSkill.Id, "Echo skill not applied");
            Require(frame.Get<CharacterAttacks>(a).BasicSkillData.Id == direct.BasicSkill.Id, "Basic skill not applied");
        });
    }

    private static bool engineReady;

    // Runs the body in a real Quantum Frame that can find the given assets.
    private static unsafe void InFrame(AssetObject[] assets, RuntimeConfig config, Action<Frame> body)
    {
        if (!engineReady) {
            FPLut.Init(Path.Combine(root, "game/Assets/Photon/Quantum/Resources/LUT"));
            DotNetRunnerFactory.Init();
            engineReady = true;
        }
        var simulation = new SimulationConfig {
            Physics = new PhysicsCommon.Config(), Navigation = new Navigation.Config(),
            Entities = new Quantum.Core.FrameBase.EntitiesConfig()
        };
        // Unity normally imports these 32 layer slots into SimulationConfig.Reset().
        simulation.Physics.Layers = new string[32];
        simulation.Physics.LayerMatrix = new int[32];
        for (int i = 0; i < 32; ++i) {
            simulation.Physics.Layers[i] = "TestLayer" + i;
            simulation.Physics.LayerMatrix[i] = -1;
        }
        var platform = DotNetRunnerFactory.CreatePlatformInfo();
        platform.CoreCount = 1;
        using var resources = new ResourceManagerStatic(assets, DotNetRunnerFactory.CreateNativeAllocator());
        using var context = new FrameContextUser(new Quantum.Core.FrameContext.Args {
            PlatformInfo = platform,
            HeapConfig = new Heap.Config(15, 64, 1), ResourceManager = resources,
            PhysicsConfig = simulation.Physics, NavigationConfig = simulation.Navigation,
            IsLocalPlayer = player => true
        });
        var frame = new Frame(context, Array.Empty<SystemBase>(), Array.Empty<SystemBase>(),
            new DeterministicSessionConfig { PlayerCount = 6, UpdateFPS = 60 },
            config, simulation, FP._1 / 60);
        try {
            *frame.RNG = new RNGSession(20261009);
            body(frame);
        }
        finally { frame.Free(); }
    }

    private static void Test(string name, Action action)
    {
        try { action(); ++passed; results.Add("PASS " + name); Console.WriteLine("PASS " + name); }
        catch (Exception e) { ++failed; results.Add("FAIL " + name + ": " + e); Console.WriteLine("FAIL " + name + ": " + e); }
    }
    private static void Rejected(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("Expected profile rejection");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
