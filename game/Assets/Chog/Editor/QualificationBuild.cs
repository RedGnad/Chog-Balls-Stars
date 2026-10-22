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
        private const string ProductName = "Chog Arena";

        // The page the game is served in: Assets/WebGLTemplates/Chog.
        private const string PageTemplate = "PROJECT:Chog";

        // The stamp of the simulation a build carries (Chog.Screens.BuildIdentity reads it), and the
        // copy beside the build that the assembled site records (tools/release/assemble_site.py).
        private const string SimulationStampAsset = "Assets/Chog/Resources/ChogSimulationStamp.txt";
        private const string SimulationStampFile = "simulation.txt";

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
            BuildWebGL(EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray());
        }

        [MenuItem("Chog/Qualification/Build Local WebGL (Bots)")]
        public static void LocalWebGL()
        {
            // One gameplay scene: its local runner needs no Photon AppId.
            BuildWebGL(new[] { "Assets/QuantumUser/Scenes/Gameplay.unity" });
        }

        // Builds the enabled network scenes and returns the path of the written build report.
        internal static string NetworkWebGL(bool clean = false)
        {
            return BuildWebGL(EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray(), clean);
        }

        private static string BuildWebGL(string[] scenes, bool clean = false)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException("Install WebGL Build Support for this Unity editor.");

            if (scenes.Length == 0 || scenes.Any(s => !File.Exists(s)))
                throw new BuildFailedException("Enabled build scenes must exist.");

            var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
            var runDirectory = Path.Combine(repository, ".artifacts", "unity", stamp + "-" + Guid.NewGuid().ToString("N"));
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
            // A wallet window takes the focus while the player confirms (a Ledger takes longer than
            // Photon's 10 s timeout): the game must keep running behind it, or Photon drops the
            // connection ("Connection failed"). Chogtanks ran the same way.
            PlayerSettings.runInBackground = true;

            // Our own page instead of the engine's: the whole screen on its side at the first
            // touch on a phone, the game's title and icon, an install manifest, and a second try
            // by itself when a file did not arrive.
            PlayerSettings.WebGL.template = PageTemplate;

            // The build carries its own stamp as its version: it is what the game tells Photon
            // (Chog.Screens.BuildIdentity), what the page compares with the build its server
            // serves, and the start of the folder's name, which the assembled site records.
            // The project's own version is put back afterwards, so a build changes no setting.
            // The simulation's assets first, then its stamp: builds whose simulation is the same (its
            // code, Quantum's own assemblies, its assets) give Photon the same name and meet, so a
            // build that changes only screens or looks opens no new version of the application on
            // Photon, which allows an application only so many in parallel.
            var simulationAssets = Path.Combine(runDirectory, "quantum-db.json");
            ExportSimulationAssets(simulationAssets);
            var simulation = SimulationStamp(simulationAssets);
            File.WriteAllText(Path.Combine(runDirectory, SimulationStampFile), simulation, new UTF8Encoding(false));
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", SimulationStampAsset)), simulation, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(SimulationStampAsset, ImportAssetOptions.ForceUpdate);
            Debug.Log("CHOG simulation stamp " + simulation);

            var projectVersion = PlayerSettings.bundleVersion;
            PlayerSettings.bundleVersion = stamp;
            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.WebGL,
                    options = clean ? BuildOptions.CleanBuildCache : BuildOptions.None
                });
            }
            finally
            {
                PlayerSettings.bundleVersion = projectVersion;
            }
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

            // The simulation's assets as this build has them (written above, before the stamp) stay
            // beside the build: a match of this build is played again outside the game with exactly
            // these (tools/quantum, "verify").
            return evidencePath;
        }

        /// <summary>
        /// Sixteen hex digits of a SHA-256 over what decides how a match plays: the simulation's code
        /// and schema (Assets/QuantumUser/Simulation, its .cs and .qtn files), Quantum's own assemblies
        /// (Assets/Photon/Quantum/Assemblies) and the exported simulation assets, each file by its path
        /// and its bytes, in the order of their paths.
        /// </summary>
        public static string SimulationStamp(string simulationAssets)
        {
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(Path.Combine(Application.dataPath, "QuantumUser", "Simulation"), "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".qtn", StringComparison.Ordinal)));
            var quantum = Path.Combine(Application.dataPath, "Photon", "Quantum", "Assemblies");
            if (Directory.Exists(quantum))
                files.AddRange(Directory.GetFiles(quantum, "*.dll", SearchOption.AllDirectories));
            var named = files.Select(file => (name: Path.GetRelativePath(project, file).Replace('\\', '/'), file))
                .OrderBy(entry => entry.name, StringComparer.Ordinal).ToList();
            named.Add(("quantum-db.json", simulationAssets));

            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                foreach (var (name, file) in named)
                {
                    var nameBytes = Encoding.UTF8.GetBytes(name + "\n");
                    hash.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);
                    var bytes = File.ReadAllBytes(file);
                    hash.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return string.Concat(hash.Hash.Take(8).Select(b => b.ToString("x2")));
            }
        }

        // What the engine's own export writes (QuantumUnityDBUtilities.ExportAsJson). That one
        // cannot be called here: it reads the id of every Unity object an asset points to as a
        // 32-bit number, this Unity version writes them on 64 bits, and the export throws. So
        // the assets are written with those references left as they are, then each is blanked
        // in the text, which is what the engine does with them.
        private static void ExportSimulationAssets(string path)
        {
            // Assets removed from the disk while the Editor stayed open (a branch put aside) leave the
            // database listing them: read it again first.
            AssetDatabase.Refresh();
            Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB(true);
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
