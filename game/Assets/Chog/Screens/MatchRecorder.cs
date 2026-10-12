using System.Collections;
using System.Text;
using Chog.Match;
using Quantum;
using Quantum.Menu;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Chog.Screens
{
    /// <summary>
    /// Test tool. On a private page opened with "?record=1" the game keeps the commands of the
    /// match as they were verified and, when the match ends, sends them to the test server next
    /// to the page ("replay-upload") as the engine's replay file. With that file the match can
    /// be played again outside the game (tools/quantum/headless.py verify) to see the same
    /// winner come out of the simulation alone.
    /// In a public match the record that counts is not this one: Photon's server sends its own
    /// to the game services, and a player's page is never asked.
    /// </summary>
    public sealed class MatchRecorder : MonoBehaviour
    {
        public const string QueryParameter = "record";
        private const string UploadPath = "replay-upload";

        /// <summary>Whether the page address asks for the record, where test entries are honoured.</summary>
        public static bool Asked(string url, bool isEditor)
        {
            return LocalGenesis.TestFlag(url, isEditor, QueryParameter);
        }

        /// <summary>Where the record of a page is sent: beside the page, whatever its query.</summary>
        public static string UploadUrl(string pageUrl, int winnerTeam, int lastTick)
        {
            var images = GenesisArt.BaseUrl(pageUrl);
            var folder = images.Substring(0, images.Length - GenesisArt.RelativeFolder.Length);
            return folder + UploadPath + "?winner=" + winnerTeam + "&tick=" + lastTick;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Asked(Application.absoluteURL, Application.isEditor))
                return;

            var host = new GameObject("Chog Match Recorder");
            DontDestroyOnLoad(host);
            host.AddComponent<MatchRecorder>();
            SceneManager.sceneLoaded += (scene, mode) => Arm();
            Arm();
        }

        // The menu starts every match from these arguments: ask for the commands to be kept.
        private static void Arm()
        {
            var menu = FindAnyObjectByType<QuantumMenuUIController>(FindObjectsInactive.Include);
            if (menu != null && menu.ConnectArgs != null)
                menu.ConnectArgs.RecordingFlags |= RecordingFlags.Input;
        }

        private void Start() => QuantumEvent.Subscribe<EventGameOver>(this, OnGameOver);

        private void OnGameOver(EventGameOver e) => StartCoroutine(Send(e.Game, e.WinnerTeam));

        private static IEnumerator Send(QuantumGame game, int winnerTeam)
        {
            // One more image, so the verified frame holds the end of the match.
            yield return null;

            var replay = game.GetRecordedReplay();
            if (replay == null)
            {
                Debug.LogWarning("CHOG_REPLAY not sent: the match was not recorded");
                yield break;
            }

            var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(replay));
            var url = UploadUrl(Application.absoluteURL, winnerTeam, replay.LastTick);
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                Debug.Log(request.result == UnityWebRequest.Result.Success
                    ? "CHOG_REPLAY sent " + body.Length + " bytes, winner team " + winnerTeam + ", last tick " + replay.LastTick
                    : "CHOG_REPLAY not sent: " + request.responseCode + " " + request.error);
            }
        }
    }
}
