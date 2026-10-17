using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// How one point of a 3D effect is drawn by Chog/Effect (Resources/ChogEffect.shader): its colour
    /// and opacity, whether the heroes' matcap shades it, how much more opaque a see-through surface gets
    /// where it turns away from the viewer (a tube or a wall of light then reads as a volume), the lighter
    /// colour that covers the part of it facing the viewer (the core of a laser as the collection draws
    /// it), whether it belongs to an ink outline shell, and whether it wears the picture of its renderer
    /// (an eyeball's eye).
    /// </summary>
    public readonly struct EffectPaint
    {
        public readonly Color Colour;
        public readonly float Lit;
        public readonly float Edge;
        public readonly Color Core;
        public readonly float CoreAmount;
        public readonly bool Shell;
        public readonly bool Picture;

        private EffectPaint(Color colour, float lit, float edge, Color core, float coreAmount, bool shell, bool picture = false)
        {
            Colour = colour;
            Lit = lit;
            Edge = edge;
            Core = core;
            CoreAmount = coreAmount;
            Shell = shell;
            Picture = picture;
        }

        /// <summary>
        /// The same paint on a ball that wears its renderer's picture on its front (around +Z), flat in
        /// the picture's colours where it has some (EffectMeshDraft.PictureDirection).
        /// </summary>
        public EffectPaint WithPicture() => new EffectPaint(Colour, Lit, Edge, Core, CoreAmount, Shell, true);

        /// <summary>Shaded as the heroes are.</summary>
        public static EffectPaint Shaded(Color colour) => new EffectPaint(Opaque(colour), 1f, 0f, Color.clear, 0f, false);

        /// <summary>The colour whatever the light: eyes drawn on a ball, a glint, a light.</summary>
        public static EffectPaint Flat(Color colour) => new EffectPaint(Opaque(colour), 0f, 0f, Color.clear, 0f, false);

        /// <summary>See-through at "alpha", "edge" more opaque at its silhouette (below zero, fainter there).</summary>
        public static EffectPaint Glow(Color colour, float alpha, float edge)
        {
            colour.a = alpha;
            return new EffectPaint(colour, 0f, edge, Color.clear, 0f, false);
        }

        /// <summary>The ink outline: a shell around a shape, seen only where it passes behind it.</summary>
        public static EffectPaint Outline(Color colour) => new EffectPaint(Opaque(colour), 0f, 0f, Color.clear, 0f, true);

        /// <summary>The same paint, its part that faces the viewer covered by "core" (amount 0 to 1).</summary>
        public EffectPaint WithCore(Color core, float amount) => new EffectPaint(Colour, Lit, Edge, core, amount, Shell, Picture);

        /// <summary>The same paint, shaded by the matcap that much (0 flat, 1 as the heroes).</summary>
        public EffectPaint WithLight(float lit) => new EffectPaint(Colour, lit, Edge, Core, CoreAmount, Shell, Picture);

        /// <summary>The same paint at another opacity.</summary>
        public EffectPaint WithAlpha(float alpha)
        {
            var colour = Colour;
            colour.a = alpha;
            return new EffectPaint(colour, Lit, Edge, Core, CoreAmount, Shell, Picture);
        }

        private static Color Opaque(Color colour)
        {
            colour.a = 1f;
            return colour;
        }
    }

    /// <summary>
    /// A mesh being put together for a 3D effect, out of the engine: points with their normal and paint,
    /// and triangles turned so that their front faces outwards (or inwards, for an outline shell). The
    /// shapes the specials are made of (GenesisSpecialMeshes) are built from its few solids: a surface of
    /// revolution, a sphere, a tube along a path, a strip and a disc on the floor; a ball can wear the
    /// picture of its renderer on its front (PictureDirection). ToMesh hands it to the engine; the rest
    /// is tested without it.
    /// </summary>
    public sealed class EffectMeshDraft
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Color> Colours = new List<Color>();
        // Chog/Effect reads x lit, y edge, z core amount, w shell; and from the second set the core colour, w the picture.
        public readonly List<Vector4> Flags = new List<Vector4>();
        public readonly List<Vector4> Cores = new List<Vector4>();
        public readonly List<int> Triangles = new List<int>();

        public int VertexCount => Vertices.Count;

        public int Add(Vector3 position, Vector3 normal, EffectPaint paint)
        {
            Vertices.Add(position);
            Normals.Add(normal.sqrMagnitude > 1e-12f ? normal.normalized : Vector3.up);
            Colours.Add(paint.Colour);
            Flags.Add(new Vector4(paint.Lit, paint.Edge, paint.CoreAmount, paint.Shell ? 1f : 0f));
            Cores.Add(new Vector4(paint.Core.r, paint.Core.g, paint.Core.b, paint.Picture ? 1f : 0f));
            return Vertices.Count - 1;
        }

        /// <summary>
        /// A triangle whose front (the side Unity draws, where its corners turn clockwise) faces along the
        /// normals of its corners, or against them when "inward" (the inside of an outline shell).
        /// </summary>
        public void Face(int a, int b, int c, bool inward = false)
        {
            var normal = Normals[a] + Normals[b] + Normals[c];
            Face(a, b, c, inward ? -normal : normal);
        }

        /// <summary>A triangle whose front faces along "outward".</summary>
        public void Face(int a, int b, int c, Vector3 outward)
        {
            var front = Vector3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]);
            if (Vector3.Dot(front, outward) < 0f)
            {
                var swap = b;
                b = c;
                c = swap;
            }
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }

        /// <summary>
        /// The surface made by turning a profile around the vertical axis through "centre": each point of
        /// the profile is (distance from the axis, height), from one end to the other. A point on the axis
        /// closes the surface there. The normals come from the profile, pointing away from the axis.
        /// "paintAt" gives the paint of each point of the profile.
        /// </summary>
        public void Lathe(IList<Vector2> profile, int segments, Vector3 centre, System.Func<int, EffectPaint> paintAt,
                          bool inward = false, float scale = 1f)
        {
            var rings = new int[profile.Count];
            for (var p = 0; p < profile.Count; p++)
            {
                var before = profile[Mathf.Max(0, p - 1)];
                var after = profile[Mathf.Min(profile.Count - 1, p + 1)];
                var along = after - before;
                // The profile's normal, turned away from the axis: for a profile that climbs, outwards.
                var flat = new Vector2(along.y, -along.x);
                if (flat.sqrMagnitude < 1e-12f)
                    flat = new Vector2(1f, 0f);
                flat.Normalize();
                var point = profile[p] * scale;
                var paint = paintAt(p);
                if (profile[p].x < 1e-5f)
                {
                    // On the axis: one point, its normal along the axis.
                    var up = p == 0 ? (profile.Count > 1 && profile[1].y < profile[0].y ? 1f : -1f)
                                    : (profile[p - 1].y < profile[p].y ? 1f : -1f);
                    rings[p] = -1 - Add(centre + new Vector3(0f, point.y, 0f), new Vector3(0f, up, 0f), paint);
                    continue;
                }

                rings[p] = VertexCount;
                for (var s = 0; s < segments; s++)
                {
                    var angle = s / (float)segments * Mathf.PI * 2f;
                    var cos = Mathf.Cos(angle);
                    var sin = Mathf.Sin(angle);
                    Add(centre + new Vector3(point.x * cos, point.y, point.x * sin), new Vector3(flat.x * cos, flat.y, flat.x * sin), paint);
                }
            }

            for (var p = 0; p + 1 < profile.Count; p++)
                Band(rings[p], rings[p + 1], segments, inward);
        }

        /// <summary>
        /// A ball (or an ellipsoid of those three radii) around "centre", "segments" around and "rings"
        /// from pole to pole.
        /// </summary>
        public void Sphere(Vector3 centre, Vector3 radii, int segments, int rings, EffectPaint paint, bool inward = false)
        {
            var poles = new int[rings + 1];
            for (var r = 0; r <= rings; r++)
            {
                var polar = r / (float)rings * Mathf.PI;
                var y = Mathf.Cos(polar);
                var across = Mathf.Sin(polar);
                if (r == 0 || r == rings)
                {
                    poles[r] = -1 - Add(centre + new Vector3(0f, y * radii.y, 0f), new Vector3(0f, y, 0f), paint);
                    continue;
                }

                poles[r] = VertexCount;
                for (var s = 0; s < segments; s++)
                {
                    var angle = s / (float)segments * Mathf.PI * 2f;
                    var unit = new Vector3(across * Mathf.Cos(angle), y, across * Mathf.Sin(angle));
                    var at = Vector3.Scale(unit, radii);
                    // The normal of an ellipsoid: the point divided by the square of its radii.
                    var normal = new Vector3(at.x / (radii.x * radii.x), at.y / (radii.y * radii.y), at.z / (radii.z * radii.z));
                    Add(centre + at, normal, paint);
                }
            }

            for (var r = 0; r < rings; r++)
                Band(poles[r], poles[r + 1], segments, inward);
        }

        /// <summary>
        /// A tube of that radius along a path of points, "sides" around, open at its two ends; each point
        /// of the path may take its own paint.
        /// </summary>
        public void Tube(IList<Vector3> path, float radius, int sides, System.Func<int, EffectPaint> paintAt)
        {
            var rings = new int[path.Count];
            for (var p = 0; p < path.Count; p++)
            {
                var along = (path[Mathf.Min(path.Count - 1, p + 1)] - path[Mathf.Max(0, p - 1)]).normalized;
                var side = Vector3.Cross(along, Mathf.Abs(along.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                var other = Vector3.Cross(along, side);
                rings[p] = VertexCount;
                var paint = paintAt(p);
                for (var s = 0; s < sides; s++)
                {
                    var angle = s / (float)sides * Mathf.PI * 2f;
                    var outward = side * Mathf.Cos(angle) + other * Mathf.Sin(angle);
                    Add(path[p] + outward * radius, outward, paint);
                }
            }

            for (var p = 0; p + 1 < path.Count; p++)
                Band(rings[p], rings[p + 1], sides, false);
        }

        /// <summary>
        /// A band on the floor (y = 0) along a path, "width" across, its colour full in the middle and
        /// gone at its two edges; closed back on its first point when "loop".
        /// </summary>
        public void FloorStrip(IList<Vector3> path, float width, Color colour, bool loop)
        {
            var count = path.Count;
            var clear = new Color(colour.r, colour.g, colour.b, 0f);
            var first = VertexCount;
            for (var p = 0; p < count; p++)
            {
                var previous = path[loop ? (p - 1 + count) % count : Mathf.Max(0, p - 1)];
                var next = path[loop ? (p + 1) % count : Mathf.Min(count - 1, p + 1)];
                var along = next - previous;
                along.y = 0f;
                var across = Vector3.Cross(Vector3.up, along.normalized) * (width * 0.5f);
                var at = new Vector3(path[p].x, 0f, path[p].z);
                Add(at - across, Vector3.up, EffectPaint.Glow(clear, 0f, 0f));
                Add(at - across * 0.35f, Vector3.up, EffectPaint.Glow(colour, colour.a, 0f));
                Add(at + across * 0.35f, Vector3.up, EffectPaint.Glow(colour, colour.a, 0f));
                Add(at + across, Vector3.up, EffectPaint.Glow(clear, 0f, 0f));
            }

            var spans = loop ? count : count - 1;
            for (var p = 0; p < spans; p++)
            {
                var here = first + p * 4;
                var there = first + (p + 1) % count * 4;
                for (var k = 0; k < 3; k++)
                {
                    Face(here + k, there + k, there + k + 1, Vector3.up);
                    Face(here + k, there + k + 1, here + k + 1, Vector3.up);
                }
            }
        }

        /// <summary>
        /// A soft disc on the floor: "colour" in its middle, fading to nothing at "radius".
        /// </summary>
        public void FloorDisc(float radius, Color colour, int segments)
        {
            var centre = Add(Vector3.zero, Vector3.up, EffectPaint.Glow(colour, colour.a, 0f));
            var middle = VertexCount;
            for (var s = 0; s < segments; s++)
                Add(OnFloor(s, segments, radius * 0.6f), Vector3.up, EffectPaint.Glow(colour, colour.a * 0.8f, 0f));
            var edge = VertexCount;
            var clear = new Color(colour.r, colour.g, colour.b, 0f);
            for (var s = 0; s < segments; s++)
                Add(OnFloor(s, segments, radius), Vector3.up, EffectPaint.Glow(clear, 0f, 0f));
            for (var s = 0; s < segments; s++)
            {
                var next = (s + 1) % segments;
                Face(centre, middle + s, middle + next, Vector3.up);
                Face(middle + s, edge + s, edge + next, Vector3.up);
                Face(middle + s, edge + next, middle + next, Vector3.up);
            }
        }

        /// <summary>
        /// The angle around a ball its picture's rim (a point at distance 1 from the picture's centre)
        /// reaches: Chog/Effect lays the picture so (its PictureRim, in radians).
        /// </summary>
        public const float PictureRimDegrees = 72f;

        /// <summary>
        /// Where a point of a flat picture lands on the front of a ball whose front looks along +Z: as far
        /// around the ball as it is from the picture's centre (PictureRimDegrees for a point at distance 1),
        /// in its direction. The picture's +x lands where a viewer in front of the ball sees its right, the
        /// ball's -X. What Chog/Effect does for each pixel of a ball that wears a picture, the other way.
        /// </summary>
        public static Vector3 PictureDirection(Vector2 point)
        {
            var distance = point.magnitude;
            if (distance < 1e-6f)
                return Vector3.forward;
            var angle = distance * PictureRimDegrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(angle) / distance;
            return new Vector3(-point.x * sin, point.y * sin, Mathf.Cos(angle));
        }

        /// <summary>The point of the picture a direction from the ball's centre shows, as Chog/Effect reads it.</summary>
        public static Vector2 PictureAt(Vector3 direction)
        {
            direction.Normalize();
            var angle = Mathf.Acos(Mathf.Clamp(direction.z, -1f, 1f));
            var across = new Vector2(-direction.x, direction.y);
            var length = across.magnitude;
            var reach = angle / (PictureRimDegrees * Mathf.Deg2Rad);
            // Straight in front, the middle; straight behind, beyond the rim (the shader shows nothing there).
            return length < 1e-6f ? Vector2.up * reach : across / length * reach;
        }

        // Joins two rings of "count" points (or a ring and a single point, given as -1 - index).
        private void Band(int from, int to, int count, bool inward)
        {
            if (from < 0 && to < 0)
                return;
            if (from < 0 || to < 0)
            {
                var pole = from < 0 ? -1 - from : -1 - to;
                var ring = from < 0 ? to : from;
                for (var s = 0; s < count; s++)
                    Face(pole, ring + s, ring + (s + 1) % count, inward);
                return;
            }

            for (var s = 0; s < count; s++)
            {
                var next = (s + 1) % count;
                Face(from + s, to + s, to + next, inward);
                Face(from + s, to + next, from + next, inward);
            }
        }

        private static Vector3 OnFloor(int index, int count, float radius)
        {
            var angle = index / (float)count * Mathf.PI * 2f;
            return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }

        /// <summary>Empties the draft, to put another shape together in the same lists (a bolt that flickers).</summary>
        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            Colours.Clear();
            Flags.Clear();
            Cores.Clear();
            Triangles.Clear();
        }

        /// <summary>Puts the draft into a mesh that is drawn again and again (its data stays readable, for the next fill).</summary>
        public void Fill(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetColors(Colours);
            mesh.SetUVs(0, Flags);
            mesh.SetUVs(1, Cores);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>The mesh for the engine.</summary>
        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (Vertices.Count > ushort.MaxValue)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetColors(Colours);
            mesh.SetUVs(0, Flags);
            mesh.SetUVs(1, Cores);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
