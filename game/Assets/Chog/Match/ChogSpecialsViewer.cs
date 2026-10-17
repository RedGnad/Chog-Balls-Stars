using System.Collections.Generic;
using Chog.Diagnostics;
using Quantum;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// One screen to look at the specials without a match (matches need Photon): for each Genesis named in
    /// the page option "?specials=1287,16,112" (numbers shown in their names; "?specials=1" for one Genesis
    /// of each of the five shapes), its 3D hero on a floor in the arena's colours, firing its special in a
    /// loop, drawn by the code a match uses (GenesisSpecialDrawing) with the numbers of its own attack data
    /// (how fast, how far, how long). Each Genesis has a row: on the left as the match camera sees it, at
    /// the size a 1280 x 720 screen shows it; on the right from the side. Options: "&enemy=1" draws them as
    /// an enemy's (outlined in red), "&special3d=0" the flat look of before, "&floor=day" the daylight
    /// floor, "&at=4" stops everything four seconds in, each special at a moment that shows it well (for
    /// pictures; a puddle needs a turn before it), "&t=0.2" stops each special that long into its turn
    /// instead (a film, one picture a moment). Only on a page served from this computer or the local
    /// network (LocalGenesis).
    /// </summary>
    public sealed class ChogSpecialsViewer : MonoBehaviour
    {
        public const string QueryParameter = "specials";

        // Shown by "?specials=1": one Genesis of each shape, by the number in its name: a purple laser
        // (beam), the owner's Anime Glass (flash), Round Crying (puddle), Side Eye (wave), Heart (nova).
        private static readonly int[] Default = { 16, 1287, 1465, 112, 1514 };

        // The match camera's field of view is that of a screen this many pixels high (ChogCameraRig).
        private const float ReferenceScreenHeight = 720f;
        private const float HeroHeight = 2.1f;
        private const float RowSpacing = 80f;
        private const float SideWidth = 17f;
        private const float FloorHeight = 0.12f;
        // Far from any scene and from the stage of the menu's hero (ChogHeroStage, at 9000 up), so the
        // cameras of the viewer see nothing else.
        private static readonly Vector3 Origin = new Vector3(4000f, 12000f, 0f);
        private static readonly Color Background = new Color(0.1f, 0.08f, 0.16f);
        private static readonly Color NightFloor = new Color(0x26 / 255f, 0x20 / 255f, 0x52 / 255f);
        private static readonly Color DayFloor = new Color(0x79 / 255f, 0xA9 / 255f, 0x42 / 255f);

        private sealed class Row
        {
            public int Number;
            public string Shape;
            public Vector3 Hero;
            public Vector3 Forward;
            public Vector3 Camera;
            public GenesisSpecialDrawing.Cast Cast;
            public GenesisSpecialDrawing.Cast Zone;
            public SpecialLoop.Plan Plan;
            public float Started;
            // The attacks drawn now: the one of this turn of the loop, and a zone that outlives it.
            public GenesisSpecialDrawing Attack;
            public GenesisSpecialDrawing Lingering;
            public float LingeringEnds;
            public int Turn = int.MinValue;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly List<RenderTexture> _pictures = new List<RenderTexture>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private float _freezeAt = -1f;
        private float _stopAt = float.MaxValue;
        private Camera _mainCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Open()
        {
            var url = Application.absoluteURL;
            if (!PerformanceSampling.TryGetQueryValue(url, QueryParameter, out _) || !LocalGenesis.TestEntriesAllowed(url, Application.isEditor))
                return;

            var host = new GameObject("Chog Specials Viewer");
            DontDestroyOnLoad(host);
            host.AddComponent<ChogSpecialsViewer>();
        }

        /// <summary>The Genesis a page asks for: numbers separated by commas, or the default set for "1".</summary>
        public static int[] Asked(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "1")
                return Default;
            var asked = new List<int>();
            foreach (var word in value.Split(','))
                if (int.TryParse(word, out var number) && number > 0 && !asked.Contains(number))
                    asked.Add(number);
            return asked.Count > 0 ? asked.ToArray() : Default;
        }

        private void Start()
        {
            var url = Application.absoluteURL;
            PerformanceSampling.TryGetQueryValue(url, QueryParameter, out var value);
            var enemy = PerformanceSampling.HasQueryFlag(url, "enemy");
            var day = PerformanceSampling.TryGetQueryValue(url, "floor", out var floorName) && floorName == "day";
            if (TryGetSeconds(url, "at", out var seconds))
                _freezeAt = Mathf.Max(0.05f, seconds);
            // "&t=0.2": every special stopped that long into its turn instead of at its best moment.
            var moment = -1f;
            if (TryGetSeconds(url, "t", out var into))
            {
                moment = Mathf.Max(0f, into);
                if (_freezeAt < 0f)
                    _freezeAt = 4f;
            }

            var roster = Resources.Load<GenesisRoster>("GenesisRoster");
            if (roster == null)
            {
                Debug.LogWarning("CHOG_SPECIALS no roster");
                return;
            }

            var numbers = Asked(value);
            var canvasHost = new GameObject("Specials Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasHost.transform.SetParent(transform, false);
            var canvas = canvasHost.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cover = new GameObject("Background", typeof(RectTransform)).AddComponent<Image>();
            Stretch(cover.rectTransform, canvasHost.transform, Vector2.zero, Vector2.one);
            cover.color = Background;
            cover.raycastTarget = false;

            var count = numbers.Length;
            var cellWidth = Mathf.Max(64, Screen.width / 2);
            var cellHeight = Mathf.Max(64, Screen.height / count);
            for (var index = 0; index < count; index++)
            {
                var row = MakeRow(roster, numbers[index], index, enemy);
                var bottom = 1f - (index + 1f) / count;
                var top = 1f - index / (float)count;
                var title = "CHOG #" + numbers[index];
                if (row == null)
                {
                    Label(canvasHost.transform, font, title + ": not in the roster", 0f, bottom, 1f, top);
                    continue;
                }

                _rows.Add(row);
                var place = Origin + Vector3.forward * (RowSpacing * index);
                Floor(place, day ? DayFloor : NightFloor);
                var hero = ChogHeroModel.Build(numbers[index], transform);
                if (hero != null)
                {
                    hero.position = row.Hero;
                    hero.rotation = Quaternion.LookRotation(row.Forward, Vector3.up);
                    hero.localScale = Vector3.one * (HeroHeight / ChogHeroModel.ModelHeight);
                    ChogBodyAnimator.Attach(hero);
                }

                var eyeValue = roster.EyeValues != null && roster.Entries != null ? EyeName(roster, numbers[index]) : "";
                MatchCamera(row, cellWidth, cellHeight, canvasHost.transform, new Vector2(0f, bottom), new Vector2(0.5f, top));
                SideCamera(row, cellWidth, cellHeight, canvasHost.transform, new Vector2(0.5f, bottom), new Vector2(1f, top));
                Label(canvasHost.transform, font, title + "  " + row.Shape + "  (" + eyeValue + ")" + (hero == null ? "  no 3D hero" : ""),
                    0.005f, top - 0.16f / count, 0.5f, top - 0.01f / count);
                Label(canvasHost.transform, font, "match camera, as a 1280 x 720 screen shows it", 0.005f, bottom + 0.01f / count,
                    0.5f, bottom + 0.13f / count);
                Label(canvasHost.transform, font, "side", 0.505f, top - 0.16f / count, 0.6f, top - 0.01f / count);
            }

            // The flat look turns its pictures as the main camera is turned: the menu's camera steps aside for
            // a match camera of this page (all of them are turned alike).
            foreach (var other in Camera.allCameras)
                if (other.CompareTag("MainCamera") && !other.transform.IsChildOf(transform))
                    other.tag = "Untagged";
            if (_mainCamera != null)
                _mainCamera.tag = "MainCamera";

            // The effects of a fight lie on this page's floor, far above the arena's.
            GenesisSpecial3D.Ground = Origin.y;
            // What faces the viewer (eyeballs, glints) turns toward the match camera of its own row.
            GenesisSpecial3D.ViewerUp = Quaternion.Euler(ChogCameraRig.Pitch, 0f, 0f) * Vector3.up;
            GenesisSpecial3D.ViewerAt = point =>
            {
                Row nearest = null;
                foreach (var row in _rows)
                    if (nearest == null || Mathf.Abs(row.Hero.z - point.z) < Mathf.Abs(nearest.Hero.z - point.z))
                        nearest = row;
                return nearest != null ? nearest.Camera : point + Vector3.up * 25f;
            };

            var start = Time.time;
            foreach (var row in _rows)
            {
                // Started so that, at the moment "at" stops the page, each special shows its best moment.
                row.Started = _freezeAt > 0f ? start + _freezeAt - (moment >= 0f ? moment : row.Plan.Showcase) : start;
            }
            _stopAt = start + _freezeAt;

            Debug.Log("CHOG_SPECIALS " + count + " asked, " + _rows.Count + " shown, " +
                      (ChogEffectMaterials.Available ? "in 3D" : "flat"));
        }

        private Row MakeRow(GenesisRoster roster, int number, int index, bool enemy)
        {
            if (roster.Entries == null)
                return null;
            var found = -1;
            for (var i = 0; i < roster.Entries.Length; i++)
                if (roster.Entries[i].MetadataId == number)
                {
                    found = i;
                    break;
                }
            if (found < 0)
                return null;

            var entry = roster.Entries[found];
            var shape = roster.ShapeIds != null && entry.Shape < roster.ShapeIds.Length ? roster.ShapeIds[entry.Shape] : "beam";
            var effect = roster.EffectIds != null && entry.Effect < roster.EffectIds.Length ? roster.EffectIds[entry.Effect] : "raw";
            var plan = SpecialLoop.For(shape);
            var name = "Genesis/Genesis" + Title(shape) + Title(effect);
            var data = Resources.Load<AttackData>(name + (shape == "puddle" ? "Lob" : "") + "AttackData");
            var zone = shape == "puddle" ? Resources.Load<AttackData>(name + "ZoneAttackData") : null;
            if (data != null)
                plan = plan.With(data.TTL.AsFloat, data is GenesisSweepAttackData sweep ? sweep.Velocity.AsFloat : plan.Speed,
                    data is GenesisSweepAttackData ahead ? ahead.ForwardOffset.AsFloat : plan.Ahead,
                    data is BallisticAttackData lob ? lob.Velocity.AsFloat : plan.Speed,
                    zone != null ? zone.TTL.AsFloat : plan.ZoneSeconds);

            var place = Origin + Vector3.forward * (RowSpacing * index);
            var row = new Row
            {
                Number = number,
                Shape = shape,
                Hero = place + Vector3.right * plan.HeroAt,
                Forward = Vector3.right,
                Plan = plan
            };

            var kind = data != null ? data.VisualKind : SpecialLoop.KindOf(shape);
            var isExpression = GenesisSpecialDrawing.IsExpression(kind);
            var tint = data != null ? data.VisualTint : Color.clear;
            var look = GenesisSpecialDrawing.LookOf(roster, entry, tint, isExpression, enemy, out var skin);
            row.Cast = new GenesisSpecialDrawing.Cast
            {
                Kind = kind,
                FlatMaterial = data != null && data.ShapeMaterial != null ? data.ShapeMaterial : roster.ShapeMaterial,
                Look = look,
                Skin = skin,
                Tint = tint,
                Radius = data != null ? data.VisualRadius : plan.Radius,
                Forward = row.Forward,
                Ahead = plan.Ahead,
                Target = row.Hero + row.Forward * plan.Reach,
                Side = enemy ? ChogTeamRing.Enemy : ChogTeamRing.Self,
                Fires = true,
                Caster = row.Hero,
                Aura = roster.EffectIcons != null && entry.Effect < roster.EffectIcons.Length ? roster.EffectIcons[entry.Effect] : GenesisEffectIcon.Burst,
                Delivery = roster.ExpressionIds != null && entry.Expression < roster.ExpressionIds.Length ? roster.ExpressionIds[entry.Expression] : ""
            };
            if (shape == "puddle")
            {
                var zoneTint = zone != null ? zone.VisualTint : Color.clear;
                row.Zone = row.Cast;
                row.Zone.Fires = false;
                row.Zone.Kind = GenesisVisualKind.GroundDisc;
                row.Zone.Look = GenesisSpecialDrawing.LookOf(roster, entry, zoneTint, false, enemy, out _);
                row.Zone.Tint = zoneTint;
                row.Zone.Radius = zone != null ? zone.VisualRadius : plan.Radius;
                row.Zone.FlatMaterial = zone != null && zone.ShapeMaterial != null ? zone.ShapeMaterial : row.Cast.FlatMaterial;
            }
            return row;
        }

        private void Update()
        {
            foreach (var row in _rows)
                Play(row);

            if (_freezeAt > 0f && Time.timeScale > 0f && Time.time >= _stopAt)
                Time.timeScale = 0f;
        }

        private static bool TryGetSeconds(string url, string name, out float seconds)
        {
            seconds = 0f;
            return PerformanceSampling.TryGetQueryValue(url, name, out var text) && float.TryParse(text,
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
        }

        // One turn of the loop after another: the attack begins, travels as its data says, and ends.
        private void Play(Row row)
        {
            // Before its start a row plays the turns that lead to it (a puddle left by the throw before).
            var since = Time.time - row.Started;
            var turn = Mathf.FloorToInt(since / row.Plan.Period);
            var t = since - turn * row.Plan.Period;

            if (turn != row.Turn)
            {
                row.Turn = turn;
                End(row);
                var cast = row.Cast;
                cast.Here = row.Hero + row.Forward * row.Plan.AheadAt(0f);
                cast.Start = cast.Here;
                row.Attack = GenesisSpecialDrawing.Begin(cast);
            }

            if (row.Attack != null)
            {
                if (t >= row.Plan.Seconds)
                {
                    row.Attack.End(row.Hero + row.Forward * row.Plan.AheadAt(row.Plan.Seconds));
                    row.Attack = null;
                    if (row.Shape == "puddle")
                    {
                        // The tear landed: its puddle stays, into the next throw.
                        if (row.Lingering != null)
                            row.Lingering.End(row.Cast.Target);
                        var zone = row.Zone;
                        zone.Here = row.Cast.Target;
                        zone.Start = zone.Here;
                        row.Lingering = GenesisSpecialDrawing.Begin(zone);
                        row.LingeringEnds = Time.time + row.Plan.ZoneSeconds;
                    }
                }
                else
                {
                    row.Attack.Follow(row.Hero + row.Forward * row.Plan.AheadAt(t));
                }
            }

            if (row.Lingering != null)
            {
                if (Time.time >= row.LingeringEnds)
                {
                    row.Lingering.End(row.Cast.Target);
                    row.Lingering = null;
                }
                else
                {
                    row.Lingering.Follow(row.Cast.Target);
                }
            }
        }

        private static void End(Row row)
        {
            if (row.Attack != null)
            {
                row.Attack.End(row.Hero + row.Forward * row.Plan.AheadAt(row.Plan.Seconds));
                row.Attack = null;
            }
        }

        private void MatchCamera(Row row, int width, int height, Transform canvas, Vector2 min, Vector2 max)
        {
            var view = NewCamera("Match Camera", width, height, canvas, min, max);
            var centre = row.Hero + row.Forward * row.Plan.ActionAt;
            // The match camera stands where it stands over a player (ChogCameraRig), centred on the action.
            var offset = ChogCameraRig.FollowOffset;
            var lead = offset.y / Mathf.Tan(ChogCameraRig.Pitch * Mathf.Deg2Rad) + offset.z;
            view.transform.position = centre + offset - Vector3.forward * lead;
            view.transform.rotation = Quaternion.Euler(ChogCameraRig.Pitch, 0f, 0f);
            // The same pixels a world unit as a whole match screen ReferenceScreenHeight high.
            var half = Mathf.Tan(ChogCameraRig.FieldOfView * 0.5f * Mathf.Deg2Rad) * height / ReferenceScreenHeight;
            view.fieldOfView = 2f * Mathf.Atan(half) * Mathf.Rad2Deg;
            view.nearClipPlane = 1f;
            view.farClipPlane = 60f;
            row.Camera = view.transform.position;
            if (_mainCamera == null)
                _mainCamera = view;
        }

        private void SideCamera(Row row, int width, int height, Transform canvas, Vector2 min, Vector2 max)
        {
            var view = NewCamera("Side Camera", width, height, canvas, min, max);
            var centre = row.Hero + row.Forward * row.Plan.ActionAt + Vector3.up * 1.1f;
            view.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            view.transform.position = centre - view.transform.forward * 30f;
            view.orthographic = true;
            view.orthographicSize = SideWidth * 0.5f * height / Mathf.Max(1f, width);
            view.nearClipPlane = 1f;
            view.farClipPlane = 60f;
        }

        // A camera that draws into its own picture, shown over that part of the screen: on WebGL a camera
        // that draws into a part of a picture clears the whole of it (ChogHeroViewer).
        private Camera NewCamera(string name, int width, int height, Transform canvas, Vector2 min, Vector2 max)
        {
            var view = new GameObject(name).AddComponent<Camera>();
            view.transform.SetParent(transform, false);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Background;
            var picture = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            _pictures.Add(picture);
            view.targetTexture = picture;
            view.aspect = width / (float)height;
            var shown = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            Stretch(shown.rectTransform, canvas, min, max);
            shown.texture = picture;
            shown.raycastTarget = false;
            return view;
        }

        // A floor in the arena's colour, flat, under a row.
        private void Floor(Vector3 place, Color colour)
        {
            var mesh = new Mesh { name = "Specials Floor" };
            const float half = 30f;
            mesh.vertices = new[]
            {
                new Vector3(-half, 0f, -half), new Vector3(-half, 0f, half), new Vector3(half, 0f, half), new Vector3(half, 0f, -half)
            };
            // Alpha 0.6: the hero material draws the colour as it is (ChogHero.shader).
            var flat = new Color(colour.r, colour.g, colour.b, 0.6f);
            mesh.colors = new[] { flat, flat, flat, flat };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            _meshes.Add(mesh);
            var floor = new GameObject("Floor", typeof(MeshFilter), typeof(MeshRenderer));
            floor.transform.SetParent(transform, false);
            floor.transform.position = place + Vector3.up * FloorHeight;
            floor.GetComponent<MeshFilter>().sharedMesh = mesh;
            floor.GetComponent<MeshRenderer>().sharedMaterial = ChogHeroModel.Material;
        }

        private static string EyeName(GenesisRoster roster, int number)
        {
            foreach (var entry in roster.Entries)
                if (entry.MetadataId == number)
                    return entry.Eye < roster.EyeValues.Length ? roster.EyeValues[entry.Eye] : "";
            return "";
        }

        private static string Title(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "";
            var words = id.Split('-');
            var made = "";
            for (var i = 0; i < words.Length; i++)
                made += (i > 0 ? "-" : "") + char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return made;
        }

        private void OnDestroy()
        {
            GenesisSpecial3D.ViewerAt = null;
            GenesisSpecial3D.ViewerUp = null;
            GenesisSpecial3D.Ground = 0f;
            foreach (var picture in _pictures)
                if (picture != null)
                    picture.Release();
            foreach (var mesh in _meshes)
                Destroy(mesh);
        }

        private static void Label(Transform canvas, Font font, string text, float left, float bottom, float right, float top)
        {
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            Stretch(label.rectTransform, canvas, new Vector2(left, bottom), new Vector2(right, top));
            label.font = font;
            label.text = text;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 22;
            label.raycastTarget = false;
        }

        private static void Stretch(RectTransform area, Transform parent, Vector2 min, Vector2 max)
        {
            area.SetParent(parent, false);
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// How the test page of the specials (ChogSpecialsViewer) plays each shape in a loop, without the
    /// engine (tested): how long one attack lasts, how fast and from how far ahead of the hero it travels,
    /// how long a puddle stays, how long a turn of the loop is, and the moment of a turn that shows it best.
    /// The defaults are the numbers of the "raw" attacks of each shape (Resources/Genesis); the page reads
    /// the Genesis's own attack data over them.
    /// </summary>
    public static class SpecialLoop
    {
        public readonly struct Plan
        {
            /// <summary>How long one attack lives (its TTL).</summary>
            public readonly float Seconds;
            /// <summary>How fast it travels from the hero, units a second (0: it stays).</summary>
            public readonly float Speed;
            /// <summary>Where it appears, ahead of the hero.</summary>
            public readonly float Ahead;
            /// <summary>A lob: how far ahead it lands.</summary>
            public readonly float Reach;
            /// <summary>A lob: how long the puddle it leaves stays.</summary>
            public readonly float ZoneSeconds;
            /// <summary>Radius of the area, when the data does not say.</summary>
            public readonly float Radius;
            /// <summary>One turn of the loop.</summary>
            public readonly float Period;
            /// <summary>The moment of a turn that shows the special well.</summary>
            public readonly float Showcase;
            /// <summary>Where the hero stands across its row, and the middle of what the special covers, ahead of it.</summary>
            public readonly float HeroAt;
            public readonly float ActionAt;
            public readonly bool Lobbed;

            public Plan(float seconds, float speed, float ahead, float reach, float zoneSeconds, float radius, float period,
                        float showcase, float heroAt, float actionAt, bool lobbed)
            {
                Seconds = seconds;
                Speed = speed;
                Ahead = ahead;
                Reach = reach;
                ZoneSeconds = zoneSeconds;
                Radius = radius;
                Period = period;
                Showcase = showcase;
                HeroAt = heroAt;
                ActionAt = actionAt;
                Lobbed = lobbed;
            }

            /// <summary>
            /// The same plan with the numbers of an attack's data. A lob flies to its reach at its speed and
            /// lives that long; the loop is never shorter than the attack and a moment of rest.
            /// </summary>
            public Plan With(float seconds, float sweepSpeed, float ahead, float lobSpeed, float zoneSeconds)
            {
                if (Lobbed)
                {
                    var flight = lobSpeed > 0.01f ? Reach / lobSpeed : Seconds;
                    return new Plan(flight, Reach / Mathf.Max(0.01f, flight), 0f, Reach, zoneSeconds, Radius,
                        Mathf.Max(Period, flight + 0.6f), Mathf.Min(Showcase, flight * 0.5f), HeroAt, ActionAt, true);
                }
                seconds = seconds > 0.01f ? seconds : Seconds;
                return new Plan(seconds, sweepSpeed, ahead, Reach, ZoneSeconds, Radius, Mathf.Max(Period, seconds + 0.6f),
                    Mathf.Min(Showcase, seconds * 0.8f), HeroAt, ActionAt, false);
            }

            /// <summary>How far ahead of the hero the attack is, "t" seconds after it appeared.</summary>
            public float AheadAt(float t)
            {
                var travelled = Ahead + Speed * Mathf.Clamp(t, 0f, Seconds);
                return Lobbed ? Mathf.Min(Reach, travelled) : travelled;
            }
        }

        /// <summary>The loop of a shape ("beam", "flash", "puddle", "wave", "nova").</summary>
        public static Plan For(string shape)
        {
            switch (shape)
            {
                case "beam":
                    // 0.34 s at 33 units a second from half a unit ahead: about 11 units.
                    return new Plan(0.34f, 33f, 0.5f, 0f, 0f, 0.35f, 1.6f, 0.22f, -4f, 4f, false);
                case "flash":
                    return new Plan(0.35f, 0f, 2f, 0f, 0f, 3.7f, 1.4f, 0.14f, -2f, 2f, false);
                case "puddle":
                    // A tear lobbed 6 units at 6 a second, then a puddle for 3 s; the next tear flies over it.
                    return new Plan(1f, 6f, 0f, 6f, 3f, 3f, 3.4f, 0.5f, -3f, 3f, true);
                case "wave":
                    return new Plan(0.75f, 9f, 1f, 0f, 0f, 1.5f, 2f, 0.3f, -3.5f, 3.5f, false);
                default:
                    return new Plan(0.4f, 0f, 0f, 0f, 0f, 3f, 1.5f, 0.32f, 0f, 0f, false);
            }
        }

        /// <summary>What a shape is drawn as, when its data cannot be read.</summary>
        public static GenesisVisualKind KindOf(string shape)
        {
            switch (shape)
            {
                case "beam": return GenesisVisualKind.Beam;
                case "flash": return GenesisVisualKind.Fan;
                case "puddle": return GenesisVisualKind.Drop;
                case "wave": return GenesisVisualKind.Stare;
                default: return GenesisVisualKind.EyeBurst;
            }
        }
    }
}
