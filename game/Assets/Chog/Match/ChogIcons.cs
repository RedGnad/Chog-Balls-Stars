using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The pictograms of the screens (docs/DESIGN-BRIEF.md): white strokes and fills with the near-black
    /// outline and the hard shadow of the outlined titles, drawn in code from a few lines, arcs and
    /// polygons on a 24 unit grid, so no picture has to be imported and each stays sharp at any size.
    /// Each is drawn once per size and kept.
    /// </summary>
    public static class ChogIcons
    {
        public enum Icon
        {
            Heroes, Trophy, Key, Question, Friends, Gear, Back, Tower, Coin, Bolt, Check, Close,
            Sound, Music, Shake, Play, Star, Lock, Crosshair, Plus,
        }

        private const float Grid = 24f;
        private const float Padding = 2.2f;
        private const float Stroke = 2.4f;
        private const float Outline = 1.3f;
        private const float ShadowDrop = 1.1f;

        private static readonly Color32 Ink = new Color32(0x14, 0x0F, 0x24, 0xFF);
        private static readonly Dictionary<string, Sprite> s_made = new Dictionary<string, Sprite>();

        /// <summary>The pictogram as a square sprite of "size" pixels, white with an ink outline.</summary>
        public static Sprite Get(Icon icon, int size = 128)
        {
            var key = icon + " " + size;
            if (s_made.TryGetValue(key, out var kept) && kept != null)
                return kept;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "icon " + key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(Pixels(icon, size));
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            s_made[key] = sprite;
            return sprite;
        }

        /// <summary>The pixels of a pictogram, rows from the bottom, as a texture takes them.</summary>
        public static Color32[] Pixels(Icon icon, int size) => Draw(Describe(icon), size);

        /// <summary>
        /// What a pictogram is made of, in units of a 24 grid with y going down (as in SVG): lines of a
        /// given width, filled polygons, and cut-outs that are taken away from both.
        /// </summary>
        public sealed class Shape
        {
            internal readonly List<(Vector2[] points, float width)> Strokes = new List<(Vector2[], float)>();
            internal readonly List<Vector2[]> Fills = new List<Vector2[]>();
            internal readonly List<Vector2[]> Cuts = new List<Vector2[]>();
            internal readonly List<(Vector2[] points, float width)> CutStrokes = new List<(Vector2[], float)>();

            public Shape Line(float width, params float[] xy)
            {
                Strokes.Add((Points(xy), width));
                return this;
            }

            public Shape Path(float width, Vector2[] points)
            {
                Strokes.Add((points, width));
                return this;
            }

            public Shape Fill(params float[] xy)
            {
                Fills.Add(Points(xy));
                return this;
            }

            public Shape FillPath(Vector2[] points)
            {
                Fills.Add(points);
                return this;
            }

            public Shape Cut(Vector2[] points)
            {
                Cuts.Add(points);
                return this;
            }

            public Shape CutLine(float width, Vector2[] points)
            {
                CutStrokes.Add((points, width));
                return this;
            }
        }

        /// <summary>Points along an arc of a circle, angles in degrees, 0 to the right and 90 downwards.</summary>
        public static Vector2[] Arc(float cx, float cy, float r, float from, float to, int steps = 0)
        {
            if (steps <= 0)
                steps = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(to - from) / 10f));
            var points = new Vector2[steps + 1];
            for (var i = 0; i <= steps; i++)
            {
                var angle = Mathf.Deg2Rad * Mathf.Lerp(from, to, i / (float)steps);
                points[i] = new Vector2(cx + r * Mathf.Cos(angle), cy + r * Mathf.Sin(angle));
            }
            return points;
        }

        /// <summary>A whole circle, closed.</summary>
        public static Vector2[] Circle(float cx, float cy, float r) => Arc(cx, cy, r, 0f, 360f, 40);

        /// <summary>Joins several runs of points into one.</summary>
        public static Vector2[] Join(params Vector2[][] runs)
        {
            var all = new List<Vector2>();
            foreach (var run in runs)
                all.AddRange(run);
            return all.ToArray();
        }

        private static Vector2[] Points(float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (var i = 0; i < points.Length; i++)
                points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }

        private static Shape Describe(Icon icon)
        {
            var s = new Shape();
            switch (icon)
            {
                case Icon.Heroes:
                    // A Chog: a round head under the spikes of its crown, its two eyes cut out.
                    s.FillPath(Circle(12f, 14.6f, 7f));
                    s.Fill(6.4f, 11.5f, 6.6f, 3.6f, 9.6f, 7.4f, 12f, 2.2f, 14.4f, 7.4f, 17.4f, 3.6f, 17.6f, 11.5f);
                    s.Cut(Circle(9.3f, 15f, 1.7f));
                    s.Cut(Circle(14.7f, 15f, 1.7f));
                    break;
                case Icon.Trophy:
                    s.FillPath(Join(new[] { new Vector2(6.5f, 3.5f), new Vector2(17.5f, 3.5f) }, Arc(12f, 9f, 5.5f, 0f, 180f)));
                    s.Path(Stroke, Join(new[] { new Vector2(17f, 5f), new Vector2(20f, 5f) }, Arc(17f, 7f, 3f, 0f, 90f)));
                    s.Path(Stroke, Join(new[] { new Vector2(7f, 5f), new Vector2(4f, 5f) }, Arc(7f, 7f, 3f, 180f, 90f)));
                    s.Line(Stroke, 12f, 14f, 12f, 19.5f);
                    s.Fill(7.5f, 19f, 16.5f, 19f, 16.5f, 21.5f, 7.5f, 21.5f);
                    break;
                case Icon.Key:
                    s.Path(Stroke, Circle(8f, 15.5f, 4.2f));
                    s.Line(Stroke, 11f, 12.5f, 20.5f, 3f);
                    s.Line(Stroke, 17.2f, 6.3f, 20.2f, 9.3f);
                    s.Line(Stroke, 14.2f, 9.3f, 16.4f, 11.5f);
                    break;
                case Icon.Question:
                    s.Path(Stroke, Circle(12f, 12f, 9.5f));
                    s.Path(Stroke, Join(Arc(12f, 9.3f, 2.7f, 180f, 405f), new[] { new Vector2(12f, 12.3f), new Vector2(12f, 13.8f) }));
                    s.FillPath(Circle(12f, 17.4f, 1.6f));
                    break;
                case Icon.Friends:
                    s.Path(Stroke, Circle(9f, 8f, 3.6f));
                    s.Path(Stroke, Arc(9f, 21f, 6.5f, 180f, 360f));
                    s.Path(Stroke, Arc(15.6f, 8f, 3.6f, -90f, 90f));
                    s.Line(Stroke, 18f, 14.6f, 20.2f, 16f, 21.3f, 18.2f, 21.3f, 20.5f);
                    break;
                case Icon.Gear:
                {
                    // Eight teeth around a wheel, with a hole in the middle.
                    var outline = new List<Vector2>();
                    const int teeth = 8;
                    for (var i = 0; i < teeth; i++)
                    {
                        var at = i * 360f / teeth;
                        foreach (var (offset, radius) in new[] { (-15f, 7.4f), (-9f, 10f), (9f, 10f), (15f, 7.4f) })
                        {
                            var angle = Mathf.Deg2Rad * (at + offset);
                            outline.Add(new Vector2(12f + radius * Mathf.Cos(angle), 12f + radius * Mathf.Sin(angle)));
                        }
                    }
                    s.FillPath(outline.ToArray());
                    s.Cut(Circle(12f, 12f, 3.3f));
                    break;
                }
                case Icon.Back:
                    s.Line(3.4f, 15.5f, 4.5f, 8f, 12f, 15.5f, 19.5f);
                    break;
                case Icon.Tower:
                    s.Fill(5f, 21.5f, 5f, 9.5f, 6.5f, 8f, 6.5f, 3.5f, 9f, 3.5f, 9f, 5.5f, 10.8f, 5.5f, 10.8f, 3.5f, 13.2f, 3.5f, 13.2f, 5.5f,
                        15f, 5.5f, 15f, 3.5f, 17.5f, 3.5f, 17.5f, 8f, 19f, 9.5f, 19f, 21.5f);
                    s.Cut(Join(new[] { new Vector2(9.8f, 21.8f) }, Arc(12f, 16.8f, 2.2f, 180f, 360f), new[] { new Vector2(14.2f, 21.8f) }));
                    break;
                case Icon.Coin:
                    s.FillPath(Circle(12f, 12f, 9.3f));
                    s.CutLine(1.6f, Circle(12f, 12f, 5.8f));
                    break;
                case Icon.Bolt:
                    s.Fill(13.5f, 1.8f, 4f, 14f, 11f, 14f, 9.8f, 22.2f, 20f, 9.8f, 12.6f, 9.8f);
                    break;
                case Icon.Check:
                    s.Line(3.6f, 4f, 12.5f, 9.5f, 18f, 20f, 6.5f);
                    break;
                case Icon.Close:
                    s.Line(3.4f, 6f, 6f, 18f, 18f);
                    s.Line(3.4f, 18f, 6f, 6f, 18f);
                    break;
                case Icon.Sound:
                    s.Fill(3.5f, 9f, 8f, 9f, 13.5f, 4.5f, 13.5f, 19.5f, 8f, 15f, 3.5f, 15f);
                    s.Path(Stroke, Arc(14f, 12f, 3.6f, -50f, 50f));
                    s.Path(Stroke, Arc(14f, 12f, 7.2f, -50f, 50f));
                    break;
                case Icon.Music:
                    s.FillPath(Circle(7f, 18f, 3f));
                    s.FillPath(Circle(17f, 16f, 3f));
                    s.Line(Stroke, 9.6f, 18f, 9.6f, 5.5f);
                    s.Line(Stroke, 19.6f, 16f, 19.6f, 3.5f);
                    s.Line(3.2f, 9.6f, 5.5f, 19.6f, 3.5f);
                    break;
                case Icon.Shake:
                    s.Path(Stroke, new[] { new Vector2(8.5f, 4f), new Vector2(15.5f, 4f), new Vector2(15.5f, 20f), new Vector2(8.5f, 20f), new Vector2(8.5f, 4f) });
                    s.Line(Stroke, 4.5f, 8f, 4.5f, 16f);
                    s.Line(Stroke, 19.5f, 8f, 19.5f, 16f);
                    s.Line(Stroke, 1.6f, 10f, 1.6f, 14f);
                    s.Line(Stroke, 22.4f, 10f, 22.4f, 14f);
                    break;
                case Icon.Play:
                    s.Fill(7f, 4f, 20f, 12f, 7f, 20f);
                    break;
                case Icon.Star:
                {
                    var points = new Vector2[10];
                    for (var i = 0; i < 10; i++)
                    {
                        var angle = Mathf.Deg2Rad * (-90f + i * 36f);
                        var radius = i % 2 == 0 ? 10f : 4.4f;
                        points[i] = new Vector2(12f + radius * Mathf.Cos(angle), 12.6f + radius * Mathf.Sin(angle));
                    }
                    s.FillPath(points);
                    break;
                }
                case Icon.Lock:
                    s.Fill(4.5f, 10.5f, 19.5f, 10.5f, 19.5f, 21f, 4.5f, 21f);
                    s.Path(Stroke, Join(new[] { new Vector2(7.8f, 10.5f) }, Arc(12f, 7.6f, 4.2f, 180f, 360f), new[] { new Vector2(16.2f, 10.5f) }));
                    s.Cut(Circle(12f, 15.4f, 1.7f));
                    break;
                case Icon.Crosshair:
                    s.Path(Stroke, Circle(12f, 12f, 7f));
                    s.Line(Stroke, 12f, 1.5f, 12f, 5f);
                    s.Line(Stroke, 12f, 19f, 12f, 22.5f);
                    s.Line(Stroke, 1.5f, 12f, 5f, 12f);
                    s.Line(Stroke, 19f, 12f, 22.5f, 12f);
                    break;
                case Icon.Plus:
                    s.Line(3.6f, 12f, 4f, 12f, 20f);
                    s.Line(3.6f, 4f, 12f, 20f, 12f);
                    break;
            }
            return s;
        }

        // Each pixel: its signed distance to the shape (negative inside), the white of the shape, the
        // ink of its outline around it, and the same outline dropped a little lower as a hard shadow.
        private static Color32[] Draw(Shape shape, int size)
        {
            var pixels = new Color32[size * size];
            var unitsPerPixel = (Grid + Padding * 2f) / size;
            var pixelsPerUnit = 1f / unitsPerPixel;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Texture rows go up; the grid goes down.
                    var point = new Vector2((x + 0.5f) * unitsPerPixel - Padding, (size - 1 - y + 0.5f) * unitsPerPixel - Padding);
                    var inside = Distance(shape, point);
                    var shadow = Distance(shape, point - new Vector2(0f, ShadowDrop));
                    var white = Mathf.Clamp01(0.5f - inside * pixelsPerUnit);
                    var ink = Mathf.Max(Mathf.Clamp01(0.5f - (inside - Outline) * pixelsPerUnit),
                        Mathf.Clamp01(0.5f - (shadow - Outline) * pixelsPerUnit));
                    var alpha = Mathf.Max(white, ink);
                    if (alpha <= 0f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    // Over the ink, the white covers its share.
                    var r = Mathf.Lerp(Ink.r, 255f, white);
                    var g = Mathf.Lerp(Ink.g, 255f, white);
                    var b = Mathf.Lerp(Ink.b, 255f, white);
                    pixels[y * size + x] = new Color32((byte)r, (byte)g, (byte)b, (byte)(alpha * 255f));
                }
            }
            return pixels;
        }

        private static float Distance(Shape shape, Vector2 p)
        {
            var best = float.MaxValue;
            foreach (var (points, width) in shape.Strokes)
                best = Mathf.Min(best, ToPolyline(points, p) - width * 0.5f);
            foreach (var polygon in shape.Fills)
                best = Mathf.Min(best, ToPolygon(polygon, p));
            // A cut-out takes its own inside away: the shape is what is in the shape and out of the cut.
            foreach (var polygon in shape.Cuts)
                best = Mathf.Max(best, -ToPolygon(polygon, p));
            foreach (var (points, width) in shape.CutStrokes)
                best = Mathf.Max(best, -(ToPolyline(points, p) - width * 0.5f));
            return best;
        }

        private static float ToPolyline(Vector2[] points, Vector2 p)
        {
            var best = float.MaxValue;
            for (var i = 0; i + 1 < points.Length; i++)
                best = Mathf.Min(best, ToSegment(points[i], points[i + 1], p));
            return best;
        }

        // Signed: negative inside the polygon (even-odd rule).
        private static float ToPolygon(Vector2[] polygon, Vector2 p)
        {
            var best = float.MaxValue;
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[j];
                var b = polygon[i];
                best = Mathf.Min(best, ToSegment(a, b, p));
                if ((b.y > p.y) != (a.y > p.y) && p.x < (a.x - b.x) * (p.y - b.y) / (a.y - b.y) + b.x)
                    inside = !inside;
            }
            return inside ? -best : best;
        }

        private static float ToSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            var along = b - a;
            var length = along.sqrMagnitude;
            var t = length <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, along) / length);
            return Vector2.Distance(p, a + along * t);
        }
    }
}
