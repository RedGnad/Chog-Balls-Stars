using System;
using System.Collections;
using System.Collections.Generic;
using Chog.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

namespace Chog.Match
{
    /// <summary>
    /// Loads the real image of a Genesis once and hands the same round texture to everything
    /// that shows it: the portraits of the screens and the face on the hero's head. Images are
    /// small JPEG copies served next to the game ("genesis-art/256/{metadata id}.jpg"), because
    /// Unity cannot decode the WebP originals. When the Genesis was cut out of its background
    /// ("genesis-art/cut/{metadata id}.png", listed in "genesis-art/cut/index.json", made by
    /// tools/genesis-art/cut_out.py), the background is see-through: the Chog shows, not a
    /// coloured disc. A Genesis without a cut keeps its whole round picture.
    /// </summary>
    public sealed class GenesisArt : MonoBehaviour
    {
        public const string RelativeFolder = "genesis-art/256/";
        public const string CutFolder = "genesis-art/cut/";
        private const string CutIndexFile = "index.json";

        // Where the Editor finds the images: the local build server (tools/unity/serve_webgl.py).
        public const string EditorBaseUrl = "http://127.0.0.1:8083/" + RelativeFolder;

        private const int MaxAttempts = 3;
        private const float RetryDelaySeconds = 3f;

        private static readonly Dictionary<int, Texture2D> Loaded = new Dictionary<int, Texture2D>();
        private static readonly Dictionary<int, List<Action<Texture2D>>> Waiting = new Dictionary<int, List<Action<Texture2D>>>();
        private static GenesisArt s_runner;
        private static Texture2D s_disc;

        // The Genesis that have a cut, once the list was read; null before, empty when there is none.
        private static HashSet<int> s_cuts;
        private static bool s_readingCuts;
        private static int s_cutsApplied;

        // Field name is the JSON key of the list.
        [Serializable]
        private sealed class CutIndex
        {
            public int[] ids;
        }

        // Counters behind the status line of the performance overlay.
        private static int s_requests;
        private static int s_failures;
        private static string s_lastError = "none";

