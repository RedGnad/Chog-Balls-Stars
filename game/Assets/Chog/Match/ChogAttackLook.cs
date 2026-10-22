using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The basic attack of a hero drawn as what the hat of its Genesis gives it
    /// (docs/ATTACKS-FROM-THE-ART.md): the snowball of a Russian Hat, the coin of a Dollar Cap, the cap
    /// itself thrown flat, a leaf of its crown for a bare head. The object comes from the library of
    /// parts (ChogHeroModel.BuildAttack), in the hero's colours, drawn with the one material of the heroes.
    /// It replaces what the sample drew for that attack (its arrow, its slash): the attack itself, where
    /// it goes and what it hits, is the simulation's and does not change. A thrown object turns as its
    /// style says; a strike (the attack of the brute, which does not travel) pops out in front, turns
    /// and fades; where an attack ends, the object bursts in its own way (ChogFxKit.AttackEnds: a coin
    /// into coins and sparkles, a cap into cloth and a puff, a bolt calls lightning).
    /// A lobbed attack (the controller's, which the simulation sends to a point on the ground) flies on
    /// a high arc instead of the straight line of the ranger's: higher the farther it goes, larger at
    /// its top, its shadow running on the floor under it, and a ring in the colour of its side on the
    /// spot where it will land from the moment it is thrown, as arena brawlers mark their throwers.
    /// </summary>
    public sealed class ChogAttackLook : MonoBehaviour
    {
        // The basic attacks of the three classes, by the name of their data in the project.
        private static readonly string[] Basic =
            { "ArcherBasicAttackData", "KnightBasicAttackData", "SpellcasterBasicFirstAttackData" };
        private const string Strike = "KnightBasicAttackData";

        private const float GrowSeconds = 0.08f;
        private const float SpinDegreesPerSecond = 720f;
        private const float TumbleDegreesPerSecond = 540f;
        private const float StrikeSeconds = 0.25f;
        // The longest a trail goes on if nothing stops it (an attack flies well under that).
        private const float TrailSeconds = 2f;
        // Above the ground, at the height of the hands of a hero.
        private const float Height = 0.9f;
        // A lob: the top of its arc for a throw of no length, and what each unit of length adds.
        private const float LobLowest = 1.6f;
        private const float LobPerUnit = 0.45f;
        private const float LobGrowAtTop = 0.3f;
        private const float LandingRadius = 1.1f;
        private const float ShadowRadius = 0.6f;
        // On the floor, whose planks stand above y = 0 (ChogTeamRing.Lift): lower, the shadow and the
        // landing ring were hidden under the planks.
        private const float OnFloor = 0.18f;

        private static Material s_ground;
        private static Mesh s_shadow;
        private static readonly Dictionary<Color, Mesh> s_landings = new Dictionary<Color, Mesh>();

        private Transform _object;
        // What this look hid of the sample's drawing, to show it again for an attack it does not dress.
        private readonly List<Renderer> _hidRenderers = new List<Renderer>();
        private readonly List<GameObject> _hidParticles = new List<GameObject>();
        private int _metadataId = int.MinValue;
        private ChogHeroModel.AttackStyle _style;
        private Color _impact;
        private float _age;
        private bool _strike;
        private bool _shown;
        private bool _lob;
        private Vector3 _lobStart;
        private Vector3 _lobTarget;
        private float _lobHeight;
        private Transform _shadow;
        private Transform _landing;
        private Color _landingColour;
        private Color _side;
        // What streams behind it while it flies (ChogFxRecipes.TrailOf), and the round of that effect.
        private ChogFxPlayback _trail;
        private int _trailRound;

        /// <summary>
        /// Dresses the view of an attack whose data is named "dataName", thrown by the hero of
        /// "metadataId" (ChogHeroModel.Recruit for a recruit, below 0 for no hero). Does nothing for an
        /// attack that is not a basic attack, or a hero the library does not know.
        /// </summary>
        public static void Dress(Transform view, string dataName, int metadataId) => Dress(view, dataName, metadataId, null, Color.white);

        /// <summary>
        /// The same, for an attack that is lobbed to "landing" (a point on the floor), its ring drawn in
        /// "side", the colour of the thrower's side (ChogTeamRing.Self, Ally or Enemy).
        /// </summary>
        public static void Dress(Transform view, string dataName, int metadataId, Vector3? landing, Color side)
        {
            if (view == null || metadataId < 0 || !GenesisEffects.Enabled || System.Array.IndexOf(Basic, dataName) < 0)
                return;
            var look = view.GetComponent<ChogAttackLook>();
            if (look == null)
                look = view.gameObject.AddComponent<ChogAttackLook>();
            look.Show(metadataId, dataName == Strike);
            look._side = side;
            look.Lob(landing, side);
        }

        /// <summary>How high a lob flies at its top, for a throw of "length" units: higher the farther.</summary>
        public static float LobHeight(float length) => LobLowest + LobPerUnit * Mathf.Max(0f, length);

        /// <summary>Height above its start of a lob of top "height" at "done", from 0 (thrown) to 1 (landed).</summary>
        public static float LobRise(float height, float done)
        {
            done = Mathf.Clamp01(done);
            return 4f * height * done * (1f - done);
        }

        private void Lob(Vector3? landing, Color side)
        {
            _lob = landing.HasValue && _shown && !_strike;
            if (!_lob)
            {
                Hide();
                return;
            }

            _lobStart = transform.position;
            _lobTarget = new Vector3(landing.Value.x, _lobStart.y, landing.Value.z);
            _lobHeight = LobHeight(Vector3.Distance(_lobStart, _lobTarget));
            var ground = Ground();
            if (ground == null)
                return;

            // Its shadow runs on the floor under it.
            if (_shadow == null)
            {
                if (s_shadow == null)
                    s_shadow = GroundMeshes.Build("ChogLobShadow", ShadowRadius, Color.clear, true, false);
                _shadow = Flat("Lob Shadow", s_shadow, ground, transform);
            }
            _shadow.gameObject.SetActive(true);

            // Where it will land, from the moment it is thrown; not a child, as the attack moves.
            if (_landing == null || _landingColour != side)
            {
                if (_landing != null)
                    Destroy(_landing.gameObject);
                if (!s_landings.TryGetValue(side, out var ring))
                    s_landings[side] = ring = GroundMeshes.Build("ChogLobLanding", LandingRadius, side, false, true);
                _landing = Flat("Lob Landing", ring, ground, null);
                _landingColour = side;
            }
            _landing.position = new Vector3(_lobTarget.x, OnFloor, _lobTarget.z);
            _landing.localScale = Vector3.zero;
            _landing.gameObject.SetActive(true);
        }

        private static Material Ground()
        {
            if (s_ground != null)
                return s_ground;
            var shader = Resources.Load<Shader>(ChogTeamRing.ShaderResource);
            if (shader == null || !shader.isSupported)
                return null;
            s_ground = new Material(shader) { name = "ChogLobGround" };
            return s_ground;
        }

        private static Transform Flat(string name, Mesh mesh, Material material, Transform parent)
        {
            var flat = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            flat.transform.SetParent(parent, false);
            flat.GetComponent<MeshFilter>().sharedMesh = mesh;
            var drawn = flat.GetComponent<MeshRenderer>();
            drawn.sharedMaterial = material;
            drawn.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            drawn.receiveShadows = false;
            return flat.transform;
        }

        private void Hide()
        {
            if (_shadow != null)
                _shadow.gameObject.SetActive(false);
            if (_landing != null)
                _landing.gameObject.SetActive(false);
        }

        private void Show(int metadataId, bool strike)
        {
            // A view the entity view pool hands back for an attack of the same hero keeps its object.
            if (_object == null || _metadataId != metadataId)
            {
                if (_object != null)
                    Destroy(_object.gameObject);
                _object = ChogHeroModel.BuildAttack(metadataId, transform, out _style, out _impact);
                }
            _metadataId = metadataId;
            _shown = _object != null;
            if (!_shown)
            {
                // A view handed back by the pool shows again what the sample drew, if an earlier attack hid it.
                foreach (var drawn in _hidRenderers)
                    if (drawn != null)
                        drawn.enabled = true;
                foreach (var particles in _hidParticles)
                    if (particles != null)
                        particles.SetActive(true);
                _hidRenderers.Clear();
                _hidParticles.Clear();
                return;
            }

            // What the sample drew for this attack steps aside (not this look's own shadow of a lob).
            foreach (var drawn in GetComponentsInChildren<Renderer>(true))
                if (!drawn.transform.IsChildOf(_object) && (_shadow == null || !drawn.transform.IsChildOf(_shadow)) && drawn.enabled)
                {
                    drawn.enabled = false;
                    _hidRenderers.Add(drawn);
                }
            foreach (var particles in GetComponentsInChildren<ParticleSystem>(true))
                if (particles.gameObject.activeSelf)
                {
                    particles.gameObject.SetActive(false);
                    _hidParticles.Add(particles.gameObject);
                }

            _strike = strike;
            _age = 0f;
            _object.localPosition = new Vector3(0f, Height, 0f);
            _object.localScale = Vector3.zero;
            StartTrail();
        }

        // Thrown fire leaves flames behind it, thrown water its bubbles; the effect follows the object.
        private void StartTrail()
        {
            StopTrail();
            if (_strike || !ChogHeroModel.TryGetAttackParts(_metadataId, out var parts))
                return;
            var trail = ChogFxRecipes.TrailOf(parts.Piece);
            if (!trail.On)
                return;
            var colour = trail.Fixed.a > 0f ? trail.Fixed : _impact;
            _trail = GenesisEffects.Play(trail.Effect, _object.position, trail.Size, colour, trail.Colour, follow: _object,
                                         lasting: TrailSeconds);
            _trailRound = _trail != null ? _trail.Round : 0;
        }

        private void StopTrail()
        {
            if (_trail != null)
                _trail.Stop(_trailRound);
            _trail = null;
        }

        private void LateUpdate()
        {
            if (!_shown || _object == null)
                return;
            _age += Time.deltaTime;
            var grown = Mathf.Clamp01(_age / GrowSeconds);
            // The library already made every object as large as the others (ChogHeroModel.BuildAttack).
            var size = 1f;
            if (_strike)
            {
                // Out, a little larger than thrown, then gone: the blow lasts as long as the attack.
                var t = Mathf.Clamp01(_age / StrikeSeconds);
                size *= 1.3f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 1.1f + 0.05f));
                _object.localRotation = Quaternion.Euler(0f, 360f * t, 0f) * Rest();
            }
            else if (_lob)
            {
                // The simulation moves the attack along the floor; the arc is drawn over it.
                var whole = Vector3.Distance(_lobStart, _lobTarget);
                var flat = new Vector3(transform.position.x, _lobStart.y, transform.position.z);
                var done = whole < 0.01f ? 1f : Mathf.Clamp01(Vector3.Distance(_lobStart, flat) / whole);
                var top = Mathf.Sin(Mathf.PI * done);
                _object.localPosition = new Vector3(0f, Height + LobRise(_lobHeight, done), 0f);
                size *= grown * (1f + LobGrowAtTop * top);
                // Thrown end over end, as a thing that is tossed.
                _object.localRotation = Quaternion.Euler(TumbleDegreesPerSecond * 0.6f * _age, 0f, 0f) * Rest();
                if (_shadow != null)
                {
                    _shadow.localPosition = new Vector3(0f, OnFloor - transform.position.y, 0f);
                    _shadow.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, top);
                }
                if (_landing != null)
                {
                    // In at once, then a slow beat until it lands.
                    var into = Mathf.Clamp01(_age / 0.12f);
                    _landing.localScale = Vector3.one * into * (0.92f + 0.08f * Mathf.Sin(_age * 12f));
                }
            }
            else
            {
                size *= grown;
                _object.localRotation = Turned(_age) * Rest();
            }
            _object.localScale = Vector3.one * Mathf.Max(0f, size);
        }

        // How the object stands before it turns: a thing that points looks where the attack goes.
        private Quaternion Rest()
        {
            return Quaternion.identity;
        }

        private Quaternion Turned(float age)
        {
            switch (_style)
            {
                case ChogHeroModel.AttackStyle.Spin:
                    return Quaternion.Euler(0f, SpinDegreesPerSecond * age, 0f);
                case ChogHeroModel.AttackStyle.Tumble:
                    return Quaternion.Euler(TumbleDegreesPerSecond * age, 0f, 0f);
                case ChogHeroModel.AttackStyle.Float:
                    return Quaternion.Euler(0f, 0f, Mathf.Sin(age * 14f) * 12f);
                default:
                    return Quaternion.Euler(0f, 0f, age * 90f);
            }
        }

        // Where the attack ends (it hit, or it went as far as it goes), its object bursts.
        private void OnDisable()
        {
            Hide();
            StopTrail();
            if (!_shown || _object == null || !gameObject.scene.isLoaded)
                return;
            _shown = false;
            // A lob bursts on the floor where it landed.
            var at = _lob ? new Vector3(_object.position.x, transform.position.y + Height * 0.5f, _object.position.z) : _object.position;
            var gesture = _strike ? ChogFxKit.Gesture.Strike : _lob ? ChogFxKit.Gesture.Lob : ChogFxKit.Gesture.Thrown;
            ChogFxKit.AttackEnds(_metadataId, at, gesture, _side, _impact);
        }

        private void OnDestroy()
        {
            if (_landing != null)
                Destroy(_landing.gameObject);
        }
    }
}
