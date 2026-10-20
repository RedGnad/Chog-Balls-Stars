using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Shapes the screens are drawn with, made in code so that no picture has to be imported: a
    /// rounded panel with a dark outline (sliced, so one sprite serves any size), a disc, and a
    /// vertical gradient. Each is made once and kept.
    /// </summary>
    public static class ChogSprites
    {
        private static readonly Dictionary<string, Sprite> s_made = new Dictionary<string, Sprite>();

        /// <summary>
        /// A rounded rectangle filled with "fill" (white when left out, so that the colour of the Image
        /// tints it) with an outline of "outline" pixels in "outlineColour", for an Image of type Sliced.
        /// Radius and outline are in pixels of the texture, shown at one pixel per unit of the canvas.
        /// </summary>
        public static Sprite Rounded(int radius, int outline, Color outlineColour, Color? fill = null)
        {
            var inside = fill ?? Color.white;
            var key = $"rounded {radius} {outline} {ColorUtility.ToHtmlStringRGBA(outlineColour)} {ColorUtility.ToHtmlStringRGBA(inside)}";
            if (s_made.TryGetValue(key, out var kept) && kept != null)
                return kept;

            var size = radius * 2 + 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var centre = (size - 1) * 0.5f;
            var half = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Distance outside the rounded square of half side "half - 1" and corner "radius".
                    var dx = Mathf.Max(Mathf.Abs(x - centre) - (half - 1f - radius), 0f);
                    var dy = Mathf.Max(Mathf.Abs(y - centre) - (half - 1f - radius), 0f);
                    var edge = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    var cover = Mathf.Clamp01(0.5f - edge);
                    var inner = Mathf.Clamp01(0.5f - (edge + outline));
                    var colour = Color.Lerp(outlineColour, inside, inner);
                    colour.a *= cover;
                    pixels[y * size + x] = colour;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var border = radius + 2;
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = key;
            s_made[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// A chunky button in the Supercell way (docs/DESIGN-BRIEF.md): a rounded rectangle with a near-black
        /// outline, its colour, and a darker lip along the bottom that gives it thickness. Sliced: the corners,
        /// the outline and the lip keep their size whatever the button's.
        /// </summary>
        public static Sprite Chunky(Color fill, Color lip, int radius = 14, int outline = 3, int lipHeight = 7)
        {
            var key = $"chunky {ColorUtility.ToHtmlStringRGBA(fill)} {ColorUtility.ToHtmlStringRGBA(lip)} {radius} {outline} {lipHeight}";
            if (s_made.TryGetValue(key, out var kept) && kept != null)
                return kept;

            var ink = new Color(0.078f, 0.059f, 0.141f, 1f);
            var width = radius * 2 + 8;
            var height = radius * 2 + lipHeight + 8;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[width * height];
            var halfW = width * 0.5f;
            var halfH = height * 0.5f;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = Mathf.Max(Mathf.Abs(x + 0.5f - halfW) - (halfW - radius), 0f);
                    var dy = Mathf.Max(Mathf.Abs(y + 0.5f - halfH) - (halfH - radius), 0f);
                    var edge = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    var cover = Mathf.Clamp01(0.5f - edge);
                    var inner = Mathf.Clamp01(0.5f - (edge + outline));
                    // A single painted surface, slightly brighter toward its upper edge, ends in one
                    // darker lower lip. It reads as a physical button without an added band or decoration.
                    var faceAmount = Mathf.InverseLerp(outline + lipHeight, height - outline, y);
                    var face = Color.Lerp(fill, Color.white, 0.18f * faceAmount);
                    var body = y < outline + lipHeight ? lip : face;
                    var colour = Color.Lerp(ink, body, inner);
                    colour.a *= cover;
                    pixels[y * width + x] = colour;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var side = radius + 3;
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(side, side + lipHeight, side, side));
            sprite.name = key;
            s_made[key] = sprite;
            return sprite;
        }

        /// <summary>A white shape rounded at its two top corners only, square below: the light band of a chunky button.</summary>
        public static Sprite TopRounded(int radius)
        {
            var key = $"toprounded {radius}";
            if (s_made.TryGetValue(key, out var kept) && kept != null)
                return kept;

            var size = radius * 2 + 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var half = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                    // Only the top half is rounded.
                    var dy = Mathf.Max(y + 0.5f - half - (half - radius), 0f);
                    var edge = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - edge));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var border = radius + 2;
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = key;
            s_made[key] = sprite;
            return sprite;
        }

        /// <summary>A white disc with a soft edge, for rings, pedestals and shadows.</summary>
        public static Sprite Disc(int radius, float softness)
        {
            var key = $"disc {radius} {softness}";
            if (s_made.TryGetValue(key, out var kept) && kept != null)
                return kept;

            var size = radius * 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = key, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = new Vector2(x + 0.5f - radius, y + 0.5f - radius).magnitude / radius;
                    var alpha = Mathf.Clamp01((1f - distance) / Mathf.Max(0.001f, softness));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            s_made[key] = sprite;
            return sprite;
        }

        /// <summary>A vertical gradient from "bottom" to "top", stretched over whatever shows it.</summary>
        public static Sprite Gradient(Color bottom, Color top)
        {
            var key = $"gradient {ColorUtility.ToHtmlStringRGBA(bottom)} {ColorUtility.ToHtmlStringRGBA(top)}";
            if (s_made.TryGetValue(key, out var kept) && kept != null)
                return kept;

            const int height = 64;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                name = key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            for (var y = 0; y < height; y++)
                texture.SetPixel(0, y, Color.Lerp(bottom, top, y / (height - 1f)));
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            s_made[key] = sprite;
            return sprite;
        }
    }
}
