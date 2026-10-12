using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The small flat sign of each Aura effect, in the colour of that Aura with a dark outline:
    /// a flame for a burn, a snowflake for a chill, a star for a hex. It marks the target a
    /// signature touched, and stays above it while the effect lasts. One mesh, a few triangles.
    /// </summary>
    public static class GenesisIcons
    {
        private static readonly Color Outline = new Color(0.08f, 0.08f, 0.1f);

        // An effect without a colour of its own (the raw hit) is marked in a warm white.
        private static readonly Color Plain = new Color(1f, 0.96f, 0.85f);

        /// <summary>The sign of an effect, upright in x and y, about "size" from its centre to its edge.</summary>
        public static Mesh Mesh(GenesisEffectIcon icon, Color color, float size)
        {
            if (color.a <= 0f)
                color = Plain;

            var painter = new ShapePainter();
            // The same drawing twice: thicker in the dark line, then in the colour.
            Draw(painter, icon, Outline, size, size * 0.14f);
            Draw(painter, icon, color, size, 0f);
            if (icon == GenesisEffectIcon.Bubbles)
            {
                // A glint on each bubble.
                var light = Color.Lerp(color, Color.white, 0.75f);
                painter.Ellipse(new Vector2(-size * 0.5f, -size * 0.1f), size * 0.14f, size * 0.14f, light);
                painter.Ellipse(new Vector2(size * 0.36f, size * 0.5f), size * 0.1f, size * 0.1f, light);
            }

            return painter.ToMesh("GenesisIcon");
        }

        // "extra" thickens every part by that much on each side.
        private static void Draw(ShapePainter painter, GenesisEffectIcon icon, Color color, float size, float extra)
        {
            switch (icon)
            {
                case GenesisEffectIcon.Flame:
                    // A round base and a tip that leans.
                    painter.Ellipse(new Vector2(0f, -size * 0.3f), size * 0.62f + extra, size * 0.62f + extra, color);
                    painter.Triangle(new Vector2(-size * 0.6f - extra, -size * 0.2f), new Vector2(size * 0.6f + extra, -size * 0.2f),
                        new Vector2(size * 0.18f, size + extra * 1.6f), color);
                    break;

                case GenesisEffectIcon.Snowflake:
                    for (var i = 0; i < 3; i++)
                        painter.Bar(Vector2.zero, size * 2f + extra * 2f, size * 0.24f + extra * 2f, 30f + i * 60f, color);
                    painter.Ellipse(Vector2.zero, size * 0.3f + extra, size * 0.3f + extra, color);
                    break;

                case GenesisEffectIcon.Star:
                    painter.Star(Vector2.zero, size + extra * 1.5f, size * 0.45f + extra, 5, color);
                    break;

                case GenesisEffectIcon.Plus:
                    painter.Bar(Vector2.zero, size * 1.9f + extra * 2f, size * 0.62f + extra * 2f, 0f, color);
                    painter.Bar(Vector2.zero, size * 1.9f + extra * 2f, size * 0.62f + extra * 2f, 90f, color);
                    break;

                case GenesisEffectIcon.Ring:
                    painter.Arc(Vector2.zero, size * 0.58f - extra, size + extra, 0f, 360f, color);
                    break;

                case GenesisEffectIcon.Arrow:
                    // An arrow pointing up.
                    painter.Triangle(new Vector2(-size * 0.9f - extra * 1.6f, size * 0.05f - extra), new Vector2(size * 0.9f + extra * 1.6f, size * 0.05f - extra),
                        new Vector2(0f, size + extra * 1.6f), color);
                    painter.Bar(new Vector2(0f, -size * 0.45f), size * 0.62f + extra * 2f, size * 1.1f + extra * 2f, 0f, color);
                    break;

                case GenesisEffectIcon.Bolt:
                    // A lightning bolt: two slanted bars joined in the middle.
                    painter.Bar(new Vector2(size * 0.12f, size * 0.42f), size * 1.25f + extra * 2f, size * 0.46f + extra * 2f, 62f, color);
                    painter.Bar(Vector2.zero, size * 0.95f + extra * 2f, size * 0.4f + extra * 2f, 0f, color);
                    painter.Bar(new Vector2(-size * 0.12f, -size * 0.42f), size * 1.25f + extra * 2f, size * 0.46f + extra * 2f, 62f, color);
                    break;

                case GenesisEffectIcon.Cloud:
                    painter.Ellipse(new Vector2(-size * 0.5f, -size * 0.15f), size * 0.5f + extra, size * 0.45f + extra, color);
                    painter.Ellipse(new Vector2(size * 0.5f, -size * 0.15f), size * 0.5f + extra, size * 0.45f + extra, color);
                    painter.Ellipse(new Vector2(0f, size * 0.2f), size * 0.6f + extra, size * 0.55f + extra, color);
                    break;

                case GenesisEffectIcon.Bubbles:
                    painter.Ellipse(new Vector2(-size * 0.4f, -size * 0.25f), size * 0.55f + extra, size * 0.55f + extra, color);
                    painter.Ellipse(new Vector2(size * 0.45f, size * 0.4f), size * 0.4f + extra, size * 0.4f + extra, color);
                    painter.Ellipse(new Vector2(size * 0.5f, -size * 0.55f), size * 0.24f + extra, size * 0.24f + extra, color);
                    break;

                default:
                    // A burst: eight rays around a centre.
                    for (var i = 0; i < 4; i++)
                        painter.Bar(Vector2.zero, size * 2f + extra * 2f, size * 0.3f + extra * 2f, i * 45f, color);
                    painter.Ellipse(Vector2.zero, size * 0.5f + extra, size * 0.5f + extra, color);
                    break;
            }
        }
    }
}
