using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The post-processing of a match, on a computer only (the quality level "Good" or above, which
    /// a desktop browser starts at, QualityDefaults): a fast glow of what is brightest, a light LDR
    /// grading (warm highlights, violet shadows, a little more contrast and colour), a violet vignette
    /// and FXAA, drawn by Resources/ChogMatchPost.shader in five short passes. A phone (level "Fast")
    /// draws nothing more. Put on the arena's camera by ChogArenaSkin; "?matchlook=-post" leaves it
    /// off, "?matchlook=post" forces it at every level, to measure what it costs.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChogMatchPost : MonoBehaviour
    {
        public const string ShaderResource = "ChogMatchPost";

        /// <summary>The cheapest quality level drawn with post-processing, unless forced.</summary>
        public const string FromLevel = "Good";

        private const int PrefilterPass = 0;
        private const int DownPass = 1;
        private const int UpPass = 2;
        private const int FinalPass = 3;

        private Material _material;
        private int _checkedLevel = -1;
        private bool _wanted;

        /// <summary>Puts the effect on the main camera among these scene roots, when the page wants it at all.</summary>
        public static void Attach(GameObject[] roots)
        {
            // A phone never gets the component, not even its pass-through copy of the picture.
            var forced = ChogMatchLook.Has(MatchLookParts.Part.PostForced);
            if (!forced && !(ChogMatchLook.Has(MatchLookParts.Part.Post)
                    && MatchLookParts.PostAllowed(QualitySettings.GetQualityLevel(), QualitySettings.names, FromLevel)))
                return;
            Camera chosen = null;
            foreach (var root in roots)
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                if (chosen == null || camera.CompareTag("MainCamera"))
                    chosen = camera;
            if (chosen != null && chosen.GetComponent<ChogMatchPost>() == null)
                chosen.gameObject.AddComponent<ChogMatchPost>();
        }

        private void Awake()
        {
            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("CHOG_POST the shader Chog/MatchPost is missing or not supported: no post-processing");
                Destroy(this);
                return;
            }

            _material = new Material(shader) { name = "ChogMatchPost", hideFlags = HideFlags.DontSave };
            var tuning = ChogMatchLook.Tuning;
            _material.SetFloat("_Threshold", tuning.BloomThreshold);
            _material.SetFloat("_Knee", 0.15f);
            _material.SetFloat("_BloomIntensity", tuning.Bloom);
            _material.SetFloat("_Contrast", tuning.Contrast);
            _material.SetFloat("_Saturation", tuning.Saturation);
            // Lift: added to the dark, toward violet; gain: the light, toward the key's warmth.
            _material.SetColor("_Lift", new Color(0.035f, 0.015f, 0.06f));
            _material.SetColor("_Gain", new Color(1.03f, 1.0f, 0.95f));
            _material.SetFloat("_Vignette", tuning.Vignette);
            _material.SetColor("_VignetteColor", new Color(0.55f, 0.45f, 0.75f));
        }

        private void Update()
        {
            // The player may change the quality level in the settings during a match.
            var level = QualitySettings.GetQualityLevel();
            if (level == _checkedLevel)
                return;
            _checkedLevel = level;
            _wanted = ChogMatchLook.Has(MatchLookParts.Part.PostForced)
                || MatchLookParts.PostAllowed(level, QualitySettings.names, FromLevel);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!_wanted || _material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            var width = source.width;
            var height = source.height;
            var format = source.format;
            var half = RenderTexture.GetTemporary(Mathf.Max(1, width / 2), Mathf.Max(1, height / 2), 0, format);
            var quarter = RenderTexture.GetTemporary(Mathf.Max(1, width / 4), Mathf.Max(1, height / 4), 0, format);
            var eighth = RenderTexture.GetTemporary(Mathf.Max(1, width / 8), Mathf.Max(1, height / 8), 0, format);

            Graphics.Blit(source, half, _material, PrefilterPass);
            Graphics.Blit(half, quarter, _material, DownPass);
            Graphics.Blit(quarter, eighth, _material, DownPass);
            Graphics.Blit(eighth, quarter, _material, UpPass);
            Graphics.Blit(quarter, half, _material, UpPass);
            _material.SetTexture("_Bloom", half);
            Graphics.Blit(source, destination, _material, FinalPass);

            RenderTexture.ReleaseTemporary(eighth);
            RenderTexture.ReleaseTemporary(quarter);
            RenderTexture.ReleaseTemporary(half);
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
