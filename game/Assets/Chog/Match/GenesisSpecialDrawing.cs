using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// What a special looks like, from the caster's own eyes, wherever it is drawn: in a match
    /// (GenesisAttackView reads the attack from the frame) and on the test page of ChogSpecialsViewer
    /// (which plays it without a match). In 3D (GenesisSpecial3D) when the device can draw it: the laser of
    /// laser eyes, the fan of light of a visor or glasses, the tear that is lobbed and the puddle it leaves,
    /// the eyeballs of a stare, the ring of eyeballs of a burst. Else, or on a private page with
    /// "special3d=0", the flat shapes of before (GenesisBeam, GenesisShape). The simulation decides where
    /// an attack goes and what it hits; this only draws it.
    /// </summary>
    public sealed class GenesisSpecialDrawing
    {
        /// <summary>The fan of light opens this many degrees on each side.</summary>
        public const float FanHalfAngle = 32f;

        // Radius of the area the two-beam laser was drawn for.
        private const float LaserRadius = 0.35f;
        private const float DropRadius = 0.45f;

        // A stare drawn flat: the pair of eyes hovers above the area it hits.
        private const float StareHeight = 2f;

        // A burst drawn flat: this many eyes open from the hero to the rim of the area.
        private const int BurstEyes = 8;
        private const float BurstEyeRadius = 0.5f;
        private const float BurstHeight = 1.4f;
        private const float BurstSeconds = 0.3f;

        // Eyes without authored colours (a 1:1, or a family not described yet) draw in white.
        private static readonly GenesisEyeLook PlainLook = new GenesisEyeLook
        {
            Outline = new Color(0.25f, 0.25f, 0.3f), Outer = new Color(0.9f, 0.9f, 0.95f), Core = Color.white
        };

        // Eyes drawn as an expression when the roster says nothing of them: plain dark eyes.
        private static readonly GenesisEyeLook PlainEyes = new GenesisEyeLook
        {
            Outline = new Color(0.08f, 0.08f, 0.1f), Outer = new Color(0.08f, 0.08f, 0.1f), Core = Color.white,
            Glyph = GenesisEyeGlyph.Ball
        };

        // An enemy's attack keeps the colours of its eyes but is outlined in red.
        private static readonly Color EnemyOutline = new Color(0.95f, 0.1f, 0.1f);

        /// <summary>What is known of an attack when it appears.</summary>
        public struct Cast
        {
            public GenesisVisualKind Kind;
            /// <summary>The material of the flat look (the roster's ShapeMaterial).</summary>
            public Material FlatMaterial;
            public GenesisEyeLook Look;
            public Color Skin;
            /// <summary>The attack's own tint (the colour of an Aura), transparent when it has none.</summary>
            public Color Tint;
            /// <summary>The radius of the area the attack hits (its VisualRadius).</summary>
            public float Radius;
            /// <summary>Where the attack is now, on the floor.</summary>
            public Vector3 Here;
            /// <summary>Where a travelling attack was created (the eyes a laser leaves from).</summary>
            public Vector3 Start;
            public Vector3 Forward;
            /// <summary>How far ahead of its caster a fan of light sits.</summary>
            public float Ahead;
            /// <summary>Where a lobbed tear lands.</summary>
            public Vector3 Target;
            /// <summary>The colour of the thrower's side (ChogTeamRing.Self, Ally or Enemy), for a tear's landing ring.</summary>
            public Color Side;
        }

        private readonly bool _flat;
        private GenesisVisualKind _kind;

        // 3D.
        private GenesisLaser _laser;
        private GenesisLightFan _fan;
        private GenesisTear _tear;
        private GenesisPuddle _puddle;
        private GenesisStare _stare;
        private GenesisLookBurst _lookBurst;

        // Flat.
        private GenesisBeam _beam;
        private GenesisShape _shape;
        private bool _shapeFollows;
        private bool _isDrop;
        private Vector3 _dropStart;
        private Vector3 _dropTarget;
        private float _dropHeight;
        private GenesisShape _eyes;
        private GenesisShape[] _burst;
        private float _burstStarted;
        private float _burstRadius;

        private GenesisSpecialDrawing(bool flat) => _flat = flat;

        /// <summary>Whether an attack of this kind is drawn as eyes (an expression) rather than as an object.</summary>
        public static bool IsExpression(GenesisVisualKind kind) => kind == GenesisVisualKind.Stare || kind == GenesisVisualKind.EyeBurst;

        /// <summary>Starts drawing an attack; null for a kind this does not draw (an effect prefab).</summary>
        public static GenesisSpecialDrawing Begin(in Cast cast)
        {
            if (cast.Kind == GenesisVisualKind.Effect)
                return null;
            var drawing = new GenesisSpecialDrawing(!ChogEffectMaterials.Available);
            drawing._kind = cast.Kind;
            if (drawing._flat)
            {
                if (cast.FlatMaterial == null)
                    return null;
                drawing.BeginFlat(cast);
            }
            else
                drawing.Begin3D(cast);
            return drawing;
        }

        private void Begin3D(in Cast cast)
        {
            var area = AreaLook(cast.Look, cast.Skin, cast.Tint);
            switch (cast.Kind)
            {
                case GenesisVisualKind.Beam:
                    _laser = GenesisLaser.Create(cast.Look, cast.Start, LaserThickness(cast.Radius));
                    break;
                case GenesisVisualKind.Fan:
                    _fan = GenesisLightFan.Create(cast.Look, cast.Here - Flat(cast.Forward) * cast.Ahead, cast.Forward, cast.Radius, FanHalfAngle);
                    break;
                case GenesisVisualKind.GroundDisc:
                    _puddle = GenesisPuddle.Create(cast.Look, cast.Here, cast.Radius);
                    break;
                case GenesisVisualKind.Drop:
                    _tear = GenesisTear.Create(cast.Look, cast.Here, cast.Target, cast.Side);
                    break;
                case GenesisVisualKind.Stare:
                    _stare = GenesisStare.Create(cast.Look, cast.Skin, area.Outer, area.Core, cast.Here, cast.Forward, cast.Radius);
                    break;
                case GenesisVisualKind.EyeBurst:
                    _lookBurst = GenesisLookBurst.Create(cast.Look, cast.Skin, area.Outer, area.Core, cast.Here, cast.Radius);
                    break;
            }
        }

        private void BeginFlat(in Cast cast)
        {
            var material = cast.FlatMaterial;
            var here = cast.Here;
            switch (cast.Kind)
            {
                case GenesisVisualKind.Beam:
                    _beam = GenesisBeam.Create(material, cast.Look, cast.Start, LaserThickness(cast.Radius));
                    break;

                case GenesisVisualKind.Fan:
                    // The attack sits ahead of its caster; the fan opens from the caster itself.
                    _shape = GenesisShape.Fan(material, cast.Look, here - Flat(cast.Forward) * cast.Ahead, cast.Forward, cast.Radius, FanHalfAngle);
                    break;

                case GenesisVisualKind.GroundDisc:
                    _shape = GenesisShape.GroundDisc(material, cast.Look, here, cast.Radius);
                    break;

                case GenesisVisualKind.Drop:
                    _isDrop = true;
                    _dropStart = here;
                    _dropTarget = cast.Target;
                    _dropHeight = 1.5f + 0.25f * Vector3.Distance(_dropStart, _dropTarget);
                    _shape = GenesisShape.Drop(material, cast.Look, here, DropRadius);
                    break;

                case GenesisVisualKind.Stare:
                    // The area it hits, on the ground, and above it the eyes that stare.
                    _shape = GenesisShape.GroundDisc(material, AreaLook(cast.Look, cast.Skin, cast.Tint), here, cast.Radius);
                    _shapeFollows = true;
                    _eyes = GenesisShape.EyePair(material, cast.Look, cast.Skin, here + Vector3.up * StareHeight, cast.Radius);
                    break;

                case GenesisVisualKind.EyeBurst:
                    _shape = GenesisShape.GroundDisc(material, AreaLook(cast.Look, cast.Skin, cast.Tint), here, cast.Radius);
                    _burst = new GenesisShape[BurstEyes];
                    _burstStarted = Time.time;
                    _burstRadius = cast.Radius;
                    for (var i = 0; i < BurstEyes; i++)
                        _burst[i] = GenesisShape.EyeBadge(material, cast.Look, cast.Skin, here, BurstEyeRadius, i % 2 == 1);
                    PlaceBurst(here);
                    break;
            }
        }

        /// <summary>The attack is now at "here" (on the floor).</summary>
        public void Follow(Vector3 here)
        {
            if (!_flat)
            {
                if (_laser != null)
                    _laser.SetHead(here);
                if (_tear != null)
                    _tear.Follow(here);
                if (_stare != null)
                    _stare.Follow(here);
                if (_puddle != null)
                    _puddle.Follow(here);
                if (_lookBurst != null)
                    _lookBurst.Follow(here);
                return;
            }

            if (_beam != null)
                _beam.SetHead(here);

            if (_shapeFollows && _shape != null)
                _shape.FollowOnGround(here);

            if (_eyes != null)
                _eyes.transform.position = here + Vector3.up * StareHeight;

            if (_burst != null)
                PlaceBurst(here);

            if (_isDrop && _shape != null)
            {
                // The simulation moves the drop along the ground; the arc is only drawn.
                var whole = Vector3.Distance(_dropStart, _dropTarget);
                var done = whole < 0.01f ? 1f : Mathf.Clamp01(Vector3.Distance(_dropStart, here) / whole);
                _shape.transform.position = here + Vector3.up * (0.8f + 4f * _dropHeight * done * (1f - done));
            }
        }

        /// <summary>The attack is over, last seen at "here": what it drew goes away.</summary>
        public void End(Vector3 here)
        {
            if (_laser != null)
            {
                _laser.SetHead(here);
                _laser.Release();
                _laser = null;
            }
            if (_fan != null)
            {
                _fan.Release();
                _fan = null;
            }
            if (_tear != null)
            {
                _tear.Release();
                _tear = null;
            }
            if (_puddle != null)
            {
                _puddle.Release();
                _puddle = null;
            }
            if (_stare != null)
            {
                _stare.Release();
                _stare = null;
            }
            if (_lookBurst != null)
            {
                _lookBurst.Release();
                _lookBurst = null;
            }

            if (_beam != null)
            {
                _beam.SetHead(here);
                _beam.Release();
                _beam = null;
            }
            if (_shape != null)
            {
                _shape.Release();
                _shape = null;
            }
            if (_eyes != null)
            {
                _eyes.Release();
                _eyes = null;
            }
            if (_burst != null)
            {
                foreach (var eye in _burst)
                    if (eye != null)
                        eye.Release();
                _burst = null;
            }
            _shapeFollows = false;
            _isDrop = false;
        }

        // The eyes open from close to the hero out to the rim of the area, fast then slower.
        private void PlaceBurst(Vector3 here)
        {
            var done = Mathf.Clamp01((Time.time - _burstStarted) / BurstSeconds);
            var eased = 1f - (1f - done) * (1f - done);
            var radius = Mathf.Lerp(_burstRadius * 0.3f, _burstRadius * 0.85f, eased);
            var center = here + Vector3.up * BurstHeight;
            for (var i = 0; i < _burst.Length; i++)
            {
                if (_burst[i] == null)
                    continue;

                var angle = i / (float)_burst.Length * Mathf.PI * 2f;
                _burst[i].transform.position = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            }
        }

        /// <summary>A beam whose area is wider than the laser's is drawn that much thicker.</summary>
        public static float LaserThickness(float radius) => radius > LaserRadius ? radius / LaserRadius : 1f;

        /// <summary>
        /// The colours of the caster's own eyes, from the roster entry of its Genesis (null when it has none):
        /// without them, the colour of the attack's tint (its Aura), or plain dark eyes for an attack drawn
        /// as an expression. An attack of the other team is outlined in red whatever its colours. Also gives
        /// the colour of the caster's skin, when known.
        /// </summary>
        public static GenesisEyeLook LookOf(GenesisRoster roster, GenesisRosterEntry? hero, Color tint, bool isExpression,
                                            bool enemy, out Color skin)
        {
            var look = isExpression ? PlainEyes : PlainLook;
            skin = Color.clear;
            var authored = isExpression;
            if (roster != null && hero.HasValue)
            {
                roster.LookOf(hero.Value, out var own, out skin);
                // An expression needs its drawing; an object needs its colours, and the
                // colours of an expression are those of an eye, not of a beam or a zone.
                if (isExpression ? own.Glyph != GenesisEyeGlyph.None : own.Glyph == GenesisEyeGlyph.None && own.Outer.a > 0f)
                {
                    look = own;
                    authored = true;
                }
            }

            if (!authored && tint.a > 0f)
            {
                look = new GenesisEyeLook
                {
                    Outline = tint * 0.4f + new Color(0f, 0f, 0f, 1f),
                    Outer = tint,
                    Core = Color.Lerp(tint, Color.white, 0.65f)
                };
            }

            if (enemy)
                look.Outline = EnemyOutline;
            return look;
        }

        /// <summary>
        /// The area of an attack drawn with eyes: in the colour of the Aura when it has one, else in the
        /// colour of the hero's skin.
        /// </summary>
        public static GenesisEyeLook AreaLook(GenesisEyeLook look, Color skin, Color tint)
        {
            var color = tint.a > 0f ? tint : skin.a > 0f ? skin : PlainLook.Outer;
            return new GenesisEyeLook { Outline = look.Outline, Outer = color, Core = Color.Lerp(color, Color.white, 0.5f) };
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        }
    }
}
