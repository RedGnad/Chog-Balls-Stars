using System.Collections.Generic;
using Chog.Diagnostics;
using Quantum;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// One screen to look at the effects of a fight without a match (ChogFxKit), as ChogSpecialsViewer does
    /// for the specials, on a page served from this computer or the local network only. "?impacts=1": one
    /// Genesis for each object a hat throws, throwing it at a recruit in a loop with the gesture of its class
    /// (thrown, struck, lobbed), drawn by the very code of a match (ChogAttackLook). "?impacts=aura": each Aura
    /// fired by a caster and hitting a target, with its lasting effect. "?impacts=big": a tower and a core
    /// that fall, a hero that falls and one that comes back, and what each rarity adds when a special fires.
    /// "?impacts=1287,39" the Genesis named. Each cell is seen by the match camera at the size a 1280 x 720
    /// screen shows it. "&t=0.15" stops the page that long after the moment of each cell (for pictures);
    /// "&fxlevel=0" shows the effects of before, "1" a phone's.
    /// </summary>
    public sealed class ChogImpactsViewer : MonoBehaviour
    {
        public const string QueryParameter = "impacts";

        // One Genesis for each family of objects, found by the piece its attack throws.
        private static readonly string[] Families =
        {
            "Hat_Cap", "Hat_BucketCap", "Hat_Crown", "Attack_CoinDollar", "Attack_Bolt", "Attack_Snowball",
            "Attack_StarYellow", "Attack_Cloud", "Attack_RainbowBall", "Attack_MoneyBag", "Attack_Peace", "Attack_Leaf"
        };

        private static readonly string[] Deliveries = { "echo", "return", "persistent-zone", "inversion" };

        private const float ReferenceScreenHeight = 720f;
        private const float Spacing = 60f;
        private const float Period = 1.6f;
        private const float BigPeriod = 3.2f;
        // When, in its turn, each cell shows its moment (the impact, the hit, the fall).
        private const float Moment = 0.5f;
        // How long a brute's blow shows before it ends (ChogAttackLook).
        private const float StrikeSeconds = 0.25f;
        private static readonly Vector3 Origin = new Vector3(-6000f, 12000f, 0f);
        private static readonly Color Background = new Color(0.1f, 0.08f, 0.16f);
        private static readonly Color NightFloor = new Color(0x26 / 255f, 0x20 / 255f, 0x52 / 255f);

        private enum Show : byte { Basic, Aura, TowerFalls, CoreFalls, HeroFalls, HeroReturns, Delivery }

        private sealed class Cell
        {
            public Show Show;
            public string Title;
            public Vector3 Place;
            public Vector3 Camera;
            public int Thrower;
            public string Data;
            public float Reach;
            public bool Lob;
            public Transform Attack;
            public Transform Hero;
            public Transform Model;
            public GenesisEffectIcon Aura;
            public Color AuraColour;
            public string Delivery;
            public GenesisEyeLook Look;
            public int Turn = int.MinValue;
            public bool Ended;
        }

        private readonly List<Cell> _cells = new List<Cell>();
        private readonly List<RenderTexture> _pictures = new List<RenderTexture>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private float _started;
        private Material _signs;
        private float _period = Period;
        private float _stopAt = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Open()
        {
            var url = Application.absoluteURL;
            if (!PerformanceSampling.TryGetQueryValue(url, QueryParameter, out _) || !LocalGenesis.TestEntriesAllowed(url, Application.isEditor))
                return;
            var host = new GameObject("Chog Impacts Viewer");
            DontDestroyOnLoad(host);
            host.AddComponent<ChogImpactsViewer>();
        }

        private void Start()
        {
            var url = Application.absoluteURL;
            PerformanceSampling.TryGetQueryValue(url, QueryParameter, out var value);
            var roster = Resources.Load<GenesisRoster>("GenesisRoster");
            var library = ChogHeroModel.Library;
            if (roster == null || library == null)
            {
                Debug.LogWarning("CHOG_IMPACTS no roster or no library of parts");
                return;
            }

            _signs = roster.ShapeMaterial;
            if (value == "aura")
                AuraCells(roster);
            else if (value == "big")
            {
                _period = BigPeriod;
                BigCells(roster);
            }
            else
                BasicCells(roster, library, value);

            var canvasHost = new GameObject("Impacts Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasHost.transform.SetParent(transform, false);
            var canvas = canvasHost.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cover = new GameObject("Background", typeof(RectTransform)).AddComponent<Image>();
            Stretch(cover.rectTransform, canvasHost.transform, Vector2.zero, Vector2.one);
            cover.color = Background;
            cover.raycastTarget = false;

            var columns = _cells.Count <= 4 ? _cells.Count : _cells.Count <= 9 ? 3 : 4;
            columns = Mathf.Max(1, columns);
            var rows = Mathf.CeilToInt(_cells.Count / (float)columns);
            var width = Mathf.Max(64, Screen.width / columns);
            var height = Mathf.Max(64, Screen.height / Mathf.Max(1, rows));
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var column = i % columns;
                var row = i / columns;
                var min = new Vector2(column / (float)columns, 1f - (row + 1f) / rows);
                var max = new Vector2((column + 1f) / columns, 1f - row / (float)rows);
                Floor(cell.Place);
                MatchCamera(cell, width, height, canvasHost.transform, min, max);
                Label(canvasHost.transform, font, cell.Title, new Vector2(min.x + 0.004f, max.y - 0.2f / rows), new Vector2(max.x, max.y - 0.005f));
            }

            // The effects of a fight lie on this page's floor, far above the arena's.
            GenesisSpecial3D.Ground = Origin.y;
            GenesisSpecial3D.ViewerUp = Quaternion.Euler(ChogCameraRig.Pitch, 0f, 0f) * Vector3.up;
            GenesisSpecial3D.ViewerAt = point =>
            {
                Cell nearest = null;
                foreach (var cell in _cells)
                    if (nearest == null || (cell.Place - point).sqrMagnitude < (nearest.Place - point).sqrMagnitude)
                        nearest = cell;
                return nearest != null ? nearest.Camera : point + Vector3.up * 25f;
            };

            _started = Time.time;
            // "&t=0.15": stopped that long after the moment of the third turn (the pools are warm by then); "&t=-0.15" that long
            // before it, an object still in flight.
            if (PerformanceSampling.TryGetQueryValue(url, "t", out var text) && float.TryParse(text,
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var after))
                _stopAt = _started + 2f * _period + Moment + Mathf.Max(-Moment, after);
            Debug.Log("CHOG_IMPACTS " + _cells.Count + " cells, effects " + GenesisEffects.Level);
        }

        // ---- What each cell shows -------------------------------------------------------------------

        private void BasicCells(GenesisRoster roster, ChogPartsLibrary library, string value)
        {
            var numbers = new List<int>();
            if (!string.IsNullOrEmpty(value) && value != "1")
                foreach (var word in value.Split(','))
                    if (int.TryParse(word, out var number) && number >= 0 && !numbers.Contains(number))
                        numbers.Add(number);
            if (numbers.Count == 0)
                foreach (var family in Families)
                {
                    var found = FirstThrowing(roster, library, family);
                    if (found >= 0)
                        numbers.Add(found);
                }

            foreach (var number in numbers)
            {
                var entry = Entry(roster, number);
                var role = entry.HasValue ? entry.Value.Role : (byte)1;
                var parts = ChogHeroModel.TryGetAttackParts(number, out var made) ? made : default;
                var recipe = ChogFxRecipes.ForAttack(parts.Piece);
                var cell = NewCell(Show.Basic, "#" + number + " " + ChogFxRecipes.Key(parts.Piece) + ": " + recipe.Words);
                cell.Thrower = number;
                cell.Data = role == 0 ? "KnightBasicAttackData" : role == 2 ? "SpellcasterBasicFirstAttackData" : "ArcherBasicAttackData";
                cell.Lob = role == 2;
                cell.Reach = role == 0 ? 1.4f : 4.4f;
                cell.Hero = Figure(number, cell.Place + Vector3.left * 2.2f, Vector3.right);
                Figure(ChogHeroModel.Recruit, cell.Place + Vector3.left * 2.2f + Vector3.right * (cell.Reach + 0.5f), Vector3.left);
                var attack = new GameObject("Attack " + number);
                // Thrown to the right, as a match turns an attack where it goes (its view follows the simulation's rotation):
                // a spear or a fireball points the way it flies (23 October 2026: the viewer left them pointing up the screen).
                attack.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                attack.SetActive(false);
                cell.Attack = attack.transform;
            }
        }

        private void AuraCells(GenesisRoster roster)
        {
            if (roster.EffectIds == null)
                return;
            for (var effect = 0; effect < roster.EffectIds.Length; effect++)
            {
                var number = FirstWithEffect(roster, effect);
                var entry = Entry(roster, number);
                var cell = NewCell(Show.Aura, "");
                cell.Aura = roster.EffectIcons != null && effect < roster.EffectIcons.Length ? roster.EffectIcons[effect] : GenesisEffectIcon.Burst;
                cell.AuraColour = roster.EffectColors != null && effect < roster.EffectColors.Length ? roster.EffectColors[effect] : Color.clear;
                cell.Title = roster.EffectIds[effect] + " (#" + number + "): " + ChogFxRecipes.ForAura(cell.Aura).Words;
                cell.Look = entry.HasValue ? GenesisSpecialDrawing.LookOf(roster, entry, cell.AuraColour, false, false, out _) : default;
                cell.Delivery = "direct";
                cell.Thrower = number;
                cell.Hero = Figure(number, cell.Place + Vector3.left * 2.2f, Vector3.right);
                cell.Model = Figure(ChogHeroModel.Recruit, cell.Place + Vector3.right * 2.2f, Vector3.left);
            }
        }

        private void BigCells(GenesisRoster roster)
        {
            var tower = NewCell(Show.TowerFalls, "a tower falls: blasts, light, a shock wave, smoke");
            tower.Model = ChogStructureModel.Build(EStructureKind.Tower, false, false, transform);
            Stand(tower.Model, tower.Place);
            var core = NewCell(Show.CoreFalls, "a core falls: a heart breaks under a BOOM, lightning");
            core.Model = ChogStructureModel.Build(EStructureKind.Core, false, false, transform);
            Stand(core.Model, core.Place);
            var falls = NewCell(Show.HeroFalls, "a hero falls: pieces, a poof, a star, a ring");
            falls.Thrower = 1287;
            falls.Hero = Figure(1287, falls.Place, Vector3.back);
            var returns = NewCell(Show.HeroReturns, "a hero comes back in a column of light");
            returns.Thrower = 1287;
            returns.Hero = Figure(1287, returns.Place, Vector3.back);
            foreach (var delivery in Deliveries)
            {
                var number = FirstWithDelivery(roster, delivery);
                var entry = Entry(roster, number);
                var cell = NewCell(Show.Delivery, "#" + number + " " + ChogFxRecipes.ForDelivery(delivery).Words);
                cell.Delivery = delivery;
                cell.Thrower = number;
                cell.Look = entry.HasValue ? GenesisSpecialDrawing.LookOf(roster, entry, Color.clear, false, false, out _) : default;
                cell.Hero = Figure(number, cell.Place, Vector3.back);
            }
        }

        private Cell NewCell(Show show, string title)
        {
            var cell = new Cell { Show = show, Title = title, Place = Origin + Vector3.forward * (Spacing * _cells.Count) };
            _cells.Add(cell);
            return cell;
        }

        // ---- Playing them ---------------------------------------------------------------------------

        private void Update()
        {
            if (_stopAt > 0f && Time.timeScale > 0f && Time.time >= _stopAt)
            {
                Time.timeScale = 0f;
                return;
            }
            var since = Time.time - _started;
            var turn = Mathf.FloorToInt(since / _period);
            var t = since - turn * _period;
            foreach (var cell in _cells)
            {
                if (cell.Turn != turn)
                {
                    cell.Turn = turn;
                    cell.Ended = false;
                    Begin(cell);
                }
                Go(cell, t);
            }
        }

        private void Begin(Cell cell)
        {
            switch (cell.Show)
            {
                case Show.Basic:
                {
                    // A brute's blow starts later, a quarter of a second before the moment (Go).
                    if (cell.Attack == null || cell.Data == "KnightBasicAttackData")
                        return;
                    cell.Attack.position = Start(cell);
                    cell.Attack.gameObject.SetActive(true);
                    Vector3? landing = cell.Lob ? End(cell) : (Vector3?)null;
                    ChogAttackLook.Dress(cell.Attack, cell.Data, cell.Thrower, landing, ChogTeamRing.Self);
                    break;
                }
                case Show.Aura:
                    ChogFxKit.SpecialFires(cell.Hero.position, Vector3.right, cell.Look, cell.Aura, cell.AuraColour, cell.Delivery);
                    break;
                case Show.HeroReturns:
                    cell.Hero.gameObject.SetActive(false);
                    break;
                case Show.HeroFalls:
                case Show.TowerFalls:
                case Show.CoreFalls:
                    if (cell.Hero != null)
                        cell.Hero.gameObject.SetActive(true);
                    if (cell.Model != null)
                        cell.Model.gameObject.SetActive(true);
                    break;
            }
        }

        private void Go(Cell cell, float t)
        {
            if (cell.Show == Show.Basic && cell.Attack != null)
            {
                // The attack travels to its end in the time before the moment, then ends (its object bursts);
                // a brute's blow, which does not travel, pops out in front for its quarter of a second up to it.
                var strike = cell.Data == "KnightBasicAttackData";
                if (strike && !cell.Attack.gameObject.activeSelf && !cell.Ended && t >= Moment - StrikeSeconds)
                {
                    cell.Attack.position = End(cell) - Vector3.right * 0.6f;
                    cell.Attack.gameObject.SetActive(true);
                    ChogAttackLook.Dress(cell.Attack, cell.Data, cell.Thrower, null, ChogTeamRing.Self);
                }
                if (!cell.Attack.gameObject.activeSelf)
                    return;
                if (!strike)
                    cell.Attack.position = Vector3.Lerp(Start(cell), End(cell), Mathf.Clamp01(t / Moment));
                if (t >= Moment)
                {
                    cell.Attack.gameObject.SetActive(false);
                    cell.Ended = true;
                }
                return;
            }
            if (cell.Ended || t < Moment)
                return;
            cell.Ended = true;
            switch (cell.Show)
            {
                case Show.Aura:
                    // The drawn sign of the Aura pops as in a match (GenesisCombatFeedback), then its effect.
                    if (_signs != null)
                        GenesisShape.Icon(_signs, cell.Aura, cell.AuraColour, cell.Model.position + Vector3.up * 1.2f, 0.6f, 0.35f);
                    ChogFxKit.AuraHits(cell.Aura, cell.AuraColour, cell.Model, cell.Model.position + Vector3.up * 1.2f, 1.1f);
                    break;
                case Show.TowerFalls:
                case Show.CoreFalls:
                {
                    var core = cell.Show == Show.CoreFalls;
                    // The pieces of before, then the blasts (StructureViews does both in a match).
                    StructureViews.Bursts(cell.Place, core ? EStructureKind.Core : EStructureKind.Tower, ChogTeamRing.Enemy);
                    ChogFxKit.StructureFalls(cell.Place, core ? 2f : 3.4f, core, ChogTeamRing.Enemy);
                    if (cell.Model != null)
                        cell.Model.gameObject.SetActive(false);
                    break;
                }
                case Show.HeroFalls:
                {
                    ChogHeroPop.Colours(cell.Thrower, out var skin, out var face);
                    var middle = cell.Place + Vector3.up * 1.1f;
                    ChogImpact.Burst(middle, skin, ChogHeroPop.BurstScale, ChogHeroPop.BurstSpeed, face);
                    ChogFxKit.HeroFalls(middle, skin, ChogTeamRing.Enemy);
                    cell.Hero.gameObject.SetActive(false);
                    break;
                }
                case Show.HeroReturns:
                    cell.Hero.gameObject.SetActive(true);
                    ChogFxKit.HeroReturns(cell.Place, ChogTeamRing.Self);
                    break;
                case Show.Delivery:
                    ChogFxKit.SpecialFires(cell.Place, Vector3.back, cell.Look, GenesisEffectIcon.Burst, Color.clear, cell.Delivery);
                    break;
            }
        }

        // Where the attack leaves from (in front of the thrower, at the floor: ChogAttackLook lifts its object) and lands.
        private static Vector3 Start(Cell cell) => cell.Place + Vector3.left * 2.2f + Vector3.right * 0.6f;
        private static Vector3 End(Cell cell) => cell.Place + Vector3.left * 2.2f + Vector3.right * cell.Reach;

        // ---- The scene ------------------------------------------------------------------------------

        private Transform Figure(int number, Vector3 at, Vector3 forward)
        {
            var hero = ChogHeroModel.Build(number, transform);
            if (hero == null)
                return null;
            hero.position = at;
            hero.rotation = Quaternion.LookRotation(forward, Vector3.up);
            hero.localScale = Vector3.one * (2.1f / ChogHeroModel.ModelHeight);
            ChogBodyAnimator.Attach(hero);
            return hero;
        }

        private static void Stand(Transform model, Vector3 at)
        {
            if (model == null)
                return;
            model.position = at;
            model.rotation = Quaternion.identity;
        }

        private void MatchCamera(Cell cell, int width, int height, Transform canvas, Vector2 min, Vector2 max)
        {
            var view = new GameObject("Impacts Camera").AddComponent<Camera>();
            view.transform.SetParent(transform, false);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Background;
            var picture = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            _pictures.Add(picture);
            view.targetTexture = picture;
            view.aspect = width / (float)height;
            var offset = ChogCameraRig.FollowOffset;
            var lead = offset.y / Mathf.Tan(ChogCameraRig.Pitch * Mathf.Deg2Rad) + offset.z;
            var centre = cell.Place + (cell.Show == Show.Basic || cell.Show == Show.Aura ? Vector3.zero : Vector3.up * 0.8f);
            view.transform.position = centre + offset - Vector3.forward * lead;
            view.transform.rotation = Quaternion.Euler(ChogCameraRig.Pitch, 0f, 0f);
            var half = Mathf.Tan(ChogCameraRig.FieldOfView * 0.5f * Mathf.Deg2Rad) * height / ReferenceScreenHeight;
            view.fieldOfView = 2f * Mathf.Atan(half) * Mathf.Rad2Deg;
            view.nearClipPlane = 1f;
            view.farClipPlane = 60f;
            cell.Camera = view.transform.position;
            var shown = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            Stretch(shown.rectTransform, canvas, min, max);
            shown.texture = picture;
            shown.raycastTarget = false;
        }

        private void Floor(Vector3 place)
        {
            var mesh = new Mesh { name = "Impacts Floor" };
            const float half = 24f;
            mesh.vertices = new[]
            {
                new Vector3(-half, 0f, -half), new Vector3(-half, 0f, half), new Vector3(half, 0f, half), new Vector3(half, 0f, -half)
            };
            var flat = new Color(NightFloor.r, NightFloor.g, NightFloor.b, 0.6f);
            mesh.colors = new[] { flat, flat, flat, flat };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            _meshes.Add(mesh);
            var floor = new GameObject("Floor", typeof(MeshFilter), typeof(MeshRenderer));
            floor.transform.SetParent(transform, false);
            floor.transform.position = place + Vector3.up * 0.12f;
            floor.GetComponent<MeshFilter>().sharedMesh = mesh;
            floor.GetComponent<MeshRenderer>().sharedMaterial = ChogHeroModel.Material;
        }

        // ---- The roster -----------------------------------------------------------------------------

        private static GenesisRosterEntry? Entry(GenesisRoster roster, int number)
        {
            if (roster.Entries != null)
                foreach (var entry in roster.Entries)
                    if (entry.MetadataId == number)
                        return entry;
            return null;
        }

        private static int FirstThrowing(GenesisRoster roster, ChogPartsLibrary library, string piece)
        {
            if (piece == "Attack_Leaf")
                return ChogHeroModel.Recruit;
            foreach (var entry in roster.Entries)
                if (library.TryGetHero(entry.MetadataId, out var hero) && hero.Attack != ChogPartsLibrary.NoAttack
                    && library.Pieces[hero.Attack].Name == piece)
                    return entry.MetadataId;
            return -1;
        }

        private static int FirstWithEffect(GenesisRoster roster, int effect)
        {
            foreach (var entry in roster.Entries)
                if (entry.Effect == effect && ChogHeroModel.Has(entry.MetadataId))
                    return entry.MetadataId;
            return ChogHeroModel.Recruit;
        }

        private static int FirstWithDelivery(GenesisRoster roster, string delivery)
        {
            var index = roster.ExpressionIds != null ? System.Array.IndexOf(roster.ExpressionIds, delivery) : -1;
            foreach (var entry in roster.Entries)
                if (entry.Expression == index && ChogHeroModel.Has(entry.MetadataId))
                    return entry.MetadataId;
            return ChogHeroModel.Recruit;
        }

        // ---- The page -------------------------------------------------------------------------------

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

        private static void Label(Transform canvas, Font font, string text, Vector2 min, Vector2 max)
        {
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            Stretch(label.rectTransform, canvas, min, max);
            label.font = font;
            label.text = text;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 16;
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
}
