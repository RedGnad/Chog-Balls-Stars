using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEngine;

namespace Chog.Editor
{
    /// <summary>
    /// Authors the special attack of every pair of signature shape (from the Eyes) and effect
    /// (from the Aura) as data assets under "Assets/QuantumUser/Resources/Genesis", plus the one
    /// attack prefab the area shapes share. A shape sets where the attack lands and its damage;
    /// an effect takes a share of that damage and gives something else in exchange, in one
    /// colour that is used for the attack, for the burst on the target and for what stays on it.
    /// Every number here is a first pass: no kit budget has been computed and the delivery of
    /// each Tier is not applied yet.
    /// </summary>
    public static class GenesisSignatureImporter
    {
        // A shape made of one area that hits each target once (GenesisSweepAttackData).
        private sealed class Sweep
        {
            public string Shape;
            // For a second form only: the name the player reads for it.
            public string FormName;
            // One sentence for the hero card; keep it true to the numbers below.
            public string Summary;
            // Effect prefab shown while the area lives, and the scale that fits it to the area.
            public string Visual;
            public float VisualScale;
            public float Damage;
            public float Radius;
            public float Velocity;
            public float Lifetime;
            public float ForwardOffset;
            public bool StopAtWalls;
            // Drawn as a shape in the colours of the caster's eyes instead of an effect prefab.
            public GenesisVisualKind Kind;

            // Farthest point the area can touch, measured from the caster.
            public float Reach => ForwardOffset + Velocity * Lifetime + Radius;
        }

        // What an Aura does to the signature.
        private sealed class Aura
        {
            public string Effect;
            // One sentence for the hero card; keep it true to the numbers below.
            public string Summary;
            // Share of the shape's damage dealt at the moment of the hit.
            public float Impact = 1f;
            // What else each target receives, and what the caster receives per target touched.
            public Effect[] OnTarget = new Effect[0];
            public Effect[] OnCaster = new Effect[0];
            // How long the view keeps the effect on the target.
            public float Duration;
            // Transparent leaves the attack's own colours.
            public Color Color = Color.clear;
            // The sign drawn on a target this effect touches.
            public GenesisEffectIcon Icon;
        }

        // How the Tier of a Genesis delivers its signature: how many times it fires, how far
        // apart, and the share of the damage each time, so that a rarer delivery is another way to
        // spend the same strength and not more of it. A delivery absent from the table below has
        // no behaviour of its own yet and plays as "direct".
        private sealed class Delivery
        {
            public string Expression;
            public string Summary;
            public int Hits = 1;
            public float Interval;
            public float Share = 1f;
            // A return: the share of the damage dealt by the strike that comes back; 0 for none.
            public float ReturnShare;
            // A persistent zone: what the zone left behind deals at each of its ticks; 0 for none.
            public float ZoneDamagePerTick;
            // The inversion, the super attack of a 1:1 (22 October 2026): this many more copies leave at once,
            // turned SpreadDegrees apart; a shape that does not aim (the nova) or a lobbed one (the puddle)
            // strikes that many more times instead, SpreadInterval apart. 0 for the other deliveries.
            public int Spread;
            public float SpreadDegrees;
            public float SpreadInterval;
        }

        private const string NotBuiltSummary = "not built yet, plays like a direct hit";

        // A strike that comes back leaves this long after the first one has ended; one that does
        // not travel strikes again after the second delay.
        private const float ReturnPause = 0.1f;
        private const float StandingReturnDelay = 0.8f;

        // A zone left behind is at least this wide, and green when its aura has no colour.
        private const float MinZoneRadius = 2.2f;
        private static readonly Color RadioactiveGreen = new Color(0.55f, 0.95f, 0.2f);

        private static readonly Delivery[] Deliveries =
        {
            new Delivery { Expression = "direct", Summary = "one hit at full strength" },
            new Delivery { Expression = "echo", Summary = "fires twice, half a second apart, 60% each",
                           Hits = 2, Interval = 0.5f, Share = 0.6f },
            // The first strike is weaker; the second one depends on where the caster stands by then.
            new Delivery { Expression = "return", Summary = "goes out at 60%, then comes back to you at 80%",
                           Share = 0.6f, ReturnShare = 0.8f },
            // The damage is spread over time, and a target can walk out of the zone.
            new Delivery { Expression = "persistent-zone", Summary = "hits at 50%, then leaves a zone for 3 s",
                           Share = 0.5f, ZoneDamagePerTick = 500f },
            // The ten 1:1 Genesis are the rarest: their signature is a super attack, three times at full
            // strength, in three directions when it aims.
            new Delivery { Expression = "inversion", Summary = "a super attack: three at once at full strength, in three directions",
                           Spread = 2, SpreadDegrees = 120f, SpreadInterval = 0.35f }
        };

        /// <summary>What the roster needs from this importer, parallel to its id tables.</summary>
        public sealed class Result
        {
            public AssetRef<SkillData>[] Skills;
            public string[] ShapeSummaries;
            public string[] EffectSummaries;
            public string[] DeliverySummaries;
            // Name and sentence of the second form of each shape, parallel to the shape ids.
            public string[] SecondFormNames;
            public string[] SecondFormSummaries;
            public GenesisEffectIcon[] Icons;
            public Color[] Colors;
            public int Authored;
            public int AuthoredSecondForms;
        }

        private const string Folder = "Assets/QuantumUser/Resources/Genesis";
        private const string Sample = "Assets/QuantumUser/Resources/";

        // The one attack prefab every sweep uses: its view shows the effect named by the data.
        private const string AttackPrefab = Folder + "/GenesisAttack.prefab";
        private const string AttackPrefabTemplate = Sample + "KnightSpecialAttack.prefab";


        // Name of the unlit vertex-coloured material every drawn shape uses, created once next to the data.
        private const string ShapeMaterialName = "GenesisShape";

