using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Draws what each team defends (Structure.qtn): the tower as a totem with the head of a
    /// Chog, whose laser eyes are what it shoots with, and the core as a heart inside a crown of
    /// Chog spikes. Flat cartoon pictures that face the camera, like the heads of the heroes,
    /// painted with the same unlit material as the attacks. Each stands on a base in the colour
    /// of its side (blue for the local player's team, red for the other) under a bar of its
    /// health; a destroyed one stays as a grey ruin. A 3D structure that is hit wobbles like jelly and
    /// flashes a little, and one that falls bursts into pieces of its colours before the ruin shows.
    /// </summary>
    public sealed class StructureViews : MonoBehaviour
    {
        private static readonly Color Outline = new Color(0.10f, 0.05f, 0.16f);
        private static readonly Color Purple = new Color(0.42f, 0.24f, 0.82f);
        private static readonly Color DeepPurple = new Color(0.27f, 0.14f, 0.55f);
        private static readonly Color Cream = new Color(0.99f, 0.95f, 0.80f);
        private static readonly Color Pink = new Color(0.95f, 0.32f, 0.58f);
        private static readonly Color Ours = new Color(0.25f, 0.56f, 1.00f);
        private static readonly Color Theirs = new Color(1.00f, 0.30f, 0.30f);
        private static readonly Color BarBack = new Color(0.08f, 0.06f, 0.12f, 0.85f);

        // The laser of a tower: the red and yellow of the Chog laser eyes.
        private static readonly GenesisEyeLook Laser = new GenesisEyeLook
        {
            Outline = new Color(0.45f, 0.05f, 0.05f),
            Outer = new Color(1.00f, 0.25f, 0.10f),
            Core = new Color(1.00f, 0.92f, 0.35f),
        };

        // Where the eyes of a tower are, up its picture, and the height GenesisBeam adds by itself.
        private const float TowerEyes = 1.92f;
        private const float BeamLift = 1.15f;
        private const float BarWidth = 1.3f;
        private const float BarHeight = 0.16f;

        private sealed class Drawing
        {
            public GameObject Root;
            public Transform Picture;
            public MeshFilter PictureMesh;
            public Transform Fill;
            public GameObject Bar;
            public EStructureKind Kind;
            public Color Side;
            public bool Ruined;
            public bool Seen;

            // The 3D model (ChogStructureModel), when the page draws one, and the parts that move.
            public Transform Model;
            public Transform Head;
            public Transform Eyes;
            public Transform Heart;
            // Whom the tower's head turns to, until when; whether the structure is being hit.
            public Vector3 AimAt;
            public float AimUntil;
            public bool Alarmed;
            // Whom a tower watches between its shots: the hero it shoots next, as the simulation picks it.
            public bool Watching;
            public Vector3 WatchAt;
            // Whether the bar of a 3D model was put over it, which needs the camera.
            public bool BarPlaced;
            // When it was last hit (below 0: not since it settled), and the size of its model at rest.
            public float HitAt = -1f;
            public Vector3 ModelScale = Vector3.one;
            // When chips last flew off it: a few times a second at most.
            public float ChipsAt = -1f;
        }

        // Chips fly off a structure that is hit at most this often.
        private const float ChipsSeconds = 0.25f;

        // A hit squashes a structure less than a hero (ChogBodyAnimator.Squash gives the spring): this much
        // lower and this much wider at the moment of the blow; and it flashes this white for this long.
        private const float JellyHeight = 0.08f;
        private const float JellyWidth = 0.06f;
        private const float HitFlash = 0.45f;
        private const float HitFlashSeconds = 0.08f;
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private MaterialPropertyBlock _flashBlock;
        private readonly List<Renderer> _flashRenderers = new List<Renderer>();

        // The core is drawn this much larger than its model: the owner found it too small to notice
        // (17 October), while it is what a team loses the match with. Its collision circle follows since
        // 18 October (Structures.CoreRadius 1.5, the plinth's width at this scale).
        public const float CoreScale = 1.5f;
        // How much the heart of a core swells on each beat.
        private const float HeartBeat = 0.14f;

        // How long the tower's head stays turned to whom it shot, and how fast it turns.
        private const float AimSeconds = 0.9f;
        private const float TurnRate = 12f;
        // Above the top of a 3D model, the bar of its health.
        private const float BarAboveModel = 0.35f;

        private readonly Dictionary<EntityRef, Drawing> _drawings = new Dictionary<EntityRef, Drawing>();
        private readonly List<EntityRef> _gone = new List<EntityRef>();
        private Transform _camera;

        // The line that tells every player a structure fell, for a few seconds.
        private const float NoticeSeconds = 3.5f;
        private string _notice = "";
        private Color _noticeColour = Color.white;
        private float _noticeUntil;
        private int _localTeam = -1;
        private GUIStyle _noticeStyle;
        // The alarm of the player's own team, empty while none of its structures is hit.
        private string _alarm = "";
        // The time a siege has left, at the top of the screen; empty in the other modes.
        private string _clock = "";
        private bool _clockShort;
        private GUIStyle _clockStyle;
        private const float ShortSeconds = 30f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Structures");
            DontDestroyOnLoad(host);
            host.AddComponent<StructureViews>();
        }

        private void Start()
        {
            QuantumEvent.Subscribe<EventStructureShot>(this, OnShot);
            QuantumEvent.Subscribe<EventStructureDestroyed>(this, OnFell);
            QuantumEvent.Subscribe<EventCharacterDamaged>(this, OnDamaged);
        }

        /// <summary>
        /// What the screen says when a structure falls, for a player of "localTeam" (-1 for
        /// somebody who plays in no team, who reads it as team 0 does).
        /// </summary>
        public static string NoticeFor(EStructureKind kind, int team, int localTeam)
        {
            var mine = team == (localTeam < 0 ? 0 : localTeam);
            if (kind == EStructureKind.Core)
                return mine ? "YOUR CORE FELL" : "ENEMY CORE DESTROYED";
            return mine ? "YOUR TOWER FELL" : "ENEMY TOWER DESTROYED";
        }

        private static unsafe bool PlayIsOn(Frame frame) => frame.Global->State == GameState.Playing;

        /// <summary>The time a siege has left as the screen shows it: minutes and seconds, rounded up, never below 0:00.</summary>
        public static string ClockText(float secondsLeft)
        {
            var whole = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
            return $"{whole / 60}:{whole % 60:00}";
        }

        /// <summary>What the screen says while a structure of the player's team is being hit.</summary>
        public static string AlarmFor(EStructureKind kind)
        {
            return kind == EStructureKind.Core ? "YOUR CORE IS UNDER ATTACK" : "YOUR TOWER IS UNDER ATTACK";
        }

        // A structure that is hit wobbles and flashes (its model only; the flat picture stays as it is).
        private void OnDamaged(EventCharacterDamaged hit)
        {
            if (!GenesisEffects.Enabled || !_drawings.TryGetValue(hit.target, out var drawing) || drawing.Model == null || drawing.Ruined)
                return;
            drawing.HitAt = Time.time;
            if (Time.time - drawing.ChipsAt < ChipsSeconds)
                return;
            drawing.ChipsAt = Time.time;
            // Chips of stone and a little flash at its top (ChogFxKit).
            ChogFxKit.StructureHit(drawing.Root.transform.position + Vector3.up * (ModelHeight(drawing) * 0.8f), drawing.Side);
        }

        // How tall a 3D structure stands over its foot.
        private static float ModelHeight(Drawing drawing)
        {
            if (drawing.Model == null)
                return drawing.Kind == EStructureKind.Core ? 1.1f : 1.9f;
            var foot = drawing.Root.transform.position.y;
            var top = foot;
            foreach (var drawn in drawing.Model.GetComponentsInChildren<Renderer>())
                top = Mathf.Max(top, drawn.bounds.max.y);
            return Mathf.Max(0.5f, top - foot);
        }

        /// <summary>
        /// How a structure stands "seconds" after a hit, as a multiplier of its size: lower and wider at once,
        /// then a spring back (ChogBodyAnimator.Squash); (1, 1, 1) when it is not hit.
        /// </summary>
        public static Vector3 Jelly(float seconds)
        {
            var squash = ChogBodyAnimator.Squash(seconds);
            return new Vector3(1f + JellyWidth * squash, 1f - JellyHeight * squash, 1f + JellyWidth * squash);
        }

        private void OnFell(EventStructureDestroyed fell)
        {
            // It bursts into pieces of its colours where it stood, before the ruin shows.
            if (_drawings.TryGetValue(fell.Structure, out var fallen) && GenesisEffects.Enabled)
            {
                var at = fallen.Root.transform.position;
                Bursts(at, fallen.Kind, fallen.Side);
                // It blows up in several blasts; a core breaks into hearts under a BOOM (ChogFxKit).
                ChogFxKit.StructureFalls(at, ModelHeight(fallen), fallen.Kind == EStructureKind.Core, fallen.Side);
                fallen.HitAt = -1f;
            }
            _notice = NoticeFor(fell.Kind, fell.Team, _localTeam);
            _noticeColour = fell.Team == (_localTeam < 0 ? 0 : _localTeam) ? Theirs : Ours;
            _noticeUntil = Time.time + NoticeSeconds;
        }

        /// <summary>The pieces a structure of that kind and side bursts into where it stood ("at", its foot).</summary>
        public static void Bursts(Vector3 at, EStructureKind kind, Color side)
        {
            var top = kind == EStructureKind.Core ? 1.1f : 1.9f;
            ChogImpact.Burst(at + Vector3.up * top, Purple, 2.6f, 1.9f, side);
            ChogImpact.Burst(at + Vector3.up * (top * 0.5f), DeepPurple, 2.2f, 1.5f, Cream);
            ChogImpact.Burst(at + Vector3.up * 0.3f, side, 1.8f, 1.2f, Outline);
        }

        // One line near the top of the screen, sized with it, on a dark band so it reads on
        // any ground: what fell, for a few seconds, or else the alarm of a structure of the
        // player's team, smaller and pulsing for as long as it is hit.
        private void OnGUI()
        {
            DrawClock();
            var fell = Time.time < _noticeUntil && _notice.Length > 0;
            if (!fell && _alarm.Length == 0)
                return;

            if (_noticeStyle == null)
            {
                _noticeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                // The title font of the game's look (Koulen), when the project has it.
                var koulen = Resources.Load<Font>("Fonts/Koulen-Regular");
                if (koulen != null)
                {
                    _noticeStyle.font = koulen;
                    _noticeStyle.fontStyle = FontStyle.Normal;
                }
            }
            _noticeStyle.fontSize = Mathf.Max(fell ? 14 : 11, Mathf.RoundToInt(Screen.height * (fell ? 0.065f : 0.045f)));
            var colour = fell ? _noticeColour : Theirs;
            if (!fell)
                colour.a = 0.55f + 0.45f * Mathf.PingPong(Time.time * 2f, 1f);
            _noticeStyle.normal.textColor = colour;

            var band = new Rect(0f, Screen.height * 0.16f, Screen.width, Screen.height * (fell ? 0.11f : 0.08f));
            var before = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, fell ? 0.6f : 0.45f);
            GUI.DrawTexture(band, Texture2D.whiteTexture);
            GUI.color = before;
            GUI.Label(band, fell ? _notice : _alarm, _noticeStyle);
        }

        /// <summary>Set by the match's scoreboard (Chog.Screens.ChogScoreboard), which draws the clock itself.</summary>
        public static bool ClockDrawnElsewhere { get; set; }

        // The siege's clock, top middle between the two scores: white, red in its last thirty seconds.
        private void DrawClock()
        {
            if (_clock.Length == 0 || ClockDrawnElsewhere)
                return;
            if (_clockStyle == null)
            {
                _clockStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                var koulen = Resources.Load<Font>("Fonts/Koulen-Regular");
                if (koulen != null)
                {
                    _clockStyle.font = koulen;
                    _clockStyle.fontStyle = FontStyle.Normal;
                }
            }
            _clockStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(Screen.height * 0.06f));
            _clockStyle.normal.textColor = _clockShort ? Theirs : Color.white;
            var area = new Rect(Screen.width * 0.40f, Screen.height * 0.015f, Screen.width * 0.20f, Screen.height * 0.085f);
            var before = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = before;
            GUI.Label(area, _clock, _clockStyle);
        }

        private void OnDestroy() => QuantumEvent.UnsubscribeListener(this);

        private void Update()
        {
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var frame = game != null && game.Frames != null ? game.Frames.Predicted : null;
            var roster = frame != null ? QuantumUnityDB.GetGlobalAsset(frame.RuntimeConfig.GenesisRoster) : null;
            foreach (var drawing in _drawings.Values)
                drawing.Seen = false;
            _alarm = "";
            _clock = "";
            if (frame != null && MatchModes.IsSiege(frame) && PlayIsOn(frame))
            {
                var left = MatchModes.SiegeTimeLeft(frame).AsFloat;
                _clock = ClockText(left);
                _clockShort = left <= ShortSeconds;
            }

            if (roster != null && roster.ShapeMaterial != null)
            {
                var localTeam = LocalTeam(game, frame);
                _localTeam = localTeam;
                var now = frame.Number * frame.DeltaTime;
                var structures = frame.Filter<Structure, Transform2D, TeamInfo, Health>();
                while (structures.Next(out var entity, out var structure, out var place, out var team, out var health))
                {
                    // The core speaks louder than the tower when both are hit.
                    if (team.Index == (localTeam < 0 ? 0 : localTeam) && !health.IsDead && structure.AlarmUntil > now
                        && (_alarm.Length == 0 || structure.Kind == EStructureKind.Core))
                        _alarm = AlarmFor(structure.Kind);

                    if (!_drawings.TryGetValue(entity, out var drawing))
                    {
                        // Without a local player (a spectator) team 0 is drawn as "ours".
                        var side = team.Index == (localTeam < 0 ? 0 : localTeam) ? Ours : Theirs;
                        drawing = Build(roster.ShapeMaterial, structure.Kind, side);
                        _drawings[entity] = drawing;
                    }

                    drawing.Seen = true;
                    drawing.Root.transform.position = place.Position.ToUnityVector3();
                    drawing.Watching = !health.IsDead && structure.Range > 0
                                       && NextTarget(frame, team.Index, place.Position, structure.Range, out drawing.WatchAt);
                    drawing.Alarmed = !health.IsDead && structure.AlarmUntil > now;
                    if (health.IsDead != drawing.Ruined)
                    {
                        drawing.Ruined = health.IsDead;
                        if (drawing.Model != null)
                            Remodel(drawing);
                        else
                            Replace(drawing.PictureMesh, Paint(drawing.Kind, drawing.Side, drawing.Ruined));
                        drawing.Bar.SetActive(!drawing.Ruined);
                    }

                    if (!drawing.Ruined)
                    {
                        var left = AttributesHelper.GetAttributeData(frame, entity, EAttributeType.Health);
                        var share = left.MaxValue > 0 ? Mathf.Clamp01((left.CurrentValue / left.MaxValue).AsFloat) : 0f;
                        drawing.Fill.localScale = new Vector3(share, 1f, 1f);
                    }
                }
            }

            // A match that ended took its structures with it.
            _gone.Clear();
            foreach (var pair in _drawings)
                if (!pair.Value.Seen)
                    _gone.Add(pair.Key);
            foreach (var entity in _gone)
            {
                Destroy(_drawings[entity].Root);
                _drawings.Remove(entity);
            }
        }

        private void LateUpdate()
        {
            if (_camera == null && Camera.main != null)
                _camera = Camera.main.transform;
            if (_camera == null)
                return;

            // A tower faces the middle of the arena, so the two sides face each other, and turns its head
            // to whom it shoots next (the owner, 17 October: the towers "all looked down"); a core faces
            // the camera, its heart seen from the front; the heart beats, faster while it is hit.
            var toCamera = -_camera.forward;
            toCamera.y = 0f;
            var facing = toCamera.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(toCamera.normalized) : Quaternion.identity;
            var turn = 1f - Mathf.Exp(-TurnRate * Time.deltaTime);
            foreach (var drawing in _drawings.Values)
            {
                drawing.Picture.rotation = _camera.rotation;
                if (drawing.Model == null)
                    continue;

                var middle = drawing.Kind == EStructureKind.Tower ? TowardTheMiddle(drawing.Root.transform.position) : Vector3.zero;
                var rest = middle != Vector3.zero ? Quaternion.LookRotation(middle) : facing;
                drawing.Model.rotation = rest;
                if (!drawing.BarPlaced)
                    PlaceBar(drawing);
                if (drawing.Head != null && !drawing.Ruined)
                {
                    var wanted = rest;
                    var watched = Time.time < drawing.AimUntil ? drawing.AimAt
                        : drawing.Watching ? drawing.WatchAt : (Vector3?)null;
                    if (watched.HasValue)
                    {
                        var toTarget = watched.Value - drawing.Head.position;
                        toTarget.y = 0f;
                        if (toTarget.sqrMagnitude > 1e-4f)
                            wanted = Quaternion.LookRotation(toTarget.normalized);
                    }
                    drawing.Head.rotation = Quaternion.Slerp(drawing.Head.rotation, wanted, turn);
                }

                // The wobble of a hit, and its flash.
                if (drawing.HitAt >= 0f)
                {
                    var since = Time.time - drawing.HitAt;
                    if (drawing.Ruined || since >= ChogBodyAnimator.FlinchSeconds)
                    {
                        drawing.HitAt = -1f;
                        drawing.Model.localScale = drawing.ModelScale;
                        Flash(drawing, 0f);
                    }
                    else
                    {
                        drawing.Model.localScale = Vector3.Scale(drawing.ModelScale, Jelly(since));
                        Flash(drawing, since < HitFlashSeconds ? HitFlash * (1f - since / HitFlashSeconds) : 0f);
                    }
                }

                if (drawing.Heart != null && !drawing.Ruined)
                {
                    var beats = drawing.Alarmed ? 3.2f : 1.3f;
                    var pulse = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * beats)), 6f);
                    drawing.Heart.localScale = Vector3.one * (1f + HeartBeat * pulse);
                }
            }
        }

        // A tower hurt a hero: its two laser eyes draw a beam to that hero, which then fades.
        private void OnShot(EventStructureShot shot)
        {
            var frame = shot.Game.Frames.Predicted;
            var roster = QuantumUnityDB.GetGlobalAsset(frame.RuntimeConfig.GenesisRoster);
            if (roster == null || roster.ShapeMaterial == null || !_drawings.TryGetValue(shot.Structure, out var tower)
                || !frame.Exists(shot.Target) || !frame.TryGet<Transform2D>(shot.Target, out var target))
                return;

            Vector3 eyes;
            if (tower.Eyes != null && !tower.Ruined)
            {
                // The 3D tower fires from its eyes and turns its head to its target.
                eyes = tower.Eyes.position - Vector3.up * BeamLift;
                tower.AimAt = target.Position.ToUnityVector3();
                tower.AimUntil = Time.time + AimSeconds;
            }
            else
            {
                // The eyes are up the picture, which leans back with the camera.
                var up = _camera != null ? _camera.up : Vector3.up;
                eyes = tower.Root.transform.position + up * TowerEyes - Vector3.up * BeamLift;
            }
            // In 3D as the lasers of the heroes' specials (GenesisLaser), else flat.
            if (ChogEffectMaterials.Available)
            {
                var laser = GenesisLaser.Create(Laser, eyes, 0.8f);
                laser.SetHead(target.Position.ToUnityVector3());
                laser.Release();
                return;
            }
            var beam = GenesisBeam.Create(roster.ShapeMaterial, Laser, eyes, 0.8f);
            beam.SetHead(target.Position.ToUnityVector3());
            beam.Release();
        }

        private static int LocalTeam(QuantumGame game, Frame frame)
        {
            var players = frame.Filter<PlayerLink, TeamInfo>();
            while (players.Next(out _, out var link, out var team))
                if (game.PlayerIsLocal(link.PlayerRef))
                    return team.Index;
            return -1;
        }

        private static Drawing Build(Material material, EStructureKind kind, Color side)
        {
            var root = new GameObject(kind == EStructureKind.Core ? "Chog Core" : "Chog Tower");
            var drawing = new Drawing { Root = root, Kind = kind, Side = side };
            if (ChogStructureModel.On)
                Remodel(drawing);

            GameObject picture;
            float top;
            if (drawing.Model != null)
            {
                // The model turns by itself (LateUpdate): only the bar faces the camera, put over the top
                // of the model as the camera sees it once there is a camera (PlaceBar).
                picture = new GameObject("Picture");
                picture.transform.SetParent(root.transform, false);
                top = 0f;
            }
            else
            {
                picture = Part("Picture", root.transform, material, Paint(kind, side, false), 1);
                drawing.PictureMesh = picture.GetComponent<MeshFilter>();
                top = kind == EStructureKind.Core ? 2.25f : 2.95f;
            }
            drawing.Picture = picture.transform;

            // The bar: a dark back and, over it, what is left, shrinking towards its left end.
            var bar = new GameObject("Health");
            bar.transform.SetParent(picture.transform, false);
            bar.transform.localPosition = new Vector3(-BarWidth * 0.5f, top, 0f);
            var back = new ShapePainter();
            back.Bar(new Vector2(BarWidth * 0.5f, 0f), BarWidth + 0.08f, BarHeight + 0.08f, 0f, BarBack);
            Part("Back", bar.transform, material, back.ToMesh("StructureBarBack"), 2);
            var left = new ShapePainter();
            left.Bar(new Vector2(BarWidth * 0.5f, 0f), BarWidth, BarHeight, 0f, side);
            var fill = Part("Left", bar.transform, material, left.ToMesh("StructureBarLeft"), 3);

            drawing.Fill = fill.transform;
            drawing.Bar = bar;
            return drawing;
        }

        // The 3D model of a structure, built again when it falls: a ruin in greys and lower, the tower
        // without its head, the core without its heart.
        private static void Remodel(Drawing drawing)
        {
            if (drawing.Model != null)
                Destroy(drawing.Model.gameObject);
            drawing.Model = ChogStructureModel.Build(drawing.Kind, drawing.Side == Ours, drawing.Ruined, drawing.Root.transform);
            if (drawing.Model == null)
                return;
            drawing.HitAt = -1f;
            if (drawing.Kind == EStructureKind.Core)
                drawing.Model.localScale *= CoreScale;
            drawing.ModelScale = drawing.Model.localScale;

            drawing.Head = PartOf(drawing.Model, ChogStructureModel.HeadPart);
            drawing.Eyes = PartOf(drawing.Model, ChogStructureModel.EyesPart);
            drawing.Heart = PartOf(drawing.Model, ChogStructureModel.HeartPart);
            if (!drawing.Ruined)
                return;
            if (drawing.Head != null)
                drawing.Head.gameObject.SetActive(false);
            if (drawing.Heart != null)
                drawing.Heart.gameObject.SetActive(false);
            drawing.Model.localScale = Vector3.Scale(drawing.ModelScale, new Vector3(1f, 0.55f, 1f));
        }

        /// <summary>
        /// The way, on the ground, from a structure standing at "at" to the middle of the arena (the origin,
        /// between the two sides); zero at the very middle.
        /// </summary>
        public static Vector3 TowardTheMiddle(Vector3 at)
        {
            var toMiddle = new Vector3(-at.x, 0f, -at.z);
            return toMiddle.sqrMagnitude > 1e-4f ? toMiddle.normalized : Vector3.zero;
        }

        // The hero a tower of "team" at "from" shoots next, by the simulation's own rule (Structures.TowerTarget):
        // the nearest living hero of another team within "range" that no block of the map hides.
        private static bool NextTarget(Frame frame, int team, Photon.Deterministic.FPVector2 from, Photon.Deterministic.FP range, out Vector3 at)
        {
            at = default;
            var target = Structures.TowerTarget(frame, team, from, range);
            if (target == default || !frame.TryGet<Transform2D>(target, out var place))
                return false;
            at = place.Position.ToUnityVector3();
            return true;
        }

        // The parts of a 3D structure drawn with the heroes' material turn that white (0: their colours).
        private void Flash(Drawing drawing, float amount)
        {
            if (drawing.Model == null)
                return;
            if (_flashBlock == null)
                _flashBlock = new MaterialPropertyBlock();
            drawing.Model.GetComponentsInChildren(false, _flashRenderers);
            foreach (var drawn in _flashRenderers)
            {
                if (drawn.sharedMaterial != ChogHeroModel.Material)
                    continue;
                drawn.GetPropertyBlock(_flashBlock);
                _flashBlock.SetFloat(FlashId, amount);
                drawn.SetPropertyBlock(_flashBlock);
            }
        }

        private static Transform PartOf(Transform model, string name)
        {
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
                if (part.name == name)
                    return part;
            return null;
        }

        // The bar of a 3D model, just over its top as the camera sees it: the camera looks down steeply,
        // so the top of the screen is the far side of the crown, not its highest point.
        private void PlaceBar(Drawing drawing)
        {
            var root = drawing.Root.transform.position;
            var up = _camera.up;
            var reach = 0f;
            foreach (var drawn in drawing.Model.GetComponentsInChildren<Renderer>())
            {
                var box = drawn.bounds;
                for (var corner = 0; corner < 8; corner++)
                {
                    var point = new Vector3((corner & 1) == 0 ? box.min.x : box.max.x, (corner & 2) == 0 ? box.min.y : box.max.y,
                        (corner & 4) == 0 ? box.min.z : box.max.z);
                    reach = Mathf.Max(reach, Vector3.Dot(point - root, up));
                }
            }
            drawing.Bar.transform.localPosition = new Vector3(-BarWidth * 0.5f, reach + BarAboveModel, 0f);
            drawing.BarPlaced = true;
        }

        private static GameObject Part(string name, Transform parent, Material material, Mesh mesh, int order)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var drawn = part.GetComponent<MeshRenderer>();
            drawn.sharedMaterial = material;
            drawn.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            drawn.receiveShadows = false;
            drawn.sortingOrder = order;
            return part;
        }

        private static void Replace(MeshFilter filter, Mesh mesh)
        {
            if (filter.sharedMesh != null)
                Destroy(filter.sharedMesh);
            filter.sharedMesh = mesh;
        }

        /// <summary>
        /// The picture of a structure, upright, its foot at the origin. A ruin is the same picture
        /// in greys, lower, without its eyes or its heart.
        /// </summary>
        internal static Mesh Paint(EStructureKind kind, Color side, bool ruined)
        {
            Color In(Color color) => ruined ? Grey(color) : color;
            var squash = ruined ? 0.55f : 1f;
            var painter = new ShapePainter();

            if (kind == EStructureKind.Core)
            {
                var heart = new Vector2(0f, 1.05f * squash);
                painter.Ellipse(new Vector2(0f, 0.16f), 0.98f, 0.38f, Outline);
                painter.Ellipse(new Vector2(0f, 0.16f), 0.88f, 0.31f, In(side));
                painter.Star(heart, 1.02f * squash, 0.68f * squash, 12, Outline);
                painter.Star(heart, 0.92f * squash, 0.63f * squash, 12, In(Purple));
                painter.Ellipse(heart, 0.62f * squash, 0.62f * squash, Outline);
                painter.Ellipse(heart, 0.55f * squash, 0.55f * squash, In(Cream));
                if (!ruined)
                    painter.Heart(heart + new Vector2(0f, -0.08f), 0.66f, Pink);
                return painter.ToMesh("ChogCore");
            }

            var head = new Vector2(0f, 1.95f * squash);
            painter.Ellipse(new Vector2(0f, 0.13f), 0.66f, 0.26f, Outline);
            painter.Ellipse(new Vector2(0f, 0.13f), 0.56f, 0.20f, In(side));
            painter.Bar(new Vector2(0f, 0.85f * squash), 1.50f * squash, 0.54f, 90f, Outline);
            painter.Bar(new Vector2(0f, 0.85f * squash), 1.42f * squash, 0.42f, 90f, In(DeepPurple));
            painter.Bar(new Vector2(0f, 0.55f * squash), 0.42f, 0.13f, 0f, In(side));
            painter.Bar(new Vector2(0f, 0.95f * squash), 0.42f, 0.13f, 0f, In(side));
            painter.Star(head, 0.88f * squash, 0.52f * squash, 9, Outline);
            painter.Star(head, 0.78f * squash, 0.47f * squash, 9, In(Purple));
            painter.Ellipse(head + new Vector2(0f, -0.07f), 0.42f * squash, 0.35f * squash, Outline);
            painter.Ellipse(head + new Vector2(0f, -0.07f), 0.36f * squash, 0.29f * squash, In(Cream));
            if (!ruined)
            {
                painter.Ellipse(head + new Vector2(-0.24f, -0.16f), 0.08f, 0.05f, Pink);
                painter.Ellipse(head + new Vector2(0.24f, -0.16f), 0.08f, 0.05f, Pink);
                foreach (var eye in new[] { -0.15f, 0.15f })
                {
                    painter.Ellipse(head + new Vector2(eye, -0.03f), 0.085f, 0.085f, Laser.Outline);
                    painter.Ellipse(head + new Vector2(eye, -0.03f), 0.065f, 0.065f, Laser.Outer);
                    painter.Ellipse(head + new Vector2(eye, -0.03f), 0.03f, 0.03f, Laser.Core);
                }
            }

            return painter.ToMesh("ChogTower");
        }

        private static Color Grey(Color color)
        {
            var level = (color.r + color.g + color.b) / 3f * 0.55f + 0.12f;
            return new Color(level, level, level * 1.05f, color.a);
        }
    }
}
