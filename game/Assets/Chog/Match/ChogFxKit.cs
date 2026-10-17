using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The effects of a fight, each read from the traits of the Genesis it comes from (ChogFxRecipes): where
    /// a basic attack ends, the object of its hat bursts in its own way (a coin pops into coins and sparkles,
    /// a cap into cloth and a puff, a Thunder Cap's bolt calls lightning from the sky); a special lights up its
    /// caster's eyes and wears its Aura (fire, ice, lightning, stars, smoke, bubbles) and its rarity (an echo's
    /// rings, a Skull's dark flash, a Radioactive glow, a 1:1's gold); its target takes the Aura's effect, and
    /// keeps it while it lasts; a hero that falls goes in a poof, comes back in a column of light; a tower or a
    /// core that falls blows up. Built from the pooled particle effects of GenesisEffects, the faked lights of
    /// ChogLight, the bolts of ChogLightning and the pieces of ChogImpact. On a phone the second layers, the
    /// lasting effects and the forks of the bolts are left out ("Lighter"); with "?fxlevel=0" on a private
    /// page only the pieces of before are drawn.
    /// </summary>
    public static class ChogFxKit
    {
        /// <summary>How a basic attack is delivered: thrown (the ranger), struck in front (the brute), lobbed onto the floor (the controller).</summary>
        public enum Gesture : byte { Thrown, Strike, Lob }

        // The eyes of a hero over its feet, in the arena (GenesisLaser draws its beams from there).
        private const float EyeHeight = 1.2f;
        // A flash is brought this far toward the camera, so the hero it lights does not cut it.
        private const float TowardCamera = 0.8f;
        // The flash of a basic attack, over the size its recipe gives.
        private const float BasicFlash = 0.6f;

        private static readonly Color[] Rainbow =
        {
            new Color(0.90f, 0.16f, 0.16f), new Color(0.97f, 0.55f, 0.12f), new Color(0.98f, 0.86f, 0.18f),
            new Color(0.24f, 0.75f, 0.30f), new Color(0.20f, 0.50f, 0.92f), new Color(0.55f, 0.28f, 0.82f)
        };
        private static readonly Color Thunder = new Color(1f, 0.9f, 0.35f);
        private static readonly Color Dust = new Color(0.86f, 0.80f, 0.92f);
        private static readonly Color Fire = new Color(1f, 0.55f, 0.18f);
        private static readonly Color Stone = new Color(0.78f, 0.74f, 0.86f);
        private static readonly Color Heart = new Color(0.98f, 0.40f, 0.62f);

        // ---- Basic attacks --------------------------------------------------------------------------

        /// <summary>
        /// A basic attack of the hero of "metadataId" ended at "at" (where its object was): the burst of its
        /// object. "side" is the colour of the thrower's side; "fallback" the colour of before when the library
        /// has no object for that hero.
        /// </summary>
        public static void AttackEnds(int metadataId, Vector3 at, Gesture gesture, Color side, Color fallback)
        {
            if (!GenesisEffects.Enabled)
                return;
            if (!ChogHeroModel.TryGetAttackParts(metadataId, out var parts))
            {
                ChogImpact.Burst(at, fallback);
                return;
            }
            if (!GenesisEffects.Rich)
            {
                ChogImpact.Burst(at, parts.Main);
                return;
            }
            AttackEnds(ChogFxRecipes.ForAttack(parts.Piece), parts, at, gesture, side);
        }

        /// <summary>The same with the recipe and parts given (the test page plays every recipe).</summary>
        public static void AttackEnds(in ImpactRecipe recipe, in ChogHeroModel.AttackParts parts, Vector3 at, Gesture gesture, Color side)
        {
            var lighter = GenesisEffects.Lighter;
            var size = gesture == Gesture.Strike ? 1.2f : gesture == Gesture.Lob ? 1.15f : 1f;
            var floor = GenesisSpecial3D.OnFloor(at);

            var look = new ChogImpact.Look
            {
                Pieces = recipe.Pieces,
                Count = lighter ? Mathf.CeilToInt(recipe.Count * 0.6f) : recipe.Count,
                Size = recipe.PieceSize * size,
                Speed = size,
                Rise = recipe.Rise,
                Colour = parts.Main,
                Other = parts.Second
            };
            if (recipe.PieceColour.a > 0f)
                look.Colour = look.Other = recipe.PieceColour;
            if (recipe.Rainbow)
                look.Palette = Rainbow;
            if (recipe.Pieces == ImpactPieces.Copies)
            {
                look.Copy = string.IsNullOrEmpty(recipe.CopiesOf) ? parts.Mesh : ChogHeroModel.PieceMesh(recipe.CopiesOf, parts.Skin);
                if (look.Copy == null)
                    look.Pieces = ImpactPieces.Diamonds;
            }
            ChogImpact.Burst(at, look);

            // In front of what it hit, as the camera sees it, so the hero hit does not hide it.
            var front = Toward(at, TowardCamera);
            Play(recipe.Effect, front, parts.Main, size);
            if (!lighter && GenesisEffects.Quiet)
                Play(recipe.Extra, front, parts.Main, size);

            // A basic attack lands often: its flash is small and short, the big ones are for specials.
            var flare = recipe.FlareColour.a > 0f ? recipe.FlareColour : FxColours.Lighter(parts.Main, 0.35f);
            if (recipe.Flare > 0f)
                ChogLight.Flash(front, flare, recipe.Flare * size * BasicFlash, 0.18f, lighter ? Mathf.Min(4, recipe.Rays) : recipe.Rays);
            if (recipe.Ring > 0f)
                ChogLight.Ring(floor, parts.Main, recipe.Ring * size, 0.35f);
            if (recipe.Lightning)
            {
                var colour = recipe.Effect.Fixed.a > 0f ? recipe.Effect.Fixed : ChogFxRecipes.Key(parts.Piece) == "Bolt" ? Thunder : parts.Main;
                // From the top of the screen down onto the spot: the camera looks down steeply, so a bolt
                // from straight above would be seen end on.
                var sky = floor + GenesisSpecial3D.UpOnScreen() * 5.5f + Vector3.right * Random.Range(-1.2f, 1.2f);
                ChogLightning.Strike(sky, floor, colour, 0.26f, 0.12f, 2);
                ChogLight.Flash(Toward(floor + Vector3.up * 0.3f, TowardCamera), colour, 1.1f, 0.2f, 0);
            }

            switch (gesture)
            {
                case Gesture.Strike:
                    // The brute's blow lands on the floor in front of it: dust and a ring.
                    if (!lighter)
                        GenesisEffects.Play(ChogFx.GroundHit, floor, 0.45f, Dust, FxColour.Painted);
                    if (recipe.Ring <= 0f)
                        ChogLight.Ring(floor, parts.Main, 1.2f, 0.3f);
                    break;
                case Gesture.Lob:
                    // A lob lands on the floor: dust, and a ring in the colour of the thrower's side.
                    if (!lighter)
                        GenesisEffects.Play(ChogFx.GroundHit, floor, 0.5f, Dust, FxColour.Painted);
                    ChogLight.Ring(floor, side, 1.4f, 0.4f, 0.18f);
                    break;
            }
        }

        // ---- Specials -------------------------------------------------------------------------------

        /// <summary>
        /// A special fires from the hero standing at "hero", facing "forward": its eyes light up in their
        /// colours, its Aura shows at its eyes (lightning for haste), and its rarity adds its touch.
        /// </summary>
        public static void SpecialFires(Vector3 hero, Vector3 forward, GenesisEyeLook look, GenesisEffectIcon aura, Color auraColour,
                                        string delivery)
        {
            if (!GenesisEffects.Rich)
                return;
            var lighter = GenesisEffects.Lighter;
            var eyes = Toward(hero + Vector3.up * EyeHeight, TowardCamera);
            var eyeColour = look.Core.a > 0f ? look.Core : look.Outer;
            var recipe = ChogFxRecipes.ForAura(aura);
            var colour = auraColour.a > 0f ? auraColour : eyeColour;
            ChogLight.Flash(eyes, eyeColour, 1.3f, 0.32f, lighter ? 4 : 6);

            Play(recipe.Cast, eyes, colour, 1f, true);
            if (recipe.Lightning)
            {
                forward.y = 0f;
                forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
                var bolts = lighter ? 1 : 3;
                for (var b = 0; b < bolts; b++)
                {
                    var spread = Quaternion.Euler(0f, (b - (bolts - 1) * 0.5f) * 28f, 0f) * forward;
                    var end = hero + spread * Random.Range(2.4f, 3.4f) + Vector3.up * Random.Range(0.4f, 1.2f);
                    ChogLightning.Strike(hero + Vector3.up * EyeHeight, end, colour, 0.28f, 0.09f, 1);
                }
            }

            var rarity = ChogFxRecipes.ForDelivery(delivery);
            if (!lighter)
                Play(rarity.Cast, eyes, rarity.Cast.Fixed, 1f, true);
            if (rarity.Flash.a > 0f)
                ChogLight.Flash(eyes + Vector3.up * 0.2f, rarity.Flash, 1.8f, 0.4f, lighter ? 6 : rarity.Rays);
            var floor = GenesisSpecial3D.OnFloor(hero);
            for (var r = 0; r < rarity.Rings; r++)
            {
                var ring = rarity.Flash.a > 0f ? rarity.Flash : eyeColour;
                if (r == 0)
                    ChogLight.Ring(floor, ring, 2.2f, 0.4f);
                else
                    GenesisEffects.After(0.14f * r, () => ChogLight.Ring(floor, ring, 2.6f, 0.4f));
            }
        }

        /// <summary>The head of a laser stops: sparks in its colours where it ended.</summary>
        public static void LaserEnds(Vector3 head, Color colour, Color core)
        {
            if (!GenesisEffects.Rich)
                return;
            GenesisEffects.Play(ChogFx.Sparks, head, 0.55f, FxColours.Lighter(colour, 0.25f), FxColour.Tinted);
            ChogLight.Flash(Toward(head, 0.3f), core.a > 0f ? core : colour, 1.1f, 0.25f, GenesisEffects.Lighter ? 4 : 6);
        }

        /// <summary>A fan of light opens from the glasses at "eyes" toward "front": a camera's flash and a burst of light ahead.</summary>
        public static void FlashOpens(Vector3 eyes, Vector3 front, GenesisEyeLook look)
        {
            if (!GenesisEffects.Rich)
                return;
            var core = look.Core.a > 0f ? look.Core : Color.white;
            ChogLight.Flash(Toward(eyes, TowardCamera), core, 1.8f, 0.3f, GenesisEffects.Lighter ? 6 : 10);
            GenesisEffects.Play(ChogFx.Light, front + Vector3.up * 0.8f, 0.8f, FxColours.Lighter(look.Outer, 0.3f), FxColour.Tinted, important: true);
        }

        /// <summary>A tear lands at "at": a liquid splash in its colour, a ripple and a glint.</summary>
        public static void TearLands(Vector3 at, GenesisEyeLook look)
        {
            if (!GenesisEffects.Rich)
                return;
            var floor = GenesisSpecial3D.OnFloor(at);
            GenesisEffects.Play(ChogFx.Splash, floor + Vector3.up * 0.2f, 0.6f, look.Outer, FxColour.Painted, important: true);
            ChogLight.Ring(floor, look.Outer, 2.4f, 0.5f, 0.15f);
            ChogLight.Flash(Toward(floor + Vector3.up * 0.4f, 0.3f), look.Core.a > 0f ? look.Core : look.Outer, 1.2f, 0.25f, 0);
        }

        /// <summary>A burst of eyes opens around "at" over "radius": a flash in the middle, a shock wave of light and dust.</summary>
        public static void NovaOpens(Vector3 at, float radius, Color colour, Color core)
        {
            if (!GenesisEffects.Rich)
                return;
            var floor = GenesisSpecial3D.OnFloor(at);
            ChogLight.Ring(floor, colour, radius * 1.15f, 0.45f, 0.25f);
            ChogLight.Flash(Toward(floor + Vector3.up * 1.2f, TowardCamera), core.a > 0f ? core : colour, 2f, 0.3f, GenesisEffects.Lighter ? 6 : 10);
            if (!GenesisEffects.Lighter)
                GenesisEffects.Play(ChogFx.GroundHit, floor, 0.35f * radius, FxColours.Lighter(colour, 0.5f), FxColour.Painted, important: true);
        }

        /// <summary>A stare starts rolling from "at": a gust of dust behind it.</summary>
        public static void StareStarts(Vector3 at, Color colour)
        {
            if (!GenesisEffects.Rich || GenesisEffects.Lighter)
                return;
            var floor = GenesisSpecial3D.OnFloor(at);
            GenesisEffects.Play(ChogFx.GroundHit, floor, 0.45f, FxColours.Lighter(colour, 0.5f), FxColour.Painted);
        }

        /// <summary>
        /// A special touched a target (at "at", its view "target"): the Aura's effect on it, in the Aura's
        /// colour; while the effect lasts ("seconds"), its lasting effect follows it on a computer. Returns that
        /// lasting effect (to stop it when the effect ends), or null.
        /// </summary>
        public static ChogFxPlayback AuraHits(GenesisEffectIcon aura, Color colour, Transform target, Vector3 at, float seconds)
        {
            if (!GenesisEffects.Rich)
                return null;
            var recipe = ChogFxRecipes.ForAura(aura);
            var tint = colour.a > 0f ? colour : Color.white;
            Play(recipe.Hit, Toward(at, TowardCamera), tint, 1f, true);
            ChogLight.Flash(Toward(at, TowardCamera), tint, 1.1f, 0.26f, GenesisEffects.Lighter ? Mathf.Min(4, recipe.Rays) : recipe.Rays);
            var floor = GenesisSpecial3D.OnFloor(at);
            if (recipe.Ring)
                ChogLight.Ring(floor, tint, 2f, 0.45f);
            if (recipe.Lightning)
            {
                var arcs = GenesisEffects.Lighter ? 1 : 3;
                for (var a = 0; a < arcs; a++)
                {
                    var from = at + Random.insideUnitSphere * 0.9f + Vector3.up * 0.6f;
                    var to = floor + new Vector3(Random.Range(-0.8f, 0.8f), 0.05f, Random.Range(-0.8f, 0.8f));
                    ChogLightning.Strike(from, to, tint, 0.22f, 0.08f, 0);
                }
            }
            if (seconds <= 0f || !recipe.Lasting.On || GenesisEffects.Lighter || target == null)
                return null;
            // The runes of a hex lie on the floor under the rooted target; the rest rises from its body.
            var place = recipe.Lasting.Effect == ChogFx.Runes ? GenesisSpecial3D.OnFloor(target.position) + Vector3.up * 0.05f
                : Toward(target.position + Vector3.up * 0.7f, TowardCamera);
            var tintLasting = recipe.Lasting.Fixed.a > 0f ? recipe.Lasting.Fixed : tint;
            return GenesisEffects.Play(recipe.Lasting.Effect, place, recipe.Lasting.Size, tintLasting, recipe.Lasting.Colour, null, target, seconds, important: true);
        }

        // ---- Heroes and structures ------------------------------------------------------------------

        /// <summary>A hero falls at "at" (its middle): a poof of smoke in its colours, a cartoon star, a flash and a ring.</summary>
        public static void HeroFalls(Vector3 at, Color skin, Color side)
        {
            if (!GenesisEffects.Rich)
                return;
            var floor = GenesisSpecial3D.OnFloor(at);
            GenesisEffects.Play(ChogFx.Smoke, at, 1f, FxColours.Lighter(skin, 0.4f), FxColour.Painted, important: true);
            if (!GenesisEffects.Lighter)
                GenesisEffects.Play(ChogFx.Hit, at + Vector3.up * 0.3f, 0.8f, important: true);
            ChogLight.Flash(Toward(at, TowardCamera), FxColours.Lighter(skin, 0.5f), 1.8f, 0.35f, GenesisEffects.Lighter ? 4 : 8);
            ChogLight.Ring(floor, side, 2.4f, 0.45f);
        }

        /// <summary>A hero comes back at "at" (its feet): a column of light in the colour of its side, a ring and a soft light.</summary>
        public static void HeroReturns(Vector3 at, Color side)
        {
            if (!GenesisEffects.Rich)
                return;
            var floor = GenesisSpecial3D.OnFloor(at);
            ChogLight.Column(floor, side, 1.1f, 5f, 0.8f);
            ChogLight.Ring(floor, side, 2f, 0.5f);
            ChogLight.Flash(Toward(floor + Vector3.up * 1.1f, TowardCamera), FxColours.Lighter(side, 0.5f), 1.4f, 0.3f, 6);
            if (!GenesisEffects.Lighter)
                GenesisEffects.Play(ChogFx.LightSoft, floor + Vector3.up * 1f, 0.8f, FxColours.Lighter(side, 0.3f), FxColour.Tinted, important: true);
        }

        /// <summary>A tower or core is hit at its top "top": chips of stone and a little flash (at most a few a second).</summary>
        public static void StructureHit(Vector3 top, Color side)
        {
            if (!GenesisEffects.Rich)
                return;
            if (!GenesisEffects.Lighter)
                GenesisEffects.Play(ChogFx.Debris, top, 0.35f, Stone, FxColour.Tinted);
            ChogLight.Flash(Toward(top, TowardCamera), FxColours.Lighter(side, 0.4f), 0.9f, 0.18f, 0);
        }

        /// <summary>
        /// A tower or a core falls at "at" (its foot), "height" tall: it blows up in several blasts, the light of
        /// them fills the floor, a shock wave runs out in the colour of its side, smoke stays; a core, a pink
        /// heart, breaks into hearts under a BOOM, with lightning.
        /// </summary>
        public static void StructureFalls(Vector3 at, float height, bool core, Color side)
        {
            if (!GenesisEffects.Rich)
                return;
            var lighter = GenesisEffects.Lighter;
            var floor = GenesisSpecial3D.OnFloor(at);
            var middle = at + Vector3.up * (height * 0.55f);
            var top = at + Vector3.up * height;
            GenesisEffects.Play(ChogFx.Explosion, middle, core ? 0.85f : 0.75f, important: true);
            ChogLight.Flash(Toward(middle, TowardCamera), Fire, core ? 2.6f : 2.2f, 0.45f, lighter ? 6 : 12);
            ChogLight.Ring(floor, side, core ? 5.5f : 4.5f, 0.6f, 0.2f);
            if (core)
            {
                GenesisEffects.Play(ChogFx.BrokenHeart, Toward(top + Vector3.up * 0.3f, TowardCamera), 0.9f, important: true);
                GenesisEffects.Play(ChogFx.Boom, Toward(top + Vector3.up * 1.4f, TowardCamera * 2f), 0.8f, important: true);
            }
            if (lighter)
                return;
            GenesisEffects.Play(ChogFx.Debris, middle, 0.7f, Stone, FxColour.Tinted, important: true);
            GenesisEffects.After(0.14f, () =>
            {
                GenesisEffects.Play(ChogFx.FireExplosion, top, 0.5f, important: true);
                ChogLight.Flash(Toward(top, TowardCamera), Fire, 1.6f, 0.35f, 8);
            });
            GenesisEffects.After(0.3f, () =>
            {
                GenesisEffects.Play(ChogFx.FireExplosion, middle + new Vector3(0.7f, -0.3f, 0f), 0.4f, important: true);
                ChogLight.Ring(floor, FxColours.Lighter(side, 0.4f), core ? 7f : 6f, 0.7f, 0.15f);
            });
            GenesisEffects.After(0.45f, () => GenesisEffects.Play(ChogFx.Smoke, middle, 1.3f, new Color(0.42f, 0.36f, 0.52f), FxColour.Painted, important: true));
            if (core)
                for (var b = 0; b < 4; b++)
                {
                    var angle = b * 90f + Random.Range(-25f, 25f);
                    var end = floor + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(2.5f, 4f);
                    ChogLightning.Strike(middle, end, Heart, 0.4f, 0.12f, 1);
                }
        }

        // ---- Helpers --------------------------------------------------------------------------------

        // A layer of a recipe at "at", in its own colour or the one given; "important" for the big moments.
        private static void Play(in FxLayer layer, Vector3 at, Color colour, float size, bool important = false)
        {
            if (!layer.On)
                return;
            var tint = layer.Fixed.a > 0f ? layer.Fixed : colour;
            // A puff painted in a dark colour reads as a hole: puffs are lighter than what they come from.
            if (layer.Colour == FxColour.Painted && layer.Fixed.a <= 0f)
                tint = FxColours.Lighter(tint, 0.35f);
            GenesisEffects.Play(layer.Effect, at, layer.Size * size, tint, layer.Colour, important: important);
        }

        // "point" brought that far toward the camera that watches it.
        private static Vector3 Toward(Vector3 point, float distance) => point + GenesisSpecial3D.TowardViewer(point) * distance;
    }
}
