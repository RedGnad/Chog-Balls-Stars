using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Chog.Editor
{
    public static class ControlsQualification
    {
        private static bool _running;
        private static string Artifacts => Path.GetFullPath(Path.Combine(Application.dataPath, "../../.artifacts/unity"));
        private static string RequestPath => Path.Combine(Artifacts, "controls-tests-request");

        [InitializeOnLoadMethod]
        private static void WatchForRequest() => EditorApplication.update += CheckRequest;

        private static void CheckRequest()
        {
            if (_running || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
            File.Delete(RequestPath);
            Run();
        }

        [MenuItem("Chog/Qualification/Run Controls Tests")]
        public static void Run()
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.Log("Stop Play Mode before running controls tests.");
                return;
            }
            _running = true;
            Directory.CreateDirectory(Artifacts);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callbacks = new Results(Path.Combine(Artifacts, "controls-tests-" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")));
            try
            {
                api.RegisterCallbacks(callbacks);
                api.Execute(new ExecutionSettings(new Filter
                {
                    testMode = TestMode.EditMode,
                    assemblyNames = new[] { "Chog.Controls.Tests" }
                }) { runSynchronously = true });
            }
            finally
            {
                api.UnregisterCallbacks(callbacks);
                UnityEngine.Object.DestroyImmediate(api);
                _running = false;
            }
        }

        [Serializable]
        private sealed class Summary
        {
            public string recordedAtUtc, unityVersion, result, xmlPath;
            public int passed, failed, skipped, inconclusive;
            public double durationSeconds;
        }

        private sealed class Results : ICallbacks
        {
            private readonly string _directory;
            public Results(string directory) { _directory = directory; }
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory(_directory);
                var xml = Path.Combine(_directory, "results.xml");
                TestRunnerApi.SaveResultToFile(result, xml);
                var summary = new Summary
                {
                    recordedAtUtc = DateTime.UtcNow.ToString("O"),
                    unityVersion = Application.unityVersion,
                    result = result.ResultState,
                    xmlPath = xml,
                    passed = result.PassCount,
                    failed = result.FailCount,
                    skipped = result.SkipCount,
                    inconclusive = result.InconclusiveCount,
                    durationSeconds = result.Duration
                };
                File.WriteAllText(Path.Combine(Artifacts, "latest-controls-tests.json"),
                    JsonUtility.ToJson(summary, true));
                Debug.Log($"CHOG controls tests: {summary.result}, {summary.passed} passed, {summary.failed} failed. {xml}");
            }
        }
    }
}
