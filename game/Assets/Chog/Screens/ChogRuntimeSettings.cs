using UnityEngine;

namespace Chog.Screens
{
    /// <summary>
    /// Engine settings applied once at start, for the cost of a frame (docs/PERF-AUDIT.md, Q5 and Q6).
    /// Unity's own physics steps every collider of the scene each frame, while nothing of the game uses
    /// it: Quantum simulates the arena, and the views and the aim read Quantum's physics. It now steps
    /// only when a script asks, which none does (queries, as particle collisions, still answer). Logs
    /// and warnings are written without their stack trace, as Photon advises for the web; errors and
    /// exceptions keep theirs.
    /// </summary>
    public static class ChogRuntimeSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics.autoSyncTransforms = false;
            if (!Application.isEditor)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            }
        }
    }
}
