using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Chog.Editor
{
    /// <summary>
    /// Lets tooling outside the Editor ask this open Editor for a WebGL build.
    /// Create the file ".artifacts/unity/webgl-build-request" at the repository root; the
    /// Editor then refreshes assets (compiling changed scripts), imports the Genesis roster,
    /// builds the enabled scenes and writes ".artifacts/unity/webgl-build-status.json".
    /// A request file whose content is "refresh" stops before the build: it only compiles and
    /// imports. Nothing happens during Play Mode.
    /// </summary>
    public static class BuildRequestWatcher
    {
        [Serializable]
        private sealed class Status
        {
            public string state;
            public string detail;
            public string buildReport;
            public string updatedAtUtc;
        }

        private const double PollSeconds = 1.0;

        private static double _nextPoll;
        private static string _lastWaitingReason;

        private static string Artifacts => Path.GetFullPath(Path.Combine(Application.dataPath, "../../.artifacts/unity"));

        // File names are a contract with the command-line tooling.
        private static string RequestPath => Path.Combine(Artifacts, "webgl-build-request");
        private static string RefreshedPath => Path.Combine(Artifacts, "webgl-build-refreshed");
        private static string ImportedPath => Path.Combine(Artifacts, "webgl-build-imported");
        private static string StatusPath => Path.Combine(Artifacts, "webgl-build-status.json");

        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

            if (!File.Exists(RequestPath))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ReportWaiting("Play Mode is active; stop it to let the build start.");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ReportWaiting("The Editor is compiling or importing.");
                return;
            }

            // Step 1: import changed files. A script change reloads the domain; this watcher
            // then starts again and reaches step 2 thanks to the marker file.
            if (!File.Exists(RefreshedPath))
            {
                File.WriteAllText(RefreshedPath, DateTime.UtcNow.ToString("O"));
                Write("refreshing", "Importing changed assets and scripts.", null);
                AssetDatabase.Refresh();
                return;
            }

            if (EditorUtility.scriptCompilationFailed)
            {
                Finish();
                Write("failed", "Scripts have compile errors; see the Unity Console.", null);
                return;
            }

            // Step 2: import generated content, then let the Editor register the changed assets.
            if (!File.Exists(ImportedPath))
            {
                File.WriteAllText(ImportedPath, "");
                try
                {
                    var roster = "Genesis roster: " + GenesisRosterImporter.Import() + "; " + ChogFontAssets.Ensure();
                    // Kept in the marker so the final status says which roster the build embeds.
                    File.WriteAllText(ImportedPath, roster);
                    Write("importing", roster, null);
                    AssetDatabase.Refresh();
                }
                catch (Exception exception)
                {
                    Finish();
                    Write("failed", exception.GetType().Name + ": " + exception.Message, null);
                    Debug.LogException(exception);
                }
                return;
            }

            // Step 3: build, unless the request only asked for a refresh.
            var request = File.ReadAllText(RequestPath).Trim();
            var refreshOnly = request == "refresh";
            // "clean": the same build from an empty build cache (a stale IL2CPP output fails to link).
            var clean = request == "clean";
            var imported = File.ReadAllText(ImportedPath);
            Finish();
            if (refreshOnly)
            {
                Write("refreshed", "Scripts compiled and content imported; no build was requested. " + imported, null);
                return;
            }

            Write("building", "WebGL build started. " + imported, null);
            try
            {
                var report = QualificationBuild.NetworkWebGL(clean);
                Write("succeeded", "WebGL build finished. " + imported, report);
            }
            catch (Exception exception)
            {
                Write("failed", exception.GetType().Name + ": " + exception.Message, null);
                Debug.LogException(exception);
            }
        }

        private static void Finish()
        {
            File.Delete(RequestPath);
            File.Delete(RefreshedPath);
            File.Delete(ImportedPath);
            _lastWaitingReason = null;
        }

        private static void ReportWaiting(string reason)
        {
            if (_lastWaitingReason == reason)
                return;

            _lastWaitingReason = reason;
            Write("waiting", reason, null);
        }

        private static void Write(string state, string detail, string buildReport)
        {
            Directory.CreateDirectory(Artifacts);
            var status = new Status
            {
                state = state,
                detail = detail,
                buildReport = buildReport,
                updatedAtUtc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(StatusPath, JsonUtility.ToJson(status, true));
            Debug.Log("CHOG build request: " + state + ". " + detail);
        }
    }
}