        // The eyes as the collection draws them, by the value of the Eyes trait: a dark outline,
        // the main colour, a lighter core, and for eyes whose two sides differ the other side.
        // Read from the Genesis images; a value not listed here has no colours of its own yet.
        // The black line of the art, and the eye colours of the coloured skeleton variants. Declared
        // before the table below, which reads them while it is built.
        private static readonly Color Ink = new Color(0.08f, 0.08f, 0.1f);
        private static readonly Color VariantGreen = new Color(0.12f, 0.5f, 0.18f);
        private static readonly Color VariantBlue = new Color(0.15f, 0.3f, 0.8f);
        private static readonly Color VariantRed = new Color(0.8f, 0.12f, 0.15f);
        private static readonly Color VariantMixed = new Color(0.5f, 0.2f, 0.65f);

        private static readonly Dictionary<string, GenesisEyeLook> EyeLooks = new Dictionary<string, GenesisEyeLook>
        {
            // Visors: a band of colour across the eyes, bright in its middle.
            { "Plasma visor", Look(new Color(0.05f, 0.1f, 0.4f), new Color(0.1f, 0.4f, 1f), new Color(0.75f, 1f, 0.2f)) },
            { "Gradient visor", Look(new Color(0.15f, 0.15f, 0.5f), new Color(0.3f, 0.35f, 0.9f), new Color(1f, 0.72f, 0.66f)) },
            { "Sunset visor", Look(new Color(0.25f, 0.02f, 0.02f), new Color(0.5f, 0.05f, 0.05f), new Color(0.98f, 0.2f, 0.12f)) },
            // Headsets and glasses: the colour of the frame or the glow, and of the lens.
            { "Classic VR", Look(new Color(0.08f, 0.08f, 0.1f), new Color(0.55f, 0.57f, 0.6f), Color.white) },
            { "Modern VR", Look(new Color(0.03f, 0.05f, 0.15f), new Color(0.1f, 0.6f, 1f), new Color(0.6f, 0.95f, 1f)) },
            { "Anime Glass", Look(new Color(0.1f, 0.35f, 0.35f), new Color(0.45f, 0.95f, 0.9f), Color.white) },
            { "Retro Glass", Look(new Color(0.45f, 0.05f, 0.08f), new Color(0.9f, 0.15f, 0.2f), new Color(0.6f, 0.82f, 0.95f)) },
            { "3D Glass", Look(new Color(0.2f, 0.2f, 0.25f), new Color(0.1f, 0.2f, 0.95f), Color.white, new Color(0.95f, 0.15f, 0.1f)) },
            { "BTC Glass", Look(new Color(0.55f, 0.28f, 0.02f), new Color(0.97f, 0.58f, 0.1f), new Color(1f, 0.85f, 0.35f)) },
            // Tears: pale blue with a darker blue line.
            { "Round Crying", Tears() },
            { "Round Tear", Tears() },
            { "Teary", Tears() },
            { "Closed Eye Crying", Tears() },
            // Lasers: two straight beams, one per eye.
            { "Green laser", Look(new Color(0.05f, 0.35f, 0.08f), new Color(0.24f, 0.84f, 0.24f), new Color(0.62f, 1f, 0.55f)) },
            { "Purple laser", Look(new Color(0.35f, 0.05f, 0.45f), new Color(0.8f, 0.25f, 0.95f), new Color(1f, 0.7f, 1f)) },
            { "fire laser", Look(new Color(0.85f, 0.08f, 0.05f), new Color(1f, 0.45f, 0.05f), new Color(1f, 0.92f, 0.25f)) },
            { "cyan laser", Look(new Color(0.05f, 0.25f, 0.6f), new Color(0.18f, 0.82f, 0.95f), new Color(0.75f, 1f, 1f)) },

            // Expressions: the eye itself is the attack (stare and burst), drawn as the art draws it.
            // Hard looks, which stare.
            { "Side Eye", Eyes(GenesisEyeGlyph.BallLidded, Ink) },
            { "Green Side Eye", Eyes(GenesisEyeGlyph.BallLidded, new Color(0.1f, 0.35f, 0.12f)) },
            { "Mixed Side Eye", Eyes(GenesisEyeGlyph.BallLidded, new Color(0.3f, 0.1f, 0.4f)) },
            { "Dead", Eyes(GenesisEyeGlyph.Cross, Ink) },
            { "Angry", Eyes(GenesisEyeGlyph.BallAngry, Ink) },
            { "Vertical", Eyes(GenesisEyeGlyph.RingSide, Ink) },
            { "Frog", Eyes(GenesisEyeGlyph.RingSide, Ink) },
            { "Smirk", Eyes(GenesisEyeGlyph.Ball, Ink) },
            { "Green Smirk", Eyes(GenesisEyeGlyph.Ball, VariantGreen) },
            { "Blue Smirk", Eyes(GenesisEyeGlyph.Ball, VariantBlue) },
            { "Up", Eyes(GenesisEyeGlyph.Ball, Ink) },
            // Clown make-up: a diamond above and below the eye.
            { "Clown Eye", Eyes(GenesisEyeGlyph.Ball, Ink, new Color(0.15f, 0.3f, 0.85f)) },
            { "Clown Eye Mix", Eyes(GenesisEyeGlyph.Ball, Ink, new Color(0.9f, 0.15f, 0.15f)) },
            { "Retard", Eyes(GenesisEyeGlyph.RingDerp, Ink) },
            { "Green Retard", Eyes(GenesisEyeGlyph.RingDerp, VariantGreen) },
            { "Blue Retard", Eyes(GenesisEyeGlyph.RingDerp, VariantBlue) },
            { "Red Retard", Eyes(GenesisEyeGlyph.RingDerp, VariantRed) },
            { "Mixed Retard", Eyes(GenesisEyeGlyph.RingDerp, VariantMixed) },
            // Ski goggles and pixel sunglasses: one object across both eyes.
            { "MOG", Band(new Color(0.98f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.2f), new Color(0.2f, 0.75f, 0.9f)) },
            { "Chog Viper", Band(new Color(0.1f, 0.35f, 0.85f), new Color(0.35f, 0.85f, 0.95f), new Color(0.1f, 0.2f, 0.6f)) },
            { "Thug", Eyes(GenesisEyeGlyph.Shades, Ink) },
            // Soft looks, which burst around the hero.
            { "Happy", Eyes(GenesisEyeGlyph.Arc, Ink) },
            { "Green Happy", Eyes(GenesisEyeGlyph.Arc, VariantGreen) },
            { "Blue Happy", Eyes(GenesisEyeGlyph.Arc, VariantBlue) },
            { "Mixed Happy", Eyes(GenesisEyeGlyph.Arc, VariantMixed) },
            { "Round", Eyes(GenesisEyeGlyph.Ring, Ink) },
            { "Round Eye", Eyes(GenesisEyeGlyph.Ring, Ink) },
            { "Green Round Eye", Eyes(GenesisEyeGlyph.Ring, VariantGreen) },
            { "Blue Round Eye", Eyes(GenesisEyeGlyph.Ring, VariantBlue) },
            { "Red Round Eye", Eyes(GenesisEyeGlyph.Ring, VariantRed) },
            { "Mixed Round Eye", Eyes(GenesisEyeGlyph.Ring, VariantMixed) },
            { "Normal Eye", Eyes(GenesisEyeGlyph.Ball, Ink) },
            { "Normal", Eyes(GenesisEyeGlyph.Ball, Ink) },
            { "Base", Eyes(GenesisEyeGlyph.Ball, Ink) },
            { "Worried", Eyes(GenesisEyeGlyph.Ball, Ink) },
            { "Heart", Eyes(GenesisEyeGlyph.Heart, new Color(0.9f, 0.12f, 0.15f)) },
            { "Tired", Eyes(GenesisEyeGlyph.BallLidded, Ink) },
            { "Flat", Eyes(GenesisEyeGlyph.Dash, Ink) },
            { "Red Flat", Eyes(GenesisEyeGlyph.Dash, VariantRed) },
            { "Blue Flat", Eyes(GenesisEyeGlyph.Dash, VariantBlue) },
            { "XD", Eyes(GenesisEyeGlyph.Chevron, Ink) }
        };