        /// <summary>One line for the performance overlay: what was asked for and what arrived.</summary>
        public static string Status
        {
            get
            {
                return "images requests " + s_requests + "  loaded " + Loaded.Count + "  cut out " + s_cutsApplied +
                       "  failed " + s_failures + "  last error " + s_lastError + "\nfrom " + BaseUrl(Application.absoluteURL);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterStatusLine() => PerformanceHud.StatusLines.Add(() => Status);

        /// <summary>
        /// Folder of the images for a game page. They sit next to the page, whatever its query
        /// string; outside a web page the Editor address is used.
        /// </summary>
        public static string BaseUrl(string pageUrl)
        {
            if (string.IsNullOrEmpty(pageUrl) || !pageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return EditorBaseUrl;

            var end = pageUrl.IndexOfAny(new[] { '?', '#' });
            var page = end < 0 ? pageUrl : pageUrl.Substring(0, end);
            var schemeEnd = page.IndexOf("://", StringComparison.Ordinal) + 3;
            var lastSlash = page.LastIndexOf('/');
            var folder = lastSlash < schemeEnd ? page + "/" : page.Substring(0, lastSlash + 1);
            return folder + RelativeFolder;
        }

        /// <summary>Folder of the cuts for a game page: beside the folder of the images.</summary>
        public static string CutBaseUrl(string pageUrl)
        {
            var images = BaseUrl(pageUrl);
            return images.Substring(0, images.Length - RelativeFolder.Length) + CutFolder;
        }

        /// <summary>
        /// Calls "ready" with the round image of that Genesis: at once when it is already loaded,
        /// later otherwise, and never when it cannot be loaded. The caller checks it still exists.
        /// </summary>
        public static void Request(int metadataId, Action<Texture2D> ready)
        {
            if (metadataId <= 0 || ready == null)
                return;

            if (Loaded.TryGetValue(metadataId, out var texture))
            {
                ready(texture);
                return;
            }

            if (Waiting.TryGetValue(metadataId, out var waiting))
            {
                waiting.Add(ready);
                return;
            }

            Waiting[metadataId] = new List<Action<Texture2D>> { ready };
            if (s_runner == null)
            {
                var host = new GameObject("Chog Genesis Art");
                DontDestroyOnLoad(host);
                s_runner = host.AddComponent<GenesisArt>();
            }

            s_runner.StartCoroutine(s_runner.Load(metadataId));
        }

        /// <summary>White disc, to tint, for a place whose image has not arrived.</summary>
        public static Texture2D Disc()
        {
            if (s_disc != null)
                return s_disc;

            const int side = 64;
            var pixels = new Color32[side * side];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            MaskOutsideCircle(pixels, side, side);

            s_disc = new Texture2D(side, side, TextureFormat.RGBA32, true);
            s_disc.wrapMode = TextureWrapMode.Clamp;
            s_disc.SetPixels32(pixels);
            s_disc.Apply(true, true);
            return s_disc;
        }

        private IEnumerator Load(int metadataId)
        {
            Texture2D texture = null;
            var url = BaseUrl(Application.absoluteURL) + metadataId + ".jpg";
            for (var attempt = 1; texture == null && attempt <= MaxAttempts; attempt++)
            {
                s_requests++;
                using (var request = UnityWebRequestTexture.GetTexture(url))
                {
                    // Unity throws here, instead of failing the request, when the build refuses
                    // plain HTTP: without the catch the image would just never come, unexplained.
                    UnityWebRequestAsyncOperation sending = null;
                    try
                    {
                        sending = request.SendWebRequest();
                    }
                    catch (InvalidOperationException refused)
                    {
                        s_failures++;
                        s_lastError = refused.Message;
                        Debug.LogWarning("CHOG_ART refused " + url + " : " + s_lastError);
                    }

                    if (sending == null)
                        break;

                    yield return sending;
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var source = DownloadHandlerTexture.GetContent(request);
                        Texture2D cut = null;
                        yield return LoadCut(metadataId, loaded => cut = loaded);
                        texture = RoundCopy(source, cut);
                        Destroy(source);
                        if (cut != null)
                        {
                            s_cutsApplied++;
                            Destroy(cut);
                        }
                    }
                    else
                    {
                        s_failures++;
                        s_lastError = request.responseCode + " " + request.error;
                        Debug.LogWarning("CHOG_ART failed " + url + " : " + s_lastError);
                    }
                }

                if (texture == null && attempt < MaxAttempts)
                    yield return new WaitForSecondsRealtime(RetryDelaySeconds);
            }

            var waiting = Waiting[metadataId];
            Waiting.Remove(metadataId);
            if (texture == null)
                yield break;

            Loaded[metadataId] = texture;
            foreach (var ready in waiting)
                ready(texture);
        }

        // Reads once which Genesis have a cut, then, for one that has, loads what to keep of its
        // picture (white is kept). "done" receives null for a Genesis without a cut, or when
        // anything fails: the round picture is then shown whole.
        private static IEnumerator LoadCut(int metadataId, Action<Texture2D> done)
        {
            var folder = CutBaseUrl(Application.absoluteURL);
            while (s_readingCuts)
                yield return null;

            if (s_cuts == null)
            {
                s_readingCuts = true;
                var found = new HashSet<int>();
                using (var request = UnityWebRequest.Get(folder + CutIndexFile))
                {
                    yield return Send(request);
                    var text = request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
                    var index = !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<CutIndex>(text) : null;
                    if (index != null && index.ids != null)
                        found.UnionWith(index.ids);
                }

                s_cuts = found;
                s_readingCuts = false;
                Debug.Log("CHOG_ART cuts known for " + found.Count + " Genesis");
            }

            if (!s_cuts.Contains(metadataId))
            {
                done(null);
                yield break;
            }

            using (var request = UnityWebRequestTexture.GetTexture(folder + metadataId + ".png"))
            {
                yield return Send(request);
                done(request.result == UnityWebRequest.Result.Success ? DownloadHandlerTexture.GetContent(request) : null);
            }
        }

        // Sends a request and waits for it; a request the build refuses (plain HTTP) is left unsent.
        private static IEnumerator Send(UnityWebRequest request)
        {
            UnityWebRequestAsyncOperation sending = null;
            try
            {
                sending = request.SendWebRequest();
            }
            catch (InvalidOperationException)
            {
            }

            if (sending != null)
                yield return sending;
        }

        // Copy with everything outside the inscribed circle made transparent, and, when the
        // Genesis has a cut of the same size, its background too; with mipmaps so a 256 px
        // image stays clean when drawn at a few dozen pixels.
        private static Texture2D RoundCopy(Texture2D source, Texture2D cut)
        {
            int width = source.width, height = source.height;
            var pixels = source.GetPixels32();
            MaskOutsideCircle(pixels, width, height);
            if (cut != null && cut.width == width && cut.height == height)
                KeepOnly(pixels, cut.GetPixels32());

            var round = new Texture2D(width, height, TextureFormat.RGBA32, true);
            round.wrapMode = TextureWrapMode.Clamp;
            round.SetPixels32(pixels);
            round.Apply(true, true);
            return round;
        }

        /// <summary>Keeps of a picture only what a cut says to keep: its red channel is how much of each point stays.</summary>
        public static void KeepOnly(Color32[] pixels, Color32[] cut)
        {
            for (var i = 0; i < pixels.Length && i < cut.Length; i++)
                pixels[i].a = (byte)(pixels[i].a * cut[i].r / 255);
        }

        private static void MaskOutsideCircle(Color32[] pixels, int width, int height)
        {
            float centerX = (width - 1) * 0.5f, centerY = (height - 1) * 0.5f;
            var radius = Mathf.Min(width, height) * 0.5f;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    float dx = x - centerX, dy = y - centerY;
                    var inside = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * width + x].a = (byte)(inside * 255f);
                }
            }
        }
    }
}
