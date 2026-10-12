using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// View of an attack whose look comes from its data instead of its prefab. Either it shows
    /// the effect prefab named by the attack data, in the colour of its Aura, or it draws a shape
    /// from the caster's own eyes: the laser of laser eyes, the fan of light of a visor or a pair
    /// of glasses, the tear that is lobbed and the puddle it leaves, the pair of eyes of a stare,
    /// the ring of eyes of a burst. One prefab with this view serves every signature.
    /// </summary>
    public sealed unsafe class GenesisAttackView : QuantumEntityViewComponent
    {
        private const float FadeSeconds = 1f;
        private const float FanHalfAngle = 32f;
        // Radius of the area the two-beam laser was drawn for.
        private const float LaserRadius = 0.35f;
        private const float DropRadius = 0.45f;

        // A stare: the pair of eyes hovers above the area it hits.
        private const float StareHeight = 2f;

        // A burst: this many eyes open from the hero to the rim of the area.
        private const int BurstEyes = 8;
        private const float BurstEyeRadius = 0.5f;
        private const float BurstHeight = 1.4f;
        private const float BurstSeconds = 0.3f;

        // An enemy's attack keeps the colours of its eyes but is outlined in red.
        private static readonly Color EnemyOutline = new Color(0.95f, 0.1f, 0.1f);

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

        private GameObject _visual;
        private GenesisBeam _beam;
        private GenesisShape _shape;
        // The shape on the ground follows the attack.
        private bool _shapeFollows;

        // A lobbed drop: where it left from, where it lands, and how high it flies.
        private bool _isDrop;
        private Vector3 _dropStart;
        private Vector3 _dropTarget;
        private float _dropHeight;

        private GenesisShape _eyes;
        private GenesisShape[] _burst;
        private float _burstStarted;
        private float _burstRadius;

        public override void OnActivate(Frame frame)
        {
            var attack = frame.Unsafe.GetPointer<Attack>(EntityRef);
            var data = frame.FindAsset<AttackData>(attack->AttackData.Id);
            if (data == null || !GenesisEffects.Enabled)
                return;

            if (data.VisualKind == GenesisVisualKind.Effect || data.ShapeMaterial == null)
            {
                _visual = GenesisEffects.Spawn(data.Visual, transform, Vector3.zero, data.VisualScale, data.VisualTint);
                return;
            }

            var isExpression = data.VisualKind == GenesisVisualKind.Stare || data.VisualKind == GenesisVisualKind.EyeBurst;
            var look = LookOf(frame, attack->Source, data.VisualTint, isExpression, out var skin);
            var here = transform.position;
            switch (data.VisualKind)
            {
                case GenesisVisualKind.Beam:
                    // A beam whose area is wider than the laser's is drawn that much thicker.
                    var thickness = data.VisualRadius > LaserRadius ? data.VisualRadius / LaserRadius : 1f;
                    _beam = GenesisBeam.Create(data.ShapeMaterial, look, StartOf(frame, attack, data, here), thickness);
                    break;

                case GenesisVisualKind.Fan:
                    // The attack sits ahead of its caster; the fan opens from the caster itself.
                    var forward = frame.Get<Transform2D>(EntityRef).Up.ToUnityVector3();
                    var ahead = data is GenesisSweepAttackData sweep ? sweep.ForwardOffset.AsFloat : 0f;
                    _shape = GenesisShape.Fan(data.ShapeMaterial, look, here - forward.normalized * ahead, forward,
                        data.VisualRadius, FanHalfAngle);
                    break;

                case GenesisVisualKind.GroundDisc:
                    _shape = GenesisShape.GroundDisc(data.ShapeMaterial, look, here, data.VisualRadius);
                    break;

                case GenesisVisualKind.Drop:
                    _isDrop = true;
                    _dropStart = here;
                    _dropTarget = data is BallisticAttackData lob ? lob.GetTargetPosition(frame, attack).ToUnityVector3() : here;
                    _dropHeight = 1.5f + 0.25f * Vector3.Distance(_dropStart, _dropTarget);
                    _shape = GenesisShape.Drop(data.ShapeMaterial, look, here, DropRadius);
                    break;

                case GenesisVisualKind.Stare:
                    // The area it hits, on the ground, and above it the eyes that stare.
                    _shape = GenesisShape.GroundDisc(data.ShapeMaterial, AreaLook(look, skin, data.VisualTint), here, data.VisualRadius);
                    _shapeFollows = true;
                    _eyes = GenesisShape.EyePair(data.ShapeMaterial, look, skin, here + Vector3.up * StareHeight, data.VisualRadius);
                    break;

                case GenesisVisualKind.EyeBurst:
                    _shape = GenesisShape.GroundDisc(data.ShapeMaterial, AreaLook(look, skin, data.VisualTint), here, data.VisualRadius);
                    _burst = new GenesisShape[BurstEyes];
                    _burstStarted = Time.time;
                    _burstRadius = data.VisualRadius;
                    for (var i = 0; i < BurstEyes; i++)
                        _burst[i] = GenesisShape.EyeBadge(data.ShapeMaterial, look, skin, here, BurstEyeRadius, i % 2 == 1);
                    PlaceBurst();
                    break;
            }
        }

        // Where a travelling attack was created. This view can wake up a few ticks after its
        // attack, which has moved by then: a beam drawn from "here" would start ahead of the eyes.
        private Vector3 StartOf(Frame frame, Attack* attack, AttackData data, Vector3 here)
        {
            if (!(data is GenesisSweepAttackData sweep) || sweep.Velocity == 0)
                return here;

            var now = frame.Get<Transform2D>(EntityRef);
            var start = (now.Position - now.Up * sweep.Velocity * attack->TTL).ToUnityVector3();
            start.y = here.y;
            return start;
        }

        public override void OnUpdateView()
        {
            if (_beam != null)
                _beam.SetHead(transform.position);

            if (_shapeFollows && _shape != null)
                _shape.FollowOnGround(transform.position);

            if (_eyes != null)
                _eyes.transform.position = transform.position + Vector3.up * StareHeight;

            if (_burst != null)
                PlaceBurst();

            if (_isDrop && _shape != null)
            {
                // The simulation moves the drop along the ground; the arc is only drawn.
                var whole = Vector3.Distance(_dropStart, _dropTarget);
                var done = whole < 0.01f ? 1f : Mathf.Clamp01(Vector3.Distance(_dropStart, transform.position) / whole);
                _shape.transform.position = transform.position + Vector3.up * (0.8f + 4f * _dropHeight * done * (1f - done));
            }
        }

        public override void OnDeactivate()
        {
            if (_beam != null)
            {
                _beam.SetHead(transform.position);
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
            if (_visual == null)
                return;

            // This object goes away with the attack; the effect stays in the scene for a moment.
            _visual.transform.SetParent(null, true);
            foreach (var particles in _visual.GetComponentsInChildren<ParticleSystem>())
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(_visual, FadeSeconds);
            _visual = null;
        }

        // The eyes open from close to the hero out to the rim of the area, fast then slower.
        private void PlaceBurst()
        {
            var done = Mathf.Clamp01((Time.time - _burstStarted) / BurstSeconds);
            var eased = 1f - (1f - done) * (1f - done);
            var radius = Mathf.Lerp(_burstRadius * 0.3f, _burstRadius * 0.85f, eased);
            var center = transform.position + Vector3.up * BurstHeight;
            for (var i = 0; i < _burst.Length; i++)
            {
                if (_burst[i] == null)
                    continue;

                var angle = i / (float)_burst.Length * Mathf.PI * 2f;
                _burst[i].transform.position = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            }
        }

        // The area of an attack drawn with eyes: in the colour of the Aura when it has one, else
        // in the colour of the hero's skin.
        private static GenesisEyeLook AreaLook(GenesisEyeLook look, Color skin, Color auraColor)
        {
            var color = auraColor.a > 0f ? auraColor : skin.a > 0f ? skin : PlainLook.Outer;
            return new GenesisEyeLook { Outline = look.Outline, Outer = color, Core = Color.Lerp(color, Color.white, 0.5f) };
        }

        // The colours of the caster's own eyes; without them, the colour of the Aura, or plain
        // dark eyes for an attack drawn as an expression. An attack of the other team is outlined
        // in red whatever its colours. Also gives the colour of the caster's skin, when known.
        private GenesisEyeLook LookOf(Frame frame, EntityRef caster, Color auraColor, bool isExpression, out Color skin)
        {
            var look = isExpression ? PlainEyes : PlainLook;
            skin = Color.clear;
            var roster = GenesisCast.Roster(frame);
            var authored = isExpression;
            if (roster != null && frame.TryGet<GenesisHero>(caster, out var hero))
            {
                var index = roster.IndexOf(hero.TokenId);
                if (index >= 0)
                {
                    roster.LookOf(roster.Entries[index], out var own, out skin);
                    // An expression needs its drawing; an object needs its colours, and the
                    // colours of an expression are those of an eye, not of a beam or a zone.
                    if (isExpression ? own.Glyph != GenesisEyeGlyph.None : own.Glyph == GenesisEyeGlyph.None && own.Outer.a > 0f)
                    {
                        look = own;
                        authored = true;
                    }
                }
            }

            if (!authored && auraColor.a > 0f)
            {
                look = new GenesisEyeLook
                {
                    Outline = auraColor * 0.4f + new Color(0f, 0f, 0f, 1f),
                    Outer = auraColor,
                    Core = Color.Lerp(auraColor, Color.white, 0.65f)
                };
            }

            if (IsEnemyOfLocalPlayer(frame, caster))
                look.Outline = EnemyOutline;
            return look;
        }

        private bool IsEnemyOfLocalPlayer(Frame frame, EntityRef caster)
        {
            if (!frame.TryGet<TeamInfo>(caster, out var casterTeam))
                return false;

            var characters = frame.Filter<PlayerLink, TeamInfo>();
            while (characters.Next(out _, out var link, out var team))
                if (Game.PlayerIsLocal(link.PlayerRef))
                    return team.Index != casterTeam.Index;
            return false;
        }
    }
}