        // The ten 1:1 Genesis carry no Eyes and no Skin value: each gets its own colours, read
        // from its image. Keyed by canonical token id. A shape drawn with eyes (wave, nova) needs
        // a drawing of the eye; a beam, a fan or a puddle needs three colours.
        private static readonly GenesisUniqueLook[] Uniques =
        {
            // Blaze: a creature of fire with two glowing eyes.
            Unique(561, Eyes(GenesisEyeGlyph.Ball, new Color(1f, 0.9f, 0.3f)), new Color(0.98f, 0.6f, 0.1f)),
            // Burning Skully: a skull, black sockets.
            Unique(900, Eyes(GenesisEyeGlyph.Ball, Ink), new Color(0.95f, 0.93f, 0.85f)),
            // Chog Scream: the painted face, small round eyes.
            Unique(942, Eyes(GenesisEyeGlyph.Ring, new Color(0.35f, 0.25f, 0.3f)), new Color(0.93f, 0.75f, 0.5f)),
            // Chogbomination: a zombie, black eyes with a green pupil.
            Unique(1002, new GenesisEyeLook { Outline = Ink, Outer = Ink, Core = new Color(0.3f, 0.9f, 0.2f), Glyph = GenesisEyeGlyph.Ring },
                   new Color(0.85f, 0.9f, 0.8f)),
            // Frogger: a frog costume; its puddle is pond green.
            Unique(1481, Look(new Color(0.15f, 0.4f, 0.1f), new Color(0.45f, 0.75f, 0.2f), new Color(0.8f, 0.95f, 0.5f)),
                   new Color(0.5f, 0.78f, 0.2f)),
            // Grim: drawn in black and white; its laser is grey.
            Unique(1514, Look(Ink, new Color(0.6f, 0.6f, 0.62f), Color.white), new Color(0.75f, 0.75f, 0.78f)),
            // Halo: an angel with a heavy-lidded look.
            Unique(1667, Eyes(GenesisEyeGlyph.BallLidded, Ink), new Color(0.97f, 0.9f, 0.75f)),
            // Krown: a king with pale blue eyes.
            Unique(1798, new GenesisEyeLook { Outline = Ink, Outer = new Color(0.2f, 0.3f, 0.6f), Core = new Color(0.78f, 0.88f, 1f), Glyph = GenesisEyeGlyph.Ring },
                   new Color(0.5f, 0.3f, 0.7f)),
            // Midas: all gold; its fan of light is gold.
            Unique(1881, Look(new Color(0.6f, 0.35f, 0.05f), new Color(1f, 0.75f, 0.1f), new Color(1f, 0.95f, 0.6f)),
                   new Color(1f, 0.78f, 0.15f)),
            // Redd: a devil with glowing red eyes; its laser is red.
            Unique(1884, Look(new Color(0.45f, 0.02f, 0.05f), new Color(0.9f, 0.1f, 0.12f), new Color(1f, 0.6f, 0.55f)),
                   new Color(0.9f, 0.3f, 0.35f))
        };

        // The burst of every Radioactive Genesis (22 October 2026): the sign of radioactivity, yellow-green on a
        // dark rim, on discs of the glow of radiation, whatever its eyes.
        public static GenesisEyeLook RadioactiveLook() => new GenesisEyeLook
        {
            Outline = Ink, Outer = new Color(0.10f, 0.12f, 0.06f), Core = new Color(0.85f, 1f, 0.25f), Glyph = GenesisEyeGlyph.Trefoil
        };
        public static readonly Color RadioactiveSkin = new Color(0.55f, 0.95f, 0.2f);

        /// <summary>The looks authored for the 1:1 Genesis, by token.</summary>
        public static GenesisUniqueLook[] UniqueLooks() => (GenesisUniqueLook[])Uniques.Clone();

        private static GenesisUniqueLook Unique(int tokenId, GenesisEyeLook look, Color skin)
        {
            return new GenesisUniqueLook { TokenId = tokenId, Look = look, Skin = skin };
        }

        private const string PuddleShape = "puddle";
        private const string PuddleSummary = "a lobbed shot that leaves a damaging zone for 3 s";
        private const float PuddleDamagePerTick = 900f;

