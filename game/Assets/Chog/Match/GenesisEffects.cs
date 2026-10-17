using System.Collections.Generic;
using CartoonFX;
using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Match
{
    /// <summary>
    /// The one place where the game spawns a particle effect of a fight. It keeps their cost bounded: no
    /// real-time light (lights are faked, ChogLight), no camera shake of the pack's own, a cap on how many
    /// live at once, every effect kept and played again rather than made anew (ChogEffectPool), and a lighter
    /// path on a phone ("Lighter": at most 30 particles a system, bursts halved, the second layers and the
    /// lasting effects left out, fewer at once). "?fx=0" in the page URL turns every effect of a fight off,
    /// to measure what they cost on a device; on a private page "?fxlevel=0" shows the effects of before
    /// (the pieces only), "1" the phone's path and "2" the computer's, to compare them on one build.
    /// The particle effects are copies of Cartoon FX Remaster (Jean Moreno) prefabs, in
    /// Assets/Chog/Effects/Resources/ChogFx under the names of ChogFx; ChogFxKit decides which a trait plays.
    /// </summary>
    public static class GenesisEffects
    {
        public const string SwitchParameter = "fx";
        public const string LevelParameter = "fxlevel";
        public const string ResourceFolder = "ChogFx/";
        // The scene of a match (ChogArenaSkin.ArenaScene): its effects are made while it loads.
        private const string MatchScene = "Gameplay";

        /// <summary>How much a fight shows: the pieces of before only, the phone's path, or everything.</summary>
        public enum FxLevel : byte { Before = 0, Light = 1, Full = 2 }

        // Beyond this many live effects a new one is skipped: the fight is already unreadable.
        private const int MaxLive = 10;
        private const int MaxPlaying = 40;
        private const int MaxPlayingLighter = 14;
        // A one-shot effect is given back this long after it started at the latest: its last particles are
        // faded out by then, and a copy kept playing for nothing counts against the cap.
        private const float LongestOneShot = 1.8f;
        // The big moments (a fall, a special, an Aura) may go this far over the cap; the many small ones may not.
        private const float ImportantOverCap = 1.5f;
        private const int MaxParticlesOnMobile = 30;
        // The quality level from which a device counts as a computer (ChogMatchPost starts there too).
        private const string FullFromLevel = "Good";

        private static readonly List<GameObject> Live = new List<GameObject>();
        private static readonly Dictionary<ChogFx, Stack<ChogFxPlayback>> s_free = new Dictionary<ChogFx, Stack<ChogFxPlayback>>();
        private static readonly Dictionary<ChogFx, GameObject> s_prefabs = new Dictionary<ChogFx, GameObject>();
        private static readonly string[] s_paths = new string[32];
        private static int s_spawned;
        private static int s_skipped;
        private static int s_played;
        private static int s_made;
        private static int s_fullFrom = -1;
        private static FxLevel? s_asked;
        private static Transform s_holder;

        /// <summary>Particle effects playing now (ChogFxPlayback counts them).</summary>
        internal static int Playing;

        public static bool Enabled { get; private set; } = true;

        /// <summary>How much a fight shows on this device now (see FxLevel).</summary>
        public static FxLevel Level
        {
            get
            {
                if (s_asked.HasValue)
                    return s_asked.Value;
                if (Application.isMobilePlatform)
                    return FxLevel.Light;
                if (s_fullFrom < 0)
                    s_fullFrom = System.Array.IndexOf(QualitySettings.names, FullFromLevel);
                return s_fullFrom >= 0 && QualitySettings.GetQualityLevel() < s_fullFrom ? FxLevel.Light : FxLevel.Full;
            }
        }

        /// <summary>How long a one-shot effect is kept playing at most.</summary>
        internal static float LongestOneShotSeconds => LongestOneShot;

        /// <summary>Whether the phone's lighter path is taken.</summary>
        public static bool Lighter => Level == FxLevel.Light;

        /// <summary>Whether the effects of 17 October (particles, lights, bolts) are drawn at all.</summary>
        public static bool Rich => Enabled && Level != FxLevel.Before;

        /// <summary>One line for the performance overlay.</summary>
        public static string Status
        {
            get
            {
                Prune();
                return "fx " + (Enabled ? "on " + Level : "off") + "  playing " + Playing + "  played " + s_played +
                       "  made " + s_made + "  skipped " + s_skipped + "  " + ChogLight.Status;
            }
        }

        public static int LiveCount
        {
            get
            {
                Prune();
                return Live.Count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialise()
        {
            var url = Application.absoluteURL;
            Enabled = !(PerformanceSampling.TryGetQueryInt(url, SwitchParameter, out var value) && value == 0);
            s_asked = null;
            if (LocalGenesis.TestEntriesAllowed(url, Application.isEditor)
                && PerformanceSampling.TryGetQueryInt(url, LevelParameter, out var level) && level >= 0 && level <= 2)
                s_asked = (FxLevel)level;

            // A real-time light per explosion relights the whole arena around it, and a shaking
            // camera hides the fight: neither is worth its cost here (ChogLight fakes the light,
            // ChogCameraShake shakes for the big moments).
            CFXR_Effect.GlobalDisableLights = true;
            CFXR_Effect.GlobalDisableCameraShake = true;

            PerformanceHud.StatusLines.Add(() => Status);
            SceneManager.sceneLoaded += (scene, _) =>
            {
                if (scene.name == MatchScene && Rich)
                    ChogFxRunner.Warm();
            };
        }

        /// <summary>Whether a small effect would still play now: the second layers wait for a quieter moment.</summary>
        public static bool Quiet => Playing < (Lighter ? MaxPlayingLighter : MaxPlaying) / 2;

        /// <summary>
        /// Makes one more copy of that effect and keeps it, so that the first time a match needs it nothing
        /// is made in the middle of a fight (ChogFxRunner warms every effect, one a frame, as a match loads).
        /// </summary>
        internal static void MakeSpare(ChogFx effect)
        {
            if (!Rich)
                return;
            if (!s_free.TryGetValue(effect, out var free))
                s_free[effect] = free = new Stack<ChogFxPlayback>();
            foreach (var kept in free)
                if (kept != null)
                    return;
            var made = Make(effect);
            if (made != null)
                free.Push(made);
        }

        /// <summary>
        /// Spawns an effect under "parent", or in the scene at "position" when there is no parent.
        /// Returns null when effects are off, the prefab is missing or too many are live already.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float scale, Color tint)
        {
            if (!Enabled || prefab == null)
                return null;

            Prune();
            if (Live.Count >= MaxLive)
            {
                s_skipped++;
                return null;
            }

            var effect = parent == null
                ? Object.Instantiate(prefab, position, Quaternion.identity)
                : Object.Instantiate(prefab, parent);
            if (parent != null)
            {
                effect.transform.localPosition = position;
                effect.transform.localRotation = Quaternion.identity;
            }

            effect.transform.localScale = Vector3.one * scale;

            foreach (var light in effect.GetComponentsInChildren<Light>(true))
                light.enabled = false;

            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                if (tint.a > 0f)
                    main.startColor = tint;
                if (Application.isMobilePlatform && main.maxParticles > MaxParticlesOnMobile)
                    main.maxParticles = MaxParticlesOnMobile;
            }

            Live.Add(effect);
            s_spawned++;
            return effect;
        }

        /// <summary>
        /// Plays a particle effect of the fight at "at", "size" times as large as it was made, in "colour" as
        /// "mode" says, turned by "rotation". With "follow" it moves with that object (an effect on a hero);
        /// "lasting" over 0 keeps a looping effect going that long, then lets it die out; an "important" one (a
        /// fall, a special) may go over the cap a little. Returns the playing effect (to stop it early), or null
        /// when effects are off, the effect is missing or too many play.
        /// </summary>
        public static ChogFxPlayback Play(ChogFx effect, Vector3 at, float size, Color colour = default,
                                          FxColour mode = FxColour.Authored, Quaternion? rotation = null,
                                          Transform follow = null, float lasting = 0f, bool important = false)
        {
            if (!Rich)
                return null;
            var cap = Lighter ? MaxPlayingLighter : MaxPlaying;
            if (Playing >= (important ? cap * ImportantOverCap : cap))
            {
                s_skipped++;
                return null;
            }

            if (!s_free.TryGetValue(effect, out var free))
                s_free[effect] = free = new Stack<ChogFxPlayback>();
            ChogFxPlayback playback = null;
            while (free.Count > 0 && playback == null)
                playback = free.Pop();
            if (playback == null)
            {
                playback = Make(effect);
                if (playback == null)
                    return null;
            }

            var placed = playback.transform;
            placed.SetParent(ChogEffectPool.Root, false);
            placed.SetPositionAndRotation(at, rotation ?? Quaternion.identity);
            placed.localScale = Vector3.one * Mathf.Max(0.01f, size);
            playback.Begin(colour, mode, follow, lasting);
            s_played++;
            return playback;
        }

        /// <summary>A playing effect is over: hidden and kept for the next one of its kind.</summary>
        internal static void GiveBack(ChogFxPlayback playback)
        {
            if (playback == null)
                return;
            playback.gameObject.SetActive(false);
            if (!s_free.TryGetValue(playback.Effect, out var free))
                s_free[playback.Effect] = free = new Stack<ChogFxPlayback>();
            free.Push(playback);
        }

        /// <summary>Runs "action" that many seconds from now, while effects are on (a fall in several blasts).</summary>
        public static void After(float seconds, System.Action action) => ChogFxRunner.After(seconds, action);

        // A new copy of that effect, made under a hidden parent so that it does not play before it is placed.
        private static ChogFxPlayback Make(ChogFx effect)
        {
            if (!s_prefabs.TryGetValue(effect, out var prefab))
            {
                var index = (int)effect;
                if (index < s_paths.Length && s_paths[index] == null)
                    s_paths[index] = ResourceFolder + effect;
                prefab = Resources.Load<GameObject>(index < s_paths.Length ? s_paths[index] : ResourceFolder + effect);
                if (prefab == null)
                    Debug.LogWarning("CHOG_FX the effect " + effect + " is missing from Resources/" + ResourceFolder);
                s_prefabs[effect] = prefab;
            }
            if (prefab == null)
                return null;

            if (s_holder == null)
            {
                s_holder = new GameObject("Chog Fx Being Made").transform;
                s_holder.gameObject.SetActive(false);
                s_holder.SetParent(ChogEffectPool.Root, false);
            }
            var made = Object.Instantiate(prefab, s_holder);
            made.name = "ChogFx " + effect;
            var playback = made.AddComponent<ChogFxPlayback>();
            playback.Prepare(effect, Lighter, MaxParticlesOnMobile);
            made.SetActive(false);
            made.transform.SetParent(ChogEffectPool.Root, false);
            s_made++;
            return playback;
        }

        private static void Prune()
        {
            for (var i = Live.Count - 1; i >= 0; i--)
                if (Live[i] == null)
                    Live.RemoveAt(i);
        }
    }

    /// <summary>
    /// One pooled copy of a Cartoon FX effect (GenesisEffects.Play): it remembers the colours it was made
    /// with, so that each play takes the colour asked for from those, plays once (or as long as asked, for a
    /// looping one), follows a hero when asked, and goes back to its pool when its last particle is gone.
    /// </summary>
    public sealed class ChogFxPlayback : MonoBehaviour
    {
        internal ChogFx Effect;

        /// <summary>Counts the plays of this copy: a round that ended is not stopped by whoever held it.</summary>
        public int Round { get; private set; }

        private ParticleSystem _root;
        private ParticleSystem[] _systems;
        private ParticleSystem.MinMaxGradient[] _authored;
        private float _life;
        private float _madeLife;
        private float _longestParticle;
        private bool _loops;
        private float _age;
        private float _stopAt;
        private bool _stopped;
        private bool _playing;
        private Transform _follow;
        private Vector3 _offset;
        private Color _lastColour;
        private FxColour _lastMode;
        private bool _recoloured;

        /// <summary>Learns its particle systems; on the lighter path, fewer particles for each.</summary>
        internal void Prepare(ChogFx effect, bool lighter, int maxParticles)
        {
            Effect = effect;
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _root = GetComponent<ParticleSystem>();
            if (_root == null && _systems.Length > 0)
                _root = _systems[0];
            _authored = new ParticleSystem.MinMaxGradient[_systems.Length];
            foreach (var cfxr in GetComponentsInChildren<CFXR_Effect>(true))
                cfxr.clearBehavior = CFXR_Effect.ClearBehavior.None;
            foreach (var light in GetComponentsInChildren<Light>(true))
                light.enabled = false;
            // Sparks and smoke cast no shadow worth the computer's shadow pass (13 of the pack's renderers did).
            foreach (var drawn in GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                drawn.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                drawn.receiveShadows = false;
            }
            for (var i = 0; i < _systems.Length; i++)
            {
                var main = _systems[i].main;
                _authored[i] = main.startColor;
                _loops |= main.loop;
                var ends = main.startDelay.constantMax + main.duration;
                _longestParticle = Mathf.Max(_longestParticle, main.startLifetime.constantMax);
                _life = Mathf.Max(_life, ends + main.startLifetime.constantMax);
                if (!lighter)
                    continue;
                if (main.maxParticles > maxParticles)
                    main.maxParticles = maxParticles;
                Halve(_systems[i]);
            }
            _life = Mathf.Clamp(_life, 0.2f, _loops ? 6f : GenesisEffects.LongestOneShotSeconds);
            _madeLife = _life;

            // The flames that burn on a hero keep their embers but not their dark smoke, which hides its head.
            if (effect == ChogFx.Burning)
                foreach (var system in _systems)
                    if (system.name == "Smoke")
                        system.gameObject.SetActive(false);
        }

        // The phone's path: half the bursts and half the stream of each system.
        private static void Halve(ParticleSystem system)
        {
            var emission = system.emission;
            emission.rateOverTimeMultiplier *= 0.5f;
            var count = emission.burstCount;
            for (var b = 0; b < count; b++)
            {
                var burst = emission.GetBurst(b);
                var curve = burst.count;
                if (curve.mode == ParticleSystemCurveMode.Constant)
                    curve.constant = Mathf.Max(1f, Mathf.Round(curve.constant * 0.5f));
                else if (curve.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    curve.constantMin = Mathf.Max(1f, Mathf.Round(curve.constantMin * 0.5f));
                    curve.constantMax = Mathf.Max(1f, Mathf.Round(curve.constantMax * 0.5f));
                }
                burst.count = curve;
                emission.SetBurst(b, burst);
            }
        }

        internal void Begin(Color colour, FxColour mode, Transform follow, float lasting)
        {
            Round++;
            Recolour(colour, mode);
            _follow = follow;
            _offset = follow != null ? transform.position - follow.position : Vector3.zero;
            _age = 0f;
            _stopped = false;
            _life = _madeLife;
            _stopAt = _loops ? Mathf.Max(0.1f, lasting > 0f ? lasting : _madeLife) : -1f;
            gameObject.SetActive(true);
            if (_root != null)
            {
                _root.Clear(true);
                _root.Play(true);
            }
            if (!_playing)
            {
                _playing = true;
                GenesisEffects.Playing++;
            }
        }

        /// <summary>Stops that round of a looping effect, if it is still the one playing.</summary>
        public void Stop(int round)
        {
            if (round == Round)
                Stop();
        }

        /// <summary>That round of a looping effect goes on "seconds" more from now (an effect renewed on its target).</summary>
        public void KeepUntil(int round, float seconds)
        {
            if (round != Round || !_playing || _stopped || !_loops)
                return;
            _stopAt = Mathf.Max(_stopAt, _age + seconds);
        }

        /// <summary>Stops a looping effect: what is out dies away, nothing new comes.</summary>
        public void Stop()
        {
            if (!_playing || _stopped)
                return;
            _stopped = true;
            if (_root != null)
                _root.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _life = _age + _longestParticle;
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_follow != null)
                transform.position = _follow.position + _offset;
            else if (!ReferenceEquals(_follow, null))
            {
                // What it followed is gone (the hero left the match).
                _follow = null;
                Stop();
            }
            if (_loops && !_stopped && _age >= _stopAt)
                Stop();
            if ((!_loops || _stopped) && _age >= _life)
                End();
        }

        /// <summary>
        /// Shows the effect frozen a quarter of a second in, every system at once (those that start late too),
        /// so that it is drawn on the next frame: the warm-up of its shaders (ChogFxRunner).
        /// </summary>
        internal void ShowFrozen()
        {
            if (_root != null)
                _root.Simulate(0.25f, true, true);
        }

        /// <summary>Ends the effect at once, its particles cleared, and keeps it for the next one of its kind.</summary>
        internal void EndNow()
        {
            if (_root != null)
                _root.Clear(true);
            End();
        }

        private void End()
        {
            if (_playing)
            {
                _playing = false;
                GenesisEffects.Playing = Mathf.Max(0, GenesisEffects.Playing - 1);
            }
            GenesisEffects.GiveBack(this);
        }

        private void OnDestroy()
        {
            if (_playing)
                GenesisEffects.Playing = Mathf.Max(0, GenesisEffects.Playing - 1);
        }

        // Each system takes the colour from the one it was made with.
        private void Recolour(Color colour, FxColour mode)
        {
            if (mode == FxColour.Authored || colour.a <= 0f)
            {
                if (!_recoloured)
                    return;
                for (var i = 0; i < _systems.Length; i++)
                {
                    var main = _systems[i].main;
                    main.startColor = _authored[i];
                }
                _recoloured = false;
                return;
            }
            if (_recoloured && _lastColour == colour && _lastMode == mode)
                return;
            for (var i = 0; i < _systems.Length; i++)
            {
                var main = _systems[i].main;
                main.startColor = Recoloured(_authored[i], colour, mode);
            }
            _recoloured = true;
            _lastColour = colour;
            _lastMode = mode;
        }

        private static ParticleSystem.MinMaxGradient Recoloured(ParticleSystem.MinMaxGradient authored, Color colour, FxColour mode)
        {
            switch (authored.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(FxColours.Mix(authored.color, colour, mode));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(FxColours.Mix(authored.colorMin, colour, mode), FxColours.Mix(authored.colorMax, colour, mode));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Recoloured(authored.gradientMin, colour, mode), Recoloured(authored.gradientMax, colour, mode));
                default:
                {
                    var made = new ParticleSystem.MinMaxGradient(Recoloured(authored.gradient, colour, mode)) { mode = authored.mode };
                    return made;
                }
            }
        }

        private static Gradient Recoloured(Gradient authored, Color colour, FxColour mode)
        {
            if (authored == null)
                return null;
            var keys = authored.colorKeys;
            for (var k = 0; k < keys.Length; k++)
                keys[k].color = FxColours.Mix(keys[k].color, colour, mode);
            var made = new Gradient { mode = authored.mode };
            made.SetKeys(keys, authored.alphaKeys);
            return made;
        }
    }

    /// <summary>
    /// Runs the effects asked for later (a structure that falls in several blasts), and makes a copy of every
    /// effect, one a frame, while a match loads (Warm), on one object of the game.
    /// </summary>
    public sealed class ChogFxRunner : MonoBehaviour
    {
        private static ChogFxRunner s_runner;
        private readonly List<(float At, System.Action Action)> _waiting = new List<(float, System.Action)>();
        private readonly Queue<ChogFx> _warming = new Queue<ChogFx>();
        // The effect drawn out of sight to compile its shaders, and the frame it was shown on.
        private ChogFxPlayback _warmShown;
        private int _warmShownAt;
        private bool _lightsToWarm;
        // How far under the floor the warm-up is drawn: in the camera's view, hidden by the floor.
        private const float WarmDepth = 3f;

        public static void After(float seconds, System.Action action)
        {
            if (action == null || !GenesisEffects.Rich)
                return;
            Runner()._waiting.Add((Time.time + Mathf.Max(0f, seconds), action));
        }

        /// <summary>Makes one copy of every effect, one a frame, from the next frame on.</summary>
        public static void Warm()
        {
            var runner = Runner();
            runner._warming.Clear();
            foreach (ChogFx effect in System.Enum.GetValues(typeof(ChogFx)))
                runner._warming.Enqueue(effect);
            runner._lightsToWarm = true;
        }

        /// <summary>
        /// A point in the camera's view, "WarmDepth" under the floor: what is drawn there is sent to the graphics
        /// card, so its shaders are compiled, and hidden by the floor. False without a camera looking down.
        /// </summary>
        public static bool WarmPoint(Vector3 camera, Vector3 forward, out Vector3 at)
        {
            at = default;
            if (forward.y > -0.2f)
                return false;
            var along = (camera.y + WarmDepth) / -forward.y;
            at = camera + forward * along;
            return true;
        }

        private static ChogFxRunner Runner()
        {
            if (s_runner == null)
            {
                var host = new GameObject("Chog Fx Runner");
                DontDestroyOnLoad(host);
                s_runner = host.AddComponent<ChogFxRunner>();
            }
            return s_runner;
        }

        private void Update()
        {
            // Each effect is made, then drawn once out of sight under the floor while the match loads: a browser
            // compiles a shader the first time it is drawn, which stalled the frame of a fight (the phone's worst
            // frames, 158 to 1,552 ms, docs/PERF-AUDIT.md, Q3). One at a time, two frames each.
            if (_warmShown != null && Time.frameCount >= _warmShownAt + 2)
            {
                _warmShown.EndNow();
                _warmShown = null;
            }
            if (_warmShown == null && _warming.Count > 0)
            {
                var effect = _warming.Dequeue();
                GenesisEffects.MakeSpare(effect);
                var view = Camera.main;
                if (view != null && WarmPoint(view.transform.position, view.transform.forward, out var at))
                {
                    _warmShown = GenesisEffects.Play(effect, at, 1f);
                    if (_warmShown != null)
                    {
                        _warmShown.ShowFrozen();
                        _warmShownAt = Time.frameCount;
                    }
                }
            }
            else if (_warmShown == null && _lightsToWarm)
            {
                _lightsToWarm = false;
                var view = Camera.main;
                if (view != null && WarmPoint(view.transform.position, view.transform.forward, out var at))
                {
                    // The rings, columns and pools of light lie on the floor, where they would show: they share the
                    // flare's material, so the flare alone (without its pool) warms them.
                    ChogLight.Flash(at, Color.white, 1f, 0.05f, 4, false);
                    ChogLightning.Strike(at + Vector3.up, at, Color.white, 0.05f);
                }
            }

            for (var i = _waiting.Count - 1; i >= 0; i--)
            {
                if (Time.time < _waiting[i].At)
                    continue;
                var action = _waiting[i].Action;
                _waiting.RemoveAt(i);
                try
                {
                    action();
                }
                catch (System.Exception problem)
                {
                    Debug.LogWarning("CHOG_FX a later effect failed: " + problem.Message);
                }
            }
        }
    }
}
