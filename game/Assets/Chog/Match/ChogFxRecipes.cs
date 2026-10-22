using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The Cartoon FX effects the fights play (Jean Moreno's Cartoon FX Remaster, in the project; the copies
    /// the game loads are in Assets/Chog/Effects/Resources/ChogFx under these names), each pooled and played
    /// by GenesisEffects.Play.
    /// </summary>
    public enum ChogFx : byte
    {
        Hit, GroundHit, Debris, Electric, Sparks, SparksAhead, Fire, Burning, Ice, Explosion, FireExplosion,
        Light, LightSoft, Smoke, BrokenHeart, PoisonCloud, RainbowStars, Runes, MagicStars, Leaves, Bubbles,
        Splash, Boom
    }

    /// <summary>How an effect takes the colour it is given: as it was made, its colours multiplied by it, or painted in it.</summary>
    public enum FxColour : byte { Authored, Tinted, Painted }

    /// <summary>How an authored colour of an effect takes the colour it is given (FxColour). Plain, tested.</summary>
    public static class FxColours
    {
        /// <summary>"Tinted" multiplies; "Painted" takes the colour, a little darker where the authored one was dark; alpha kept.</summary>
        public static Color Mix(Color authored, Color colour, FxColour mode)
        {
            switch (mode)
            {
                case FxColour.Tinted:
                    return new Color(authored.r * colour.r, authored.g * colour.g, authored.b * colour.b, authored.a);
                case FxColour.Painted:
                {
                    var light = Mathf.Lerp(1f, Mathf.Clamp01(Luma(authored) * 1.4f), 0.4f);
                    return new Color(colour.r * light, colour.g * light, colour.b * light, authored.a);
                }
                default:
                    return authored;
            }
        }

        /// <summary>How light a colour reads (Rec. 601).</summary>
        public static float Luma(Color colour) => colour.r * 0.299f + colour.g * 0.587f + colour.b * 0.114f;

        /// <summary>A colour lighter by "share" toward white, for a puff of a dark object that must still read.</summary>
        public static Color Lighter(Color colour, float share) =>
            new Color(Mathf.Lerp(colour.r, 1f, share), Mathf.Lerp(colour.g, 1f, share), Mathf.Lerp(colour.b, 1f, share), colour.a);
    }

    /// <summary>What flies out where a basic attack ends.</summary>
    public enum ImpactPieces : byte
    {
        /// <summary>Small diamonds in the colours of the object (the look of before).</summary>
        Diamonds,
        /// <summary>Flat pieces that flutter down: cloth, wool, a feather.</summary>
        Flakes,
        /// <summary>Small copies of the object itself (or of another piece of the library): coins, stars, leaves.</summary>
        Copies
    }

    /// <summary>One Cartoon FX effect of a recipe, its size and how it takes a colour.</summary>
    public struct FxLayer
    {
        public bool On;
        public ChogFx Effect;
        public float Size;
        public FxColour Colour;
        /// <summary>The colour it takes; transparent for the colour of the object (or of the aura).</summary>
        public Color Fixed;

        public static FxLayer Of(ChogFx effect, float size, FxColour colour = FxColour.Authored) =>
            new FxLayer { On = true, Effect = effect, Size = size, Colour = colour };

        public static FxLayer Of(ChogFx effect, float size, FxColour colour, Color fixedColour) =>
            new FxLayer { On = true, Effect = effect, Size = size, Colour = colour, Fixed = fixedColour };
    }

    /// <summary>
    /// What the end of one basic attack looks like, from the object its hat gives it (ChogAttackLook): the
    /// pieces that fly out, one or two Cartoon FX effects in its colours, a flash of light (ChogLight), a ring
    /// on the floor, a bolt of lightning. Plain data, read by ChogFxKit.
    /// </summary>
    public struct ImpactRecipe
    {
        /// <summary>What a player sees, in words (the test page writes it under each hero).</summary>
        public string Words;
        public ImpactPieces Pieces;
        public int Count;
        /// <summary>The piece of the library the copies are; empty for the object itself.</summary>
        public string CopiesOf;
        /// <summary>World size of a piece.</summary>
        public float PieceSize;
        /// <summary>How hard the pieces are thrown up, over the burst of before.</summary>
        public float Rise;
        /// <summary>Colours of the pieces when the object's own do not say it (the drops of a cloud).</summary>
        public Color PieceColour;
        public FxLayer Effect;
        /// <summary>A second, softer layer (a puff), left out on a phone.</summary>
        public FxLayer Extra;
        /// <summary>A flash of light of this size (0 for none) with this many rays.</summary>
        public float Flare;
        public int Rays;
        /// <summary>The colour of the flash; transparent for the colour of the object.</summary>
        public Color FlareColour;
        /// <summary>A ring on the floor in the colour of the object, this wide (0 for none).</summary>
        public float Ring;
        /// <summary>A bolt of lightning from above onto the spot.</summary>
        public bool Lightning;
        /// <summary>Several colours at once: the pieces take the colours of the rainbow.</summary>
        public bool Rainbow;
    }

    /// <summary>What an Aura does to the target of a special, and to its caster when it fires.</summary>
    public struct AuraRecipe
    {
        public string Words;
        /// <summary>On the target, at the moment of the hit.</summary>
        public FxLayer Hit;
        /// <summary>On the target while the effect lasts (burn, hex, toxin), a computer only.</summary>
        public FxLayer Lasting;
        /// <summary>At the caster's eyes when the special fires.</summary>
        public FxLayer Cast;
        /// <summary>Lightning around the target (haste) and from the caster's eyes.</summary>
        public bool Lightning;
        /// <summary>A ring of light on the floor (purify).</summary>
        public bool Ring;
        public int Rays;
    }

    /// <summary>What the rarity of a Genesis (its delivery) adds when its special fires.</summary>
    public struct DeliveryRecipe
    {
        public string Words;
        public FxLayer Cast;
        /// <summary>Rings of light in the colour of the eyes, one after the other (an echo).</summary>
        public int Rings;
        /// <summary>A flash in this colour; transparent for none.</summary>
        public Color Flash;
        public int Rays;
    }

    /// <summary>
    /// Which effects each trait gives (the owner, 17 October: be creative and varied while respecting the
    /// traits): the object of the hat decides what the end of a basic attack looks like
    /// (docs/ATTACKS-FROM-THE-ART.md, "What hits"), the Aura what a special does to its target and how its
    /// caster lights up, the rarity (the delivery of the special) what it adds when it fires. Plain data,
    /// tested without the engine.
    /// </summary>
    public static class ChogFxRecipes
    {
        private static readonly Color Gold = new Color(1f, 0.80f, 0.30f);
        private static readonly Color White = Color.white;
        private static readonly Color SnowWhite = new Color(0.92f, 0.96f, 1f);
        private static readonly Color RainDrop = new Color(0.55f, 0.78f, 1f);
        private static readonly Color Bronze = new Color(0.95f, 0.72f, 0.38f);
        private static readonly Color RoyalLight = new Color(0.45f, 0.62f, 1f);
        private static readonly Color Flame = new Color(1f, 0.55f, 0.15f);
        private static readonly Color SeaWater = new Color(0.40f, 0.72f, 1f);
        private static readonly Color Coffee = new Color(0.50f, 0.32f, 0.18f);
        private static readonly Color Steam = new Color(0.92f, 0.90f, 0.88f);
        private static readonly Color CigarSmoke = new Color(0.72f, 0.70f, 0.74f);
        private static readonly Color Ember = new Color(1f, 0.42f, 0.12f);
        private static readonly Color HeartPink = new Color(1f, 0.55f, 0.72f);
        private static readonly Color FrogGreen = new Color(0.36f, 0.78f, 0.30f);

        /// <summary>The impact of a basic attack that throws this piece of the library ("Attack_Snowball", "Hat_Cap").</summary>
        /// <summary>
        /// What streams behind a basic attack while it flies, for the few objects that are made of it (the
        /// fire of a dragon suit, the water of a shark suit); not On for every other.
        /// </summary>
        public static FxLayer TrailOf(string piece)
        {
            switch (Key(piece))
            {
                case "Fireball":
                    return FxLayer.Of(ChogFx.Burning, 0.32f);
                case "WaterDrop":
                    return FxLayer.Of(ChogFx.Bubbles, 0.3f, FxColour.Tinted, SeaWater);
                default:
                    return default;
            }
        }

        public static ImpactRecipe ForAttack(string piece)
        {
            var key = Key(piece);
            switch (key)
            {
                case "Leaf":
                    return Recipe("a splash of small leaves", ImpactPieces.Copies, 8, 0.3f, 0.9f,
                        FxLayer.Of(ChogFx.Leaves, 0.45f, FxColour.Tinted), FxLayer.Of(ChogFx.Smoke, 0.32f, FxColour.Painted), 0.6f, 0);
                case "Snowball":
                    return Recipe("snow bursting into chunks and frost", ImpactPieces.Copies, 6, 0.22f, 0.9f,
                        FxLayer.Of(ChogFx.Ice, 0.5f), FxLayer.Of(ChogFx.Smoke, 0.45f, FxColour.Painted, SnowWhite), 0.9f, 4,
                        SnowWhite);
                case "StarYellow":
                case "StarWhite":
                    return Recipe("a burst of stars and sparkles", ImpactPieces.Copies, 7, 0.26f, 1.1f,
                        FxLayer.Of(ChogFx.Light, 0.55f, FxColour.Tinted), default, 1.1f, 8);
                case "Cloud":
                {
                    var cloud = Recipe("a puff of cloud and rain drops", ImpactPieces.Diamonds, 8, 0.12f, 0.5f,
                        FxLayer.Of(ChogFx.Smoke, 0.75f, FxColour.Painted, White), default, 0.7f, 0, White);
                    cloud.PieceColour = RainDrop;
                    return cloud;
                }
                case "Ribbon":
                    return WithRing(Recipe("dark cloth flying and a dark ring", ImpactPieces.Flakes, 7, 0.16f, 0.9f,
                        FxLayer.Of(ChogFx.Smoke, 0.35f, FxColour.Painted), default, 0.5f, 0), 1.2f);
                case "Curl":
                    return Recipe("yellow curls and puffs", ImpactPieces.Copies, 5, 0.24f, 0.9f,
                        FxLayer.Of(ChogFx.Smoke, 0.5f, FxColour.Painted), default, 0.6f, 0);
                case "CoinDollar":
                case "CoinBitcoin":
                case "CoinButtcoin":
                    return Recipe("coins popping into sparkles", ImpactPieces.Copies, 7, 0.26f, 1.5f,
                        FxLayer.Of(ChogFx.Light, 0.55f, FxColour.Tinted, Gold), FxLayer.Of(ChogFx.MagicStars, 0.5f, FxColour.Tinted, Gold),
                        1.2f, 8, Gold);
                case "MoneyBag":
                {
                    var bag = Recipe("the bag bursts into dollar coins", ImpactPieces.Copies, 7, 0.24f, 1.5f,
                        FxLayer.Of(ChogFx.Smoke, 0.45f, FxColour.Painted), FxLayer.Of(ChogFx.Light, 0.45f, FxColour.Tinted, Gold),
                        1f, 6, Gold);
                    bag.CopiesOf = "Attack_CoinDollar";
                    return bag;
                }
                case "Bolt":
                {
                    var bolt = Recipe("a crackle of lightning from the sky", ImpactPieces.Diamonds, 6, 0.14f, 1.2f,
                        FxLayer.Of(ChogFx.Electric, 0.7f), default, 1.4f, 4);
                    bolt.Lightning = true;
                    return bolt;
                }
                case "Peace":
                    return WithRing(Recipe("a white ring of peace signs", ImpactPieces.Copies, 4, 0.24f, 0.9f,
                        default, default, 0.9f, 6, White), 1.5f);
                case "SunDisc":
                    return WithRing(Recipe("a rising sun: red rays and a white ring", ImpactPieces.Diamonds, 7, 0.14f, 1f,
                        FxLayer.Of(ChogFx.LightSoft, 0.55f, FxColour.Tinted), default, 1.4f, 12), 1.4f);
                case "RedRing":
                    return WithRing(Recipe("a red ring", ImpactPieces.Diamonds, 7, 0.15f, 1f, default, default, 0.6f, 0), 1.6f);
                case "YellowRing":
                    return WithRing(Recipe("a magic ring and stars", ImpactPieces.Diamonds, 6, 0.13f, 1f,
                        FxLayer.Of(ChogFx.MagicStars, 0.55f, FxColour.Tinted), default, 0.9f, 6), 1.5f);
                case "Spear":
                    return Recipe("sparks off the spear's tip", ImpactPieces.Diamonds, 6, 0.12f, 1.1f,
                        FxLayer.Of(ChogFx.Sparks, 0.6f, FxColour.Tinted, Bronze), default, 0.8f, 4, Bronze);
                case "Arrow":
                    return Recipe("a black feather and splinters", ImpactPieces.Flakes, 5, 0.14f, 0.8f,
                        FxLayer.Of(ChogFx.Debris, 0.45f, FxColour.Tinted), default, 0.5f, 0);
                case "Pizza":
                    return Recipe("crumbs of crust, cheese and pepperoni", ImpactPieces.Diamonds, 10, 0.1f, 1f,
                        FxLayer.Of(ChogFx.Smoke, 0.3f, FxColour.Painted), default, 0.5f, 0);
                case "MiniChog":
                    return Recipe("a lavender puff and a cartoon pop", ImpactPieces.Diamonds, 6, 0.13f, 1f,
                        FxLayer.Of(ChogFx.Smoke, 0.55f, FxColour.Painted), FxLayer.Of(ChogFx.Hit, 0.45f, FxColour.Tinted), 0.7f, 0);
                case "Yarn":
                    return Recipe("wool flying in its two blues", ImpactPieces.Flakes, 8, 0.15f, 0.9f,
                        FxLayer.Of(ChogFx.Smoke, 0.35f, FxColour.Painted), default, 0.5f, 0);
                case "RainbowBall":
                {
                    var rainbow = Recipe("a burst of every colour", ImpactPieces.Diamonds, 10, 0.14f, 1.2f,
                        FxLayer.Of(ChogFx.RainbowStars, 0.55f), default, 1.1f, 8, White);
                    rainbow.Rainbow = true;
                    return rainbow;
                }
                case "Nose":
                    return WithRing(Recipe("a red squash", ImpactPieces.Diamonds, 5, 0.14f, 0.6f,
                        FxLayer.Of(ChogFx.Splash, 0.45f, FxColour.Painted), FxLayer.Of(ChogFx.Hit, 0.45f, FxColour.Tinted), 0.7f, 0), 1.2f);
                case "Bone":
                    return Recipe("bones and dust", ImpactPieces.Copies, 5, 0.24f, 0.9f,
                        FxLayer.Of(ChogFx.Smoke, 0.35f, FxColour.Painted), default, 0.5f, 0);
                // What a Genesis holds in its hand, thrown first (chog_attack_objects.ATTACK_OF_HELD, 22 October 2026).
                // A candle of the chart bursts into small candles and sparks of its own colour, green or red.
                case "Candle":
                    return Recipe("candles of the chart bursting into sparks of their colour", ImpactPieces.Copies, 5, 0.22f, 1.3f,
                        FxLayer.Of(ChogFx.Sparks, 0.55f, FxColour.Tinted), FxLayer.Of(ChogFx.Light, 0.45f, FxColour.Tinted), 1f, 6);
                case "Cigar":
                {
                    var cigar = Recipe("a puff of cigar smoke and embers", ImpactPieces.Diamonds, 6, 0.11f, 1f,
                        FxLayer.Of(ChogFx.Smoke, 0.55f, FxColour.Painted, CigarSmoke), FxLayer.Of(ChogFx.Fire, 0.3f), 0.7f, 4, Ember);
                    cigar.PieceColour = Ember;
                    return cigar;
                }
                case "Cup":
                {
                    var coffee = WithRing(Recipe("a splash of hot coffee and steam", ImpactPieces.Diamonds, 8, 0.11f, 0.9f,
                        FxLayer.Of(ChogFx.Splash, 0.5f, FxColour.Painted, Coffee), FxLayer.Of(ChogFx.Smoke, 0.4f, FxColour.Painted, Steam),
                        0.5f, 0), 1.2f);
                    coffee.PieceColour = Coffee;
                    return coffee;
                }
                case "Coin":
                    return Recipe("a gold coin popping into sparkles", ImpactPieces.Copies, 6, 0.24f, 1.5f,
                        FxLayer.Of(ChogFx.Light, 0.55f, FxColour.Tinted, Gold), FxLayer.Of(ChogFx.MagicStars, 0.5f, FxColour.Tinted, Gold),
                        1.2f, 8, Gold);
                case "Knife":
                    return Recipe("a slash of steel sparks", ImpactPieces.Diamonds, 6, 0.10f, 1.1f,
                        FxLayer.Of(ChogFx.SparksAhead, 0.6f), FxLayer.Of(ChogFx.Hit, 0.45f, FxColour.Tinted), 0.9f, 6, White);
                case "Heart":
                    return Recipe("little hearts popping in pink", ImpactPieces.Copies, 6, 0.24f, 1.2f,
                        FxLayer.Of(ChogFx.LightSoft, 0.5f, FxColour.Tinted, HeartPink), default, 1f, 8, HeartPink);
                case "Fwog":
                case "Pepe":
                    return Recipe("frogs hopping off and a green splash", ImpactPieces.Copies, 4, 0.24f, 1.3f,
                        FxLayer.Of(ChogFx.Splash, 0.45f, FxColour.Painted, FrogGreen), FxLayer.Of(ChogFx.Hit, 0.4f, FxColour.Tinted), 0.6f, 0);
                // Full costumes (chog_attack_objects.ATTACK_OF_BODY).
                case "Fireball":
                    return Recipe("the fireball bursts into flames", ImpactPieces.Diamonds, 7, 0.13f, 1.1f,
                        FxLayer.Of(ChogFx.FireExplosion, 0.45f), FxLayer.Of(ChogFx.Fire, 0.4f), 1.3f, 6, Flame);
                case "WaterDrop":
                {
                    var water = WithRing(Recipe("a splash of sea water and bubbles", ImpactPieces.Diamonds, 9, 0.11f, 1.1f,
                        FxLayer.Of(ChogFx.Splash, 0.55f, FxColour.Painted, SeaWater), FxLayer.Of(ChogFx.Bubbles, 0.45f, FxColour.Tinted, SeaWater),
                        0.6f, 0, SeaWater), 1.3f);
                    water.PieceColour = SeaWater;
                    return water;
                }
                case "Banana":
                    return Recipe("bananas bouncing off and a pop", ImpactPieces.Copies, 4, 0.22f, 1.2f,
                        FxLayer.Of(ChogFx.Hit, 0.5f, FxColour.Tinted), FxLayer.Of(ChogFx.Smoke, 0.3f, FxColour.Painted), 0.6f, 0);
                case "Planet":
                    return Recipe("a burst of stars from space", ImpactPieces.Diamonds, 8, 0.12f, 1.1f,
                        FxLayer.Of(ChogFx.MagicStars, 0.55f, FxColour.Tinted), FxLayer.Of(ChogFx.Light, 0.4f, FxColour.Tinted), 1.1f, 8);
                case "LilyPad":
                    return Recipe("green leaves and a splash", ImpactPieces.Flakes, 7, 0.15f, 0.9f,
                        FxLayer.Of(ChogFx.Leaves, 0.5f, FxColour.Tinted), FxLayer.Of(ChogFx.Splash, 0.35f, FxColour.Painted, SeaWater), 0.5f, 0);
                case "Trident":
                {
                    var trident = Recipe("purple sparks and a crackle", ImpactPieces.Diamonds, 6, 0.13f, 1.1f,
                        FxLayer.Of(ChogFx.Sparks, 0.6f, FxColour.Tinted), default, 1f, 6);
                    trident.Lightning = true;
                    return trident;
                }
                // Hats thrown whole.
                case "Cap":
                case "BucketCap":
                case "Beanie":
                case "Fedora":
                case "Turban":
                case "Bandana":
                    return WithRing(Recipe("the hat bursts into cloth and a puff", ImpactPieces.Flakes, 9, 0.22f, 1f,
                        FxLayer.Of(ChogFx.Hit, 0.5f, FxColour.Tinted), FxLayer.Of(ChogFx.Smoke, 0.45f, FxColour.Painted), 0.6f, 0), 1.3f);
                case "Crown":
                case "Seal":
                case "GoldHelmet":
                case "Halo":
                    return Recipe("gold sparks", ImpactPieces.Diamonds, 8, 0.13f, 1.2f,
                        FxLayer.Of(ChogFx.Sparks, 0.7f, FxColour.Tinted, Gold), default, 1.2f, 6, Gold);
                case "Plunger":
                    return WithRing(Recipe("a cartoon pop", ImpactPieces.Diamonds, 5, 0.14f, 0.8f,
                        FxLayer.Of(ChogFx.Hit, 0.6f), default, 0.6f, 0), 1.1f);
                case "Laurel":
                case "ThornCrown":
                case "Mushrooms":
                    return Recipe("a burst of leaves", ImpactPieces.Flakes, 6, 0.15f, 0.9f,
                        FxLayer.Of(ChogFx.Leaves, 0.5f, FxColour.Tinted), default, 0.5f, 0);
                case "SkullFlames":
                    return Recipe("a burst of flames", ImpactPieces.Diamonds, 6, 0.13f, 1f,
                        FxLayer.Of(ChogFx.Fire, 0.5f), default, 0.9f, 4, new Color(1f, 0.55f, 0.15f));
                case "Gem":
                case "ThirdEye":
                case "HeadLaser":
                    return Recipe("a glint of light", ImpactPieces.Diamonds, 7, 0.13f, 1.1f,
                        FxLayer.Of(ChogFx.Light, 0.5f, FxColour.Tinted), default, 1f, 6);
                // Horns, a taped banana and a brain (22 October 2026: the last hats that only popped).
                // A ram's horns butt: chips of horn and a thump. Demon, devil and void horns: a puff of
                // hellfire in the colour of the horns (orange, red, purple).
                case "RamHorns":
                    return WithRing(Recipe("a ram's butt: chips of horn and a thump", ImpactPieces.Diamonds, 8, 0.14f, 1.2f,
                        FxLayer.Of(ChogFx.Debris, 0.5f, FxColour.Tinted), FxLayer.Of(ChogFx.Hit, 0.5f, FxColour.Tinted), 0.8f, 0), 1.3f);
                case "DemonHorns":
                    return Recipe("a puff of hellfire in the colour of the horns", ImpactPieces.Diamonds, 6, 0.13f, 1.1f,
                        FxLayer.Of(ChogFx.Fire, 0.45f, FxColour.Painted), FxLayer.Of(ChogFx.Smoke, 0.35f, FxColour.Painted), 0.9f, 4);
                case "TapedBanana":
                    return Recipe("taped bananas bouncing off and a pop", ImpactPieces.Copies, 4, 0.22f, 1.2f,
                        FxLayer.Of(ChogFx.Hit, 0.5f, FxColour.Tinted), FxLayer.Of(ChogFx.Smoke, 0.3f, FxColour.Painted), 0.6f, 0);
                case "Brain":
                    return WithRing(Recipe("a pink squelch", ImpactPieces.Diamonds, 7, 0.13f, 0.7f,
                        FxLayer.Of(ChogFx.Splash, 0.5f, FxColour.Painted), FxLayer.Of(ChogFx.Hit, 0.4f, FxColour.Tinted), 0.6f, 0), 1.1f);
                default:
                    return Recipe("pieces of its colours and a pop", ImpactPieces.Diamonds, 7, 0.16f, 1f,
                        FxLayer.Of(ChogFx.Hit, 0.45f, FxColour.Tinted), default, 0.6f, 0);
            }
        }

        /// <summary>
        /// The name of the object without its family: "Attack_Snowball" gives "Snowball", "Hat_Cap" gives
        /// "Cap"; an empty name stays empty.
        /// </summary>
        public static string Key(string piece)
        {
            if (string.IsNullOrEmpty(piece))
                return "";
            var cut = piece.IndexOf('_');
            return cut >= 0 ? piece.Substring(cut + 1) : piece;
        }

        /// <summary>What the Aura drawn with this sign does to a target and its caster.</summary>
        public static AuraRecipe ForAura(GenesisEffectIcon icon)
        {
            switch (icon)
            {
                case GenesisEffectIcon.Flame:
                    return new AuraRecipe
                    {
                        Words = "burn: a burst of fire, flames on the target while it burns",
                        Hit = FxLayer.Of(ChogFx.Fire, 0.7f), Lasting = FxLayer.Of(ChogFx.Burning, 0.7f),
                        Cast = FxLayer.Of(ChogFx.Fire, 0.35f), Rays = 6
                    };
                case GenesisEffectIcon.Snowflake:
                    return new AuraRecipe
                    {
                        Words = "chill: ice shards and frost",
                        Hit = FxLayer.Of(ChogFx.Ice, 0.7f), Cast = FxLayer.Of(ChogFx.Ice, 0.35f), Rays = 6
                    };
                case GenesisEffectIcon.Star:
                    return new AuraRecipe
                    {
                        Words = "hex: purple stars, a circle of runes at the rooted target's feet",
                        Hit = FxLayer.Of(ChogFx.MagicStars, 0.8f, FxColour.Tinted),
                        Lasting = FxLayer.Of(ChogFx.Runes, 0.8f, FxColour.Tinted),
                        Cast = FxLayer.Of(ChogFx.MagicStars, 0.45f, FxColour.Tinted), Rays = 5
                    };
                case GenesisEffectIcon.Plus:
                    return new AuraRecipe
                    {
                        Words = "mend: a soft pink light",
                        Hit = FxLayer.Of(ChogFx.LightSoft, 0.7f, FxColour.Tinted), Cast = FxLayer.Of(ChogFx.LightSoft, 0.45f, FxColour.Tinted),
                        Rays = 4
                    };
                case GenesisEffectIcon.Ring:
                    return new AuraRecipe
                    {
                        Words = "purify: a white light and a ring on the floor",
                        Hit = FxLayer.Of(ChogFx.Light, 0.7f, FxColour.Tinted), Ring = true, Rays = 8
                    };
                case GenesisEffectIcon.Arrow:
                    return new AuraRecipe
                    {
                        Words = "rally: a burst of royal blue sparks, sparks driven forward from the caster",
                        Hit = FxLayer.Of(ChogFx.Sparks, 0.9f, FxColour.Tinted, RoyalLight),
                        Cast = FxLayer.Of(ChogFx.SparksAhead, 0.55f, FxColour.Tinted, RoyalLight),
                        Rays = 4
                    };
                case GenesisEffectIcon.Bolt:
                    return new AuraRecipe
                    {
                        Words = "haste: electricity, lightning around the target and from the eyes",
                        Hit = FxLayer.Of(ChogFx.Electric, 1f), Cast = FxLayer.Of(ChogFx.Electric, 0.45f), Lightning = true, Rays = 4
                    };
                case GenesisEffectIcon.Cloud:
                    return new AuraRecipe
                    {
                        Words = "veil: a cloud of smoke",
                        Hit = FxLayer.Of(ChogFx.Smoke, 0.85f, FxColour.Painted), Cast = FxLayer.Of(ChogFx.Smoke, 0.45f, FxColour.Painted)
                    };
                case GenesisEffectIcon.Bubbles:
                    return new AuraRecipe
                    {
                        Words = "toxin: green bubbles, a poison cloud while it lasts",
                        Hit = FxLayer.Of(ChogFx.Bubbles, 0.8f, FxColour.Tinted),
                        Lasting = FxLayer.Of(ChogFx.PoisonCloud, 0.3f, FxColour.Tinted),
                        Cast = FxLayer.Of(ChogFx.Bubbles, 0.45f, FxColour.Tinted), Rays = 4
                    };
                default:
                    return new AuraRecipe { Words = "raw: a cartoon hit", Hit = FxLayer.Of(ChogFx.Hit, 0.9f), Rays = 4 };
            }
        }

        /// <summary>What a delivery ("direct", "echo", "return", "persistent-zone", "inversion") adds when its special fires.</summary>
        public static DeliveryRecipe ForDelivery(string delivery)
        {
            switch (delivery)
            {
                case "echo":
                    return new DeliveryRecipe { Words = "echo (Uncommon): two rings of light one after the other", Rings = 2 };
                case "return":
                    return new DeliveryRecipe
                    {
                        Words = "return (Rare Skull): a dark flash and a wisp of smoke",
                        Cast = FxLayer.Of(ChogFx.Smoke, 0.5f, FxColour.Painted, new Color(0.35f, 0.22f, 0.45f)),
                        Flash = new Color(0.55f, 0.30f, 0.85f), Rays = 6
                    };
                case "persistent-zone":
                    return new DeliveryRecipe
                    {
                        Words = "zone (Epic Radioactive): a green glow and bubbles",
                        Cast = FxLayer.Of(ChogFx.Bubbles, 0.55f, FxColour.Tinted, new Color(0.45f, 1f, 0.25f)),
                        Flash = new Color(0.45f, 1f, 0.25f), Rays = 6
                    };
                case "inversion":
                    return new DeliveryRecipe
                    {
                        Words = "inversion (Legendary 1:1): golden stars and rays",
                        Cast = FxLayer.Of(ChogFx.MagicStars, 0.7f, FxColour.Tinted, Gold), Flash = Gold, Rays = 12, Rings = 1
                    };
                default:
                    return new DeliveryRecipe { Words = "direct (Common)" };
            }
        }

        private static ImpactRecipe Recipe(string words, ImpactPieces pieces, int count, float size, float rise,
                                           FxLayer effect, FxLayer extra, float flare, int rays, Color flareColour = default)
        {
            return new ImpactRecipe
            {
                Words = words, Pieces = pieces, Count = count, PieceSize = size, Rise = rise, Effect = effect, Extra = extra,
                Flare = flare, Rays = rays, FlareColour = flareColour, CopiesOf = ""
            };
        }

        private static ImpactRecipe WithRing(ImpactRecipe recipe, float ring)
        {
            recipe.Ring = ring;
            return recipe;
        }
    }
}