        private static readonly Sweep[] Sweeps =
        {
            // Fast enough to read as a laser, slow enough that no enemy slips between two ticks.
            new Sweep { Shape = "beam", Summary = "a laser from the eyes that pierces every enemy on its line", Kind = GenesisVisualKind.Beam,
                        Damage = 2200f, Radius = 0.35f, Velocity = 33f, Lifetime = 0.34f, ForwardOffset = 0.5f,
                        StopAtWalls = true },
            new Sweep { Shape = "wave", Summary = "a stare that rolls forward as a wide front, even over walls", Kind = GenesisVisualKind.Stare,
                        Damage = 1800f, Radius = 1.5f, Velocity = 9f, Lifetime = 0.75f, ForwardOffset = 1f },
            new Sweep { Shape = "flash", Summary = "a fan of light from the eyes, right in front of you", Kind = GenesisVisualKind.Fan,
                        Damage = 2600f, Radius = 1.7f, Velocity = 0f, Lifetime = 0.35f, ForwardOffset = 2f },
            new Sweep { Shape = "nova", Summary = "a burst of looks all around you", Kind = GenesisVisualKind.EyeBurst,
                        Damage = 2000f, Radius = 3f, Velocity = 0f, Lifetime = 0.4f, ForwardOffset = 0f }
        };

        // The second form of each shape: another way to throw the same attack, wider or longer at
        // the price of damage or reach, never plainly better. A player equips it once the mastery
        // of the Genesis unlocked it; the first form stays the one the eyes draw.
        private static readonly Sweep[] SecondSweeps =
        {
            // More than twice as wide and a fifth shorter: easier to land, weaker.
            new Sweep { Shape = "beam", FormName = "broad beam", Summary = "a thick laser: easier to land, shorter and weaker", Kind = GenesisVisualKind.Beam,
                        Damage = 1700f, Radius = 0.9f, Velocity = 24f, Lifetime = 0.34f, ForwardOffset = 0.5f,
                        StopAtWalls = true },
            // Narrower and faster: it goes a third further and hits less.
            new Sweep { Shape = "wave", FormName = "long stare", Summary = "a narrow stare that travels a third further, even over walls", Kind = GenesisVisualKind.Stare,
                        Damage = 1500f, Radius = 1f, Velocity = 14f, Lifetime = 0.75f, ForwardOffset = 1f },
            new Sweep { Shape = "flash", FormName = "wide flash", Summary = "a larger fan of light that reaches further and hits less", Kind = GenesisVisualKind.Fan,
                        Damage = 2000f, Radius = 2.5f, Velocity = 0f, Lifetime = 0.35f, ForwardOffset = 2.6f },
            new Sweep { Shape = "nova", FormName = "tight nova", Summary = "a small burst right around you that hits much harder", Kind = GenesisVisualKind.EyeBurst,
                        Damage = 2800f, Radius = 1.9f, Velocity = 0f, Lifetime = 0.4f, ForwardOffset = 0f }
        };

        // The second form of the puddle: a wider zone that hurts less at each tick.
        private const string SecondPuddleName = "wide puddle";
        private const string SecondPuddleSummary = "a lobbed shot that leaves a wider zone for 3 s, weaker at each tick";
        private const float SecondPuddleDamagePerTick = 650f;
        private const float SecondPuddleRadiusScale = 1.35f;

        // What the name of an asset of the second form ends with.
        private const string SecondFormSuffix = "Second";

        private static readonly Aura[] Auras =
        {
            new Aura { Effect = "raw", Icon = GenesisEffectIcon.Burst, Summary = "full damage, nothing else" },
            // Fire that keeps burning: less at once, 900 more over three seconds.
            new Aura { Effect = "burn", Icon = GenesisEffectIcon.Flame, Summary = "less damage at once, then burns for 3 s", Impact = 0.7f, Duration = 3f, Color = new Color(1f, 0.45f, 0.1f),
                       OnTarget = new[] { Timed(EAttributeType.Health, EModifierAppliance.Continuous, EModifierOperation.Subtract, 300f, 3f) } },
            // Cold that slows the target down.
            new Aura { Effect = "chill", Icon = GenesisEffectIcon.Snowflake, Summary = "slows the target for 2.5 s", Impact = 0.85f, Duration = 2.5f, Color = new Color(0.35f, 0.75f, 1f),
                       OnTarget = new[] { Timed(EAttributeType.Speed, EModifierAppliance.TemporaryGreatestOnly, EModifierOperation.Subtract, 1.5f, 2.5f) } },
            // A curse that roots the target for a moment. Capped short on purpose.
            new Aura { Effect = "hex", Icon = GenesisEffectIcon.Star, Summary = "roots the target for 1.2 s", Impact = 0.75f, Duration = 1.2f, Color = new Color(0.7f, 0.3f, 1f),
                       OnTarget = new[] { Timed(EAttributeType.Stun, EModifierAppliance.Temporary, EModifierOperation.Add, 1f, 1.2f) } },
            // Heals the caster for each enemy touched.
            new Aura { Effect = "mend", Icon = GenesisEffectIcon.Plus, Summary = "heals you for each enemy hit", Impact = 0.8f, Color = new Color(1f, 0.5f, 0.75f),
                       OnCaster = new[] { Instant(EAttributeType.Health, EModifierOperation.Add, 700f) } },
            // Wipes the energy the target attacks with.
            new Aura { Effect = "purify", Icon = GenesisEffectIcon.Ring, Summary = "drains the target's attack energy", Impact = 0.9f, Color = Color.white,
                       OnTarget = new[] { Instant(EAttributeType.Energy, EModifierOperation.Subtract, 2f) } },
            // The caster hits harder for four seconds.
            new Aura { Effect = "rally", Icon = GenesisEffectIcon.Arrow, Summary = "your attacks hit 35% harder for 4 s", Impact = 0.85f, Color = new Color(0.25f, 0.4f, 1f),
                       OnCaster = new[] { Timed(EAttributeType.Power, EModifierAppliance.Temporary, EModifierOperation.Add, 0.35f, 4f) } },
            // Gives the caster its attack energy back.
            new Aura { Effect = "haste", Icon = GenesisEffectIcon.Bolt, Summary = "refills your attack energy", Impact = 0.9f, Color = new Color(1f, 0.9f, 0.2f),
                       OnCaster = new[] { Instant(EAttributeType.Energy, EModifierOperation.Add, 2f) } },
            // Takes half of the special the target was charging.
            new Aura { Effect = "veil", Icon = GenesisEffectIcon.Cloud, Summary = "removes half of the target's special charge", Impact = 0.9f, Color = new Color(0.55f, 0.55f, 0.6f),
                       OnTarget = new[] { Instant(EAttributeType.Special, EModifierOperation.Subtract, 0.5f) } },
            // A weak hit and a long poison: 1100 over five seconds.
            new Aura { Effect = "toxin", Icon = GenesisEffectIcon.Bubbles, Summary = "a weak hit, then poison for 5 s", Impact = 0.5f, Duration = 5f, Color = new Color(0.4f, 0.9f, 0.2f),
                       OnTarget = new[] { Timed(EAttributeType.Health, EModifierAppliance.Continuous, EModifierOperation.Subtract, 220f, 5f) } }
        };

