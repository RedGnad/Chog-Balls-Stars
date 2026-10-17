using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Collects flat shapes, in the order they are painted, into one mesh with vertex colours:
    /// what is added later covers what was added before. Upright, in x and y.
    /// </summary>
    internal sealed class ShapePainter
    {
        private const int EllipseSegments = 18;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        /// <summary>The painted points, in x and y, in the order they were added.</summary>
        public IReadOnlyList<Vector3> Vertices => _vertices;

        public IReadOnlyList<Color> Colors => _colors;

        /// <summary>Three indices a triangle, in the order they were painted (later ones cover).</summary>
        public IReadOnlyList<int> Triangles => _triangles;

        public void Ellipse(Vector2 center, float radiusX, float radiusY, Color color)
        {
            var first = Add(center, color);
            for (var i = 0; i < EllipseSegments; i++)
            {
                var angle = i / (float)EllipseSegments * Mathf.PI * 2f;
                Add(center + new Vector2(Mathf.Sin(angle) * radiusX, Mathf.Cos(angle) * radiusY), color);
            }

            for (var i = 0; i < EllipseSegments; i++)
                Join(first, first + 1 + i, first + 1 + (i + 1) % EllipseSegments);
        }

        /// <summary>A rectangle centred on "center", turned counter-clockwise by "degrees".</summary>
        public void Bar(Vector2 center, float length, float thickness, float degrees, Color color)
        {
            var angle = degrees * Mathf.Deg2Rad;
            var along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (length * 0.5f);
            var across = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * (thickness * 0.5f);
            var first = Add(center - along - across, color);
            Add(center - along + across, color);
            Add(center + along + across, color);
            Add(center + along - across, color);
            Join(first, first + 1, first + 2);
            Join(first, first + 2, first + 3);
        }

        /// <summary>A horizontal bar with round ends: "halfLength" from its centre to the centre of an end.</summary>
        public void Capsule(Vector2 center, float halfLength, float radius, Color color)
        {
            Bar(center, halfLength * 2f, radius * 2f, 0f, color);
            Ellipse(center + new Vector2(-halfLength, 0f), radius, radius, color);
            Ellipse(center + new Vector2(halfLength, 0f), radius, radius, color);
        }

        /// <summary>Part of a ring, between two angles measured counter-clockwise from the right.</summary>
        public void Arc(Vector2 center, float inner, float outer, float fromDegrees, float toDegrees, Color color)
        {
            var steps = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(toDegrees - fromDegrees) / 15f));
            var first = _vertices.Count;
            for (var i = 0; i <= steps; i++)
            {
                var angle = Mathf.Lerp(fromDegrees, toDegrees, i / (float)steps) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Add(center + direction * inner, color);
                Add(center + direction * outer, color);
            }

            for (var i = 0; i < steps; i++)
            {
                var at = first + i * 2;
                Join(at, at + 1, at + 3);
                Join(at, at + 3, at + 2);
            }
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            var first = Add(a, color);
            Add(b, color);
            Add(c, color);
            Join(first, first + 1, first + 2);
        }

        /// <summary>A star with that many points, the first one upwards.</summary>
        public void Star(Vector2 center, float outer, float inner, int points, Color color)
        {
            var first = Add(center, color);
            for (var i = 0; i < points * 2; i++)
            {
                var angle = i / (float)(points * 2) * Mathf.PI * 2f;
                var radius = i % 2 == 0 ? outer : inner;
                Add(center + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius, color);
            }

            for (var i = 0; i < points * 2; i++)
                Join(first, first + 1 + i, first + 1 + (i + 1) % (points * 2));
        }

        /// <summary>The usual heart curve, about "size" wide and tall around "center".</summary>
        public void Heart(Vector2 center, float size, Color color)
        {
            const int steps = 28;
            var first = Add(center + new Vector2(0f, -size * 0.1f), color);
            for (var i = 0; i < steps; i++)
            {
                var t = i / (float)steps * Mathf.PI * 2f;
                var sine = Mathf.Sin(t);
                var x = 16f * sine * sine * sine;
                var y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                Add(center + new Vector2(x, y + 2.5f) * (size / 17f), color);
            }

            for (var i = 0; i < steps; i++)
                Join(first, first + 1 + i, first + 1 + (i + 1) % steps);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private int Add(Vector2 point, Color color)
        {
            color.a = 1f;
            _vertices.Add(new Vector3(point.x, point.y, 0f));
            _colors.Add(color);
            return _vertices.Count - 1;
        }

        private void Join(int a, int b, int c)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
        }
    }
}
