using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// What a hit does to the screen, all of it in the view (the simulation is never touched, nor
    /// Time.timeScale): the hero that is hit flashes white for a tenth of a second (the _Flash of the heroes'
    /// shader, through a property block, so the one shared material is untouched), squashes and springs back
    /// (ChogBodyAnimator.Flinch), and a hit sound plays (ChogSounds). When the local hero is hit or hits, the
    /// sound is louder, the poses of the two heroes wait for about two frames of a phone (the hit stop; their
    /// places keep following the simulation) and, when it is hit, the camera shakes lightly
    /// (ChogCameraShake). A tower or core that is hit knocks, and one that falls crashes and shakes the camera.
    /// A hit can be raised by a predicted frame and cancelled later: every effect here is shorter than a
    /// quarter of a second, so a cancelled one is gone before it could mislead.
    /// </summary>
    public sealed class ChogHitFeedback : MonoBehaviour
    {
        /// <summary>The flash, from full white to none.</summary>
        public const float FlashSeconds = 0.09f;
        /// <summary>The hit stop: about two frames of a phone that draws 32 a second.</summary>
        public const float HoldSeconds = 0.06f;
        // A hero that cast within this long, near enough to the target, is taken as the one that hit it
        // (the hit carries no attacker).
        private const float CastMemorySeconds = 1.2f;
        private const float AttackerReach = 12f;

        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        private sealed class Flash
        {
            public readonly List<Renderer> Renderers = new List<Renderer>();
            public float Started;
        }

        private readonly Dictionary<EntityRef, Flash> _flashes = new Dictionary<EntityRef, Flash>();
        private readonly List<EntityRef> _done = new List<EntityRef>();
        private readonly Stack<Flash> _spare = new Stack<Flash>();
        private readonly Dictionary<EntityRef, float> _lastCast = new Dictionary<EntityRef, float>();
        private readonly List<MeshRenderer> _found = new List<MeshRenderer>();
        private MaterialPropertyBlock _block;
        private QuantumEntityViewUpdater _views;

        // What was done, for the console line CHOG_JUICE (every 15 seconds while it changes) and the
        // performance overlay: hits on heroes, of them flashed (a 3D hero was found), held (the hit stop),
        // hits on structures, falls of structures.
        private static int s_hits, s_flashed, s_held, s_structureHits, s_falls;
        private int _hitsLogged = -1;
        private float _nextLog;

        /// <summary>One line for the performance overlay and the console.</summary>
        public static string Status => "juice hits " + s_hits + "  flashed " + s_flashed + "  held " + s_held +
                                       "  structure hits " + s_structureHits + "  falls " + s_falls +
                                       "  trauma " + ChogCameraShake.Trauma.ToString("0.00");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Hit Feedback");
            DontDestroyOnLoad(host);
            host.AddComponent<ChogHitFeedback>();
            Chog.Diagnostics.PerformanceHud.StatusLines.Add(() => Status);
        }

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            QuantumEvent.Subscribe<EventCharacterDamaged>(this, OnDamaged);
            QuantumEvent.Subscribe<EventCharacterSkill>(this, OnSkill);
            QuantumEvent.Subscribe<EventStructureDestroyed>(this, OnStructureFell);
            QuantumEvent.Subscribe<EventGameOver>(this, _ => ChogCameraShake.Clear());
        }

        private void OnDestroy() => QuantumEvent.UnsubscribeListener(this);

        /// <summary>The local hero's entity in that frame, or EntityRef.None for a spectator.</summary>
        public static EntityRef LocalHero(QuantumGame game, Frame frame)
        {
            if (game == null || frame == null)
                return EntityRef.None;
            var characters = frame.Filter<PlayerLink>();
            while (characters.Next(out var entity, out var link))
                if (game.PlayerIsLocal(link.PlayerRef))
                    return entity;
            return EntityRef.None;
        }

        /// <summary>The share of its health a blow of "damage" takes from that entity, 0 to 1.</summary>
        public static float ShareOfHealth(Frame frame, EntityRef entity, float damage)
        {
            if (frame == null || !frame.Exists(entity) || !AttributesHelper.Has(frame, entity, EAttributeType.Health))
                return 0f;
            var health = AttributesHelper.GetAttributeData(frame, entity, EAttributeType.Health);
            var max = health.MaxValue.AsFloat;
            return max > 0f ? Mathf.Clamp01(damage / max) : 0f;
        }

        private void OnSkill(EventCharacterSkill skill) => _lastCast[skill.character] = Time.time;

        private void OnDamaged(EventCharacterDamaged hit)
        {
            var game = hit.Game;
            var frame = game?.Frames?.Predicted;
            if (frame == null || !frame.Exists(hit.target))
                return;

            var local = LocalHero(game, frame);
            if (frame.Has<Structure>(hit.target))
            {
                s_structureHits++;
                // A structure knocks; louder when the local hero is the one hitting it.
                ChogSounds.StructureHit(local != EntityRef.None && Attacker(frame, hit.target) == local);
                return;
            }

            var attacker = Attacker(frame, hit.target);
            var localHit = local != EntityRef.None && hit.target == local;
            var localHits = local != EntityRef.None && attacker == local;
            s_hits++;

            StartFlash(hit.target);
            var target = Body(hit.target);
            if (target != null)
                target.Flinch();

            ChogSounds.Hit(localHit || localHits);
            if (localHit)
            {
                ChogCameraShake.Add(ChogCameraShake.LocalHit);
                ChogHaptics.Pulse(ChogHaptics.HitMilliseconds);
            }

            if (localHit || localHits)
            {
                // The hit stop: both poses wait an instant; where the heroes are keeps following the simulation.
                if (target != null)
                {
                    target.Hold(HoldSeconds);
                    s_held++;
                }
                var hitter = attacker != EntityRef.None ? Body(attacker) : null;
                if (hitter != null)
                    hitter.Hold(HoldSeconds);
            }
        }

        private void OnStructureFell(EventStructureDestroyed fell)
        {
            s_falls++;
            ChogSounds.StructureFall();
            ChogCameraShake.Add(ChogCameraShake.BigBlow);
            ChogHaptics.Pulse(ChogHaptics.StructureMilliseconds);
        }

        // The hero of the other team that cast last, within a moment and in reach of the target: the hit
        // does not say who struck it.
        private EntityRef Attacker(Frame frame, EntityRef target)
        {
            if (!frame.TryGet<TeamInfo>(target, out var targetTeam) || !frame.TryGet<Transform2D>(target, out var at))
                return EntityRef.None;
            var best = EntityRef.None;
            var bestTime = Time.time - CastMemorySeconds;
            foreach (var pair in _lastCast)
            {
                if (pair.Value < bestTime || !frame.Exists(pair.Key))
                    continue;
                if (!frame.TryGet<TeamInfo>(pair.Key, out var team) || team.Index == targetTeam.Index)
                    continue;
                if (!frame.TryGet<Transform2D>(pair.Key, out var place)
                    || (place.Position - at.Position).SqrMagnitude.AsFloat > AttackerReach * AttackerReach)
                    continue;
                best = pair.Key;
                bestTime = pair.Value;
            }
            return best;
        }

        private ChogBodyAnimator Body(EntityRef entity)
        {
            var view = ViewOf(entity);
            return view == null ? null : view.GetComponentInChildren<ChogBodyAnimator>();
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

        // The parts of the 3D hero (drawn with the heroes' material) turn white, then back.
        private void StartFlash(EntityRef entity)
        {
            if (!GenesisEffects.Enabled)
                return;
            var view = ViewOf(entity);
            if (view == null)
                return;

            if (!_flashes.TryGetValue(entity, out var flash))
            {
                flash = _spare.Count > 0 ? _spare.Pop() : new Flash();
                _flashes[entity] = flash;
            }
            flash.Renderers.Clear();
            var material = ChogHeroModel.Material;
            view.GetComponentsInChildren(false, _found);
            foreach (var drawn in _found)
                if (drawn.sharedMaterial == material)
                    flash.Renderers.Add(drawn);
            flash.Started = Time.time;
            if (flash.Renderers.Count > 0)
                s_flashed++;
            Apply(flash, 1f);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextLog)
            {
                _nextLog = Time.unscaledTime + 15f;
                if (s_hits + s_structureHits != _hitsLogged)
                {
                    _hitsLogged = s_hits + s_structureHits;
                    Debug.Log("CHOG_JUICE " + Status + "  " + ChogEffectPool.Status + "  " + ChogSounds.Status);
                }
            }

            if (_flashes.Count == 0)
                return;
            foreach (var pair in _flashes)
            {
                var flash = pair.Value;
                var t = (Time.time - flash.Started) / FlashSeconds;
                if (t >= 1f)
                {
                    Apply(flash, 0f);
                    _done.Add(pair.Key);
                    continue;
                }
                // Fast at first, then the colours come back.
                var left = 1f - t;
                Apply(flash, left * left);
            }

            foreach (var entity in _done)
            {
                var flash = _flashes[entity];
                flash.Renderers.Clear();
                _spare.Push(flash);
                _flashes.Remove(entity);
            }
            _done.Clear();
        }

        private void Apply(Flash flash, float amount)
        {
            foreach (var drawn in flash.Renderers)
            {
                if (drawn == null)
                    continue;
                drawn.GetPropertyBlock(_block);
                _block.SetFloat(FlashId, amount);
                drawn.SetPropertyBlock(_block);
            }
        }
    }
}
