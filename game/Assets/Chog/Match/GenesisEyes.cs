using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Draws the eyes of a Genesis the way the collection draws them, for the attacks that are a
    /// look and not an object: the pair of eyes on a strip of the hero's skin, or one eye on a
    /// disc of it. Flat shapes, one mesh each, painted back to front with vertex colours; the
    /// shapes and colours of each Eyes value are authored in GenesisSignatureImporter.
    /// </summary>
    public static class GenesisEyes
    {
        // The face of a Genesis whose skin colour is not known.
        private static readonly Color PlainSkin = new Color(0.93f, 0.89f, 0.8f);
        private static readonly Color Glint = Color.white;

        /// <summary>
        /// The pair of eyes on a strip of skin, upright in x and y, "halfWidth" wide on each side
        /// of its centre. The strip is outlined in the outline colour of the look.
        /// </summary>
        public static Mesh Pair(GenesisEyeLook look, Color skin, float halfWidth)
        {
            skin = Backing(skin, look);

            var eye = halfWidth * 0.34f;
            var apart = halfWidth * 0.46f;
            var painter = new ShapePainter();
            painter.Capsule(Vector2.zero, apart + eye * 0.2f, eye * 1.62f, look.Outline);
            painter.Capsule(Vector2.zero, apart + eye * 0.2f, eye * 1.42f, skin);

            if (look.Glyph == GenesisEyeGlyph.Band)
            {
                // Ski goggles: one band, its three colours side by side.
                var right = look.Other.a > 0f ? look.Other : look.Outer;
                painter.Capsule(Vector2.zero, apart, eye * 1.08f, Dark);
                painter.Capsule(Vector2.zero, apart, eye * 0.9f, look.Outer);
                painter.Capsule(new Vector2(apart * 0.5f, 0f), apart * 0.5f, eye * 0.9f, right);
                painter.Bar(Vector2.zero, apart * 0.9f, eye * 1.8f, 0f, look.Core);
                painter.Bar(new Vector2(-apart * 0.55f, eye * 0.42f), eye * 0.9f, eye * 0.16f, 0f, Glint);
            }
            else if (look.Glyph == GenesisEyeGlyph.Shades)
            {
                // Pixel sunglasses: two blocks, a bridge, and stepped glints.
                painter.Bar(new Vector2(0f, eye * 0.42f), apart * 2f + eye * 2.2f, eye * 0.42f, 0f, look.Outer);
                for (var side = -1; side <= 1; side += 2)
                {
                    var center = new Vector2(side * apart, 0f);
                    painter.Bar(center, eye * 2f, eye * 1.25f, 0f, look.Outer);
                    painter.Bar(center + new Vector2(-eye * 0.5f, eye * 0.25f), eye * 0.34f, eye * 0.3f, 0f, Glint);
                    painter.Bar(center + new Vector2(-eye * 0.16f, -eye * 0.05f), eye * 0.34f, eye * 0.3f, 0f, Glint);
                }
            }
            else
            {
                Eye(painter, look, skin, new Vector2(-apart, 0f), eye, false);
                Eye(painter, look, skin, new Vector2(apart, 0f), eye, true);
            }

            return painter.ToMesh("GenesisEyePair");
        }

        /// <summary>
        /// One eye on a disc of skin of that radius. "second" draws the other eye of the pair,
        /// for eyes whose two sides differ.
        /// </summary>
        public static Mesh Single(GenesisEyeLook look, Color skin, float radius, bool second)
        {
            skin = Backing(skin, look);

            var eye = radius * 0.62f;
            var painter = new ShapePainter();
            painter.Ellipse(Vector2.zero, radius, radius, look.Outline);
            painter.Ellipse(Vector2.zero, radius * 0.88f, radius * 0.88f, skin);

            if (look.Glyph == GenesisEyeGlyph.Band)
            {
                // One lens of the goggles.
                painter.Capsule(Vector2.zero, eye * 0.4f, eye * 0.78f, Dark);
                painter.Capsule(Vector2.zero, eye * 0.4f, eye * 0.62f, second && look.Other.a > 0f ? look.Other : look.Outer);
                painter.Bar(Vector2.zero, eye * 0.5f, eye * 1.24f, 0f, look.Core);
            }
            else if (look.Glyph == GenesisEyeGlyph.Shades)
            {
                painter.Bar(Vector2.zero, eye * 2f, eye * 1.25f, 0f, look.Outer);
                painter.Bar(new Vector2(-eye * 0.5f, eye * 0.25f), eye * 0.34f, eye * 0.3f, 0f, Glint);
                painter.Bar(new Vector2(-eye * 0.16f, -eye * 0.05f), eye * 0.34f, eye * 0.3f, 0f, Glint);
            }
            else
            {
                Eye(painter, look, skin, Vector2.zero, eye, second);
            }

            return painter.ToMesh("GenesisEyeBadge");
        }

        private static readonly Color Dark = new Color(0.08f, 0.08f, 0.1f);

        // How close two colours may be, as the sum of their differences in red, green and blue,
        // before an eye no longer stands out on the skin it is drawn on.
        private const float TooClose = 0.35f;

        /// <summary>
        /// The skin an eye is drawn on: the hero's own, a plain face when it is not known, and a
        /// lighter shade of it when the eye has nearly the same colour (a red heart on red skin).
        /// </summary>
        public static Color Backing(Color skin, GenesisEyeLook look)
        {
            if (skin.a <= 0f)
                return PlainSkin;

            var eye = look.Outer.a > 0f ? look.Outer : Dark;
            var difference = Mathf.Abs(skin.r - eye.r) + Mathf.Abs(skin.g - eye.g) + Mathf.Abs(skin.b - eye.b);
            return difference < TooClose ? Color.Lerp(skin, Color.white, 0.6f) : skin;
        }

        // One eye of radius "size". "second" is the right eye of the pair: mirrored where the two
        // eyes of the art mirror each other, and in the other colour when the look names one.
        private static void Eye(ShapePainter painter, GenesisEyeLook look, Color skin, Vector2 center, float size, bool second)
        {
            var side = second ? 1f : -1f;
            var main = look.Outer.a > 0f ? look.Outer : Dark;
            var light = look.Core.a > 0f ? look.Core : Glint;
            switch (look.Glyph)
            {
                case GenesisEyeGlyph.Ring:
                case GenesisEyeGlyph.RingSide:
                case GenesisEyeGlyph.RingDerp:
                    painter.Ellipse(center, size, size, main);
                    painter.Ellipse(center, size * 0.78f, size * 0.78f, light);
                    if (look.Glyph == GenesisEyeGlyph.Ring)
                        painter.Ellipse(center, size * 0.46f, size * 0.46f, main);
                    else if (look.Glyph == GenesisEyeGlyph.RingSide)
                        painter.Ellipse(center + new Vector2(size * 0.36f, -size * 0.05f), size * 0.3f, size * 0.3f, main);
                    else
                        painter.Ellipse(center + new Vector2(side * size * 0.34f, -side * size * 0.3f), size * 0.28f, size * 0.28f, main);
                    break;

                case GenesisEyeGlyph.Cross:
                    painter.Bar(center, size * 2.3f, size * 0.5f, 45f, main);
                    painter.Bar(center, size * 2.3f, size * 0.5f, -45f, main);
                    break;

                case GenesisEyeGlyph.Heart:
                    painter.Heart(center, size * 1.3f, Dark);
                    painter.Heart(center, size * 1.05f, main);
                    painter.Ellipse(center + new Vector2(-size * 0.36f, size * 0.3f), size * 0.18f, size * 0.24f, light);
                    break;

                case GenesisEyeGlyph.Arc:
                    painter.Arc(center + new Vector2(0f, -size * 0.35f), size * 0.62f, size * 1f, 15f, 165f, main);
                    break;

                case GenesisEyeGlyph.Dash:
                    painter.Bar(center, size * 2f, size * 0.36f, 0f, main);
                    break;

                case GenesisEyeGlyph.Chevron:
                    // Squeezed shut: the point of each eye is towards the nose.
                    painter.Bar(center + new Vector2(0f, size * 0.36f), size * 1.8f, size * 0.36f, side * 26f, main);
                    painter.Bar(center + new Vector2(0f, -size * 0.36f), size * 1.8f, size * 0.36f, -side * 26f, main);
                    break;

                default:
                    // A dark ball with two glints; make-up above and below when the look has some.
                    if (look.Other.a > 0f)
                    {
                        painter.Bar(center + new Vector2(0f, size * 1.05f), size * 0.62f, size * 0.62f, 45f, look.Other);
                        painter.Bar(center + new Vector2(0f, -size * 1.05f), size * 0.62f, size * 0.62f, 45f, look.Other);
                    }

                    painter.Ellipse(center, size, size, main);
                    painter.Ellipse(center + new Vector2(size * 0.3f, size * 0.34f), size * 0.27f, size * 0.27f, light);
                    painter.Ellipse(center + new Vector2(-size * 0.3f, -size * 0.28f), size * 0.12f, size * 0.12f, light);
                    if (look.Glyph == GenesisEyeGlyph.BallLidded || look.Glyph == GenesisEyeGlyph.BallAngry)
                    {
                        // The eyelid hides the top of the eye; an angry one slants down to the nose.
                        // It stays inside the patch of skin the eye is drawn on.
                        var slant = look.Glyph == GenesisEyeGlyph.BallAngry ? side * 18f : 0f;
                        painter.Bar(center + new Vector2(0f, size * 0.72f), size * 1.9f, size * 0.76f, slant, skin);
                        painter.Bar(center + new Vector2(0f, size * 0.34f), size * 1.9f, size * 0.2f, slant, Dark);
                    }

                    break;
            }
        }
    }
}
