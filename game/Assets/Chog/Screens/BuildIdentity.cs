using Quantum.Menu;
using UnityEngine;
using UnityEngine.Events;

namespace Chog.Screens
{
    /// <summary>
    /// A build names itself to Photon. Photon only lets pages that give the same version meet,
    /// so two players always run the same simulation; and the game services, which Photon tells
    /// that version with every room, refuse a room of another simulation than the one they serve,
    /// since their judge only replays right what was played with its own simulation.
    /// The name is the stamp of the build's simulation ("sim-" and sixteen hex digits, written by
    /// QualificationBuild into Resources/ChogSimulationStamp): builds that change only screens or
    /// looks keep it, so they open no new version of the application on Photon, which allows an
    /// application only so many in parallel ("Too many application versions are in use", seen on
    /// 17 October 2026 after a day of builds that each named themselves). A build without that
    /// stamp names itself by its own stamp (Application.version) as before. In the Editor it is
    /// "editor": the Editor meets other Editors only. The mode the player chose follows it
    /// ("-siege", "-coins"; MatchModeChoice), so players who chose another mode do not meet either.
    /// </summary>
    public sealed class BuildIdentity : MonoBehaviour
    {
        public const string EditorVersion = "editor";

        // The menu is looked for a few times a second: it comes back with its scene after
        // every match, and a connection must never leave without the name.
        private const float LookEverySeconds = 0.2f;

        private QuantumMenuUIController _named;
        private float _nextLook;

        public const string SimulationResource = "ChogSimulationStamp";

        /// <summary>The build's own stamp (Application.version), as the page and its server compare it.</summary>
        public static string Version => VersionOf(Application.isEditor, Application.version);

        /// <summary>The name given to Photon, before the mode: the simulation's stamp when the build carries one.</summary>
        public static string PhotonVersion => PhotonVersionOf(Application.isEditor, Application.version, Simulation);

        private static string s_simulation;

        private static string Simulation
        {
            get
            {
                if (s_simulation == null)
                {
                    var stamp = Resources.Load<TextAsset>(SimulationResource);
                    s_simulation = stamp != null ? stamp.text.Trim() : "";
                }
                return s_simulation;
            }
        }

        public static string VersionOf(bool inEditor, string buildVersion)
        {
            return inEditor || string.IsNullOrEmpty(buildVersion) ? EditorVersion : buildVersion;
        }

        /// <summary>"sim-" and the simulation's stamp when it is sixteen hex digits, the build's own stamp otherwise.</summary>
        public static string PhotonVersionOf(bool inEditor, string buildVersion, string simulation)
        {
            if (inEditor)
                return EditorVersion;
            return IsStamp(simulation) ? "sim-" + simulation : VersionOf(false, buildVersion);
        }

        private static bool IsStamp(string simulation)
        {
            if (string.IsNullOrEmpty(simulation) || simulation.Length != 16)
                return false;
            foreach (var digit in simulation)
                if (!((digit >= '0' && digit <= '9') || (digit >= 'a' && digit <= 'f')))
                    return false;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Build Identity");
            DontDestroyOnLoad(host);
            host.AddComponent<BuildIdentity>();
            Debug.Log("CHOG_BUILD version " + Version + ", to Photon " + PhotonVersion);
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
                menu.ConnectArgs.AppVersion = PhotonVersion + MatchModeChoice.Suffix;
            menu.Connection.OnBeforeConnect ??= new UnityEvent<QuantumMenuConnectArgs>();
            menu.Connection.OnBeforeConnect.AddListener(Name);
            _named = menu;
        }

        private static void Name(QuantumMenuConnectArgs connection)
        {
            connection.AppVersion = PhotonVersion + MatchModeChoice.Suffix;
        }
    }
}
