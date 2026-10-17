using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The specials in 3D, each drawn from the caster's own eyes (GenesisEyeLook), as GenesisSpecialDrawing
    /// asks: the laser of laser eyes (GenesisLaser), the fan of light of a visor or glasses
    /// (GenesisLightFan), the tear that is lobbed (GenesisTear) and the puddle it leaves (GenesisPuddle),
    /// the eyes of a stare rolling forward (GenesisStare) and the ring of eyes of a burst
    /// (GenesisLookBurst). Their shapes are GenesisSpecialMeshes, in the materials of ChogEffectMaterials.
    /// Each one, once over, is kept to draw the next of its kind (ChogEffectPool). What faces the viewer
    /// (an eyeball, a glint, a tear's highlight) turns toward the match camera, or toward the camera
    /// ViewerAt names (the page of ChogSpecialsViewer).
    /// </summary>
    public static class GenesisSpecial3D
    {
        /// <summary>Where the camera that watches a point stands; Camera.main when not set.</summary>
        public static System.Func<Vector3, Vector3> ViewerAt;

        /// <summary>The up of that camera, set with ViewerAt; Camera.main's when not set.</summary>
        public static Vector3? ViewerUp;

        /// <summary>
        /// The way up on the screen. What faces the camera is turned with it, not with the world's up: the
        /// match camera looks down at 75 degrees, and an eye turned with the world's up would lean on screen.
        /// </summary>
        public static Vector3 UpOnScreen()
        {
            if (ViewerUp.HasValue)
                return ViewerUp.Value;
            return Camera.main != null ? Camera.main.transform.up : Vector3.up;
        }

        // Just above the arena's floor, whose planks stand above y = 0 (ChogTeamRing lifts its rings 0.18).
        public const float FloorLift = 0.17f;

        /// <summary>The height of the floor the effects of a fight lie on: 0 in the arena; a test page far from it sets its own.</summary>
        public static float Ground;

        /// <summary>The point of the floor under "point", just above it (FloorLift), where a light or a ring lies.</summary>
        public static Vector3 OnFloor(Vector3 point) => new Vector3(point.x, Ground + FloorLift, point.z);

        /// <summary>From "point" toward the camera that watches it, of length 1.</summary>
        public static Vector3 TowardViewer(Vector3 point)
        {
            Vector3 viewer;
            if (ViewerAt != null)
                viewer = ViewerAt(point);
            else if (Camera.main != null)
                viewer = Camera.main.transform.position;
            else
                viewer = point + new Vector3(0f, 25f, -7.5f);
            var toward = viewer - point;
            return toward.sqrMagnitude > 1e-6f ? toward.normalized : Vector3.up;
        }

        /// <summary>
        /// The way an eyeball at "point" looks: toward the viewer, turned that much ("lean") toward
        /// "heading", so that its eye is seen whichever way the attack goes and still reads as looking there.
        /// </summary>
        public static Vector3 Gaze(Vector3 point, Vector3 heading, float lean)
        {
            heading.y = 0f;
            var gaze = TowardViewer(point) + (heading.sqrMagnitude > 1e-6f ? heading.normalized * lean : Vector3.zero);
            return gaze.sqrMagnitude > 1e-6f ? gaze.normalized : Vector3.forward;
        }

        /// <summary>Ease out: fast, then slower.</summary>
        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>Ease out with a small overshoot, for things that pop into place.</summary>
        public static float Pop(float t)
        {
            t = Mathf.Clamp01(t);
            const float back = 1.70158f;
            var u = t - 1f;
            return 1f + (back + 1f) * u * u * u + back * u * u;
        }
    }

    /// <summary>One drawn part of a 3D special: a mesh, a material, a place, and how faded it is.</summary>
    internal sealed class EffectPiece
    {
        public readonly Transform Transform;
        private readonly MeshFilter _filter;
        private readonly MeshRenderer _renderer;
        private Texture _picture;
        private float _fade = 1f;

        public EffectPiece(Transform parent, string name)
        {
            var made = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            made.transform.SetParent(parent, false);
            Transform = made.transform;
            _filter = made.GetComponent<MeshFilter>();
            _renderer = made.GetComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        /// <summary>Draws that mesh in that material (and, on an eyeball, that picture of an eye).</summary>
        public void Show(Mesh mesh, Material material, Texture picture = null)
        {
            _filter.sharedMesh = mesh;
            _renderer.sharedMaterial = material;
            _picture = picture;
            Transform.gameObject.SetActive(mesh != null && material != null);
            Fade(1f);
        }

        /// <summary>Wears another picture, in the same place, mesh and material.</summary>
        public void Swap(Texture picture)
        {
            if (_picture == picture)
                return;
            _picture = picture;
            Fade(_fade);
        }

        public void Fade(float fade)
        {
            _fade = fade;
            ChogEffectMaterials.Apply(_renderer, fade, _picture);
        }

        public void Hide() => Transform.gameObject.SetActive(false);

        public void Place(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Transform.SetPositionAndRotation(position, rotation);
            Transform.localScale = scale;
        }
    }

    /// <summary>
    /// The laser of laser eyes in 3D: two tubes, one from each eye to the head of the attack, in the eye
    /// colour with its lighter core and an ink outline, as the collection draws the beams; a glow at each
    /// eye, a spark turning at each head, and the glow of the beams on the floor along the line they hit.
    /// Once the attack is over the beams stay whole for a moment, then thin out and fade, and the head
    /// bursts into pieces of the beam's colours.
    /// </summary>
    public sealed class GenesisLaser : MonoBehaviour
    {
        // Half the distance between the two eyes, and their height, in world units (as GenesisBeam).
        private const float EyeSpacing = 0.22f;
        private const float EyeHeight = 1.15f;
        // The colour's radius (GenesisBeam drew it 0.24 wide); the outline is in the tube's mesh.
        private const float TubeRadius = 0.12f;
        private const float GlowRadius = 0.24f;
        private const float SparkRadius = 0.32f;
        private const float HoldSeconds = 0.3f;
        private const float FadeSeconds = 0.25f;

        private static readonly Stack<GenesisLaser> s_free = new Stack<GenesisLaser>();

        private EffectPiece[] _tubes;
        private EffectPiece[] _eyeGlows;
        private EffectPiece[] _headGlows;
        private EffectPiece[] _sparks;
        private EffectPiece _floor;
        private Vector3 _origin;
        private Vector3 _head;
        private float _thickness;
        private float _age;
        private float _fadeStarted = -1f;
        private Color _colour;
        private Color _core;

        /// <summary>"thickness" multiplies the width of the beams: 1 for the laser as the eyes draw it.</summary>
        public static GenesisLaser Create(GenesisEyeLook look, Vector3 origin, float thickness = 1f)
        {
            var laser = ChogEffectPool.Take(s_free);
            if (laser == null)
            {
                laser = ChogEffectPool.Make("GenesisLaser").AddComponent<GenesisLaser>();
                laser.Make();
            }

            laser._origin = origin;
            laser._thickness = thickness;
            laser._age = 0f;
            laser._fadeStarted = -1f;
            laser._colour = look.Outer;
            laser._core = look.Core;
            var tube = GenesisSpecialMeshes.Laser(look);
            var glow = GenesisSpecialMeshes.GlowBall(look.Outer, look.Core);
            var spark = GenesisSpecialMeshes.Spark(Color.Lerp(look.Core, Color.white, 0.4f), GenesisSpecialMeshes.OutlineOf(look));
            for (var eye = 0; eye < 2; eye++)
            {
                laser._tubes[eye].Show(tube, ChogEffectMaterials.Solid);
                laser._eyeGlows[eye].Show(glow, ChogEffectMaterials.Glow);
                laser._headGlows[eye].Show(glow, ChogEffectMaterials.Glow);
                laser._sparks[eye].Show(spark, ChogEffectMaterials.Solid);
            }
            laser._floor.Show(GenesisSpecialMeshes.FloorGlow(look.Outer), ChogEffectMaterials.Floor);
            laser.SetHead(origin);
            return laser;
        }

        private void Make()
        {
            _tubes = new EffectPiece[2];
            _eyeGlows = new EffectPiece[2];
            _headGlows = new EffectPiece[2];
            _sparks = new EffectPiece[2];
            for (var eye = 0; eye < 2; eye++)
            {
                _tubes[eye] = new EffectPiece(transform, "Tube");
                _eyeGlows[eye] = new EffectPiece(transform, "Eye Glow");
                _headGlows[eye] = new EffectPiece(transform, "Head Glow");
                _sparks[eye] = new EffectPiece(transform, "Spark");
            }
            _floor = new EffectPiece(transform, "Floor Glow");
        }

        /// <summary>Moves the far end of the beams to where the attack is now.</summary>
        public void SetHead(Vector3 head)
        {
            _head = head;
            Draw(1f);
        }

        /// <summary>The attack is over: the beams hold, thin out and fade, and the head bursts.</summary>
        public void Release()
        {
            if (_fadeStarted >= 0f)
                return;
            _fadeStarted = Time.time;
            ChogImpact.Burst(_head + Vector3.up * EyeHeight, _colour, 0.8f, 1f, _core);
            ChogFxKit.LaserEnds(_head + Vector3.up * EyeHeight, _colour, _core);
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            var left = 1f;
            if (_fadeStarted >= 0f)
            {
                left = Mathf.Clamp01(1f - (Time.time - _fadeStarted - HoldSeconds) / FadeSeconds);
                if (left <= 0f)
                {
                    ChogEffectPool.Give(s_free, this);
                    return;
                }
            }
            Draw(left);
        }

        private void Draw(float left)
        {
            var from = _origin + Vector3.up * EyeHeight;
            var to = _head + Vector3.up * EyeHeight;
            var along = to - from;
            var length = along.magnitude;
            var direction = length > 1e-4f ? along / length : Vector3.forward;
            var across = Vector3.Cross(Vector3.up, direction);
            var side = (across.sqrMagnitude > 1e-6f ? across.normalized : Vector3.right) * EyeSpacing;
            var tube = Quaternion.FromToRotation(Vector3.up, direction);
            var radius = TubeRadius * _thickness * left;
            var pulse = 1f + 0.15f * Mathf.Sin(_age * 40f);
            var toViewer = Quaternion.LookRotation(GenesisSpecial3D.TowardViewer(to), GenesisSpecial3D.UpOnScreen());
            for (var eye = 0; eye < 2; eye++)
            {
                var offset = eye == 0 ? side : -side;
                _tubes[eye].Place(from + offset, tube, new Vector3(radius, Mathf.Max(0.001f, length), radius));
                _eyeGlows[eye].Place(from + offset, Quaternion.identity, Vector3.one * (GlowRadius * _thickness * pulse));
                _eyeGlows[eye].Fade(left);
                _headGlows[eye].Place(to + offset, Quaternion.identity, Vector3.one * (GlowRadius * 1.2f * _thickness * pulse));
                _headGlows[eye].Fade(left);
                _sparks[eye].Place(to + offset, toViewer * Quaternion.Euler(0f, 0f, _age * 540f + eye * 45f),
                    Vector3.one * (SparkRadius * _thickness * left * (0.85f + 0.15f * pulse)));
            }

            // On the floor, under the line the beams hit.
            var flat = new Vector3(along.x, 0f, along.z);
            _floor.Place(new Vector3(from.x, _origin.y + GenesisSpecial3D.FloorLift, from.z),
                Quaternion.LookRotation(flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward, Vector3.up),
                new Vector3((EyeSpacing * 2f + 0.5f) * _thickness * left, 1f, Mathf.Max(0.001f, flat.magnitude)));
        }
    }

    /// <summary>
    /// The fan of light of a visor or a pair of glasses in 3D: a see-through wedge from the caster, low at
    /// its feet and as high as a hero at its front, in the visor's colours across it (one side, the bright
    /// middle, the other side) with a bright front edge; its footprint outlined on the floor in the outline
    /// colour of the look (red for an enemy); a glint that pops and turns on the glasses. It bursts out
    /// from the caster, then fades.
    /// </summary>
    public sealed class GenesisLightFan : MonoBehaviour
    {
        private const float EyeHeight = 1.15f;
        private const float GrowSeconds = 0.09f;
        private const float FadeSeconds = 0.25f;
        private const float GlintSize = 0.34f;

        private static readonly Stack<GenesisLightFan> s_free = new Stack<GenesisLightFan>();

        private EffectPiece _wedge;
        private EffectPiece _outline;
        private EffectPiece _glint;
        private EffectPiece _glintGlow;
        private Vector3 _apex;
        private Quaternion _facing;
        private float _age;
        private float _fadeStarted = -1f;

        public static GenesisLightFan Create(GenesisEyeLook look, Vector3 apex, Vector3 direction, float radius, float halfAngle)
        {
            var fan = ChogEffectPool.Take(s_free);
            if (fan == null)
            {
                fan = ChogEffectPool.Make("GenesisLightFan").AddComponent<GenesisLightFan>();
                fan._wedge = new EffectPiece(fan.transform, "Wedge");
                fan._outline = new EffectPiece(fan.transform, "Outline");
                fan._glint = new EffectPiece(fan.transform, "Glint");
                fan._glintGlow = new EffectPiece(fan.transform, "Glint Glow");
            }

            direction.y = 0f;
            fan._apex = apex;
            fan._facing = Quaternion.LookRotation(direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward, Vector3.up);
            fan._age = 0f;
            fan._fadeStarted = -1f;
            fan._wedge.Show(GenesisSpecialMeshes.Fan(look, radius, halfAngle), ChogEffectMaterials.Glow);
            fan._outline.Show(GenesisSpecialMeshes.FanOutline(GenesisSpecialMeshes.OutlineOf(look), radius, halfAngle), ChogEffectMaterials.Floor);
            fan._glint.Show(GenesisSpecialMeshes.Spark(Color.white, GenesisSpecialMeshes.OutlineOf(look)), ChogEffectMaterials.Solid);
            fan._glintGlow.Show(GenesisSpecialMeshes.GlowBall(look.Core, Color.white), ChogEffectMaterials.Glow);
            fan.Draw();
            return fan;
        }

        public void Release()
        {
            if (_fadeStarted >= 0f)
                return;
            _fadeStarted = Time.time;
            _outline.Hide();
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_fadeStarted >= 0f && Time.time - _fadeStarted >= FadeSeconds)
            {
                ChogEffectPool.Give(s_free, this);
                return;
            }
            Draw();
        }

        private void Draw()
        {
            var left = _fadeStarted < 0f ? 1f : Mathf.Clamp01(1f - (Time.time - _fadeStarted) / FadeSeconds);
            var grown = Mathf.Lerp(0.35f, 1f, GenesisSpecial3D.EaseOut(_age / GrowSeconds)) * (1f + 0.06f * (1f - left));
            var floor = _apex + Vector3.up * (GenesisSpecial3D.FloorLift - 0.04f);
            _wedge.Place(floor, _facing, new Vector3(grown, 1f, grown));
            _wedge.Fade(left);
            _outline.Place(_apex + Vector3.up * GenesisSpecial3D.FloorLift, _facing, Vector3.one);

            // The glint on the glasses: in, a little larger, back to its size; it turns as it shines.
            var at = _apex + Vector3.up * EyeHeight + _facing * Vector3.forward * 0.4f;
            var pop = _age < 0.1f ? 1.35f * _age / 0.1f : Mathf.Lerp(1.35f, 1f, Mathf.Clamp01((_age - 0.1f) / 0.12f));
            var size = GlintSize * pop * left;
            _glint.Place(at, Quaternion.LookRotation(GenesisSpecial3D.TowardViewer(at), GenesisSpecial3D.UpOnScreen()) * Quaternion.Euler(0f, 0f, _age * 260f),
                Vector3.one * size);
            _glintGlow.Place(at, Quaternion.identity, Vector3.one * (size * 1.25f));
            _glintGlow.Fade(left);
        }
    }

    /// <summary>
    /// The tear a crying Genesis lobs, in 3D: a drop with its tip trailing, shaded in the colour of its
    /// tears with a white highlight and an ink outline. It flies the arc of the lobbed basic attacks
    /// (ChogAttackLook.LobHeight and LobRise), larger at its top, its shadow running on the floor under it
    /// and the ring of the thrower's side on the spot where it lands from the moment it is thrown; it
    /// splashes there into pieces of its colours.
    /// </summary>
    public sealed class GenesisTear : MonoBehaviour
    {
        // At the hands of a hero, as the basic attacks (ChogAttackLook).
        private const float Height = 0.9f;
        private const float Size = 0.3f;
        private const float GrowAtTop = 0.3f;
        private const float ShadowRadius = 0.6f;

        private static readonly Stack<GenesisTear> s_free = new Stack<GenesisTear>();

        private EffectPiece _tear;
        private EffectPiece _shadow;
        private EffectPiece _landing;
        private Vector3 _start;
        private Vector3 _target;
        private Vector3 _at;
        private float _height;
        private float _age;
        private Color _colour;
        private Color _core;

        public static GenesisTear Create(GenesisEyeLook look, Vector3 start, Vector3 target, Color side)
        {
            var tear = ChogEffectPool.Take(s_free);
            if (tear == null)
            {
                tear = ChogEffectPool.Make("GenesisTear").AddComponent<GenesisTear>();
                tear._tear = new EffectPiece(tear.transform, "Tear");
                tear._shadow = new EffectPiece(tear.transform, "Shadow");
                tear._landing = new EffectPiece(tear.transform, "Landing");
            }

            tear._start = start;
            tear._target = new Vector3(target.x, start.y, target.z);
            tear._height = ChogAttackLook.LobHeight(Vector3.Distance(tear._start, tear._target));
            tear._age = 0f;
            tear._colour = look.Outer;
            tear._core = look.Core;
            tear._tear.Show(GenesisSpecialMeshes.Tear(look), ChogEffectMaterials.Solid);
            tear._shadow.Show(GenesisSpecialMeshes.Shadow, ChogEffectMaterials.Floor);
            tear._landing.Show(GenesisSpecialMeshes.Landing(side), ChogEffectMaterials.Floor);
            tear.Follow(start);
            return tear;
        }

        /// <summary>The attack is at "position" on the floor: the tear is drawn over it, on its arc.</summary>
        public void Follow(Vector3 position)
        {
            var flat = new Vector3(position.x, _start.y, position.z);
            var whole = Vector3.Distance(_start, _target);
            var done = whole < 0.01f ? 1f : Mathf.Clamp01(Vector3.Distance(_start, flat) / whole);
            var top = Mathf.Sin(Mathf.PI * done);
            var at = flat + Vector3.up * (Height + ChogAttackLook.LobRise(_height, done));
            _at = at;

            // The round end leads, the tip trails along the arc: its slope is the arc's at that point.
            var heading = whole < 0.01f ? Vector3.forward : (_target - _start) / whole;
            var slope = whole < 0.01f ? -1f : 4f * _height * (1f - 2f * done) / whole;
            var motion = (heading + Vector3.up * slope).normalized;
            var tip = -motion;
            var front = Vector3.ProjectOnPlane(GenesisSpecial3D.TowardViewer(at), tip);
            var rotation = front.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(front.normalized, tip) : Quaternion.FromToRotation(Vector3.up, tip);
            var grown = Mathf.Clamp01(_age / 0.08f);
            _tear.Place(at, rotation, Vector3.one * (Size * grown * (1f + GrowAtTop * top)));
            _shadow.Place(flat + Vector3.up * GenesisSpecial3D.FloorLift, Quaternion.identity,
                Vector3.one * (ShadowRadius * Mathf.Lerp(1f, 0.55f, top)));
        }

        /// <summary>It landed: a splash of its colours, then it is kept for the next one.</summary>
        public void Release()
        {
            ChogImpact.Burst(new Vector3(_at.x, _start.y + Height * 0.5f, _at.z), _colour, 1.2f, 1f, _core);
            ChogEffectPool.Give(s_free, this);
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            // In at once, then a slow beat until it lands (the ring of a lobbed basic attack).
            var into = Mathf.Clamp01(_age / 0.12f);
            _landing.Place(_target + Vector3.up * GenesisSpecial3D.FloorLift, Quaternion.identity,
                Vector3.one * (into * (0.92f + 0.08f * Mathf.Sin(_age * 12f))));
        }
    }

    /// <summary>
    /// A zone that stays, in 3D: a low blob of liquid on the floor, its rim a little wavy, in the colours
    /// of the look (the tears of a crying Genesis), its rim outlined on the floor in the outline colour,
    /// and ripples that spread from its middle to its rim. It spills out with a small overshoot, then
    /// shrinks away when the zone ends.
    /// </summary>
    public sealed class GenesisPuddle : MonoBehaviour
    {
        private const int Ripples = 3;
        private const float RipplePeriod = 1.5f;
        private const float GrowSeconds = 0.25f;
        private const float ShrinkSeconds = 0.2f;

        private static readonly Stack<GenesisPuddle> s_free = new Stack<GenesisPuddle>();

        private EffectPiece _dome;
        private EffectPiece _outline;
        private EffectPiece[] _ripples;
        private Vector3 _centre;
        private float _radius;
        private float _age;
        private float _shrinkStarted = -1f;

        public static GenesisPuddle Create(GenesisEyeLook look, Vector3 centre, float radius)
        {
            var puddle = ChogEffectPool.Take(s_free);
            if (puddle == null)
            {
                puddle = ChogEffectPool.Make("GenesisPuddle").AddComponent<GenesisPuddle>();
                puddle._dome = new EffectPiece(puddle.transform, "Puddle");
                puddle._outline = new EffectPiece(puddle.transform, "Outline");
                puddle._ripples = new EffectPiece[Ripples];
                for (var i = 0; i < Ripples; i++)
                    puddle._ripples[i] = new EffectPiece(puddle.transform, "Ripple");
            }

            puddle._centre = centre;
            puddle._radius = Mathf.Max(0.2f, radius);
            puddle._age = 0f;
            puddle._shrinkStarted = -1f;
            puddle._dome.Show(GenesisSpecialMeshes.Puddle(look, puddle._radius), ChogEffectMaterials.Glow);
            puddle._outline.Show(GenesisSpecialMeshes.PuddleOutline(GenesisSpecialMeshes.OutlineOf(look), puddle._radius),
                ChogEffectMaterials.Floor);
            var ripple = GenesisSpecialMeshes.Ripple(Color.Lerp(look.Core, Color.white, 0.5f));
            foreach (var piece in puddle._ripples)
                piece.Show(ripple, ChogEffectMaterials.Glow);
            puddle.Draw();
            return puddle;
        }

        /// <summary>The zone moved: the puddle lies under that point.</summary>
        public void Follow(Vector3 point) => _centre = point;

        public void Release()
        {
            if (_shrinkStarted >= 0f)
                return;
            _shrinkStarted = Time.time;
            _outline.Hide();
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_shrinkStarted >= 0f && Time.time - _shrinkStarted >= ShrinkSeconds)
            {
                ChogEffectPool.Give(s_free, this);
                return;
            }
            Draw();
        }

        private void Draw()
        {
            var left = _shrinkStarted < 0f ? 1f : Mathf.Clamp01(1f - (Time.time - _shrinkStarted) / ShrinkSeconds);
            var spread = GenesisSpecial3D.Pop(_age / GrowSeconds) * left;
            var floor = _centre + Vector3.up * (GenesisSpecial3D.FloorLift - 0.05f);
            _dome.Place(floor, Quaternion.identity, new Vector3(spread, Mathf.Max(0.01f, left), spread));
            _dome.Fade(left);
            _outline.Place(_centre + Vector3.up * GenesisSpecial3D.FloorLift, Quaternion.identity, Vector3.one * Mathf.Min(1f, spread));

            for (var i = 0; i < _ripples.Length; i++)
            {
                var phase = Mathf.Repeat(_age / RipplePeriod + i / (float)_ripples.Length, 1f);
                var rho = Mathf.Lerp(0.12f, 0.92f, phase);
                var reach = _radius * rho * spread;
                _ripples[i].Place(floor + Vector3.up * (GenesisSpecialMeshes.PuddleBulge(rho) * left + 0.012f), Quaternion.identity,
                    new Vector3(reach, 1f, reach));
                _ripples[i].Fade(Mathf.Sin(Mathf.PI * phase) * left);
            }
        }
    }

    /// <summary>
    /// The hero's own pair of eyes as two eyeballs, rolling forward with the stare: each a white ball (the
    /// skin for a closed eye) wearing the hero's eye on its front, shaded and outlined as the heroes are,
    /// turned to be seen from the match camera while leaning toward where the stare goes, rocking as it
    /// rolls, with its shadow on the floor; ahead of them a see-through curved front as wide as the area
    /// hit, and a soft wash of that area on the floor (in the colour of the Aura, else of the skin).
    /// </summary>
    public sealed class GenesisStare : MonoBehaviour
    {
        private const float EyeHeight = 1.05f;
        private const float GrowSeconds = 0.12f;
        private const float ShrinkSeconds = 0.2f;
        // As shares of the area's radius (GenesisEyes.Pair drew eyes 0.34 large, 0.46 apart).
        private const float EyeSize = 0.34f;
        private const float EyesApart = 0.5f;

        private static readonly Stack<GenesisStare> s_free = new Stack<GenesisStare>();

        private EffectPiece[] _eyes;
        private EffectPiece[] _shadows;
        private EffectPiece _front;
        private EffectPiece _wash;
        private Texture _first;
        private Texture _second;
        private Vector3 _at;
        private Vector3 _forward;
        private float _width;
        private float _age;
        private float _shrinkStarted = -1f;

        public static GenesisStare Create(GenesisEyeLook look, Color skin, Color area, Color areaCore, Vector3 position,
                                          Vector3 forward, float width)
        {
            var stare = ChogEffectPool.Take(s_free);
            if (stare == null)
            {
                stare = ChogEffectPool.Make("GenesisStare").AddComponent<GenesisStare>();
                stare._eyes = new[] { new EffectPiece(stare.transform, "Eye"), new EffectPiece(stare.transform, "Eye") };
                stare._shadows = new[] { new EffectPiece(stare.transform, "Shadow"), new EffectPiece(stare.transform, "Shadow") };
                stare._front = new EffectPiece(stare.transform, "Front");
                stare._wash = new EffectPiece(stare.transform, "Wash");
            }

            forward.y = 0f;
            stare._forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            stare._width = Mathf.Max(0.3f, width);
            stare._age = 0f;
            stare._shrinkStarted = -1f;
            var ball = GenesisSpecialMeshes.Eyeball(look, skin);
            stare._first = GenesisSpecialMeshes.EyePicture(look, skin, false);
            stare._second = GenesisSpecialMeshes.EyePicture(look, skin, true);
            stare._eyes[0].Show(ball, ChogEffectMaterials.Solid, stare._first);
            stare._eyes[1].Show(ball, ChogEffectMaterials.Solid, stare._second);
            foreach (var shadow in stare._shadows)
                shadow.Show(GenesisSpecialMeshes.Shadow, ChogEffectMaterials.Floor);
            stare._front.Show(GenesisSpecialMeshes.Front(area, areaCore), ChogEffectMaterials.Glow);
            stare._wash.Show(GenesisSpecialMeshes.FloorWash(area), ChogEffectMaterials.Floor);
            stare.Follow(position);
            return stare;
        }

        public void Follow(Vector3 position)
        {
            _at = position;
            Draw();
        }

        public void Release()
        {
            if (_shrinkStarted < 0f)
                _shrinkStarted = Time.time;
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_shrinkStarted >= 0f && Time.time - _shrinkStarted >= ShrinkSeconds)
            {
                ChogEffectPool.Give(s_free, this);
                return;
            }
            Draw();
        }

        private void Draw()
        {
            var left = _shrinkStarted < 0f ? 1f : Mathf.Clamp01(1f - (Time.time - _shrinkStarted) / ShrinkSeconds);
            var grown = GenesisSpecial3D.Pop(_age / GrowSeconds) * left;
            var across = Vector3.Cross(Vector3.up, _forward);
            var middle = _at + Vector3.up * EyeHeight;
            // The eye on the viewer's left is the first eye of the pair, as the flat pair drew it.
            var viewerRight = Vector3.Cross(Vector3.up, -GenesisSpecial3D.TowardViewer(middle));
            var flip = Vector3.Dot(across, viewerRight) < 0f;
            for (var e = 0; e < 2; e++)
            {
                var sign = e == 0 ? -1f : 1f;
                var bob = Mathf.Sin(_age * 10f + e * 1.7f) * 0.08f;
                var at = middle + across * (sign * EyesApart * _width) + Vector3.up * bob;
                var gaze = GenesisSpecial3D.Gaze(at, _forward, 0.5f);
                var roll = Quaternion.AngleAxis(Mathf.Sin(_age * 14f + e) * 12f, across);
                var leftOfViewer = (sign < 0f) != flip;
                _eyes[e].Swap(leftOfViewer ? _first : _second);
                _eyes[e].Place(at, roll * Quaternion.LookRotation(gaze, GenesisSpecial3D.UpOnScreen()), Vector3.one * (EyeSize * _width * grown));
                _shadows[e].Place(new Vector3(at.x, _at.y + GenesisSpecial3D.FloorLift, at.z), Quaternion.identity,
                    Vector3.one * (EyeSize * _width * 1.1f * grown));
            }

            var floor = _at + Vector3.up * (GenesisSpecial3D.FloorLift - 0.02f);
            _front.Place(floor, Quaternion.LookRotation(_forward, Vector3.up), new Vector3(_width * grown, Mathf.Max(0.01f, grown) * 1.3f, _width * grown));
            _front.Fade(left);
            _wash.Place(_at + Vector3.up * GenesisSpecial3D.FloorLift, Quaternion.identity, Vector3.one * (_width * Mathf.Min(1f, grown)));
        }
    }

    /// <summary>
    /// A burst of looks in 3D: the hero's own eye as eight eyeballs (its two eyes in turn) that burst out
    /// from it to the rim of the area, the ring twisting and each eyeball spinning once as it flies, then
    /// looking out and up at the camera; a shock ring that runs along the floor to the rim; the reach of the
    /// area outlined on the floor in the outline colour, and a soft wash of it.
    /// </summary>
    public sealed class GenesisLookBurst : MonoBehaviour
    {
        private const int Count = 8;
        private const float EyeRadius = 0.42f;
        private const float Height = 1.2f;
        private const float BurstSeconds = 0.3f;
        private const float ShrinkSeconds = 0.2f;

        private static readonly Stack<GenesisLookBurst> s_free = new Stack<GenesisLookBurst>();

        private EffectPiece[] _eyes;
        private EffectPiece[] _shadows;
        private EffectPiece _shock;
        private EffectPiece _wash;
        private EffectPiece _reach;
        private Vector3 _centre;
        private float _radius;
        private float _age;
        private float _shrinkStarted = -1f;

        public static GenesisLookBurst Create(GenesisEyeLook look, Color skin, Color area, Color areaCore, Vector3 centre, float radius)
        {
            var burst = ChogEffectPool.Take(s_free);
            if (burst == null)
            {
                burst = ChogEffectPool.Make("GenesisLookBurst").AddComponent<GenesisLookBurst>();
                burst._eyes = new EffectPiece[Count];
                burst._shadows = new EffectPiece[Count];
                for (var i = 0; i < Count; i++)
                {
                    burst._eyes[i] = new EffectPiece(burst.transform, "Eye");
                    burst._shadows[i] = new EffectPiece(burst.transform, "Shadow");
                }
                burst._shock = new EffectPiece(burst.transform, "Shock");
                burst._wash = new EffectPiece(burst.transform, "Wash");
                burst._reach = new EffectPiece(burst.transform, "Reach");
            }

            burst._centre = centre;
            burst._radius = Mathf.Max(0.5f, radius);
            burst._age = 0f;
            burst._shrinkStarted = -1f;
            var ball = GenesisSpecialMeshes.Eyeball(look, skin);
            var first = GenesisSpecialMeshes.EyePicture(look, skin, false);
            var second = GenesisSpecialMeshes.EyePicture(look, skin, true);
            for (var i = 0; i < Count; i++)
            {
                burst._eyes[i].Show(ball, ChogEffectMaterials.Solid, i % 2 == 0 ? first : second);
                burst._shadows[i].Show(GenesisSpecialMeshes.Shadow, ChogEffectMaterials.Floor);
            }
            burst._shock.Show(GenesisSpecialMeshes.Shock(area, areaCore), ChogEffectMaterials.Glow);
            burst._wash.Show(GenesisSpecialMeshes.FloorWash(area), ChogEffectMaterials.Floor);
            burst._reach.Show(GenesisSpecialMeshes.FloorRing(GenesisSpecialMeshes.OutlineOf(look)), ChogEffectMaterials.Floor);
            burst.Draw();
            return burst;
        }

        public void Follow(Vector3 point) => _centre = point;

        public void Release()
        {
            if (_shrinkStarted >= 0f)
                return;
            _shrinkStarted = Time.time;
            _reach.Hide();
            _wash.Hide();
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_shrinkStarted >= 0f && Time.time - _shrinkStarted >= ShrinkSeconds)
            {
                ChogEffectPool.Give(s_free, this);
                return;
            }
            Draw();
        }

        private void Draw()
        {
            var left = _shrinkStarted < 0f ? 1f : Mathf.Clamp01(1f - (Time.time - _shrinkStarted) / ShrinkSeconds);
            var opened = GenesisSpecial3D.EaseOut(_age / BurstSeconds);
            var ring = Mathf.Lerp(_radius * 0.3f, _radius * 0.85f, opened);
            var twist = 40f * opened;
            var size = EyeRadius * Mathf.Lerp(0.5f, 1f, GenesisSpecial3D.Pop(_age / 0.16f)) * left;
            for (var i = 0; i < Count; i++)
            {
                var angle = i * 360f / Count + twist;
                var radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var at = _centre + radial * ring + Vector3.up * (Height + Mathf.Sin(_age * 9f + i) * 0.06f);
                var gaze = GenesisSpecial3D.Gaze(at, radial, 0.55f);
                // One whole turn on itself as it flies out, ending on its gaze.
                var spin = Quaternion.AngleAxis(360f * (1f - opened), Vector3.up);
                _eyes[i].Place(at, spin * Quaternion.LookRotation(gaze, GenesisSpecial3D.UpOnScreen()), Vector3.one * size);
                _shadows[i].Place(new Vector3(at.x, _centre.y + GenesisSpecial3D.FloorLift, at.z), Quaternion.identity, Vector3.one * (size * 1.1f));
            }

            var shock = Mathf.Lerp(_radius * 0.25f, _radius, opened);
            var shockFade = Mathf.Clamp01(1f - (_age - BurstSeconds) / 0.15f) * left;
            _shock.Place(_centre + Vector3.up * (GenesisSpecial3D.FloorLift - 0.03f), Quaternion.identity,
                new Vector3(shock, Mathf.Max(0.01f, shockFade), shock));
            _shock.Fade(shockFade);
            _wash.Place(_centre + Vector3.up * GenesisSpecial3D.FloorLift, Quaternion.identity, Vector3.one * shock);
            _reach.Place(_centre + Vector3.up * (GenesisSpecial3D.FloorLift + 0.005f), Quaternion.identity, Vector3.one * _radius);
        }
    }
}
