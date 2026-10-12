using UnityEngine;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// The real image of a Genesis shown as a round portrait in a screen. Until the image
    /// arrives, and if it never does, the portrait is a plain grey disc, so a missing image is
    /// visible as such. The image itself comes from GenesisArt.
    /// </summary>
    public sealed class GenesisPortrait : MonoBehaviour
    {
        public const string ObjectName = "GenesisPortrait";
        public const string EditorBaseUrl = GenesisArt.EditorBaseUrl;

        private static readonly Color WaitingColor = new Color(0.25f, 0.25f, 0.3f, 0.85f);

        private RawImage _image;
        private int _metadataId;

        /// <summary>Folder of the images for a game page; see GenesisArt.BaseUrl.</summary>
        public static string BaseUrl(string pageUrl) => GenesisArt.BaseUrl(pageUrl);

        /// <summary>
        /// Makes "parent" show the portrait of that Genesis, or none when the id is 0. Safe to call
        /// every frame: nothing happens while the portrait already shown is the right one.
        /// </summary>
        public static void Show(RectTransform parent, int metadataId, float size, Vector2 anchor, Vector2 pivot,
                                Vector2 anchoredPosition)
        {
            var existing = parent.Find(ObjectName);
            var portrait = existing == null ? null : existing.GetComponent<GenesisPortrait>();

            if (metadataId <= 0)
            {
                if (portrait != null)
                    portrait.gameObject.SetActive(false);
                return;
            }

            if (portrait == null)
            {
                var created = new GameObject(ObjectName, typeof(RectTransform), typeof(RawImage));
                created.transform.SetParent(parent, false);
                portrait = created.AddComponent<GenesisPortrait>();
                portrait._image = created.GetComponent<RawImage>();
                portrait._image.raycastTarget = false;
            }

            var rect = (RectTransform)portrait.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(size, size);

            if (!portrait.gameObject.activeSelf)
                portrait.gameObject.SetActive(true);

            if (portrait._metadataId == metadataId)
                return;

            portrait._metadataId = metadataId;
            portrait._image.texture = GenesisArt.Disc();
            portrait._image.color = WaitingColor;
            GenesisArt.Request(metadataId, texture =>
            {
                // The portrait may be gone, or showing another Genesis, by the time the image arrives.
                if (portrait == null || portrait._metadataId != metadataId)
                    return;

                portrait._image.texture = texture;
                portrait._image.color = Color.white;
            });
        }
    }
}
