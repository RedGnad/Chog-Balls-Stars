using System;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>What each part of a body adds to its rest pose, in its own axes: metres and degrees.</summary>
    public struct ChogBodyPose
    {
        public Vector3 BodyOffset;
        public Vector3 BodyTurn;
        public Vector3 HeadTurn;
        public Vector3 ArmLeftTurn;
        public Vector3 ArmRightTurn;
        // How the whole body is stretched, about its feet: (1, 1, 1) at rest; a hit squashes it.
        public Vector3 BodyScale;
    }

    /// <summary>
    /// Moves a body made of rigid parts, without a skeleton, from what its hero does: it bobs and
    /// swings its arms as it runs, throws both arms forward when it attacks, raises them when its
    /// team wins and slumps when it loses, squashes and springs back when it is hit, and can hold its
    /// pose for an instant (the hit stop of a blow that involves the local hero: only the pose waits,
    /// the hero keeps following the simulation). It works on any model that has a part named Body
    /// with, under it, parts whose names end with Head, ArmLeft and ArmRight (any letter case):
    /// the models of the sample are built that way, and so is the Chog body. It replaces the
    /// animation clips of the sample. The model looks along +Z with +Y up, arms hanging along -Y,
    /// and each arm turns about its own origin, which is therefore placed at the shoulder.
    /// The pose is a plain function of a few numbers, so it is tested without an engine.
    /// </summary>
    public sealed class ChogBodyAnimator : MonoBehaviour
    {
        // One full stride, both arms forward once each, for this distance run.
        public const float StrideLength = 2.4f;
        // The speed at which the run is at full swing, in units per second.
        public const float FullRunSpeed = 3f;
        public const float AttackSeconds = 0.3f;
        // A hit squashes the body to this height and this width, and it is back in FlinchSeconds.
        public const float FlinchHeight = 0.85f;
        public const float FlinchWidth = 1.1f;
        public const float FlinchSeconds = 0.25f;

        private const float RunBlendPerSecond = 9f;
        private const float MoodSecondsToSettle = 0.45f;

        private Transform _body, _head, _armLeft, _armRight;
        private Vector3 _bodyPosition;
        private Vector3 _bodyScale = Vector3.one;
        // The height of the feet in the body's own axes: a squash keeps them on the ground.
        private float _feet;
        private Quaternion _bodyRotation, _headRotation, _armLeftRotation, _armRightRotation;

        private float _cycle;
        private float _run;
        private float _attackLeft;
        private float _mood;
        private float _moodTarget;
        // Seconds since the last hit (below 0: none since it settled), the clock of the idle moves, and
        // how long the pose still waits.
        private float _flinch = -1f;
        private float _clock;
        private float _holdLeft;

        /// <summary>How fast the hero moves over the ground, in units per second.</summary>
        public float Speed { get; set; }

        /// <summary>
        /// Takes over the body found under "model". Null when the parts are not there: the
        /// model then keeps whatever animated it before.
        /// </summary>
        public static ChogBodyAnimator Attach(Transform model)
        {
            if (model == null)
                return null;

            var body = Find(model, "Body", exact: true);
            if (body == null)
                return null;

            // One model of the sample spells its left arm "ArnLeft".
            var armLeft = Find(model, "ArmLeft", exact: false) ?? Find(model, "ArnLeft", exact: false);
            var armRight = Find(model, "ArmRight", exact: false);
            if (armLeft == null || armRight == null)
                return null;

            // A model whose head and arms sit beside its body, not under it, is rearranged so
            // that they follow the body; where they stand does not change.
            var head = Find(model, "Head", exact: false);
            foreach (var part in new[] { head, armLeft, armRight })
                if (part != null && !part.IsChildOf(body))
                    part.SetParent(body, true);

            var animator = body.gameObject.AddComponent<ChogBodyAnimator>();
            animator._body = body;
            animator._head = head;
            animator._armLeft = armLeft;
            animator._armRight = armRight;
            animator._bodyPosition = body.localPosition;
            animator._bodyScale = body.localScale;
            var drawn = body.GetComponent<MeshFilter>();
            animator._feet = drawn != null && drawn.sharedMesh != null ? drawn.sharedMesh.bounds.min.y : 0f;
            animator._bodyRotation = body.localRotation;
            animator._headRotation = animator._head == null ? Quaternion.identity : animator._head.localRotation;
            animator._armLeftRotation = armLeft.localRotation;
            animator._armRightRotation = armRight.localRotation;
            return animator;
        }

        /// <summary>The hero starts an attack: both arms are thrown forward once.</summary>
        public void Attack() => _attackLeft = AttackSeconds;

        /// <summary>The hero was hit: the body squashes and springs back (FlinchSeconds).</summary>
        public void Flinch() => _flinch = 0f;

        /// <summary>
        /// The pose waits that long (the hit stop): nothing of the body moves, and the clocks of its run,
        /// its attack, its flinch and its idle start again where they stopped.
        /// </summary>
        public void Hold(float seconds) => _holdLeft = Mathf.Max(_holdLeft, seconds);

        /// <summary>Whether the pose is waiting (Hold).</summary>
        public bool Holding => _holdLeft > 0f;

        /// <summary>The match is won: arms up, hopping, until the body is gone.</summary>
        public void Cheer() => _moodTarget = 1f;

        /// <summary>The match is lost: the body slumps and stays so.</summary>
        public void Defeated() => _moodTarget = -1f;

        /// <summary>
        /// The pose of a body. "cycle" grows by 2π for each stride run, "run" goes from 0
        /// (standing) to 1 (full run), "strike" from 0 to 1 over one attack, "mood" is 0 in play,
        /// up to 1 for a win and down to -1 for a loss, and "time" is seconds, for what moves
        /// when nothing else does.
        /// </summary>
        public static ChogBodyPose Pose(float cycle, float run, float strike, float mood, float time)
            => Pose(cycle, run, strike, mood, time, -1f);

        /// <summary>
        /// The pose, with "flinch" the seconds since the body was hit (below 0 or from FlinchSeconds on:
        /// no squash).
        /// </summary>
        public static ChogBodyPose Pose(float cycle, float run, float strike, float mood, float time, float flinch)
        {
            run = Mathf.Clamp01(run);
            var swing = Mathf.Sin(cycle);
            // Arms out and back once over the attack, fastest at the start.
            var thrown = strike <= 0f || strike >= 1f ? 0f : Mathf.Sin(Mathf.PI * Mathf.Sqrt(strike));
            var cheer = Mathf.Clamp01(mood);
            var slump = Mathf.Clamp01(-mood);
            var hop = Mathf.Abs(Mathf.Sin(time * 7f)) * cheer;

            var pose = new ChogBodyPose();
            // Two steps a stride lift the body twice; standing, it only breathes.
            pose.BodyOffset = new Vector3(0f,
                Mathf.Abs(swing) * 0.07f * run + Mathf.Sin(time * 2.2f) * 0.012f * (1f - run) + hop * 0.14f - slump * 0.1f,
                thrown * 0.12f);
            // It leans into the run and into the blow, rolls a little from one foot to the other,
            // and folds forward when the match is lost.
            pose.BodyTurn = new Vector3(12f * run + 16f * thrown + 28f * slump, 0f, swing * 5f * run);
            pose.HeadTurn = new Vector3(-6f * run - 8f * thrown + 30f * slump, 0f, -swing * 3f * run);

            // A negative turn about X brings a hanging arm forward. The arms swing against each
            // other in a run and go forward together in an attack; about Z they rise sideways.
            // A win raises them in a V beside the head, a little forward, pumping with each hop (22 October
            // 2026: raised 150 degrees they met over the head and sank into it, the owner saw them merge).
            var forward = -100f * thrown - 25f * cheer;
            pose.ArmLeftTurn = new Vector3(swing * 55f * run + forward + 12f * slump, 0f, -115f * cheer + hop * 20f);
            pose.ArmRightTurn = new Vector3(-swing * 55f * run + forward + 12f * slump, 0f, 115f * cheer - hop * 20f);

            // Squashed at once, then a spring that overshoots once and settles.
            var squash = Squash(flinch);
            pose.BodyScale = new Vector3(1f + (FlinchWidth - 1f) * squash, 1f - (1f - FlinchHeight) * squash,
                1f + (FlinchWidth - 1f) * squash);
            return pose;
        }

        /// <summary>How squashed a body is "seconds" after a hit: 1 at the hit, 0 from FlinchSeconds on.</summary>
        public static float Squash(float seconds)
        {
            if (seconds < 0f || seconds >= FlinchSeconds)
                return 0f;
            // A damped spring: about 3.2 swings a second, nearly still by the end of FlinchSeconds; the last
            // part of the time fades it to exactly nothing so the body never jumps.
            var spring = Mathf.Exp(-14f * seconds) * Mathf.Cos(seconds * 2f * Mathf.PI * 3.2f);
            var end = Mathf.Clamp01((FlinchSeconds - seconds) / 0.05f);
            return spring * end;
        }

        private void LateUpdate()
        {
            var delta = Time.deltaTime;
            if (_holdLeft > 0f)
            {
                // The hit stop: the pose of the moment of the blow, its clocks stopped.
                _holdLeft = Mathf.Max(0f, _holdLeft - delta);
                Apply(PoseNow());
                return;
            }

            _clock += delta;
            if (_flinch >= 0f)
            {
                _flinch += delta;
                if (_flinch >= FlinchSeconds)
                    _flinch = -1f;
            }
            _cycle = Mathf.Repeat(_cycle + Speed / StrideLength * 2f * Mathf.PI * delta, 2f * Mathf.PI);
            _run = Mathf.MoveTowards(_run, Mathf.Clamp01(Speed / FullRunSpeed), RunBlendPerSecond * delta);
            _attackLeft = Mathf.Max(0f, _attackLeft - delta);
            _mood = Mathf.MoveTowards(_mood, _moodTarget, delta / MoodSecondsToSettle);
            // A body that stands still stops mid-stride: bring it back to rest between two steps.
            if (Speed <= 0f)
                _cycle = Mathf.MoveTowards(_cycle, _cycle < Mathf.PI ? 0f : 2f * Mathf.PI, 6f * delta);

            Apply(PoseNow());
        }

        private ChogBodyPose PoseNow() =>
            Pose(_cycle, _run, _attackLeft > 0f ? 1f - _attackLeft / AttackSeconds : 0f, _mood, _clock, _flinch);

        private void Apply(ChogBodyPose pose)
        {
            // A squash about the feet: the body is lowered by what its lower half lost.
            var scale = pose.BodyScale == Vector3.zero ? Vector3.one : pose.BodyScale;
            _body.localScale = Vector3.Scale(_bodyScale, scale);
            var keepFeet = _bodyRotation * new Vector3(0f, _feet * _bodyScale.y * (1f - scale.y), 0f);
            _body.localPosition = _bodyPosition + pose.BodyOffset + keepFeet;
            _body.localRotation = _bodyRotation * Quaternion.Euler(pose.BodyTurn);
            if (_head != null)
                _head.localRotation = _headRotation * Quaternion.Euler(pose.HeadTurn);
            _armLeft.localRotation = _armLeftRotation * Quaternion.Euler(pose.ArmLeftTurn);
            _armRight.localRotation = _armRightRotation * Quaternion.Euler(pose.ArmRightTurn);
        }

        // The first part under "root" with that name, or, when not exact, whose name ends with it.
        private static Transform Find(Transform root, string name, bool exact)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
            {
                if (part == root)
                    continue;

                if (exact ? string.Equals(part.name, name, StringComparison.OrdinalIgnoreCase)
                        : part.name.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                    return part;
            }

            return null;
        }
    }
}
