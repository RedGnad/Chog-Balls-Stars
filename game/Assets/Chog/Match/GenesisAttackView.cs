using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// View of an attack whose look comes from its data instead of its prefab. Either it shows
    /// the effect prefab named by the attack data, in the colour of its Aura, or it draws the special
    /// from the caster's own eyes (GenesisSpecialDrawing, in 3D): the laser of laser eyes, the fan of
    /// light of a visor or a pair of glasses, the tear that is lobbed and the puddle it leaves, the
    /// eyeballs of a stare, the ring of eyeballs of a burst. One prefab with this view serves every
    /// signature. It reads the attack from the frame; where it goes and what it hits stay the simulation's.
    /// </summary>
    public sealed unsafe class GenesisAttackView : QuantumEntityViewComponent
    {
        private const float FadeSeconds = 1f;

        private GameObject _visual;
        private GenesisSpecialDrawing _drawing;

        public override void OnActivate(Frame frame)
        {
            var attack = frame.Unsafe.GetPointer<Attack>(EntityRef);
            var data = frame.FindAsset<AttackData>(attack->AttackData.Id);
            if (data == null || !GenesisEffects.Enabled)
                return;

            // The moment of the special: the eyes it comes from, large above the hero (ChogSignatureFlash).
            var fires = false;
            var casterPlace = transform.position;
            var delivery = "";
            if (data.IsGenesisSignature && frame.TryGet<GenesisHero>(attack->Source, out var caster)
                && frame.TryGet<Transform2D>(attack->Source, out var casterAt))
            {
                casterPlace = casterAt.Position.ToUnityVector3();
                ChogSignatureFlash.ShowOnce(attack->Source, casterPlace, caster.MetadataId);
                fires = true;
                var roster = GenesisCast.Roster(frame);
                if (roster != null && roster.ExpressionIds != null && caster.Expression < roster.ExpressionIds.Length)
                    delivery = roster.ExpressionIds[caster.Expression];
            }

            if (data.VisualKind == GenesisVisualKind.Effect || data.ShapeMaterial == null)
            {
                _visual = GenesisEffects.Spawn(data.Visual, transform, Vector3.zero, data.VisualScale, data.VisualTint);
                return;
            }

            var here = transform.position;
            var enemy = IsEnemyOfLocalPlayer(frame, attack->Source);
            var look = GenesisSpecialDrawing.LookOf(GenesisCast.Roster(frame), EntryOf(frame, attack->Source), data.VisualTint,
                GenesisSpecialDrawing.IsExpression(data.VisualKind), enemy, out var skin);
            var cast = new GenesisSpecialDrawing.Cast
            {
                Kind = data.VisualKind,
                FlatMaterial = data.ShapeMaterial,
                Look = look,
                Skin = skin,
                Tint = data.VisualTint,
                Radius = data.VisualRadius,
                Here = here,
                Start = StartOf(frame, attack, data, here),
                Forward = frame.TryGet<Transform2D>(EntityRef, out var placed) ? placed.Up.ToUnityVector3() : Vector3.forward,
                Ahead = data is GenesisSweepAttackData sweep ? sweep.ForwardOffset.AsFloat : 0f,
                Target = data is BallisticAttackData lob ? lob.GetTargetPosition(frame, attack).ToUnityVector3() : here,
                Side = SideOf(frame, attack->Source, enemy),
                Fires = fires,
                Caster = new Vector3(casterPlace.x, here.y, casterPlace.z),
                Aura = AuraOf(frame, data.GenesisEffect),
                Delivery = delivery
            };
            _drawing = GenesisSpecialDrawing.Begin(cast);
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
            _drawing?.Follow(transform.position);
        }

        public override void OnDeactivate()
        {
            if (_drawing != null)
            {
                _drawing.End(transform.position);
                _drawing = null;
            }

            if (_visual == null)
                return;

            // This object goes away with the attack; the effect stays in the scene for a moment.
            _visual.transform.SetParent(null, true);
            foreach (var particles in _visual.GetComponentsInChildren<ParticleSystem>())
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(_visual, FadeSeconds);
            _visual = null;
        }

        // The sign of the Aura effect this attack carries (a position in the roster's effect table).
        private static GenesisEffectIcon AuraOf(Frame frame, byte effect)
        {
            var roster = GenesisCast.Roster(frame);
            return roster != null && roster.EffectIcons != null && effect < roster.EffectIcons.Length
                ? roster.EffectIcons[effect]
                : GenesisEffectIcon.Burst;
        }

        // The roster entry of the caster's Genesis, when it plays one.
        private static GenesisRosterEntry? EntryOf(Frame frame, EntityRef caster)
        {
            var roster = GenesisCast.Roster(frame);
            if (roster == null || !frame.TryGet<GenesisHero>(caster, out var hero))
                return null;
            var index = roster.IndexOf(hero.TokenId);
            return index >= 0 ? roster.Entries[index] : (GenesisRosterEntry?)null;
        }

        // The colour of the caster's side as the local player reads it: its own, its team's, the other team's.
        private Color SideOf(Frame frame, EntityRef caster, bool enemy)
        {
            if (enemy)
                return ChogTeamRing.Enemy;
            return frame.TryGet<PlayerLink>(caster, out var link) && Game.PlayerIsLocal(link.PlayerRef)
                ? ChogTeamRing.Self
                : ChogTeamRing.Ally;
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
