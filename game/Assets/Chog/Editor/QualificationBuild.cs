using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Chog.Editor
{
    // Editor-only build entry. It does not change sample gameplay; it sets three player settings
    // (plain-HTTP downloads, the product name and the page of the game), explained where they
    // are set.
    public static class QualificationBuild
    {
        // Same working title as the menu (Chog.Screens.MenuTitle), in the page's own casing.
        private const string ProductName = "Chog Genesis";

        // The page the game is served in: Assets/WebGLTemplates/Chog.
        private const string PageTemplate = "PROJECT:Chog";

        [Serializable]
        private sealed class BuildEvidence
        {
            public string recordedAtUtc;
            public string unityVersion;
            public string target;
            public string outputPath;
            public string result;
            public string[] scenes;
            public int errors;
            public int warnings;
            public ulong totalSizeBytes;
            public double durationSeconds;
        }

        [MenuItem("Chog/Qualification/Build WebGL")]
        public static void WebGL()
        {
            BuildWebGL(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray());
        }

        [MenuItem("Chog/Qualification/Build Local WebGL (Bots)")]
        public static void LocalWebGL()
        {
            // One gameplay scene: its local runner needs no Photon AppId.
            BuildWebGL(new[] { "Assets/QuantumUser/Scenes/Gameplay.unity" });
        }

        // Builds the enabled network scenes and returns the path of the written build report.
        internal static string NetworkWebGL()
        {
            return BuildWebGL(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray());
        }

        private static string BuildWebGL(string[] scenes)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException("Install WebGL Build Support for this Unity editor.");

            if (scenes.Length == 0 || scenes.Any(s => !File.Exists(s)))
                throw new BuildFailedException("Enabled build scenes must exist.");

            var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var runDirectory = Path.Combine(repository, ".artifacts", "unity",
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N"));
            var outputPath = Path.Combine(runDirectory, "webgl");
            Directory.CreateDirectory(runDirectory);

            // These builds are tested from a phone on the local network, over plain HTTP. Unity
            // refuses plain-HTTP downloads by default except from this machine itself, so the game
            // could not fetch the Genesis portraits there. Behind HTTPS the browser itself blocks
            // plain HTTP, whatever this setting says.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

            // The name the page shows in its browser tab and under the game. A working title:
            // the sample's own was "quantum.unity". Changing it moves what the browser keeps for
            // the game (settings, the wallet choice), so it is settled before anyone plays.
            PlayerSettings.productName = ProductName;

            // Our own page instead of the engine's: the whole screen on its side at the first
            // touch on a phone, the game's title and icon, an install manifest, and a second try
            // by itself when a file did not arrive.
            PlayerSettings.WebGL.template = PageTemplate;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            var summary = report.summary;
            var evidence = new BuildEvidence
            {
                recordedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                target = summary.platform.ToString(),
                outputPath = outputPath,
                result = summary.result.ToString(),
                scenes = scenes,
                errors = summary.totalErrors,
                warnings = summary.totalWarnings,
                totalSizeBytes = summary.totalSize,
                durationSeconds = summary.totalTime.TotalSeconds
            };
            var evidencePath = Path.Combine(runDirectory, "build-report.json");
            File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true));
            Debug.Log("CHOG qualification build report: " + evidencePath);

            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("WebGL build failed. See " + evidencePath);

            // The simulation's assets as this build has them, beside the build: a match of this
            // build is played again outside the game with exactly these (tools/quantum, "verify").
            ExportSimulationAssets(Path.Combine(runDirectory, "quantum-db.json"));

            return evidencePath;
        }

        // What the engine's own export writes (QuantumUnityDBUtilities.ExportAsJson). That one
        // cannot be called here: it reads the id of every Unity object an asset points to as a
        // 32-bit number, this Unity version writes them on 64 bits, and the export throws. So
        // the assets are written with those references left as they are, then each is blanked
        // in the text, which is what the engine does with them.
        private static void ExportSimulationAssets(string path)
        {
            var assets = new List<Quantum.AssetObject>();
            foreach (var entry in Quantum.QuantumUnityDB.Global.Entries)
            {
                var asset = entry.Source.EditorInstance;
                if (asset == null)
                    throw new BuildFailedException("No editor instance for the simulation asset " + entry);

                assets.Add(asset);
            }

            var serializer = new Quantum.QuantumUnityJsonSerializer { NullifyUnityObjectReferences = false };
            using (var written = new MemoryStream())
            {
                serializer.SerializeAssets(written, assets.ToArray());
                var text = new UTF8Encoding(false).GetString(written.ToArray());
                File.WriteAllText(path, Regex.Replace(text, "\\{\"instanceID\":-?[0-9]+\\}", "null"), new UTF8Encoding(false));
            }

            Debug.Log("CHOG simulation assets: " + assets.Count + " written to " + path);
        }
    }
}
