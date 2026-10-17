using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The shapes of the fake lights of a fight (ChogLight, ChogLightning), drawn additively by Chog/Effect:
    /// nothing in them is lit, they add their colour to what is behind them, as a light would. Plain drafts,
    /// tested without the engine.
    /// </summary>
    public static class ChogFxMeshes
    {
        /// <summary>How far a ray of a flare reaches, over the radius of its glow.</summary>
        public const float RayLength = 1.9f;
        private const float RayWidth = 0.07f;
        private const int FlareSides = 20;

        /// <summary>
        /// A flare of radius 1 in the XY plane, facing +Z (its object turns it to the camera): white-hot in its
        /// middle, its colour around, fading to nothing at its edge; "rays" thin points of light out to RayLength.
        /// </summary>
        public static EffectMeshDraft FlareDraft(Color colour, int rays)
        {
            var draft = new EffectMeshDraft();
            var hot = Color.Lerp(colour, Color.white, 0.75f);
            var centre = draft.Add(Vector3.zero, Vector3.forward, EffectPaint.Glow(hot, 1f, 0f));
            var middle = draft.VertexCount;
            for (var s = 0; s < FlareSides; s++)
                draft.Add(Around(s, FlareSides, 0.32f), Vector3.forward, EffectPaint.Glow(Color.Lerp(colour, hot, 0.35f), 0.75f, 0f));
            var edge = draft.VertexCount;
            for (var s = 0; s < FlareSides; s++)
                draft.Add(Around(s, FlareSides, 1f), Vector3.forward, EffectPaint.Glow(colour, 0f, 0f));
            for (var s = 0; s < FlareSides; s++)
            {
                var next = (s + 1) % FlareSides;
                draft.Face(centre, middle + s, middle + next, Vector3.forward);
                draft.Face(middle + s, edge + s, edge + next, Vector3.forward);
                draft.Face(middle + s, edge + next, middle + next, Vector3.forward);
            }

            // The rays, each a long thin diamond from the middle, bright at its root.
            for (var r = 0; r < rays; r++)
            {
                var angle = (r + 0.5f) / rays * Mathf.PI * 2f;
                var along = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var across = new Vector3(-along.y, along.x, 0f);
                var length = RayLength * (r % 2 == 0 ? 1f : 0.62f);
                var root = draft.Add(Vector3.zero, Vector3.forward, EffectPaint.Glow(hot, 0.95f, 0f));
                var left = draft.Add(along * 0.18f + across * RayWidth, Vector3.forward, EffectPaint.Glow(colour, 0.7f, 0f));
                var right = draft.Add(along * 0.18f - across * RayWidth, Vector3.forward, EffectPaint.Glow(colour, 0.7f, 0f));
                var tip = draft.Add(along * length, Vector3.forward, EffectPaint.Glow(colour, 0f, 0f));
                draft.Face(root, left, tip, Vector3.forward);
                draft.Face(root, tip, right, Vector3.forward);
            }
            return draft;
        }

        /// <summary>
        /// A ring of light on the floor of radius 1: brightest along its middle line, fading inward and outward
        /// over "width" (a share of the radius). Grown, it is a shock wave.
        /// </summary>
        public static EffectMeshDraft RingDraft(Color colour, float width)
        {
            const int segments = 40;
            var draft = new EffectMeshDraft();
            width = Mathf.Clamp(width, 0.02f, 0.9f);
            var radii = new[] { 1f - width, 1f - width * 0.35f, 1f, 1f + width * 0.25f };
            var alphas = new[] { 0f, 0.9f, 1f, 0f };
            var rows = new int[radii.Length];
            for (var k = 0; k < radii.Length; k++)
            {
                rows[k] = draft.VertexCount;
                var tint = k == 2 ? Color.Lerp(colour, Color.white, 0.5f) : colour;
                for (var s = 0; s < segments; s++)
                    draft.Add(OnFloor(s, segments, radii[k]), Vector3.up, EffectPaint.Glow(tint, alphas[k], 0f));
            }
            for (var k = 0; k + 1 < radii.Length; k++)
                for (var s = 0; s < segments; s++)
                {
                    var next = (s + 1) % segments;
                    draft.Face(rows[k] + s, rows[k + 1] + s, rows[k + 1] + next, Vector3.up);
                    draft.Face(rows[k] + s, rows[k + 1] + next, rows[k] + next, Vector3.up);
                }
            return draft;
        }

        /// <summary>A soft disc of light on the floor, radius 1: the light a flash casts on the ground.</summary>
        public static EffectMeshDraft PoolDraft(Color colour)
        {
            var draft = new EffectMeshDraft();
            draft.FloorDisc(1f, new Color(colour.r, colour.g, colour.b, 0.75f), 28);
            return draft;
        }

        /// <summary>
        /// A column of light, radius 1 and height 1 standing on its origin: open, brightest at the floor and
        /// along its edges, gone at its top. A hero comes back inside it.
        /// </summary>
        public static EffectMeshDraft ColumnDraft(Color colour)
        {
            const int segments = 24;
            var draft = new EffectMeshDraft();
            var heights = new[] { 0f, 0.35f, 1f };
            var alphas = new[] { 0.85f, 0.45f, 0f };
            var rows = new int[heights.Length];
            for (var k = 0; k < heights.Length; k++)
            {
                rows[k] = draft.VertexCount;
                var tint = k == 0 ? Color.Lerp(colour, Color.white, 0.5f) : colour;
                for (var s = 0; s < segments; s++)
                {
                    var radial = OnFloor(s, segments, 1f);
                    draft.Add(radial + Vector3.up * heights[k], radial, EffectPaint.Glow(tint, alphas[k], 0f));
                }
            }
            for (var k = 0; k + 1 < heights.Length; k++)
                for (var s = 0; s < segments; s++)
                {
                    var next = (s + 1) % segments;
                    draft.Face(rows[k] + s, rows[k + 1] + s, rows[k + 1] + next);
                    draft.Face(rows[k] + s, rows[k + 1] + next, rows[k] + next);
                }
            return draft;
        }

        /// <summary>
        /// The points of a bolt of lightning from "from" to "to": "segments" steps, each point pushed sideways at
        /// random, most in the middle and none at either end (a bolt starts and lands where it is aimed).
        /// </summary>
        public static void BoltPath(List<Vector3> into, Vector3 from, Vector3 to, int segments, float jitter, System.Random random)
        {
            into.Clear();
            segments = Mathf.Max(1, segments);
            var along = to - from;
            var length = along.magnitude;
            var direction = length > 1e-5f ? along / length : Vector3.up;
            var side = Vector3.Cross(direction, Mathf.Abs(direction.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            var other = Vector3.Cross(direction, side).normalized;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var point = from + along * t;
                if (i > 0 && i < segments)
                {
                    // Pushed most in the middle of the bolt.
                    var reach = jitter * Mathf.Sin(t * Mathf.PI);
                    point += side * (float)(random.NextDouble() * 2.0 - 1.0) * reach
                             + other * (float)(random.NextDouble() * 2.0 - 1.0) * reach * 0.5f;
                }
                into.Add(point);
            }
        }

        /// <summary>
        /// A flat band of light along "path", "width" wide, turned to face "toViewer" (from the band toward the
        /// camera): its colour at "alpha" along the middle, nothing at its two edges.
        /// </summary>
        public static void Ribbon(EffectMeshDraft draft, IList<Vector3> path, Vector3 toViewer, float width, Color colour, float alpha)
        {
            if (path.Count < 2)
                return;
            var clear = new Color(colour.r, colour.g, colour.b, 0f);
            var start = draft.VertexCount;
            for (var i = 0; i < path.Count; i++)
            {
                var ahead = path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)];
                var side = Vector3.Cross(ahead, toViewer);
                side = side.sqrMagnitude > 1e-8f ? side.normalized * (width * 0.5f) : Vector3.right * (width * 0.5f);
                // Thinner at its two ends.
                var taper = i == 0 || i == path.Count - 1 ? 0.35f : 1f;
                draft.Add(path[i] - side * taper, toViewer, EffectPaint.Glow(clear, 0f, 0f));
                draft.Add(path[i], toViewer, EffectPaint.Glow(colour, alpha, 0f));
                draft.Add(path[i] + side * taper, toViewer, EffectPaint.Glow(clear, 0f, 0f));
            }
            for (var i = 0; i + 1 < path.Count; i++)
            {
                var a = start + i * 3;
                var b = a + 3;
                draft.Face(a, b, b + 1, toViewer);
                draft.Face(a, b + 1, a + 1, toViewer);
                draft.Face(a + 1, b + 1, b + 2, toViewer);
                draft.Face(a + 1, b + 2, a + 2, toViewer);
            }
        }

        private static Vector3 Around(int index, int count, float radius)
        {
            var angle = index / (float)count * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        private static Vector3 OnFloor(int index, int count, float radius)
        {
            var angle = index / (float)count * Mathf.PI * 2f;
            return new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }
    }
}
