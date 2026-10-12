using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Chog.Editor
{
    public static class FusionBuild
    {
        [Serializable] private sealed class Report
        {
            public string recordedAtUtc, unityVersion, result, buildBytes, unitySummaryBytes;
            public int files;
            public string[] scenes;
            public bool browserTested = false;
        }

        [MenuItem("Chog/Qualification/Build WebGL")]
        public static void BuildWeb()
        {
            if (!File.Exists(FusionProjectSetup.Boot) || !File.Exists(FusionProjectSetup.Arena))
                throw new InvalidOperationException("Prepare the Fusion scenes first.");
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.runInBackground = true;
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var output = Path.GetFullPath("Builds/WebGL");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            var files = Directory.Exists(output) ? Directory.GetFiles(output, "*", SearchOption.AllDirectories) : Array.Empty<string>();
            var report = new Report
            {
                recordedAtUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                result = result.summary.result.ToString(),
                unitySummaryBytes = result.summary.totalSize.ToString(),
                buildBytes = files.Sum(file => new FileInfo(file).Length).ToString(),
                scenes = scenes, files = files.Length
            };
            var root = Directory.GetParent(Application.dataPath).Parent.FullName;
            var reports = Path.Combine(root, ".artifacts/fusion");
            Directory.CreateDirectory(reports);
            File.WriteAllText(Path.Combine(reports, "latest-webgl-build.json"), JsonUtility.ToJson(report, true) + "\n");
            if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("WebGL build failed; inspect build log.");
            Debug.Log("CHOG_FUSION_WEBGL_BUILD_OK: " + report.buildBytes + " bytes");
        }
    }
}
