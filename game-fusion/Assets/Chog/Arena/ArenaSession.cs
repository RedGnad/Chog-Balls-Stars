using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Arena
{
    // P0 transport/combat harness. NFT admission is implemented in a later milestone.
    public sealed class ArenaSession : MonoBehaviour, INetworkRunnerCallbacks
    {
        public NetworkRunner Runner { get; private set; }
        public string Status { get; private set; } = "Prêt";
        public bool RuntimeStarted { get; private set; }
        public Func<ArenaInput> InputOverride;
        private readonly HashSet<PlayerRef> _pending = new HashSet<PlayerRef>();
        private int _spawnOrdinal;
        private bool _botSpawned;
        private ArenaControls _controls;

        public async Task<StartGameResult> StartSession(GameMode mode, string room, NetworkProjectConfig config = null)
        {
            if (Runner != null) throw new InvalidOperationException("Quitter la session avant de rejoindre une partie.");
            _controls?.BeginSession();
            if (mode != GameMode.Server && mode != GameMode.Client && mode != GameMode.Single)
                throw new ArgumentException("Only Server, Client and offline Single are supported.");
            Status = "Connexion…";
            var runnerObject = new GameObject("Fusion " + mode);
            Runner = runnerObject.AddComponent<NetworkRunner>();
            Runner.ProvideInput = mode != GameMode.Server;
            Runner.AddCallbacks(this);
            var sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();
            var sceneInfo = new NetworkSceneInfo();
            // NetworkSceneManagerDefault creates the isolated physics world itself in MultiPeer.
            sceneInfo.AddSceneRef(SceneRef.FromIndex(1), LoadSceneMode.Additive);
            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = room,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                Config = config,
                PlayerCount = 6,
                IsVisible = false,
                EnableClientSessionCreation = false
            });
            Status = result.Ok ? (mode == GameMode.Server ? "Serveur prêt" : "Connecté") : result.ShutdownReason.ToString();
            if (!result.Ok)
            {
                Destroy(runnerObject);
                Runner = null;
            }
            return result;
        }

        public async Task StopSession()
        {
            var runner = Runner;
            if (runner != null) await runner.Shutdown();
            Runner = null;
            _pending.Clear();
            _spawnOrdinal = 0;
            _botSpawned = false;
            Status = "Déconnecté";
        }

        private void Start()
        {
            RuntimeStarted = true;
            if (!Application.isBatchMode) _controls = GetComponent<ArenaControls>();
        }

        private void Update()
        {
            if (Runner == null || !Runner.IsRunning || !Runner.IsServer || Runner.SceneManager.IsBusy) return;
            foreach (var player in _pending)
            {
                if (Runner.TryGetPlayerObject(player, out _)) continue;
                int slot = _spawnOrdinal++;
                byte team = (byte)(slot % 2);
                var point = new Vector3(team == 0 ? -4 : 4, 0.05f, (slot / 2 - 1) * 2);
                var prefab = Resources.Load<NetworkObject>("Chog/Hero");
                var hero = Runner.Spawn(prefab, point, Quaternion.identity, player, (r, obj) =>
                {
                    var state = obj.GetComponent<ArenaHero>();
                    state.Team = team;
                    state.SpawnPoint = point;
                });
                Runner.SetPlayerObject(player, hero);
            }
            _pending.Clear();
            if (Runner.GameMode == GameMode.Single && !_botSpawned && _spawnOrdinal > 0)
            {
                var botPoint = new Vector3(4, .05f, 0);
                var bot = Runner.Spawn(Resources.Load<NetworkObject>("Chog/Hero"), botPoint, Quaternion.Euler(0, 180, 0),
                    onBeforeSpawned: (runner, obj) =>
                    {
                        var state = obj.GetComponent<ArenaHero>();
                        state.Team = 1;
                        state.IsBot = true;
                        state.SpawnPoint = botPoint;
                    });
                _botSpawned = bot != null;
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer && player != PlayerRef.None) _pending.Add(player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            _pending.Remove(player);
            if (runner.IsServer && runner.TryGetPlayerObject(player, out var obj)) runner.Despawn(obj);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(InputOverride != null ? InputOverride() : (_controls != null ? _controls.Read(runner) : default));
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { Status = "Déconnecté : " + reason; }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Status = "Connexion perdue : " + reason; }
        public void OnConnectFailed(NetworkRunner runner, NetAddress address, NetConnectFailedReason reason) { Status = "Échec : " + reason; }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { request.Accept(); }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { input.Set(default(ArenaInput)); }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessions) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}
