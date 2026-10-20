using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The building blocks the newer screens are made of (docs/DESIGN-BRIEF.md): areas placed in shares
    /// of their parent or at a fixed size from a corner, outlined titles and plain sentences, chunky
    /// buttons with a pictogram, small outlined plates for chips and badges, and progress bars. Sizes
    /// are in units of the menu canvas (1920 by 1080 at 16:9).
    /// </summary>
    public static class ChogUi
    {
        /// <summary>An empty area stretched between two points given in shares of its parent.</summary>
        public static RectTransform Area(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var area = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            area.SetParent(parent, false);
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            return area;
        }

        /// <summary>
        /// An empty area of a fixed size, pinned to a point of its parent ("anchor", in shares) by its
        /// own point "pivot", moved by "position" units.
        /// </summary>
        public static RectTransform Pinned(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var area = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            area.SetParent(parent, false);
            Pin(area, anchor, pivot, position, size);
            return area;
        }

        /// <summary>Moves an existing area to a fixed size and place, as Pinned makes one.</summary>
        public static void Pin(RectTransform area, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            area.anchorMin = anchor;
            area.anchorMax = anchor;
            area.pivot = pivot;
            area.sizeDelta = size;
            area.anchoredPosition = position;
        }

        /// <summary>Moves an existing area between two points in shares of its parent.</summary>
        public static void Stretch(RectTransform area, Vector2 min, Vector2 max)
        {
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            area.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>A title in Lilita One, outlined in ink, at most "largest" units high, filling its area.</summary>
        public static TextMeshProUGUI Title(string name, Transform parent, Vector2 min, Vector2 max, float largest,
                                            TextAlignmentOptions alignment = TextAlignmentOptions.Center, Color? colour = null)
        {
            var text = Words(name, parent, min, max, largest, alignment);
            ChogTheme.AsTitle(text);
            text.color = colour ?? Color.white;
            return text;
        }

        /// <summary>A sentence in Nunito.</summary>
        public static TextMeshProUGUI Body(string name, Transform parent, Vector2 min, Vector2 max, float largest,
                                           TextAlignmentOptions alignment = TextAlignmentOptions.Left, Color? colour = null)
        {
            var text = Words(name, parent, min, max, largest, alignment);
            ChogTheme.AsBody(text);
            text.color = colour ?? ChogTheme.Text;
            return text;
        }

        /// <summary>A number in Nunito Black, outlined unless asked otherwise.</summary>
        public static TextMeshProUGUI Number(string name, Transform parent, Vector2 min, Vector2 max, float largest,
                                             TextAlignmentOptions alignment = TextAlignmentOptions.Center, Color? colour = null,
                                             bool outlined = true)
        {
            var text = Words(name, parent, min, max, largest, alignment);
            ChogTheme.AsNumber(text, outlined);
            text.color = colour ?? Color.white;
            return text;
        }

        private static TextMeshProUGUI Words(string name, Transform parent, Vector2 min, Vector2 max, float largest,
                                             TextAlignmentOptions alignment)
        {
            var text = Area(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.richText = true;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = largest;
            return text;
        }

        /// <summary>
        /// A chunky button of that kind filling an area, with a label, a pictogram, or a pictogram over a
        /// label. "label" may be empty.
        /// </summary>
        public static Button Button(string name, Transform parent, Vector2 min, Vector2 max, ChogTheme.ButtonKind kind,
                                    string label, float largest, ChogIcons.Icon? icon = null)
        {
            var area = Area(name, parent, min, max);
            return Dress(area, kind, label, largest, icon);
        }

        /// <summary>A chunky button of a fixed size, pinned as Pinned places an area.</summary>
        public static Button PinnedButton(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
                                          ChogTheme.ButtonKind kind, string label, float largest, ChogIcons.Icon? icon = null)
        {
            var area = Pinned(name, parent, anchor, pivot, position, size);
            return Dress(area, kind, label, largest, icon);
        }

        private static Button Dress(RectTransform area, ChogTheme.ButtonKind kind, string label, float largest, ChogIcons.Icon? icon)
        {
            var image = area.gameObject.AddComponent<Image>();
            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var hasLabel = !string.IsNullOrEmpty(label);
            if (icon.HasValue)
            {
                // Alone, the pictogram takes the middle; over a label, the upper part.
                var picture = Icon("Icon", area, icon.Value, hasLabel ? new Vector2(0.2f, 0.42f) : new Vector2(0.18f, 0.16f),
                    hasLabel ? new Vector2(0.8f, 0.92f) : new Vector2(0.82f, 0.88f));
                picture.raycastTarget = false;
            }
            if (hasLabel)
            {
                var text = Title("Label", area, icon.HasValue ? new Vector2(0.06f, 0.08f) : new Vector2(0.05f, 0.12f),
                    icon.HasValue ? new Vector2(0.94f, 0.42f) : new Vector2(0.95f, 0.9f), largest);
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.text = label;
            }
            ChogTheme.Style(button, kind);
            return button;
        }

        /// <summary>The label of a button made here.</summary>
        public static TextMeshProUGUI LabelOf(Button button)
        {
            var label = button.transform.Find("Label");
            return label != null ? label.GetComponent<TextMeshProUGUI>() : null;
        }

        /// <summary>A pictogram (ChogIcons) kept square inside its area.</summary>
        public static Image Icon(string name, Transform parent, ChogIcons.Icon icon, Vector2 min, Vector2 max)
        {
            var image = Area(name, parent, min, max).gameObject.AddComponent<Image>();
            // The Chog mark has a real collection-derived outline and is rasterised at double resolution.
            image.sprite = ChogIcons.Get(icon, icon == ChogIcons.Icon.Heroes ? 256 : 128);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A card as ChogTheme draws them.</summary>
        public static Image Card(string name, Transform parent, Vector2 min, Vector2 max, bool raised = false)
        {
            var image = Area(name, parent, min, max).gameObject.AddComponent<Image>();
            ChogTheme.AsCard(image, raised);
            return image;
        }

        /// <summary>
        /// A small plate in "fill" with the ink outline of the buttons, for chips, badges and tiles; no
        /// lip and no press. Its image is tinted: change its colour to change the plate's.
        /// </summary>
        public static Image Plate(string name, Transform parent, Vector2 min, Vector2 max, Color fill, int radius = 12)
        {
            var image = Area(name, parent, min, max).gameObject.AddComponent<Image>();
            image.sprite = ChogSprites.Rounded(radius, 3, ChogTheme.Ink);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.8f;
            image.color = fill;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A bar that fills from the left: an ink trough and a coloured fill, its share set by SetBar.</summary>
        public static RectTransform Bar(string name, Transform parent, Vector2 min, Vector2 max, Color fill)
        {
            var trough = Plate(name, parent, min, max, ChogTheme.Ink, 8);
            var level = Plate("Fill", trough.transform, Vector2.zero, new Vector2(0f, 1f), fill, 8);
            level.rectTransform.offsetMin = new Vector2(2f, 2f);
            level.rectTransform.offsetMax = new Vector2(-2f, -2f);
            return trough.rectTransform;
        }

        /// <summary>Fills a bar made by Bar to a share between 0 and 1.</summary>
        public static void SetBar(RectTransform bar, float share)
        {
            var fill = (RectTransform)bar.Find("Fill");
            if (fill == null)
                return;
            share = Mathf.Clamp01(share);
            fill.anchorMax = new Vector2(share, 1f);
            fill.gameObject.SetActive(share > 0.001f);
        }

        /// <summary>An area that lays its children side by side at their own width, centred or from the left.</summary>
        public static RectTransform Row(string name, Transform parent, Vector2 min, Vector2 max, float spacing,
                                        TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var area = Area(name, parent, min, max);
            var row = area.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = alignment;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return area;
        }

        /// <summary>
        /// A chip for a Row: a dark rounded plate with a lavender line, as wide as its words, which are
        /// written at a fixed "size". Returns the words; change them with SetChip so the chip follows.
        /// </summary>
        public static TextMeshProUGUI Chip(string name, Transform row, string words, float size, Color? line = null)
        {
            var chip = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            chip.transform.SetParent(row, false);
            chip.sprite = ChogSprites.Rounded(16, 3, Color.white, new Color(0.08f, 0.06f, 0.14f, 0.78f));
            chip.type = Image.Type.Sliced;
            chip.pixelsPerUnitMultiplier = 0.9f;
            chip.color = line ?? ChogTheme.PurpleSoft;
            chip.raycastTarget = false;
            chip.gameObject.AddComponent<LayoutElement>();
            var text = Area("Text", chip.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            ChogTheme.AsNumber(text, false);
            text.color = Color.white;
            SetChip(text, words);
            return text;
        }

        /// <summary>Changes the words of a chip made by Chip, and its width with them; no words hide it.</summary>
        public static void SetChip(TextMeshProUGUI text, string words)
        {
            text.text = words ?? "";
            var chip = text.transform.parent;
            chip.gameObject.SetActive(text.text.Length > 0);
            var size = text.GetPreferredValues(text.text);
            var element = chip.GetComponent<LayoutElement>();
            element.preferredWidth = size.x + text.fontSize * 1.5f;
            element.preferredHeight = text.fontSize * 1.9f;
        }

        /// <summary>A dim layer over a whole screen that catches the taps meant for what it covers.</summary>
        public static Image Dim(string name, Transform screen, float alpha = 0.82f)
        {
            var dim = Area(name, screen, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            dim.color = new Color(ChogTheme.Night.r, ChogTheme.Night.g, ChogTheme.Night.b, alpha);
            return dim;
        }
    }
}
