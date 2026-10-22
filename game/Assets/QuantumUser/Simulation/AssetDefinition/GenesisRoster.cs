namespace Quantum
{
    using System;

    [Serializable]
    public struct GenesisRosterEntry
    {
        // Canonical on-chain id. It differs from the number holders see in the name.
        public int TokenId;
        public int MetadataId;
        public string Name;
        public byte Role;
        public byte Shape;
        public byte Effect;
        public byte Expression;
        public byte Tier;

        // The trait values behind the kit, as positions in the roster's display tables
        // (0 is an absent trait). Only read to show them; the simulation uses the kit above.
        public byte Body;
        public byte Eye;
        public byte Aura;
        public byte Skin;
    }

#if QUANTUM_UNITY
    // The flat colours the collection draws a pair of eyes with, read from the Genesis images:
    // a dark outline, the main colour, and a lighter core. A visor or a pair of glasses whose two
    // sides differ (the red and blue 3D glasses) names the colour of the other side; left
    // transparent, both sides use the main colour.
    [Serializable]
    public struct GenesisEyeLook
    {
        public UnityEngine.Color Outline;
        public UnityEngine.Color Outer;
        public UnityEngine.Color Core;
        public UnityEngine.Color Other;

        // For eyes that are an expression and not an object: how one eye is drawn.
        public GenesisEyeGlyph Glyph;
    }

    // The look of one 1:1 Genesis: its eyes and the colour its hero takes.
    [Serializable]
    public struct GenesisUniqueLook
    {
        public int TokenId;
        public GenesisEyeLook Look;
        public UnityEngine.Color Skin;
    }

    // How the collection draws one eye of an expression, read from the Genesis images.
    public enum GenesisEyeGlyph
    {
        None = 0,
        Ball = 1,        // a dark ball with a white glint
        BallLidded = 2,  // the same under a flat eyelid: the side eye, the tired eye
        BallAngry = 3,   // the same under a slanted eyelid
        Ring = 4,        // a white eye with a rim and a pupil in its middle
        RingSide = 5,    // the pupil small and to one side
        RingDerp = 6,    // the two pupils looking apart
        Cross = 7,       // an X
        Heart = 8,
        Arc = 9,         // a closed, smiling eye
        Dash = 10,       // a closed, flat eye
        Chevron = 11,    // squeezed shut: > and <
        Band = 12,       // ski goggles: one band across both eyes
        Shades = 13,     // pixel sunglasses
        Trefoil = 14     // the sign of radioactivity, three blades round a dot: the burst of a Radioactive Genesis
    }

    // The sign drawn on a target for each Aura effect.
    public enum GenesisEffectIcon
    {
        Burst = 0,     // a plain hit
        Flame = 1,
        Snowflake = 2,
        Star = 3,
        Plus = 4,
        Ring = 5,
        Arrow = 6,
        Bolt = 7,
        Cloud = 8,
        Bubbles = 9
    }

    // What an attack is drawn as.
    public enum GenesisVisualKind
    {
        Effect = 0,      // the effect prefab named by the attack data
        Beam = 1,        // two laser beams from the eyes to the head of the attack
        Fan = 2,         // a flat fan of light in front of the hero
        Drop = 3,        // a drop lobbed in an arc to where the attack lands
        GroundDisc = 4,  // a flat disc on the ground, for a zone that stays
        Stare = 5,       // the hero's own pair of eyes, large, travelling with the attack
        EyeBurst = 6     // the hero's eye, repeated in a ring that opens around it
    }
#endif

    // One line of the hero card. Text only: nothing in the simulation reads it.
    public struct GenesisCardLine
    {
        public string Trait;
        public string Value;
        public string Gives;
        public string Summary;
    }

    // The playable cast: every Genesis the hero catalog resolves, with the kit its traits give it.
    // Imported from content/genesis-roster.json; not edited by hand.
    [Serializable]
    public class GenesisRoster : AssetObject
    {
        public string RulesetVersion;
        public string CatalogHash;

        // Id tables in catalog order; an entry stores positions in them.
        public string[] RoleIds;
        public string[] ShapeIds;
        public string[] EffectIds;
        public string[] ExpressionIds;
        public string[] TierNames;

        // Text for the hero card. Trait values as written in the metadata (position 0 is an
        // absent trait), and one short sentence for each class, shape and effect, parallel to
        // RoleIds, ShapeIds and EffectIds and written next to the numbers they describe.
        public string[] BodyValues;
        public string[] EyeValues;
        public string[] AuraValues;
        public string[] SkinValues;
        public string[] RoleSummaries;
        public string[] ShapeSummaries;
        public string[] EffectSummaries;
        public string[] DeliverySummaries;

        // Character played by each role, parallel to RoleIds. An invalid reference means the
        // role has no character yet, so its Genesis cannot enter a match.
        public AssetRef<EntityPrototype>[] RolePrototypes;

        // Special attack fired by each combination of form, delivery, shape and effect, at
        // position ((form * ExpressionIds.Length + expression) * ShapeIds.Length + shape)
        // * EffectIds.Length + effect. A second form that was not authored plays as the first
        // form; a delivery that has no signature of its own uses the first one (direct); with
        // none at all, the special of the character's own class stays in place.
        public AssetRef<SkillData>[] SignatureSkills;

        // Every shape has a second form, another way to throw the same attack: its name and one
        // sentence about it, parallel to ShapeIds. A hero plays the first form unless its player
        // equipped the second one.
        public const int FormCount = 2;

        // The delivery of every Radioactive Genesis, whose special is the radioactive burst (22 October 2026).
        public const string RadioactiveExpression = "persistent-zone";
        public string[] SecondFormNames;
        public string[] SecondFormSummaries;

#if QUANTUM_UNITY
        // What the view shows on a target touched by a signature, parallel to EffectIds: the sign
        // of the effect in its colour, at the moment of the hit and, smaller, while it lasts.
        public GenesisEffectIcon[] EffectIcons;
        public UnityEngine.Color[] EffectColors;

        // The unlit, vertex-coloured material every drawn shape uses.
        public UnityEngine.Material ShapeMaterial;

        // How the eyes of a Genesis colour its own attack, parallel to EyeValues. A transparent
        // Outer colour means nothing was authored for that value yet.
        public GenesisEyeLook[] EyeLooks;

        // The colour of each Skin value, parallel to SkinValues (transparent when unknown), and
        // the unlit material the real image of a Genesis is drawn with on its hero's head.
        public UnityEngine.Color[] SkinColors;
        public UnityEngine.Material FaceMaterial;

        // The 1:1 Genesis carry no trait: the look of each is authored for its token.
        public GenesisUniqueLook[] UniqueLooks;

        // What every Radioactive Genesis fires (22 October 2026): its burst draws the sign of radioactivity in the
        // colours of radiation, not its eyes, and its hero takes that glow. Unused when its Glyph is None.
        public GenesisEyeLook RadioactiveLook;
        public UnityEngine.Color RadioactiveSkin;

        // The eyes and the colour of a hero: its own when it is a 1:1, else those of its Eyes and
        // Skin values. What is unknown stays transparent.
        public void LookOf(GenesisRosterEntry hero, out GenesisEyeLook look, out UnityEngine.Color skin)
        {
            look = default;
            skin = default;
            if (UniqueLooks != null)
            {
                for (var i = 0; i < UniqueLooks.Length; i++)
                {
                    if (UniqueLooks[i].TokenId != hero.TokenId) continue;
                    look = UniqueLooks[i].Look;
                    skin = UniqueLooks[i].Skin;
                    return;
                }
            }

            if (RadioactiveLook.Glyph != GenesisEyeGlyph.None && ExpressionIds != null && hero.Expression < ExpressionIds.Length
                && ExpressionIds[hero.Expression] == RadioactiveExpression)
            {
                look = RadioactiveLook;
                skin = RadioactiveSkin;
                return;
            }

            if (EyeLooks != null && hero.Eye < EyeLooks.Length) look = EyeLooks[hero.Eye];
            if (SkinColors != null && hero.Skin < SkinColors.Length) skin = SkinColors[hero.Skin];
        }
#endif

        // Sorted by TokenId.
        public GenesisRosterEntry[] Entries;

        public int IndexOf(int tokenId)
        {
            if (Entries == null) return -1;
            int low = 0, high = Entries.Length - 1;
            while (low <= high)
            {
                var middle = low + (high - low) / 2;
                var candidate = Entries[middle].TokenId;
                if (candidate == tokenId) return middle;
                if (candidate < tokenId) low = middle + 1;
                else high = middle - 1;
            }
            return -1;
        }

        public bool TryGetPrototype(byte role, out AssetRef<EntityPrototype> prototype)
        {
            prototype = default;
            if (RolePrototypes == null || role >= RolePrototypes.Length) return false;
            prototype = RolePrototypes[role];
            return prototype.IsValid;
        }

        // The three lines of the hero card: a trait of the Genesis, its value as written in the
        // metadata ("none" when absent), what that gives in play, and one sentence about it.
        // A 1:1 has no trait values: its kit was authored for it, and the lines say so.
        // Name of the card line whose "Gives" is the effect, written in the effect's colour.
        public const string EffectTrait = "Aura";

        // "form" is the form of the signature the card describes: 0 the first one, 1 the second.
        public GenesisCardLine[] CardLines(GenesisRosterEntry hero, int form = 0)
        {
            var authored = hero.Body == 0 && hero.Eye == 0 && hero.Aura == 0 && IsLast(TierNames, hero.Tier);
            var eyes = CardLine("Eyes", authored, EyeValues, hero.Eye, ShapeIds, ShapeSummaries, hero.Shape);
            // A Radioactive Genesis fires its radioactive burst whatever its eyes: the card says where it comes from.
            if (ExpressionIds != null && hero.Expression < ExpressionIds.Length && ExpressionIds[hero.Expression] == RadioactiveExpression)
            {
                eyes.Trait = "Radioactivity";
                eyes.Value = "Radioactive";
                eyes.Gives = "radioactive burst";
                eyes.Summary = "a burst of radioactivity all around you, then a toxic zone";
            }
            else if (form == 1 && HasSecondForm(hero.Shape))
            {
                eyes.Gives = SecondFormNames[hero.Shape];
                eyes.Summary = SecondFormSummaries != null && hero.Shape < SecondFormSummaries.Length
                    ? SecondFormSummaries[hero.Shape] ?? "" : "";
            }

            return new[]
            {
                CardLine("Outfit", authored, BodyValues, hero.Body, RoleIds, RoleSummaries, hero.Role),
                eyes,
                CardLine(EffectTrait, authored, AuraValues, hero.Aura, EffectIds, EffectSummaries, hero.Effect),
                new GenesisCardLine
                {
                    Trait = "Rarity",
                    Value = TierNames != null && hero.Tier < TierNames.Length ? TierNames[hero.Tier] : "?",
                    Gives = ExpressionIds != null && hero.Expression < ExpressionIds.Length ? ExpressionIds[hero.Expression] : "?",
                    Summary = DeliverySummaries != null && hero.Expression < DeliverySummaries.Length
                        ? DeliverySummaries[hero.Expression] ?? "" : ""
                }
            };
        }

        private static GenesisCardLine CardLine(string trait, bool authored, string[] values, byte value,
                                                string[] ids, string[] summaries, byte id)
        {
            var written = values != null && value > 0 && value < values.Length ? values[value] : "none";
            return new GenesisCardLine
            {
                Trait = trait,
                Value = authored ? "one of one" : written,
                Gives = ids != null && id < ids.Length ? ids[id] : "?",
                Summary = summaries != null && id < summaries.Length ? summaries[id] ?? "" : ""
            };
        }

        private static bool IsLast(string[] values, byte position)
        {
            return values != null && values.Length > 0 && position == values.Length - 1;
        }

        // Whether a second form was authored for this shape.
        public bool HasSecondForm(byte shape)
        {
            return SecondFormNames != null && shape < SecondFormNames.Length && !string.IsNullOrEmpty(SecondFormNames[shape]);
        }

        // Name of the form a hero plays, for a button or a list: the shape itself for the first
        // form, the name of the second form otherwise.
        public string FormName(byte shape, int form)
        {
            if (form == 1 && HasSecondForm(shape)) return SecondFormNames[shape];
            return ShapeIds != null && shape < ShapeIds.Length ? ShapeIds[shape] : "?";
        }

        public bool TryGetSignature(byte form, byte expression, byte shape, byte effect, out AssetRef<SkillData> skill)
        {
            skill = default;
            if (SignatureSkills == null || EffectIds == null || ShapeIds == null || ExpressionIds == null) return false;
            if (effect >= EffectIds.Length || shape >= ShapeIds.Length) return false;

            if (TryGetAt(form, expression, shape, effect, out skill)) return true;
            // A second form that was not authored plays as the first form of the same delivery.
            if (form > 0 && TryGetAt(0, expression, shape, effect, out skill)) return true;
            // A delivery without its own signature plays as the first one.
            return TryGetAt(0, 0, shape, effect, out skill);
        }

        private bool TryGetAt(int form, int expression, int shape, int effect, out AssetRef<SkillData> skill)
        {
            skill = default;
            var position = ((form * ExpressionIds.Length + expression) * ShapeIds.Length + shape) * EffectIds.Length + effect;
            if (position < 0 || position >= SignatureSkills.Length || !SignatureSkills[position].IsValid) return false;
            skill = SignatureSkills[position];
            return true;
        }
    }
}
