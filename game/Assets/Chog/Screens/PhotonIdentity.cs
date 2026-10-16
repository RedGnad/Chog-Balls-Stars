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
    /// </summary>
    public sealed class PhotonIdentity : MonoBehaviour
    {
        public const string SessionParameter = "session";
        public const string PassParameter = "pass";

        private static bool _asked;

        /// <summary>
        /// Whether the game services check who plays: then a Genesis is only played by a wallet that
        /// signed in (or a guest with a pass), and a wallet that is only connected plays a recruit.
        /// </summary>
        public static bool SignInNeededToPlay => _asked;
        // What the menu was last given: null until then, so that the first frame applies it.
        private string _given;

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
            SceneManager.sceneLoaded += (scene, mode) => identity._given = null;
            // Asked on this object: the wallet's own may not exist yet at this point of the start.
            identity.StartCoroutine(GameServices.Call("photon", null, null, (status, text) =>
            {
                var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<Answer>(text) : null;
                _asked = answer != null && answer.authentication;
                Debug.Log("CHOG_PHOTON identity " + (_asked ? "asked by the game server" : "not asked"));
            }));
        }

        private void Update()
        {
            if (!_asked)
                return;

            var (name, value) = Credential(PlayerWallet.Session, GuestPass.Pass);
            var wanted = name + "=" + value;
            if (wanted == _given)
                return;

            var menu = FindAnyObjectByType<QuantumMenuUIController>(FindObjectsInactive.Include);
            if (menu == null || menu.ConnectArgs == null)
                return;

            var values = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };
            if (name != null)
                values.AddAuthParameter(name, value);

            menu.ConnectArgs.AuthValues = values;
            _given = wanted;
        }
    }
}
