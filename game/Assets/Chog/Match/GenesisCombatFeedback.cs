using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Shows what a signature did to the target it touched: the sign of the Aura effect, in its
    /// colour, pops on the target at the moment of the hit, and, for an effect that lasts (burn,
    /// chill, hex, toxin), a smaller one stays above it until the effect ends. The signs are
    /// drawn (GenesisIcons); which sign and which colour is listed on the roster asset.
    /// </summary>
    public sealed class GenesisCombatFeedback : MonoBehaviour
    {
        private const float HitSize = 0.6f;
        private const float HitSeconds = 0.35f;
        private const float StatusSize = 0.3f;

        // Where the signs sit, from the feet of the target: on its body, and beside its head.
        private static readonly Vector3 BodyOffset = new Vector3(0f, 1.2f, 0f);
        private static readonly Vector3 StatusOffset = new Vector3(0.95f, 2.3f, 0f);

        private sealed class Status
        {
            public GenesisShape Mark;
            public Transform Target;
            public float EndsAt;
        }

        // One lasting mark per target and effect; a new hit renews it instead of stacking another.
        private readonly Dictionary<(EntityRef, byte), Status> _statuses = new Dictionary<(EntityRef, byte), Status>();
        private readonly List<(EntityRef, byte)> _ended = new List<(EntityRef, byte)>();
        private QuantumEntityViewUpdater _views;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Combat Feedback");
            DontDestroyOnLoad(host);
            host.AddComponent<GenesisCombatFeedback>();
        }

        private void Start() => QuantumEvent.Subscribe<EventGenesisSignatureHit>(this, OnSignatureHit);

        private void OnSignatureHit(EventGenesisSignatureHit hit)
        {
            if (!GenesisEffects.Enabled)
                return;

            var roster = QuantumUnityDB.GetGlobalAsset(hit.Game.Frames.Verified.RuntimeConfig.GenesisRoster);
            var target = ViewOf(hit.Target);
            if (roster == null || roster.ShapeMaterial == null || target == null)
                return;

            var icon = At(roster.EffectIcons, hit.Effect, GenesisEffectIcon.Burst);
            var color = At(roster.EffectColors, hit.Effect, Color.clear);
            GenesisShape.Icon(roster.ShapeMaterial, icon, color, target.position + BodyOffset, HitSize, HitSeconds);

            var seconds = hit.Duration.AsFloat;
            if (seconds <= 0f)
                return;

            var key = (hit.Target, hit.Effect);
            if (_statuses.TryGetValue(key, out var status) && status.Mark != null)
            {
                status.EndsAt = Time.time + seconds;
                return;
            }

            _statuses[key] = new Status
            {
                Mark = GenesisShape.Icon(roster.ShapeMaterial, icon, color, target.position + StatusOffset, StatusSize, 0f),
                Target = target,
                EndsAt = Time.time + seconds
            };
        }

        private void LateUpdate()
        {
            if (_statuses.Count == 0)
                return;

            foreach (var entry in _statuses)
            {
                var status = entry.Value;
                // The view of the target is gone when its character left the match.
                if (status.Mark == null || status.Target == null || Time.time >= status.EndsAt)
                    _ended.Add(entry.Key);
                else
                    status.Mark.transform.position = status.Target.position + StatusOffset;
            }

            foreach (var key in _ended)
            {
                if (_statuses[key].Mark != null)
                    _statuses[key].Mark.Release();
                _statuses.Remove(key);
            }

            _ended.Clear();
        }

        private Transform ViewOf(EntityRef entity)
        {
            if (_views == null)
                _views = FindFirstObjectByType<QuantumEntityViewUpdater>();
            if (_views == null)
                return null;

            var view = _views.GetView(entity);
            return view == null ? null : view.transform;
        }

        private static T At<T>(T[] values, int position, T fallback)
        {
            return values != null && position < values.Length ? values[position] : fallback;
        }
    }
}