        // Assets written by the first version of this importer, one per shape. Removed so the
        // folder only holds what the roster references.
        private static readonly string[] Superseded =
        {
            "GenesisBeamAttackData", "GenesisBeamSkillData", "GenesisWaveAttackData", "GenesisWaveSkillData",
            "GenesisFlashAttackData", "GenesisFlashSkillData", "GenesisNovaAttackData", "GenesisNovaSkillData",
            "GenesisPuddleZoneAttackData", "GenesisPuddleLobAttackData", "GenesisPuddleSkillData"
        };

        /// <summary>
        /// Creates or updates the signature assets. Skills are returned at position
        /// ((form * expressionIds.Length + expression) * shapeIds.Length + shape) * effectIds.Length
        /// + effect; a combination this importer does not know stays invalid.
        /// </summary>
        public static Result Import(string[] shapeIds, string[] effectIds, string[] expressionIds)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/QuantumUser/Resources", "Genesis");

            foreach (var name in Superseded)
                AssetDatabase.DeleteAsset(Folder + "/" + name + ".asset");

            var result = new Result
            {
                Skills = new AssetRef<SkillData>[GenesisRoster.FormCount * expressionIds.Length * shapeIds.Length * effectIds.Length],
                ShapeSummaries = new string[shapeIds.Length],
                EffectSummaries = new string[effectIds.Length],
                DeliverySummaries = new string[expressionIds.Length],
                SecondFormNames = new string[shapeIds.Length],
                SecondFormSummaries = new string[shapeIds.Length],
                Icons = new GenesisEffectIcon[effectIds.Length],
                Colors = new Color[effectIds.Length]
            };
            var written = new List<AssetObject>();
            var skills = new Dictionary<string, SkillData>();

            var plainSkill = Load<KnightSpecialSkillData>("KnightSpecialSkillData.asset");
            var plainAttack = Load<KnightSpecialAttackData>("KnightSpecialAttackData.asset");
            var sampleZone = Load<ArcherSpecialSecondAttackData>("ArcherSpecialSecondAttackData.asset");
            var attackPrototype = EnsureAttackPrefab();
            var shapeMaterial = UnlitMaterial(ShapeMaterialName);
            AssetDatabase.DeleteAsset(Folder + "/GenesisBeam.mat");

            for (var i = 0; i < expressionIds.Length; i++)
                result.DeliverySummaries[i] = NotBuiltSummary;

