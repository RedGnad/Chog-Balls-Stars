using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Chog.Match
{
    /// <summary>
    /// Calls to the game services (ownership read, sign-in, careers): JSON over HTTP, in the
    /// "api/" folder next to the game page.
    /// </summary>
    public static class GameServices
    {
        private const string Folder = "api/";

        // Where the Editor finds the game services: the local build server forwards them.
        public const string EditorUrl = "http://127.0.0.1:8083/" + Folder;

        /// <summary>
        /// Folder of the game services for a game page: "api/" next to the page, whatever its
        /// query string; outside a web page the Editor address is used.
        /// </summary>
        public static string Url(string pageUrl)
        {
            var art = GenesisArt.BaseUrl(pageUrl);
            return art == GenesisArt.EditorBaseUrl
                ? EditorUrl
                : art.Substring(0, art.Length - GenesisArt.RelativeFolder.Length) + Folder;
        }

        private const string HeroFolder = "hero/";

        /// <summary>
        /// Address of the "find your hero" page, which sits next to the game page like the
        /// services do.
        /// </summary>
        public static string HeroPageUrl(string pageUrl)
        {
            var services = Url(pageUrl);
            return services.Substring(0, services.Length - Folder.Length) + HeroFolder;
        }

        /// <summary>How a sign-in token is presented to the services.</summary>
        public const string SessionScheme = "Bearer";

        /// <summary>How the pass of a lent Genesis is presented to the services.</summary>
        public const string PassScheme = "Pass";

        /// <summary>
        /// Sends one request and calls "done" with the HTTP status and the answer text. A request
        /// that could not be sent at all answers status 0 and the reason. "body" is JSON for a
        /// POST and null for a GET; "session" is the sign-in token, when the call needs one, or,
        /// with "scheme" set to PassScheme, the pass of a lent Genesis.
        /// </summary>
        public static IEnumerator Call(string path, string body, string session, Action<long, string> done,
                                       string scheme = SessionScheme)
        {
            var url = Url(Application.absoluteURL) + path;
            using (var request = new UnityWebRequest(url, body == null ? UnityWebRequest.kHttpVerbGET : UnityWebRequest.kHttpVerbPOST))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                if (!string.IsNullOrEmpty(session))
                    request.SetRequestHeader("Authorization", scheme + " " + session);

                // Unity throws here, instead of failing the request, when the build refuses plain HTTP.
                UnityWebRequestAsyncOperation sending = null;
                string refusal = null;
                try
                {
                    sending = request.SendWebRequest();
                }
                catch (InvalidOperationException refused)
                {
                    refusal = refused.Message;
                }

                if (sending == null)
                {
                    done(0, refusal);
                    yield break;
                }

                yield return sending;
                var text = request.downloadHandler.text;
                done(request.responseCode, string.IsNullOrEmpty(text) ? request.error ?? "" : text);
            }
        }
    }
}
