using Quantum.Menu;
using UnityEngine;
using UnityEngine.Events;

namespace Chog.Screens
{
    /// <summary>
    /// A build names itself to Photon. Photon only lets pages that give the same version meet,
    /// so two players always run the same simulation; and the game services, which Photon tells
    /// that version with every room, refuse a room of another build than the one they serve,
    /// since their judge only replays right what was played with its own simulation.
    /// The name is the stamp the build script gives each build (Application.version, set in
    /// QualificationBuild). In the Editor it is "editor": the Editor meets other Editors only.
    /// </summary>
    public sealed class BuildIdentity : MonoBehaviour
    {
        public const string EditorVersion = "editor";

        // The menu is looked for a few times a second: it comes back with its scene after
        // every match, and a connection must never leave without the name.
        private const float LookEverySeconds = 0.2f;

        private QuantumMenuUIController _named;
        private float _nextLook;

        public static string Version => VersionOf(Application.isEditor, Application.version);

        public static string VersionOf(bool inEditor, string buildVersion)
        {
            return inEditor || string.IsNullOrEmpty(buildVersion) ? EditorVersion : buildVersion;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Build Identity");
            DontDestroyOnLoad(host);
            host.AddComponent<BuildIdentity>();
            Debug.Log("CHOG_BUILD version " + Version);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextLook)
                return;
            _nextLook = Time.unscaledTime + LookEverySeconds;

            var menu = FindAnyObjectByType<QuantumMenuUIController>(FindObjectsInactive.Include);
            if (menu == null || menu == _named || menu.Connection == null)
                return;

            // Given now, for whoever reads the settings, and again at every connection: the
            // menu's own settings screen can write another version over it.
            if (menu.ConnectArgs != null)
                menu.ConnectArgs.AppVersion = Version;
            menu.Connection.OnBeforeConnect ??= new UnityEvent<QuantumMenuConnectArgs>();
            menu.Connection.OnBeforeConnect.AddListener(Name);
            _named = menu;
        }

        private static void Name(QuantumMenuConnectArgs connection)
        {
            connection.AppVersion = Version;
        }
    }
}