            foreach (var delivery in Deliveries)
            foreach (var aura in Auras)
            {
                var effect = Array.IndexOf(effectIds, aura.Effect);
                if (effect < 0)
                    continue;

                SetSummary(result.DeliverySummaries, expressionIds, delivery.Expression, delivery.Summary);
                result.EffectSummaries[effect] = aura.Summary;
                result.Icons[effect] = aura.Icon;
                result.Colors[effect] = aura.Color;

                // The direct signatures keep the plain names they had before deliveries existed, and
                // the first form the names it had before second forms existed.
                var deliverySuffix = delivery.Expression == "direct" ? "" : Capitalised(delivery.Expression);
                for (var form = 0; form < GenesisRoster.FormCount; form++)
                foreach (var sweep in form == 0 ? Sweeps : SecondSweeps)
                {
                    var suffix = deliverySuffix + (form == 0 ? "" : SecondFormSuffix);
                    var name = "Genesis" + Capitalised(sweep.Shape) + Capitalised(aura.Effect) + suffix;

                    var attack = LoadOrCreate<GenesisSweepAttackData>(name + "AttackData", written);
                    ConfigureSweep(attack, sweep, aura, effect, sweep.Damage * delivery.Share, plainAttack.SFX, shapeMaterial);

                    // A persistent zone: where the area ends, a zone stays and keeps hurting.
                    attack.LeavesBehindPrototype = default;
                    attack.LeavesBehindData = default;
                    if (delivery.ZoneDamagePerTick > 0f)
                    {
                        var zone = LoadOrCreate<ArcherSpecialSecondAttackData>(name + "ZoneAttackData", written);
                        CopyAttack(sampleZone, zone);
                        zone.EffectInterval = sampleZone.EffectInterval;
                        zone.EffectRepeatCount = sampleZone.EffectRepeatCount;
                        zone.Shape = JsonUtility.FromJson<Shape2DConfig>(JsonUtility.ToJson(sampleZone.Shape));
                        zone.Shape.CircleRadius = Fixed(Mathf.Max(sweep.Radius, MinZoneRadius));
                        ApplyAura(zone, aura, effect, delivery.ZoneDamagePerTick);
                        zone.VisualKind = GenesisVisualKind.GroundDisc;
                        zone.ShapeMaterial = shapeMaterial;
                        zone.VisualRadius = zone.Shape.CircleRadius.AsFloat;
                        zone.VisualTint = aura.Color.a > 0f ? aura.Color : RadioactiveGreen;
                        EditorUtility.SetDirty(zone);

                        attack.LeavesBehindPrototype = attackPrototype;
                        attack.LeavesBehindData = new AssetRef<AttackData> { Id = GuidOf(zone) };
                    }

                    EditorUtility.SetDirty(attack);

                    var skill = LoadOrCreate<GenesisSignatureSkillData>(name + "SkillData", written);
                    CopySkill(plainSkill, skill);
                    Repeat(skill, delivery);
                    // The super attack: copies turned apart for a shape that aims, more strikes for the nova.
                    skill.Spread = 0;
                    skill.SpreadDegrees = FP._0;
                    if (delivery.Spread > 0)
                    {
                        if (sweep.Shape == "nova")
                            Repeat(skill, new Delivery { Hits = delivery.Spread + 1, Interval = delivery.SpreadInterval });
                        else
                        {
                            skill.Spread = delivery.Spread;
                            skill.SpreadDegrees = Fixed(delivery.SpreadDegrees);
                        }
                    }
                    skill.AttackPrototype = attackPrototype;
                    skill.AttackDataOverride = new AssetRef<AttackData> { Id = GuidOf(attack) };
                    skill.Reach = Fixed(sweep.Reach);

                    // A return: a second strike comes back to the caster. A shape that travels comes
                    // back from where it ended; a shape that stands is struck again where the caster is.
                    skill.ReturnAttackData = default;
                    skill.ReturnDistance = FP._0;
                    if (delivery.ReturnShare > 0f)
                    {
                        var travels = sweep.Velocity > 0f;
                        var distance = sweep.ForwardOffset + sweep.Velocity * sweep.Lifetime;
                        var back = LoadOrCreate<GenesisSweepAttackData>(name + "BackAttackData", written);
                        ConfigureSweep(back, sweep, aura, effect, sweep.Damage * delivery.ReturnShare, plainAttack.SFX, shapeMaterial);
                        if (travels)
                        {
                            // It starts at the far end and covers the same distance, over walls.
                            back.ForwardOffset = FP._0;
                            back.TTL = Fixed(distance / sweep.Velocity);
                            back.StopAtWalls = false;
                        }

                        EditorUtility.SetDirty(back);

                        skill.ReturnAttackData = new AssetRef<AttackData> { Id = GuidOf(back) };
                        skill.ReturnDistance = Fixed(travels ? distance : 0f);
                        skill.HasTTL = true;
                        skill.ActionAmount = 2;
                        skill.ActionInterval = Fixed(travels ? sweep.Lifetime + ReturnPause : StandingReturnDelay);
                    }

                    EditorUtility.SetDirty(skill);
                    skills[form + "/" + delivery.Expression + "/" + sweep.Shape + "/" + aura.Effect] = skill;
                }

                // No Genesis has both the puddle and a delivery other than these two.
                if (delivery.ReturnShare > 0f || delivery.ZoneDamagePerTick > 0f)
                    continue;

                // A lobbed puddle cannot turn: the super attack lobs it that many more times.
                var lobbed = delivery.Spread > 0
                    ? new Delivery { Expression = delivery.Expression, Hits = delivery.Spread + 1, Interval = delivery.SpreadInterval, Share = delivery.Share }
                    : delivery;
                for (var form = 0; form < GenesisRoster.FormCount; form++)
                {
                    skills[form + "/" + delivery.Expression + "/" + PuddleShape + "/" + aura.Effect] =
                        ImportPuddle(aura, effect, lobbed, deliverySuffix + (form == 0 ? "" : SecondFormSuffix), form == 1,
                            written, attackPrototype, shapeMaterial);
                }
            }

            foreach (var sweep in Sweeps)
                SetSummary(result.ShapeSummaries, shapeIds, sweep.Shape, sweep.Summary);
            SetSummary(result.ShapeSummaries, shapeIds, PuddleShape, PuddleSummary);
            foreach (var sweep in SecondSweeps)
            {
                SetSummary(result.SecondFormNames, shapeIds, sweep.Shape, sweep.FormName);
                SetSummary(result.SecondFormSummaries, shapeIds, sweep.Shape, sweep.Summary);
            }

            SetSummary(result.SecondFormNames, shapeIds, PuddleShape, SecondPuddleName);
            SetSummary(result.SecondFormSummaries, shapeIds, PuddleShape, SecondPuddleSummary);

            AssetDatabase.SaveAssets();

            var required = new List<AssetGuid> { attackPrototype.Id };
            foreach (var asset in written)
                required.Add(GuidOf(asset));
            EnsureRegistered(required);

            for (var form = 0; form < GenesisRoster.FormCount; form++)
            for (var expression = 0; expression < expressionIds.Length; expression++)
            {
                for (var shape = 0; shape < shapeIds.Length; shape++)
                {
                    for (var effect = 0; effect < effectIds.Length; effect++)
                    {
                        var key = form + "/" + expressionIds[expression] + "/" + shapeIds[shape] + "/" + effectIds[effect];
                        if (!skills.TryGetValue(key, out var skill))
                            continue;

                        var position = ((form * expressionIds.Length + expression) * shapeIds.Length + shape) * effectIds.Length + effect;
                        result.Skills[position] = new AssetRef<SkillData> { Id = GuidOf(skill) };
                        // "Authored" counts the first forms, as it did before second forms existed.
                        if (form == 0)
                            result.Authored++;
                        else
                            result.AuthoredSecondForms++;
                    }
                }
            }

            return result;
        }

        // One area that hits each target once: the numbers of its shape, the damage of its delivery,
        // what its aura adds, and how it is drawn.
        private static void ConfigureSweep(GenesisSweepAttackData attack, Sweep sweep, Aura aura, int effect, float damage,
                                           Sound sound, Material shapeMaterial)
        {
            attack.SFX = sound;
            var drawn = sweep.Kind != GenesisVisualKind.Effect;
            attack.VisualKind = sweep.Kind;
            attack.ShapeMaterial = drawn ? shapeMaterial : null;
            // A fan opens from the caster to its reach; the other shapes are the area itself.
            attack.VisualRadius = sweep.Kind == GenesisVisualKind.Fan ? sweep.Reach : sweep.Radius;
            attack.Visual = drawn ? null : AssetDatabase.LoadAssetAtPath<GameObject>(sweep.Visual);
            if (attack.Visual == null && !drawn)
                throw new InvalidOperationException("Effect prefab not found: " + sweep.Visual);
            attack.VisualScale = sweep.VisualScale;
            attack.VisualTint = aura.Color;
            attack.TTL = Fixed(sweep.Lifetime);
            attack.IgnoreOwner = true;
            attack.HitEnemies = true;
            attack.HitAlies = false;
            attack.DestroyOnHitDynamic = false;
            attack.DestroyOnHitStatic = false;
            ApplyAura(attack, aura, effect, damage);
            attack.Velocity = Fixed(sweep.Velocity);
            attack.ForwardOffset = Fixed(sweep.ForwardOffset);
            attack.Radius = Fixed(sweep.Radius);
            attack.StopAtWalls = sweep.StopAtWalls;
        }

