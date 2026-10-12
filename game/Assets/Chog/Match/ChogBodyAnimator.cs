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
    }

    /// <summary>
    /// Moves a body made of rigid parts, without a skeleton, from what its hero does: it bobs and
    /// swings its arms as it runs, throws both arms forward when it attacks, raises them when its
    /// team wins and slumps when it loses. It works on any model that has a part named Body
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

        private const float RunBlendPerSecond = 9f;
        private const float MoodSecondsToSettle = 0.45f;

        private Transform _body, _head, _armLeft, _armRight;
        private Vector3 _bodyPosition;
        private Quaternion _bodyRotation, _headRotation, _armLeftRotation, _armRightRotation;

        private float _cycle;
        private float _run;
        private float _attackLeft;
        private float _mood;
        private float _moodTarget;

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
            animator._bodyRotation = body.localRotation;
            animator._headRotation = animator._head == null ? Quaternion.identity : animator._head.localRotation;
            animator._armLeftRotation = armLeft.localRotation;
            animator._armRightRotation = armRight.localRotation;
            return animator;
        }

        /// <summary>The hero starts an attack: both arms are thrown forward once.</summary>
        public void Attack() => _attackLeft = AttackSeconds;

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
            var forward = -100f * thrown;
            pose.ArmLeftTurn = new Vector3(swing * 55f * run + forward + 12f * slump, 0f, -150f * cheer + hop * 12f);
            pose.ArmRightTurn = new Vector3(-swing * 55f * run + forward + 12f * slump, 0f, 150f * cheer - hop * 12f);
            return pose;
        }

        private void LateUpdate()
        {
            var delta = Time.deltaTime;
            _cycle = Mathf.Repeat(_cycle + Speed / StrideLength * 2f * Mathf.PI * delta, 2f * Mathf.PI);
            _run = Mathf.MoveTowards(_run, Mathf.Clamp01(Speed / FullRunSpeed), RunBlendPerSecond * delta);
            _attackLeft = Mathf.Max(0f, _attackLeft - delta);
            _mood = Mathf.MoveTowards(_mood, _moodTarget, delta / MoodSecondsToSettle);
            // A body that stands still stops mid-stride: bring it back to rest between two steps.
            if (Speed <= 0f)
                _cycle = Mathf.MoveTowards(_cycle, _cycle < Mathf.PI ? 0f : 2f * Mathf.PI, 6f * delta);

            Apply(Pose(_cycle, _run, _attackLeft > 0f ? 1f - _attackLeft / AttackSeconds : 0f, _mood, Time.time));
        }

        private void Apply(ChogBodyPose pose)
        {
            _body.localPosition = _bodyPosition + pose.BodyOffset;
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
