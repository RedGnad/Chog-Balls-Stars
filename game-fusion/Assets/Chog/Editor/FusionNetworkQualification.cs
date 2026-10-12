using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chog.Arena;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chog.Editor
{
    [InitializeOnLoad]
    public static class FusionNetworkQualification
    {
        private const string Requested = "Chog.Fusion.NetworkQualification";
        [Serializable] private sealed class Report
        {
            public string recordedAtUtc, unityVersion, sdk, result, error, room;
            public string scenario = "One GameMode.Server and two independent GameMode.Client runners in one headless Unity Editor process; real Photon session, separate physics scenes; not a browser/phone test.";
            public List<string> passedChecks = new List<string>();
            public float authoritativeMovementDistance;
            public int authoritativeShots, authoritativeReceivedHits;
        }

        private static double _readySince;
        static FusionNetworkQualification()
        {
            _readySince = EditorApplication.timeSinceStartup;
            EditorApplication.update += StartWhenReady;
        }

        [MenuItem("Chog/Qualification/Tester serveur et deux clients Photon")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before starting qualification.");
            EditorSceneManager.OpenScene(FusionProjectSetup.Boot);
            SessionState.SetBool(Requested, true);
            EditorApplication.isPlaying = true;
        }

        private static void StartWhenReady()
        {
            if (!SessionState.GetBool(Requested, false)) return;
            var boot = UnityEngine.Object.FindFirstObjectByType<ArenaSession>();
            if (!EditorApplication.isPlaying || !Application.isPlaying || boot == null || !boot.RuntimeStarted
                || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                _readySince = EditorApplication.timeSinceStartup;
                return;
            }
            // Importing the project config can schedule a second weave/compilation.
            // Starting a runner before it settles makes Fusion's recompile hook shut it down.
            if (EditorApplication.timeSinceStartup - _readySince < 3) return;
            SessionState.SetBool(Requested, false);
            RunProbe();
        }

        private static async void RunProbe()
        {
            var report = new Report
            {
                recordedAtUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                sdk = "Fusion 2.1.3 build 2390", result = "Failed", room = "chog-qual-" + Guid.NewGuid().ToString("N").Substring(0, 12)
            };
            var sessions = new List<ArenaSession>();
            var config = NetworkProjectConfig.Global;
            var originalPeerMode = config.PeerMode;
            var originalRenderMode = config.InvokeRenderMode;
            try
            {
                Debug.Log("CHOG_FUSION_RUNTIME_READY: " + Application.isPlaying + "; scene manager initialized: "
                    + (typeof(NetworkSceneManagerDefault).GetField("_allOwnedScenes", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null) != null));
                config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                // These headless clients need Render to update interpolated proxy transforms.
                config.InvokeRenderMode = NetworkProjectConfig.InvokeRenderModes.Always;
                var server = NewSession("Qualification Server", sessions);
                var first = NewSession("Qualification Client A", sessions);
                var second = NewSession("Qualification Client B", sessions);
                Require((await server.StartSession(GameMode.Server, report.room, config)).Ok, "Server start failed.");
                Require((await first.StartSession(GameMode.Client, report.room, config)).Ok, "Client A join failed.");
                Require((await second.StartSession(GameMode.Client, report.room, config)).Ok, "Client B join failed.");
                await Until(() => server.Runner.ActivePlayers.Count() == 2 && Hero(server, first.Runner.LocalPlayer) != null
                    && Hero(server, second.Runner.LocalPlayer) != null && Hero(first, second.Runner.LocalPlayer) != null
                    && Hero(second, first.Runner.LocalPlayer) != null, "Both avatars must replicate to both clients.");
                Require(server.Runner.LocalPlayer == PlayerRef.None, "Dedicated server must not own a player slot.");
                Require(first.Runner.LocalPlayer != second.Runner.LocalPlayer, "Clients must have different player identities.");
                Require(!server.Runner.GetPhysicsScene().Equals(first.Runner.GetPhysicsScene())
                    && !first.Runner.GetPhysicsScene().Equals(second.Runner.GetPhysicsScene()), "Peers must use separate physics worlds.");
                report.passedChecks.Add("Real Photon room: dedicated server plus two clients, no server player slot, isolated peer physics scenes.");

                PlayerRef playerA = first.Runner.LocalPlayer, playerB = second.Runner.LocalPlayer;
                var actorA = Hero(server, playerA);
                var actorB = Hero(server, playerB);
                Require(actorA.HasStateAuthority && !Hero(first, playerA).HasStateAuthority, "Combat authority must remain on server.");
                Require(Hero(first, playerA).HasInputAuthority && !Hero(first, playerB).HasInputAuthority, "Input ownership must follow player.");
                report.passedChecks.Add("Server owns authoritative heroes; clients own only their own input.");
                var initial = actorA.transform.position;
                first.InputOverride = () => new ArenaInput { Move = Vector2.up, Aim = Vector3.right, Valid = true };
                await Until(() => Vector3.Distance(initial, actorA.transform.position) > .7f, "Client input must move authoritative hero.");
                first.InputOverride = () => new ArenaInput { Aim = Vector3.right, Valid = true };
                await Task.Delay(900);
                report.authoritativeMovementDistance = Vector3.Distance(initial, actorA.transform.position);
                await Until(() => Vector3.Distance(actorA.transform.position, Hero(second, playerA).transform.position) < .35f,
                    "Remote client must receive authoritative movement.");
                report.passedChecks.Add("Client input moves the server hero; other client converges to the same position.");

                first.InputOverride = () => new ArenaInput { Aim = actorB.transform.position - actorA.transform.position,
                    PrimaryRelease = 1, Valid = true };
                await Until(() => actorB.ReceivedHits > 0, "Server projectile must damage opposing hero.");
                first.InputOverride = () => new ArenaInput { Aim = Vector3.right, PrimaryRelease = 1, Valid = true };
                await Until(() => Hero(second, playerB).ReceivedHits == actorB.ReceivedHits
                    && Hero(first, playerB).Health == actorB.Health, "Server damage must replicate to both clients.");
                report.authoritativeShots = actorA.ShotsFired;
                report.authoritativeReceivedHits = actorB.ReceivedHits;
                report.passedChecks.Add("Server creates projectile and applies hit; both clients receive authoritative damage.");

                await Until(() => actorA.ShotsFired >= ArcherRules.BasicCount,
                    "The released primary cast must emit seven arrows.");
                Require(actorA.PrimaryCasts == 1, "A held release counter must create exactly one volley.");
                report.passedChecks.Add("A release creates one seven-arrow volley, with no duplicate casts from repeated network input.");
                await Until(() => actorB.Health > 0, "Target must be alive for the area attack check.");
                await Task.Delay(1100);
                int beforeAreaHits = actorB.ReceivedHits;
                first.InputOverride = () => new ArenaInput { Aim = actorB.transform.position - actorA.transform.position,
                    PrimaryRelease = 1, SpecialRelease = 1, Valid = true };
                await Until(() => actorA.SpecialCasts == 1, "Released special must be accepted by the server.");
                await Until(() => actorB.ReceivedHits > beforeAreaHits,
                    "Ballistic special must leave a server-owned damaging area.");
                await Until(() => Hero(first, playerA).SpecialCasts == 1 &&
                    Hero(second, playerB).ReceivedHits == actorB.ReceivedHits,
                    "Special cast and area damage must replicate to both clients.");
                first.InputOverride = () => new ArenaInput { Aim = Vector3.right,
                    PrimaryRelease = 1, SpecialRelease = 1, Valid = true };
                report.passedChecks.Add("Ballistic special spawns an authoritative damage zone observed by both clients.");

                await first.StopSession();
                await Until(() => server.Runner.ActivePlayers.Count() == 1 && Hero(server, playerA) == null && Hero(second, playerA) == null,
                    "Disconnect must remove authoritative and remote hero.");
                report.passedChecks.Add("Leaving client removes its hero on server and remaining client.");
                var replacement = NewSession("Qualification Client A fresh join", sessions);
                Require((await replacement.StartSession(GameMode.Client, report.room, config)).Ok, "Fresh join after disconnect failed.");
                await Until(() => server.Runner.ActivePlayers.Count() == 2 && Hero(server, replacement.Runner.LocalPlayer) != null
                    && Hero(second, replacement.Runner.LocalPlayer) != null, "Fresh join must spawn and replicate new hero.");
                report.passedChecks.Add("New client can join the same surviving server session after disconnect (fresh join, not authenticated resume).");
                report.result = "Passed";
            }
            catch (Exception e) { report.error = e.ToString(); Debug.LogException(e); }
            finally
            {
                foreach (var session in sessions.AsEnumerable().Reverse())
                {
                    try
                    {
                        var cleanup = session.StopSession();
                        if (await Task.WhenAny(cleanup, Task.Delay(10000)) != cleanup)
                            throw new TimeoutException("Runner shutdown exceeded 10 seconds.");
                        await cleanup;
                    }
                    catch (Exception e) { Debug.LogWarning("Qualification cleanup: " + e.Message); }
                }
                config.PeerMode = originalPeerMode;
                config.InvokeRenderMode = originalRenderMode;
                var root = Directory.GetParent(Application.dataPath).Parent.FullName;
                var directory = Path.Combine(root, ".artifacts/fusion"); Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "latest-network-qualification.json"), JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("CHOG_FUSION_NETWORK_QUALIFICATION: " + report.result);
                if (Application.isBatchMode) EditorApplication.Exit(report.result == "Passed" ? 0 : 1);
                else EditorApplication.isPlaying = false;
            }
        }

        private static ArenaSession NewSession(string name, List<ArenaSession> sessions)
        {
            var session = new GameObject(name).AddComponent<ArenaSession>(); sessions.Add(session); return session;
        }

        private static ArenaHero Hero(ArenaSession session, PlayerRef player)
        {
            return session.Runner != null && session.Runner.TryGetPlayerObject(player, out var obj) && obj != null
                ? obj.GetComponent<ArenaHero>() : null;
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> condition, string message)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException(message);
                await Task.Delay(100);
            }
        }
    }
}