        // The puddle is the sample's lobbed projectile and ground zone, with its own numbers. The
        // zone applies the aura at every tick, so its damage over time and its slow keep renewing
        // while a target stays in it.
        // "second" is the second form: a wider zone, weaker at each tick.
        private static SkillData ImportPuddle(Aura aura, int effect, Delivery delivery, string suffix, bool second,
                                              List<AssetObject> written, AssetRef<EntityPrototype> attackPrototype,
                                              Material shapeMaterial)
        {
            var name = "GenesisPuddle" + Capitalised(aura.Effect) + suffix;
            var sampleSkill = Load<ArcherSpecialSkillData>("ArcherSpecialSkillData.asset");
            var sampleLob = Load<ArcherSpecialFirstAttackData>("ArcherSpecialFirstAttackData.asset");
            var sampleZone = Load<ArcherSpecialSecondAttackData>("ArcherSpecialSecondAttackData.asset");

            var zone = LoadOrCreate<ArcherSpecialSecondAttackData>(name + "ZoneAttackData", written);
            CopyAttack(sampleZone, zone);
            zone.EffectInterval = sampleZone.EffectInterval;
            zone.EffectRepeatCount = sampleZone.EffectRepeatCount;
            zone.Shape = JsonUtility.FromJson<Shape2DConfig>(JsonUtility.ToJson(sampleZone.Shape));
            if (second)
                zone.Shape.CircleRadius = Fixed(sampleZone.Shape.CircleRadius.AsFloat * SecondPuddleRadiusScale);
            ApplyAura(zone, aura, effect, (second ? SecondPuddleDamagePerTick : PuddleDamagePerTick) * delivery.Share);
            // Drawn as a puddle of tears in the colours of the caster's eyes.
            zone.VisualKind = GenesisVisualKind.GroundDisc;
            zone.ShapeMaterial = shapeMaterial;
            zone.VisualRadius = zone.Shape.CircleRadius.AsFloat;
            zone.VisualTint = aura.Color;
            EditorUtility.SetDirty(zone);

            var lob = LoadOrCreate<ArcherSpecialFirstAttackData>(name + "LobAttackData", written);
            CopyAttack(sampleLob, lob);
            lob.EffectsOnOthers = new Effect[0];
            lob.EffectsOnSource = new Effect[0];
            lob.Velocity = sampleLob.Velocity;
            // Both the lobbed tear and the puddle are drawn by the shared attack prefab.
            lob.VisualKind = GenesisVisualKind.Drop;
            lob.ShapeMaterial = shapeMaterial;
            lob.VisualTint = aura.Color;
            lob.PostImpactPrototype = attackPrototype;
            lob.PostImpactDataOverride = new AssetRef<AttackData> { Id = GuidOf(zone) };
            EditorUtility.SetDirty(lob);

            var skill = LoadOrCreate<ArcherSpecialSkillData>(name + "SkillData", written);
            CopySkill(sampleSkill, skill);
            Repeat(skill, delivery);
            skill.MaxDistance = sampleSkill.MaxDistance;
            skill.AttackPrototype = attackPrototype;
            skill.AttackDataOverride = new AssetRef<AttackData> { Id = GuidOf(lob) };
            skill.Reach = sampleSkill.MaxDistance;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        // Damage first, then what the aura adds; the attack reports each target to the view.
        private static void ApplyAura(AttackData attack, Aura aura, int effect, float damage)
        {
            var onTarget = new List<Effect> { Instant(EAttributeType.Health, EModifierOperation.Subtract, damage * aura.Impact) };
            onTarget.AddRange(aura.OnTarget);
            attack.EffectsOnOthers = onTarget.ToArray();
            attack.EffectsOnSource = (Effect[])aura.OnCaster.Clone();
            attack.IsGenesisSignature = true;
            attack.GenesisEffect = checked((byte)effect);
            attack.GenesisEffectDuration = Fixed(aura.Duration);
        }

        // A copy of a sample attack prefab, emptied of its own visuals and given the view that
        // reads the look from the attack data. Quantum writes its entity prototype next to it.
        private static AssetRef<EntityPrototype> EnsureAttackPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AttackPrefab) == null)
            {
                if (!AssetDatabase.CopyAsset(AttackPrefabTemplate, AttackPrefab))
                    throw new InvalidOperationException("Could not copy " + AttackPrefabTemplate);

                var root = PrefabUtility.LoadPrefabContents(AttackPrefab);
                try
                {
                    for (var i = root.transform.childCount - 1; i >= 0; i--)
                        UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

                    // The sample's team tint works on the particle systems just removed.
                    foreach (var component in root.GetComponents<MonoBehaviour>())
                    {
                        var typeName = component == null ? "" : component.GetType().Name;
                        if (typeName == "AttackView" || typeName == "ParticlesColorSetter")
                            UnityEngine.Object.DestroyImmediate(component);
                    }

                    root.AddComponent<Chog.Match.GenesisAttackView>();
                    PrefabUtility.SaveAsPrefabAsset(root, AttackPrefab);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                AssetDatabase.Refresh();
            }

            return Prototype("Genesis/GenesisAttack");
        }

        // A skill that fires several times keeps acting at that interval until it has fired them
        // all; a single hit keeps the timing copied from the sample.
        private static void Repeat(SkillData skill, Delivery delivery)
        {
            if (delivery.Hits <= 1)
                return;

            skill.HasTTL = true;
            skill.ActionInterval = Fixed(delivery.Interval);
            skill.ActionAmount = delivery.Hits;
        }

        private static void CopySkill(SkillData from, SkillData to)
        {
            to.SFX = from.SFX;
            to.SkillPrototype = from.SkillPrototype;
            to.HasTTL = from.HasTTL;
            to.ActionInterval = from.ActionInterval;
            to.ActionAmount = from.ActionAmount;
            to.RotationLockDuration = from.RotationLockDuration;
            to.MovementLockDuration = from.MovementLockDuration;
            to.AutoAimCheckSight = from.AutoAimCheckSight;
            to.AutoAimRadius = from.AutoAimRadius;
            to.Cost = FP._1;
            to.CostType = EAttributeType.Special;
        }

        private static void CopyAttack(AttackData from, AttackData to)
        {
            to.SFX = from.SFX;
            to.TTL = from.TTL;
            to.IgnoreOwner = from.IgnoreOwner;
            to.HitEnemies = from.HitEnemies;
            to.HitAlies = from.HitAlies;
            to.DestroyOnHitDynamic = from.DestroyOnHitDynamic;
            to.DestroyOnHitStatic = from.DestroyOnHitStatic;
        }

        private static Effect Instant(EAttributeType attribute, EModifierOperation operation, float amount)
        {
            return new Effect
            {
                Value = Fixed(amount),
                AttributeType = attribute,
                Appliance = EModifierAppliance.OneTime,
                Operation = operation
            };
        }

        // "Continuous" amounts are per second; the others apply once and are undone at the end.
        private static Effect Timed(EAttributeType attribute, EModifierAppliance appliance, EModifierOperation operation,
                                    float amount, float seconds)
        {
            return new Effect
            {
                Value = Fixed(amount),
                AttributeType = attribute,
                Appliance = appliance,
                Operation = operation,
                Duration = Fixed(seconds)
            };
        }

        // Authoring-time conversion: the fixed-point value is what gets stored in the asset.
        private static FP Fixed(float value) => FP.FromFloat_UNSAFE(value);

        /// <summary>How each value of the Eyes trait colours its own attack; unlisted values stay transparent.</summary>
        public static GenesisEyeLook[] EyeLooksFor(string[] eyeValues)
        {
            var looks = new GenesisEyeLook[eyeValues.Length];
            for (var i = 0; i < eyeValues.Length; i++)
                EyeLooks.TryGetValue(eyeValues[i], out looks[i]);
            return looks;
        }

        private static GenesisEyeLook Look(Color outline, Color outer, Color core, Color other = default)
        {
            return new GenesisEyeLook { Outline = outline, Outer = outer, Core = core, Other = other };
        }

        // An eye drawn as an expression: its shape, its main colour, and a second colour for make-up.
        private static GenesisEyeLook Eyes(GenesisEyeGlyph glyph, Color main, Color other = default)
        {
            return new GenesisEyeLook { Outline = Ink, Outer = main, Core = Color.white, Other = other, Glyph = glyph };
        }

        // Goggles: the colours of the band from one side to the other.
        private static GenesisEyeLook Band(Color left, Color middle, Color right)
        {
            return new GenesisEyeLook { Outline = Ink, Outer = left, Core = middle, Other = right, Glyph = GenesisEyeGlyph.Band };
        }

        private static GenesisEyeLook Tears()
        {
            return Look(new Color(0.25f, 0.45f, 0.8f), new Color(0.66f, 0.83f, 0.94f), new Color(0.88f, 0.95f, 1f));
        }

        /// <summary>
        /// An unlit, transparent, vertex-coloured material kept as an asset of that name next to the
        /// data, so its shader is part of the build. Used for the beams and for the hero faces.
        /// </summary>
        public static Material UnlitMaterial(string name)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/QuantumUser/Resources", "Genesis");

            var path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                throw new InvalidOperationException("Shader Sprites/Default not found for " + name + ".");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static string Capitalised(string id) => char.ToUpperInvariant(id[0]) + id.Substring(1);

        private static void SetSummary(string[] summaries, string[] ids, string id, string summary)
        {
            var position = Array.IndexOf(ids, id);
            if (position >= 0)
                summaries[position] = summary;
        }

        private static T Load<T>(string sampleFile) where T : AssetObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Sample + sampleFile);
            if (asset == null)
                throw new InvalidOperationException("Sample asset not found: " + Sample + sampleFile);
            return asset;
        }

        private static T LoadOrCreate<T>(string name, List<AssetObject> written) where T : AssetObject
        {
            var path = Folder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            written.Add(asset);
            return asset;
        }

        private static AssetRef<EntityPrototype> Prototype(string prefabName)
        {
            var path = Sample + prefabName + "EntityPrototype.qprototype";
            var prototype = AssetDatabase.LoadAssetAtPath<EntityPrototype>(path);
            if (prototype == null || !prototype.Guid.IsValid)
                throw new InvalidOperationException("Attack prefab prototype not found: " + path);
            return new AssetRef<EntityPrototype> { Id = prototype.Guid };
        }

        private static AssetGuid GuidOf(AssetObject asset) => QuantumUnityDBUtilities.GetExpectedAssetGuid(asset, out _);

        // Quantum registers new assets on a delayed Editor call; a build must not depend on it.
        // This covers the attack prefab's prototype as well as the data: a build without it throws
        // "Could not find prototype asset" every time a signature is fired.
        private static void EnsureRegistered(List<AssetGuid> guids)
        {
            if (!AllRegistered(guids))
            {
                QuantumUnityDBUtilities.RefreshGlobalDB(true);
                if (!AllRegistered(guids))
                    throw new InvalidOperationException("Signature assets are missing from the Quantum asset database.");
            }
        }

        private static bool AllRegistered(List<AssetGuid> guids)
        {
            foreach (var guid in guids)
            {
                var reference = new AssetRef<AssetObject> { Id = guid };
                if (!QuantumUnityDB.TryGetGlobalAsset(reference, out AssetObject registered) || registered == null)
                    return false;
            }

            return true;
        }
    }
}
