using Chog.Match;
using Photon.Realtime;
using Quantum.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Screens
{
    /// <summary>
    /// Tells Photon who the player is when the game connects. Once the game services say that
    /// Photon's cloud asks them (the answer of "api/photon"), the game connects with the session
    /// of the signed-in wallet, or with the pass of a guest, or with nothing for a visitor.
    /// Photon hands it to the services, which name the player and remember, out of the page's
    /// reach, which wallet or which pass it is. From then on the Genesis a player enters a match
    /// with is checked by the services when the player joins, not declared by the page.
    /// While the services do not say so the game connects as it always did.
    /// Until they answer, a build assumes they do, as the public site's services say (a page that
    /// connected with nothing was refused by Photon, "Authentication type 'None' not supported");
    /// the Editor assumes they do not. A question that fails (a server waking or being replaced) is
    /// asked again, a little later each time, until an answer comes: it used to be asked once, and a
    /// player who opened the page during a restart could not play before reloading it.
    /// </summary>
    public sealed class PhotonIdentity : MonoBehaviour
    {
        public const string SessionParameter = "session";
        public const string PassParameter = "pass";

        // The answer of the game services: null until they gave one.
        private static bool? _answer;

        /// <summary>
        /// Whether the game services check who plays: then a Genesis is only played by a wallet that
        /// signed in (or a guest with a pass), and a wallet that is only connected plays a recruit.
        /// Before the services answer: what a build of the public site expects.
        /// </summary>
        public static bool SignInNeededToPlay => AskedOrAssumed(_answer, Application.isEditor);

        /// <summary>The services' answer, or before it what a build ("inEditor" false) assumes: asked.</summary>
        public static bool AskedOrAssumed(bool? answer, bool inEditor) => answer ?? !inEditor;

        /// <summary>Seconds before the question is asked again after its "failures"-th failure (from 1).</summary>
        public static float RetryDelay(int failures) => Mathf.Min(15f, 2f * Mathf.Pow(2f, Mathf.Max(0, failures - 1)));

        // What the menu was last given: whether asked, then the parameter and its value, compared each
        // frame without building a text (the session token is long); not given until the first frame.
        private bool _givenOnce;
        private bool _givenAsked;
        private string _givenName;
        private string _givenValue;

        [System.Serializable]
        private sealed class Answer
        {
            public bool authentication;
        }

        /// <summary>
        /// What is sent to Photon for this player: the wallet's session first, then a guest's
        /// pass, then nothing. The name is the parameter, the value what it carries.
        /// </summary>
        public static (string name, string value) Credential(string session, string pass)
        {
            if (!string.IsNullOrEmpty(session))
                return (SessionParameter, session);

            return !string.IsNullOrEmpty(pass) ? (PassParameter, pass) : (null, null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Photon Identity");
            DontDestroyOnLoad(host);
            var identity = host.AddComponent<PhotonIdentity>();
            // A menu scene loaded again has new connection settings: they are given again.
            SceneManager.sceneLoaded += (scene, mode) => identity._givenOnce = false;
            // Asked on this object: the wallet's own may not exist yet at this point of the start.
            identity.StartCoroutine(identity.Ask());
        }

        private System.Collections.IEnumerator Ask()
        {
            var failures = 0;
            while (_answer == null)
            {
                long status = 0;
                string text = null;
                yield return GameServices.Call("photon", null, null, (code, answer) =>
                {
                    status = code;
                    text = answer;
                });
                Answer read = null;
                if (status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{')
                {
                    try
                    {
                        read = JsonUtility.FromJson<Answer>(text);
                    }
                    catch (System.ArgumentException)
                    {
                        // Not the answer of the services: asked again below.
                    }
                }
                if (read != null)
                {
                    _answer = read.authentication;
                    Debug.Log("CHOG_PHOTON identity " + (read.authentication ? "asked by the game server" : "not asked"));
                    yield break;
                }
                failures++;
                var wait = RetryDelay(failures);
                Debug.LogWarning($"CHOG_PHOTON the game server did not answer ({status}); asked again in {wait:0} s");
                yield return new WaitForSecondsRealtime(wait);
            }
        }

        private void Update()
        {
            var asked = SignInNeededToPlay;
            var (name, value) = asked ? Credential(PlayerWallet.Session, GuestPass.Pass) : (null, null);
            if (_givenOnce && asked == _givenAsked && string.Equals(name, _givenName) && string.Equals(value, _givenValue))
                return;

            var menu = FindAnyObjectByType<QuantumMenuUIController>(FindObjectsInactive.Include);
            if (menu == null || menu.ConnectArgs == null)
                return;

            if (asked)
            {
                var values = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };
                if (name != null)
                    values.AddAuthParameter(name, value);
                menu.ConnectArgs.AuthValues = values;
            }
            else if (_givenOnce)
            {
                // Assumed before the services said they do not ask: the menu connects as it always did.
                menu.ConnectArgs.AuthValues = null;
            }
            _givenOnce = true;
            _givenAsked = asked;
            _givenName = name;
            _givenValue = value;
        }
    }
}
