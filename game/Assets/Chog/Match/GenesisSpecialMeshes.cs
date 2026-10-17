using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The 3D shapes the specials are made of (GenesisSpecial3D), low poly for phones, in the colours of
    /// the caster's eyes (GenesisEyeLook): the laser's tube with its lighter core and ink outline, the
    /// glow at the eyes and the spark at the head of a beam; the fan of light of a visor as a volume with
    /// a bright front edge, and the glint on the glasses; the tear (a drop with its tip and a highlight)
    /// and the low blob of the puddle with its ripples; the eyeball (a white ball wearing the hero's own
    /// eye, as GenesisEyes draws it, wrapped on its front); the front of a stare and the shock ring of a
    /// burst; and what lies on the floor (a glow along a laser, the outline of an area, a soft disc).
    /// Each "...Draft" builds one out of the engine (tested); the other methods keep one mesh for each
    /// look and size, as attacks come often and a match has six heroes.
    /// </summary>
    public static class GenesisSpecialMeshes
    {
        // A laser: its colour and its outline as shares of its radius (GenesisBeam drew 0.24 and 0.34 wide).
        public const float LaserOutline = 1.42f;
        // The fan of light: how high it stands at the caster and at its front edge, over the floor.
        public const float FanApexHeight = 0.45f;
        public const float FanRimHeight = 1.0f;
        public const float FanFloor = 0.04f;
        // The puddle: how high its middle bulges.
        public const float PuddleHeight = 0.16f;
        // The outline shell of an eyeball and its picture, as shares of its radius.
        public const float EyeballShell = 1.1f;
        public const float EyeballPicture = 0.62f;

        private static readonly Color White = new Color(1f, 1f, 1f, 1f);
        private static readonly Color Ink = new Color(0.08f, 0.06f, 0.12f, 1f);

        private static readonly Dictionary<MeshKey, Mesh> s_meshes = new Dictionary<MeshKey, Mesh>();

        // ---- The laser ------------------------------------------------------------------------------

        /// <summary>
        /// One tube of a laser, radius 1, from y = 0 to y = 1 (its object is stretched along the beam):
        /// the eye colour shaded as a volume, the lighter core down the side that faces the viewer, the
        /// ink outline around it.
        /// </summary>
        public static EffectMeshDraft LaserDraft(GenesisEyeLook look)
        {
            var draft = new EffectMeshDraft();
            var body = EffectPaint.Shaded(look.Outer).WithLight(0.45f).WithCore(Lighter(look.Core), 1f);
            draft.Lathe(new[] { new Vector2(1f, 0f), new Vector2(1f, 1f) }, 10, Vector3.zero, _ => body);
            var ink = EffectPaint.Outline(OutlineOf(look));
            draft.Lathe(new[] { new Vector2(LaserOutline, 0f), new Vector2(LaserOutline, 1f) }, 10, Vector3.zero, _ => ink, true);
            return draft;
        }

        public static Mesh Laser(GenesisEyeLook look) => Kept(new MeshKey(1, look), () => LaserDraft(look), "GenesisLaser");

        /// <summary>A soft ball of light of radius 1: the core colour in its middle, fading at its edge.</summary>
        public static EffectMeshDraft GlowBallDraft(Color colour, Color core)
        {
            var draft = new EffectMeshDraft();
            draft.Sphere(Vector3.zero, Vector3.one, 10, 7, EffectPaint.Glow(colour, 0.62f, -0.6f).WithCore(Lighter(core), 1f));
            return draft;
        }

        public static Mesh GlowBall(Color colour, Color core) =>
            Kept(new MeshKey(2, new GenesisEyeLook { Outer = colour, Core = core }), () => GlowBallDraft(colour, core), "GenesisGlowBall");

        /// <summary>
        /// A spark of radius 1: four long points across and two short ones, flat in its colour, outlined in
        /// ink: the head of a laser, the glint on a visor.
        /// </summary>
        public static EffectMeshDraft SparkDraft(Color colour, Color outline)
        {
            var draft = new EffectMeshDraft();
            var paint = EffectPaint.Flat(colour);
            var ink = EffectPaint.Outline(outline);
            var directions = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (var d = 0; d < directions.Length; d++)
            {
                var length = d < 4 ? 1f : 0.45f;
                Spike(draft, directions[d], length, 0.17f, paint, false);
                Spike(draft, directions[d], length * 1.14f, 0.17f * 1.7f, ink, true);
            }
            return draft;
        }

        public static Mesh Spark(Color colour, Color outline) =>
            Kept(new MeshKey(3, new GenesisEyeLook { Outer = colour, Outline = outline }), () => SparkDraft(colour, outline), "GenesisSpark");

        // ---- The fan of light -----------------------------------------------------------------------

        /// <summary>
        /// The light of a visor or a pair of glasses: a wedge of "radius" opening "halfAngle" degrees on
        /// each side of +Z from the caster, low at the caster and as high as a hero at its front edge,
        /// see-through, its colours across as the visor's (one side, the bright middle, the other side),
        /// brightest at the caster, with a bright tube along its front edge.
        /// </summary>
        public static EffectMeshDraft FanDraft(GenesisEyeLook look, float radius, float halfAngle)
        {
            const int arc = 14;
            const int rings = 4;
            var draft = new EffectMeshDraft();
            var slope = (FanRimHeight - FanApexHeight) / Mathf.Max(0.01f, radius);

            // The top, from the caster to the front edge.
            var apex = draft.Add(new Vector3(0f, FanApexHeight, 0f), Vector3.up, EffectPaint.Glow(Lighter(look.Core), 0.7f, 0.25f));
            var rows = new int[rings + 1];
            for (var j = 1; j <= rings; j++)
            {
                var v = j / (float)rings;
                rows[j] = draft.VertexCount;
                for (var i = 0; i <= arc; i++)
                {
                    var u = i / (float)arc;
                    var radial = Radial(Mathf.Lerp(-halfAngle, halfAngle, u));
                    var colour = Color.Lerp(Across(look, u), look.Core, (1f - v) * (1f - v) * 0.6f);
                    var alpha = 0.62f - 0.32f * v + 0.25f * Mathf.Pow(v, 6f);
                    draft.Add(radial * (radius * v) + Vector3.up * (FanApexHeight + slope * radius * v),
                        Vector3.up - radial * slope, EffectPaint.Glow(colour, alpha, 0.25f));
                }
            }
            for (var i = 0; i < arc; i++)
                draft.Face(apex, rows[1] + i, rows[1] + i + 1);
            for (var j = 1; j < rings; j++)
                for (var i = 0; i < arc; i++)
                {
                    draft.Face(rows[j] + i, rows[j + 1] + i, rows[j + 1] + i + 1);
                    draft.Face(rows[j] + i, rows[j + 1] + i + 1, rows[j] + i + 1);
                }

            // The front edge, from the floor to the top: the light where it reaches.
            var heights = new[] { FanFloor, (FanFloor + FanRimHeight) * 0.5f, FanRimHeight };
            var alphas = new[] { 0.8f, 0.6f, 0.55f };
            var wall = new int[heights.Length];
            for (var k = 0; k < heights.Length; k++)
            {
                wall[k] = draft.VertexCount;
                for (var i = 0; i <= arc; i++)
                {
                    var radial = Radial(Mathf.Lerp(-halfAngle, halfAngle, i / (float)arc));
                    var colour = Color.Lerp(Across(look, i / (float)arc), White, k == 0 ? 0.35f : 0.1f);
                    draft.Add(radial * radius + Vector3.up * heights[k], radial, EffectPaint.Glow(colour, alphas[k], 0.35f));
                }
            }
            for (var k = 0; k + 1 < heights.Length; k++)
                for (var i = 0; i < arc; i++)
                {
                    draft.Face(wall[k] + i, wall[k + 1] + i, wall[k + 1] + i + 1);
                    draft.Face(wall[k] + i, wall[k + 1] + i + 1, wall[k] + i + 1);
                }

            // The two sides.
            for (var s = -1; s <= 1; s += 2)
            {
                var angle = halfAngle * s;
                var radial = Radial(angle);
                var outward = Radial(angle + 90f * s);
                var colour = Across(look, s < 0 ? 0f : 1f);
                var bottom = draft.VertexCount;
                for (var j = 0; j <= rings; j++)
                {
                    var along = radius * j / rings;
                    draft.Add(radial * along + Vector3.up * FanFloor, outward, EffectPaint.Glow(colour, 0.4f, 0.4f));
                    draft.Add(radial * along + Vector3.up * (FanApexHeight + slope * along), outward, EffectPaint.Glow(colour, 0.4f, 0.4f));
                }
                for (var j = 0; j < rings; j++)
                {
                    var at = bottom + j * 2;
                    draft.Face(at, at + 1, at + 3);
                    draft.Face(at, at + 3, at + 2);
                }
            }

            // The bright edge along the top of the front.
            var edge = new List<Vector3>();
            for (var i = 0; i <= arc; i++)
                edge.Add(Radial(Mathf.Lerp(-halfAngle, halfAngle, i / (float)arc)) * radius + Vector3.up * FanRimHeight);
            draft.Tube(edge, 0.07f, 5, p => EffectPaint.Glow(Color.Lerp(Across(look, p / (float)arc), White, 0.6f), 0.95f, 0f)
                .WithCore(White, 1f));
            return draft;
        }

        public static Mesh Fan(GenesisEyeLook look, float radius, float halfAngle) =>
            Kept(new MeshKey(4, look, radius, halfAngle), () => FanDraft(look, radius, halfAngle), "GenesisFan3D");

        /// <summary>The outline of the fan's footprint on the floor (y = 0), in the outline colour of the look.</summary>
        public static Mesh FanOutline(Color outline, float radius, float halfAngle)
        {
            return Kept(new MeshKey(5, new GenesisEyeLook { Outline = outline }, radius, halfAngle), () =>
            {
                var path = new List<Vector3> { Vector3.zero };
                for (var i = 0; i <= 14; i++)
                    path.Add(Radial(Mathf.Lerp(-halfAngle, halfAngle, i / 14f)) * radius);
                var draft = new EffectMeshDraft();
                draft.FloorStrip(path, 0.12f, WithAlpha(outline, 0.9f), true);
                return draft;
            }, "GenesisFanOutline");
        }

        // ---- The tear and its puddle ----------------------------------------------------------------

        /// <summary>
        /// A tear 2 high and about 1.5 wide around its centre, its tip up (+Y): the tear colour of the look
        /// shaded as a volume, lighter where it faces the viewer, a white highlight on its front (+Z), and
        /// an ink outline.
        /// </summary>
        public static EffectMeshDraft TearDraft(GenesisEyeLook look)
        {
            var draft = new EffectMeshDraft();
            var profile = TearProfile(14);
            var body = EffectPaint.Shaded(look.Outer).WithCore(Lighter(look.Core), 0.55f);
            draft.Lathe(profile, 12, Vector3.zero, _ => body);
            draft.Sphere(new Vector3(0.27f, 0.2f, 0.53f), new Vector3(0.11f, 0.2f, 0.07f), 6, 4, EffectPaint.Flat(White));
            draft.Lathe(profile, 12, Vector3.zero, _ => EffectPaint.Outline(OutlineOf(look)), true, 1.13f);
            return draft;
        }

        /// <summary>
        /// The profile of a tear from its round bottom to its tip, as (distance from the axis, height):
        /// the teardrop curve x = sin t sin(t/2), y = cos t.
        /// </summary>
        public static Vector2[] TearProfile(int steps)
        {
            var profile = new Vector2[steps + 1];
            for (var k = 0; k <= steps; k++)
            {
                var t = Mathf.PI * (1f - k / (float)steps);
                profile[k] = new Vector2(Mathf.Max(0f, Mathf.Sin(t) * Mathf.Sin(t * 0.5f)), Mathf.Cos(t));
            }
            return profile;
        }

        public static Mesh Tear(GenesisEyeLook look) => Kept(new MeshKey(6, look), () => TearDraft(look), "GenesisTear");

        /// <summary>
        /// The puddle a zone leaves: a low blob of "radius" on the floor, its rim a little wavy, the
        /// colour of the look lighter in its middle, see-through and shaded as a liquid.
        /// </summary>
        public static EffectMeshDraft PuddleDraft(GenesisEyeLook look, float radius)
        {
            const int segments = 40;
            var steps = new[] { 0.3f, 0.55f, 0.75f, 0.88f, 0.96f, 1f };
            var draft = new EffectMeshDraft();
            var centre = draft.Add(Vector3.up * PuddleHeight, Vector3.up,
                EffectPaint.Glow(look.Core, 0.45f, 0.25f).WithLight(0.5f));
            var rows = new int[steps.Length];
            for (var k = 0; k < steps.Length; k++)
            {
                var rho = steps[k];
                rows[k] = draft.VertexCount;
                var colour = Color.Lerp(look.Core, look.Outer, Mathf.Pow(rho, 1.5f));
                for (var s = 0; s < segments; s++)
                {
                    var angle = s / (float)segments * Mathf.PI * 2f;
                    var reach = radius * PuddleRim(angle);
                    var radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    var slope = (PuddleBulge(Mathf.Min(1f, rho + 0.03f)) - PuddleBulge(Mathf.Max(0f, rho - 0.03f))) / (0.06f * reach);
                    draft.Add(radial * (reach * rho) + Vector3.up * PuddleBulge(rho), Vector3.up - radial * slope,
                        EffectPaint.Glow(colour, Mathf.Lerp(0.45f, 0.62f, rho), 0.25f).WithLight(0.5f));
                }
            }
            for (var s = 0; s < segments; s++)
                draft.Face(centre, rows[0] + s, rows[0] + (s + 1) % segments);
            for (var k = 0; k + 1 < steps.Length; k++)
                for (var s = 0; s < segments; s++)
                {
                    var next = (s + 1) % segments;
                    draft.Face(rows[k] + s, rows[k + 1] + s, rows[k + 1] + next);
                    draft.Face(rows[k] + s, rows[k + 1] + next, rows[k] + next);
                }
            return draft;
        }

        /// <summary>The wavy rim of a puddle: its reach at that angle, as a share of its radius.</summary>
        public static float PuddleRim(float angle)
        {
            return 1f + 0.05f * Mathf.Sin(5f * angle) + 0.03f * Mathf.Sin(3f * angle + 1.3f) + 0.015f * Mathf.Sin(9f * angle + 0.5f);
        }

        /// <summary>How high the puddle stands at "rho", the share of the way from its middle to its rim.</summary>
        public static float PuddleBulge(float rho)
        {
            return PuddleHeight * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Clamp01(rho), 2.5f)));
        }

        public static Mesh Puddle(GenesisEyeLook look, float radius) =>
            Kept(new MeshKey(7, look, radius), () => PuddleDraft(look, radius), "GenesisPuddle");

        /// <summary>The outline of a puddle's rim on the floor.</summary>
        public static Mesh PuddleOutline(Color outline, float radius)
        {
            return Kept(new MeshKey(8, new GenesisEyeLook { Outline = outline }, radius), () =>
            {
                var path = new List<Vector3>();
                for (var s = 0; s < 40; s++)
                {
                    var angle = s / 40f * Mathf.PI * 2f;
                    path.Add(new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (radius * PuddleRim(angle)));
                }
                var draft = new EffectMeshDraft();
                draft.FloorStrip(path, 0.12f, WithAlpha(outline, 0.85f), true);
                return draft;
            }, "GenesisPuddleOutline");
        }

        /// <summary>A ripple: a thin flat ring of radius 1 lying on the puddle.</summary>
        public static Mesh Ripple(Color colour)
        {
            return Kept(new MeshKey(9, new GenesisEyeLook { Outer = colour }), () =>
            {
                const int segments = 40;
                var draft = new EffectMeshDraft();
                var paint = EffectPaint.Glow(colour, 0.8f, 0f);
                var first = draft.VertexCount;
                for (var s = 0; s < segments; s++)
                {
                    var angle = s / (float)segments * Mathf.PI * 2f;
                    var radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    draft.Add(radial * 0.9f, Vector3.up, paint);
                    draft.Add(radial, Vector3.up, paint);
                }
                for (var s = 0; s < segments; s++)
                {
                    var at = first + s * 2;
                    var next = first + (s + 1) % segments * 2;
                    draft.Face(at, at + 1, next + 1, Vector3.up);
                    draft.Face(at, next + 1, next, Vector3.up);
                }
                return draft;
            }, "GenesisRipple");
        }

        // ---- Eyeballs, the front of a stare, the ring of a burst -----------------------------------

        /// <summary>
        /// An eyeball of radius 1 looking along +Z: a ball (white for an open eye, the skin for a closed
        /// one) shaded as the heroes are, that wears its renderer's picture on its front (EyePicture: the
        /// hero's own eye as GenesisEyes draws it), and an ink outline.
        /// </summary>
        public static EffectMeshDraft EyeballDraft(GenesisEyeLook look, Color skin)
        {
            var draft = new EffectMeshDraft();
            draft.Sphere(Vector3.zero, Vector3.one, 16, 11, EffectPaint.Shaded(GenesisEyes.Sclera(look, skin)).WithPicture());
            draft.Sphere(Vector3.zero, Vector3.one * EyeballShell, 12, 8, EffectPaint.Outline(OutlineOf(look)), true);
            return draft;
        }

        public static Mesh Eyeball(GenesisEyeLook look, Color skin) =>
            Kept(new MeshKey(10, look, skin, 0), () => EyeballDraft(look, skin), "GenesisEyeball");

        /// <summary>How many pixels across the picture of an eye is.</summary>
        public const int EyePictureSize = 128;

        private static readonly Dictionary<MeshKey, Texture2D> s_pictures = new Dictionary<MeshKey, Texture2D>();

        /// <summary>
        /// The hero's own eye (its other eye when "second") as GenesisEyes draws it, in pixels, for an
        /// eyeball to wear: the eyelid of a lidded eye is the ball itself closing over it, in the ball's
        /// colour. Transparent where the eye leaves the ball showing.
        /// </summary>
        public static Color32[] EyePictureDraft(GenesisEyeLook look, Color skin, bool second, int size)
        {
            // The eyelid is painted in a colour no eye uses, then cleared: there the ball shows, shaded.
            var picture = new ShapePainter();
            GenesisEyes.PaintSingle(picture, look, LidMark, EyeballPicture, second);
            return Raster(picture, size, 2, LidMark);
        }

        private static readonly Color LidMark = new Color(1f, 0f, 1f, 1f);

        /// <summary>The picture of an eye as a texture, made once for each look, skin and side.</summary>
        public static Texture2D EyePicture(GenesisEyeLook look, Color skin, bool second)
        {
            var key = new MeshKey(16, look, skin, second ? 1 : 0);
            if (s_pictures.TryGetValue(key, out var kept) && kept != null)
                return kept;
            var texture = new Texture2D(EyePictureSize, EyePictureSize, TextureFormat.RGBA32, true)
            {
                name = "GenesisEyePicture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(EyePictureDraft(look, skin, second, EyePictureSize));
            texture.Apply(true, true);
            s_pictures[key] = texture;
            return texture;
        }

        /// <summary>
        /// Paints a flat picture (ShapePainter, its points within 1 of its centre) into "size" by "size"
        /// pixels that cover -1 to 1 on both sides, each triangle over those painted before it, with
        /// "samples" by "samples" samples a pixel for soft edges; transparent where nothing is painted.
        /// The first row is the bottom of the picture, as a texture reads it. What is painted in "hole"
        /// clears what was painted under it.
        /// </summary>
        internal static Color32[] Raster(ShapePainter picture, int size, int samples, Color? hole = null)
        {
            Color32 cleared = hole ?? Color.clear;
            var clears = hole.HasValue;
            var across = size * samples;
            var painted = new Color32[across * across];
            var points = picture.Vertices;
            var triangles = picture.Triangles;
            for (var t = 0; t + 2 < triangles.Count; t += 3)
            {
                var a = Pixel(points[triangles[t]], across);
                var b = Pixel(points[triangles[t + 1]], across);
                var c = Pixel(points[triangles[t + 2]], across);
                Color32 colour = picture.Colors[triangles[t]];
                colour.a = 255;
                if (clears && colour.r == cleared.r && colour.g == cleared.g && colour.b == cleared.b)
                    colour = new Color32(0, 0, 0, 0);
                var left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                var right = Mathf.Min(across - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                var bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                var top = Mathf.Min(across - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (var y = bottom; y <= top; y++)
                    for (var x = left; x <= right; x++)
                    {
                        var at = new Vector2(x + 0.5f, y + 0.5f);
                        var ab = Side(a, b, at);
                        var bc = Side(b, c, at);
                        var ca = Side(c, a, at);
                        if ((ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f))
                            painted[y * across + x] = colour;
                    }
            }

            // Each pixel, the average of its samples (weighted by how much of it is painted).
            var pixels = new Color32[size * size];
            var share = 1f / (samples * samples);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    float r = 0f, g = 0f, b = 0f, alpha = 0f;
                    for (var sy = 0; sy < samples; sy++)
                        for (var sx = 0; sx < samples; sx++)
                        {
                            var sample = painted[(y * samples + sy) * across + x * samples + sx];
                            r += sample.r * (float)sample.a;
                            g += sample.g * (float)sample.a;
                            b += sample.b * (float)sample.a;
                            alpha += sample.a;
                        }
                    pixels[y * size + x] = alpha > 0f
                        ? new Color32((byte)(r / alpha), (byte)(g / alpha), (byte)(b / alpha), (byte)(alpha * share))
                        : new Color32(0, 0, 0, 0);
                }
            return pixels;
        }

        // A point of the picture (-1 to 1) in samples (0 to "across").
        private static Vector2 Pixel(Vector3 point, int across) => new Vector2((point.x + 1f) * 0.5f * across, (point.y + 1f) * 0.5f * across);

        // Which side of the line from "from" to "to" a point is on.
        private static float Side(Vector2 from, Vector2 to, Vector2 point) =>
            (to.x - from.x) * (point.y - from.y) - (to.y - from.y) * (point.x - from.x);

        /// <summary>
        /// The front of a stare: a curved sheet ahead along +Z, from 72 degrees on one side to 72 on the
        /// other, rising from the floor at 0.7 of the area's radius and leaning forward as a wave does to
        /// 1.12 of it at its top (as high as a hero in its middle), so that the camera above sees its
        /// width; strong at the floor, gone at its top; seen from both sides.
        /// </summary>
        public static EffectMeshDraft FrontDraft(Color colour, Color core)
        {
            const int arc = 16;
            const float half = 72f;
            var draft = new EffectMeshDraft();
            var heights = new[] { 0.03f, 0.4f, 1f };
            var reaches = new[] { 0.7f, 0.95f, 1.12f };
            var alphas = new[] { 0.8f, 0.4f, 0f };
            for (var side = 0; side < 2; side++)
            {
                var rows = new int[heights.Length];
                for (var k = 0; k < heights.Length; k++)
                {
                    rows[k] = draft.VertexCount;
                    for (var i = 0; i <= arc; i++)
                    {
                        var angle = Mathf.Lerp(-half, half, i / (float)arc);
                        var top = 0.75f + 0.45f * Mathf.Cos(angle / half * 90f * Mathf.Deg2Rad);
                        var radial = Radial(angle);
                        var height = k == 0 ? heights[0] : heights[k] * top;
                        // Its normal, across the sheet as it leans forward: the front faces out and down.
                        var normal = (radial - Vector3.up * 0.42f).normalized;
                        draft.Add(radial * reaches[k] + Vector3.up * height, side == 0 ? normal : -normal,
                            EffectPaint.Glow(k == 0 ? core : colour, alphas[k], 0.3f));
                    }
                }
                for (var k = 0; k + 1 < heights.Length; k++)
                    for (var i = 0; i < arc; i++)
                    {
                        draft.Face(rows[k] + i, rows[k + 1] + i, rows[k + 1] + i + 1);
                        draft.Face(rows[k] + i, rows[k + 1] + i + 1, rows[k] + i + 1);
                    }
            }
            return draft;
        }

        public static Mesh Front(Color colour, Color core) =>
            Kept(new MeshKey(11, new GenesisEyeLook { Outer = colour, Core = core }), () => FrontDraft(colour, core), "GenesisStareFront");

        /// <summary>The shock ring of a burst: a low round wall of radius 1, strong at the floor, gone at its top.</summary>
        public static EffectMeshDraft ShockDraft(Color colour, Color core)
        {
            const int segments = 40;
            var draft = new EffectMeshDraft();
            var heights = new[] { 0.03f, 0.22f, 0.6f };
            var alphas = new[] { 0.85f, 0.45f, 0f };
            for (var side = 0; side < 2; side++)
            {
                var rows = new int[heights.Length];
                for (var k = 0; k < heights.Length; k++)
                {
                    rows[k] = draft.VertexCount;
                    for (var s = 0; s < segments; s++)
                    {
                        var radial = Radial(s / (float)segments * 360f);
                        draft.Add(radial + Vector3.up * heights[k], side == 0 ? radial : -radial,
                            EffectPaint.Glow(k == 0 ? core : colour, alphas[k], 0.3f));
                    }
                }
                for (var k = 0; k + 1 < heights.Length; k++)
                    for (var s = 0; s < segments; s++)
                    {
                        var next = (s + 1) % segments;
                        draft.Face(rows[k] + s, rows[k + 1] + s, rows[k + 1] + next);
                        draft.Face(rows[k] + s, rows[k + 1] + next, rows[k] + next);
                    }
            }
            return draft;
        }

        public static Mesh Shock(Color colour, Color core) =>
            Kept(new MeshKey(12, new GenesisEyeLook { Outer = colour, Core = core }), () => ShockDraft(colour, core), "GenesisShock");

        // ---- On the floor ---------------------------------------------------------------------------

        /// <summary>The glow a laser casts on the floor: a strip 1 wide from z = 0 to z = 1.</summary>
        public static Mesh FloorGlow(Color colour)
        {
            return Kept(new MeshKey(13, new GenesisEyeLook { Outer = colour }), () =>
            {
                var draft = new EffectMeshDraft();
                draft.FloorStrip(new[] { Vector3.zero, Vector3.forward }, 1f, WithAlpha(colour, 0.42f), false);
                return draft;
            }, "GenesisFloorGlow");
        }

        /// <summary>A soft disc of radius 1 on the floor in that colour.</summary>
        public static Mesh FloorWash(Color colour)
        {
            return Kept(new MeshKey(14, new GenesisEyeLook { Outer = colour }), () =>
            {
                var draft = new EffectMeshDraft();
                draft.FloorDisc(1f, WithAlpha(colour, 0.3f), 32);
                return draft;
            }, "GenesisFloorWash");
        }

        /// <summary>A thin ring of radius 1 on the floor: the reach of a burst, in the outline colour.</summary>
        public static Mesh FloorRing(Color outline)
        {
            return Kept(new MeshKey(15, new GenesisEyeLook { Outline = outline }), () =>
            {
                var path = new List<Vector3>();
                for (var s = 0; s < 48; s++)
                    path.Add(Radial(s / 48f * 360f));
                var draft = new EffectMeshDraft();
                draft.FloorStrip(path, 0.045f, WithAlpha(outline, 0.9f), true);
                return draft;
            }, "GenesisFloorRing");
        }

        private static Mesh s_shadow;
        private static readonly Dictionary<Color, Mesh> s_landings = new Dictionary<Color, Mesh>();

        /// <summary>The soft shadow of a thing in the air, radius 1 (GroundMeshes, as under the heroes).</summary>
        public static Mesh Shadow => s_shadow != null ? s_shadow : s_shadow = GroundMeshes.Build("GenesisSpecialShadow", 1f, Color.clear, true, false);

        /// <summary>
        /// The ring on the spot a lob will land on, in the colour of the thrower's side: the very ring of a
        /// lobbed basic attack (ChogAttackLook), 1.1 across its outer edge.
        /// </summary>
        public static Mesh Landing(Color side)
        {
            if (!s_landings.TryGetValue(side, out var ring) || ring == null)
                s_landings[side] = ring = GroundMeshes.Build("GenesisLobLanding", 1.1f, side, false, true);
            return ring;
        }

        // ---- Helpers --------------------------------------------------------------------------------

        /// <summary>The colour of the look across the fan, from one side (0) through the bright middle to the other (1).</summary>
        public static Color Across(GenesisEyeLook look, float across)
        {
            var other = look.Other.a > 0f ? look.Other : look.Outer;
            return across < 0.5f
                ? Color.Lerp(look.Outer, look.Core, across * 2f)
                : Color.Lerp(look.Core, other, across * 2f - 1f);
        }

        /// <summary>The ink an effect of this look is outlined with: its own outline, else a dark ink.</summary>
        public static Color OutlineOf(GenesisEyeLook look) => look.Outline.a > 0f ? WithAlpha(look.Outline, 1f) : Ink;

        // A direction on the floor, "degrees" from +Z toward +X.
        private static Vector3 Radial(float degrees)
        {
            var angle = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }

        // A core that still shows over its colour: a light colour of the look, a little lighter.
        private static Color Lighter(Color colour)
        {
            if (colour.a <= 0f)
                return White;
            return WithAlpha(Color.Lerp(colour, White, 0.2f), 1f);
        }

        private static Color WithAlpha(Color colour, float alpha)
        {
            colour.a = alpha;
            return colour;
        }

        // A point from the middle along "direction", "length" out, "width" thick, as a double cone.
        private static void Spike(EffectMeshDraft draft, Vector3 direction, float length, float width, EffectPaint paint, bool inward)
        {
            var first = draft.VertexCount;
            draft.Lathe(new[] { new Vector2(0f, -width), new Vector2(width, 0f), new Vector2(0f, length) }, 4, Vector3.zero, _ => paint, inward);
            // Turned so that its axis (+Y) lies along "direction": x, y, z to side, direction, other, a turn
            // and not a mirror (side x direction = other, as x x y = z), so the triangles keep their front.
            var axis = direction.normalized;
            var side = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            var other = Vector3.Cross(side, axis);
            for (var v = first; v < draft.VertexCount; v++)
            {
                var at = draft.Vertices[v];
                var normal = draft.Normals[v];
                draft.Vertices[v] = side * at.x + axis * at.y + other * at.z;
                draft.Normals[v] = side * normal.x + axis * normal.y + other * normal.z;
            }
        }

        private static Mesh Kept(MeshKey key, Func<EffectMeshDraft> draft, string name)
        {
            if (s_meshes.TryGetValue(key, out var kept) && kept != null)
                return kept;
            var mesh = draft().ToMesh(name);
            s_meshes[key] = mesh;
            return mesh;
        }

        // What a kept mesh was made from: its kind, the colours of the look (and of a skin), and sizes.
        private readonly struct MeshKey : IEquatable<MeshKey>
        {
            private readonly int _kind, _outline, _outer, _core, _other, _glyph, _skin, _a, _b, _extra;

            public MeshKey(int kind, GenesisEyeLook look, float a = 0f, float b = 0f) : this(kind, look, Color.clear, 0, a, b)
            {
            }

            public MeshKey(int kind, GenesisEyeLook look, Color skin, int extra, float a = 0f, float b = 0f)
            {
                _kind = kind;
                _outline = Pack(look.Outline);
                _outer = Pack(look.Outer);
                _core = Pack(look.Core);
                _other = Pack(look.Other);
                _glyph = (int)look.Glyph;
                _skin = Pack(skin);
                _a = Mathf.RoundToInt(a * 100f);
                _b = Mathf.RoundToInt(b * 100f);
                _extra = extra;
            }

            private static int Pack(Color colour)
            {
                Color32 c = colour;
                return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
            }

            public bool Equals(MeshKey other) =>
                _kind == other._kind && _outline == other._outline && _outer == other._outer && _core == other._core
                && _other == other._other && _glyph == other._glyph && _skin == other._skin && _a == other._a && _b == other._b
                && _extra == other._extra;

            public override bool Equals(object obj) => obj is MeshKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = _kind;
                    hash = hash * 31 + _outline;
                    hash = hash * 31 + _outer;
                    hash = hash * 31 + _core;
                    hash = hash * 31 + _other;
                    hash = hash * 31 + _glyph;
                    hash = hash * 31 + _skin;
                    hash = hash * 31 + _a;
                    hash = hash * 31 + _b;
                    return hash * 31 + _extra;
                }
            }
        }
    }
}
