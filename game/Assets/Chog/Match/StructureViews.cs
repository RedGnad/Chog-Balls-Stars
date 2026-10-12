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
    /// health; a destroyed one stays as a grey ruin.
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
        }

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

        private void OnFell(EventStructureDestroyed fell)
        {
            _notice = NoticeFor(fell.Kind, fell.Team, _localTeam);
            _noticeColour = fell.Team == (_localTeam < 0 ? 0 : _localTeam) ? Theirs : Ours;
            _noticeUntil = Time.time + NoticeSeconds;
        }

        // One line near the top of the screen, sized with it, on a dark band so it reads on
        // any ground.
        private void OnGUI()
        {
            if (Time.time >= _noticeUntil || _notice.Length == 0)
                return;

            if (_noticeStyle == null)
                _noticeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _noticeStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(Screen.height * 0.065f));
            _noticeStyle.normal.textColor = _noticeColour;

            var band = new Rect(0f, Screen.height * 0.16f, Screen.width, Screen.height * 0.11f);
            var before = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(band, Texture2D.whiteTexture);
            GUI.color = before;
            GUI.Label(band, _notice, _noticeStyle);
        }

        private void OnDestroy() => QuantumEvent.UnsubscribeListener(this);

        private void Update()
        {
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var frame = game != null && game.Frames != null ? game.Frames.Predicted : null;
            var roster = frame != null ? QuantumUnityDB.GetGlobalAsset(frame.RuntimeConfig.GenesisRoster) : null;
            foreach (var drawing in _drawings.Values)
                drawing.Seen = false;

            if (roster != null && roster.ShapeMaterial != null)
            {
                var localTeam = LocalTeam(game, frame);
                _localTeam = localTeam;
                var structures = frame.Filter<Structure, Transform2D, TeamInfo, Health>();
                while (structures.Next(out var entity, out var structure, out var place, out var team, out var health))
                {
                    if (!_drawings.TryGetValue(entity, out var drawing))
                    {
                        // Without a local player (a spectator) team 0 is drawn as "ours".
                        var side = team.Index == (localTeam < 0 ? 0 : localTeam) ? Ours : Theirs;
                        drawing = Build(roster.ShapeMaterial, structure.Kind, side);
                        _drawings[entity] = drawing;
                    }

                    drawing.Seen = true;
                    drawing.Root.transform.position = place.Position.ToUnityVector3();
                    if (health.IsDead != drawing.Ruined)
                    {
                        drawing.Ruined = health.IsDead;
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

            foreach (var drawing in _drawings.Values)
                drawing.Picture.rotation = _camera.rotation;
        }

        // A tower hurt a hero: its two laser eyes draw a beam to that hero, which then fades.
        private void OnShot(EventStructureShot shot)
        {
            var frame = shot.Game.Frames.Predicted;
            var roster = QuantumUnityDB.GetGlobalAsset(frame.RuntimeConfig.GenesisRoster);
            if (roster == null || roster.ShapeMaterial == null || !_drawings.TryGetValue(shot.Structure, out var tower)
                || !frame.Exists(shot.Target) || !frame.TryGet<Transform2D>(shot.Target, out var target))
                return;

            // The eyes are up the picture, which leans back with the camera.
            var up = _camera != null ? _camera.up : Vector3.up;
            var eyes = tower.Root.transform.position + up * TowerEyes - Vector3.up * BeamLift;
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
            var picture = Part("Picture", root.transform, material, Paint(kind, side, false), 1);

            // The bar: a dark back and, over it, what is left, shrinking towards its left end.
            var top = kind == EStructureKind.Core ? 2.25f : 2.95f;
            var bar = new GameObject("Health");
            bar.transform.SetParent(picture.transform, false);
            bar.transform.localPosition = new Vector3(-BarWidth * 0.5f, top, 0f);
            var back = new ShapePainter();
            back.Bar(new Vector2(BarWidth * 0.5f, 0f), BarWidth + 0.08f, BarHeight + 0.08f, 0f, BarBack);
            Part("Back", bar.transform, material, back.ToMesh("StructureBarBack"), 2);
            var left = new ShapePainter();
            left.Bar(new Vector2(BarWidth * 0.5f, 0f), BarWidth, BarHeight, 0f, side);
            var fill = Part("Left", bar.transform, material, left.ToMesh("StructureBarLeft"), 3);

            return new Drawing
            {
                Root = root, Picture = picture.transform, PictureMesh = picture.GetComponent<MeshFilter>(),
                Fill = fill.transform, Bar = bar, Kind = kind, Side = side,
            };
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
