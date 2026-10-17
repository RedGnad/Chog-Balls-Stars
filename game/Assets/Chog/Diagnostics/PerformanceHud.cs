using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Quantum;
using UnityEngine;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

namespace Chog.Diagnostics
{
    /// <summary>
    /// On-device performance overlay for measured device runs. Disabled unless requested:
    /// a WebGL page URL carrying "?perf=1", or PlayerPrefs "chog.perf" set to 1 (Editor menu).
    /// It samples only while a Quantum session is running, so menu time is excluded.
    /// Every value shown is measured at runtime; nothing is estimated.
    ///
    /// With "?perf=1" the URL may also carry test overrides, applied before a session starts:
    /// "hz" (simulation rate), "rollback" (rollback window), "checksum" (checksum interval),
    /// "fps" (Application.targetFrameRate, -1 lets the browser pace frames) and "quality"
    /// (index of a Unity quality level, 0 being the cheapest).
    /// Session overrides only take effect when this client creates the room, because the
    /// server keeps the first session config it receives. The overlay prints the config of
    /// the running session, which is the one actually in force.
    /// </summary>
    // Created only from code, so keep it through managed code stripping in IL2CPP builds.
    [Preserve]
    [DefaultExecutionOrder(-32000)]
    public sealed class PerformanceHud : MonoBehaviour
    {
        // Protocol string shared with the Editor menu toggle.
        public const string PlayerPrefsKey = "chog.perf";

        /// <summary>
        /// One-line reports other parts of the game add under the overlay, such as the state of
        /// the portrait downloads. Each provider is called when the overlay text is rebuilt.
        /// </summary>
        public static readonly System.Collections.Generic.List<System.Func<string>> StatusLines =
            new System.Collections.Generic.List<System.Func<string>>();

        // Query names and the log prefix are data contracts for testers and external tooling.
        private const string QueryFlag = "perf";
        private const string SimulationRateParameter = "hz";
        private const string RollbackWindowParameter = "rollback";
        private const string ChecksumIntervalParameter = "checksum";
        private const string FrameRateParameter = "fps";
        private const string QualityLevelParameter = "quality";
        private const string ReportPrefix = "CHOG_PERF ";

        private const int Capacity = 4096;
        private const float WarmUpSeconds = 3f;
        private const float RefreshSeconds = 1f;
        private const float ReportSeconds = 60f;

        // Buckets sit between display periods: 16.7 ms is a 60 fps frame, 33.3 ms a 30 fps frame.
        private const float SmoothFrameMs = 20f;
        private const float SlowFrameMs = 40f;
        private const float HitchFrameMs = 100f;

        private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;
        private static bool s_hasFrameRateOverride;
        private static int s_frameRateOverride;
        private static bool s_hasQualityOverride;
        private static int s_qualityOverride;

        private readonly float[] _frameMs = new float[Capacity];
        private readonly float[] _busyMs = new float[Capacity];
        private readonly float[] _simulationMs = new float[Capacity];
        private readonly float[] _otherMs = new float[Capacity];
        private readonly float[] _scratch = new float[Capacity];
        private readonly StringBuilder _builder = new StringBuilder(768);
        private readonly GUIContent _content = new GUIContent("perf: waiting for a Quantum session");

        private int _next;
        private int _count;
        private long _totalFrames;
        private long _smoothFrames;
        private long _slowFrames;
        private long _hitchFrames;
        private float _worstFrameMs;
        private long _predictedSum;
        private int _predictedMax;
        private float _sessionSeconds;
        private float _refreshTimer;
        private float _reportTimer;
        private object _session;

        private long _frameStartTicks;
        private float _lastBusyMs;
        private bool _hasBusy;
        private Coroutine _endOfFrame;

        private GUIStyle _style;
        private Texture2D _background;
        private Vector2 _size;
        private bool _layoutDirty = true;

        private const string CombatLogFlag = "combatlog";

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var url = Application.absoluteURL;
            if (PlayerPrefs.GetInt(PlayerPrefsKey, 0) != 1 && !PerformanceSampling.HasQueryFlag(url, QueryFlag))
                return;

            ApplyOverrides(url);

            var host = new GameObject("Chog Performance HUD");
            DontDestroyOnLoad(host);
            host.AddComponent<PerformanceHud>();
            // The log of every cast and hit writes about ten lines a second with their stack traces: a
            // measured run paid for it (docs/PERF-AUDIT.md, 2.3). Asked for apart, with "combatlog=1".
            if (PerformanceSampling.HasQueryFlag(url, CombatLogFlag))
                host.AddComponent<CombatLog>();
            host.AddComponent<InputProbe>();
        }

        private static void ApplyOverrides(string url)
        {
            s_hasFrameRateOverride = PerformanceSampling.TryGetQueryInt(url, FrameRateParameter, out s_frameRateOverride);
            s_hasQualityOverride = PerformanceSampling.TryGetQueryInt(url, QualityLevelParameter, out s_qualityOverride);
            if (s_hasQualityOverride)
                s_qualityOverride = Mathf.Clamp(s_qualityOverride, 0, QualitySettings.names.Length - 1);

            var hasRate = PerformanceSampling.TryGetQueryInt(url, SimulationRateParameter, out var rate);
            var hasRollback = PerformanceSampling.TryGetQueryInt(url, RollbackWindowParameter, out var rollback);
            var hasChecksum = PerformanceSampling.TryGetQueryInt(url, ChecksumIntervalParameter, out var checksum);
            if (!hasRate && !hasRollback && !hasChecksum)
                return;

            // The Editor never has a page URL, so the project asset is only changed in player memory.
            var config = QuantumDeterministicSessionConfigAsset.DefaultConfig;
            if (config == null)
            {
                Debug.LogWarning(ReportPrefix + "session overrides ignored: no global session config");
                return;
            }

            if (hasRate)
                config.UpdateFPS = Mathf.Clamp(rate, 10, 120);
            if (hasRollback)
                config.RollbackWindow = Mathf.Clamp(rollback, 10, 120);
            if (hasChecksum)
                config.ChecksumInterval = Mathf.Clamp(checksum, 0, 600);

            Debug.Log(ReportPrefix + "session overrides requested: hz " + config.UpdateFPS + ", rollback " +
                      config.RollbackWindow + ", checksum " + config.ChecksumInterval);
        }

        private void Awake()
        {
            // Skip the IMGUI layout pass: one repaint call per frame keeps the overlay cheap.
            useGUILayout = false;
        }

        private void OnEnable()
        {
            _endOfFrame = StartCoroutine(MeasureEndOfFrame());
        }

        private void OnDisable()
        {
            if (_endOfFrame != null)
                StopCoroutine(_endOfFrame);
            _endOfFrame = null;
        }

        private void OnDestroy()
        {
            if (_background != null)
                Destroy(_background);
        }

        // Main-thread work of a frame: from the first script update to the end of rendering.
        // The gap to the full frame time is waiting for the display or the browser.
        private IEnumerator MeasureEndOfFrame()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return endOfFrame;
                if (_frameStartTicks == 0)
                    continue;

                _lastBusyMs = (float)((Stopwatch.GetTimestamp() - _frameStartTicks) * TicksToMs);
                _hasBusy = true;
            }
        }

        private void Update()
        {
            // This component updates first, so every value read below describes the previous frame.
            var busyMs = _hasBusy ? _lastBusyMs : 0f;
            _frameStartTicks = Stopwatch.GetTimestamp();

            // Re-applied every frame because the sample menu restores its own graphics settings.
            if (s_hasFrameRateOverride && Application.targetFrameRate != s_frameRateOverride)
                Application.targetFrameRate = s_frameRateOverride;
            if (s_hasQualityOverride && QualitySettings.GetQualityLevel() != s_qualityOverride)
                QualitySettings.SetQualityLevel(s_qualityOverride, true);

            var runner = QuantumRunner.Default;
            var session = runner != null && runner.IsRunning && runner.Game != null ? runner.Game.Session : null;
            if (session == null)
            {
                if (_session != null)
                {
                    _session = null;
                    SetText("perf: no running Quantum session");
                }
                return;
            }

            if (!ReferenceEquals(session, _session))
            {
                _session = session;
                ResetSamples();
            }

            var deltaSeconds = Time.unscaledDeltaTime;
            _sessionSeconds += deltaSeconds;
            if (_sessionSeconds < WarmUpSeconds)
            {
                SetText("perf: warming up");
                return;
            }

            var frameMs = deltaSeconds * 1000f;
            var stats = session.Stats;
            var simulationMs = (float)(stats.UpdateTime * 1000.0);
            var predicted = session.PredictedFrames;

            _frameMs[_next] = frameMs;
            _busyMs[_next] = busyMs;
            _simulationMs[_next] = simulationMs;
            _otherMs[_next] = Mathf.Max(0f, busyMs - simulationMs);
            _next = (_next + 1) % Capacity;
            if (_count < Capacity)
                _count++;

            _totalFrames++;
            if (frameMs < SmoothFrameMs)
                _smoothFrames++;
            if (frameMs > SlowFrameMs)
                _slowFrames++;
            if (frameMs > HitchFrameMs)
                _hitchFrames++;
            if (frameMs > _worstFrameMs)
                _worstFrameMs = frameMs;
            _predictedSum += predicted;
            if (predicted > _predictedMax)
                _predictedMax = predicted;

            _refreshTimer += deltaSeconds;
            _reportTimer += deltaSeconds;
            if (_refreshTimer < RefreshSeconds)
                return;

            _refreshTimer = 0f;
            var writeReport = _reportTimer >= ReportSeconds;
            if (writeReport)
                _reportTimer = 0f;

            var config = session.SessionConfig;
            Refresh(stats.Ping, stats.Offset, session.IsOnline, session.IsStalling,
                config != null ? config.UpdateFPS : 0,
                config != null ? config.RollbackWindow : 0,
                config != null ? config.ChecksumInterval : 0,
                writeReport);
        }

        private void ResetSamples()
        {
            _next = 0;
            _count = 0;
            _totalFrames = 0;
            _smoothFrames = 0;
            _slowFrames = 0;
            _hitchFrames = 0;
            _worstFrameMs = 0f;
            _predictedSum = 0;
            _predictedMax = 0;
            _sessionSeconds = 0f;
            _refreshTimer = 0f;
            _reportTimer = 0f;
        }

        private void SortIntoScratch(float[] samples)
        {
            Array.Copy(samples, _scratch, _count);
            Array.Sort(_scratch, 0, _count);
        }

        private void Refresh(int ping, int inputOffset, bool online, bool stalling,
            int simulationRate, int rollbackWindow, int checksumInterval, bool writeReport)
        {
            SortIntoScratch(_frameMs);
            var frameP50 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 50f);
            var frameP95 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 95f);
            var frameP99 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 99f);

            SortIntoScratch(_busyMs);
            var busyP50 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 50f);
            var busyP95 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 95f);

            SortIntoScratch(_otherMs);
            var otherP50 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 50f);
            var otherP95 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 95f);

            SortIntoScratch(_simulationMs);
            var simulationP50 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 50f);
            var simulationP95 = PerformanceSampling.PercentileOfSorted(_scratch, _count, 95f);
            var simulationMax = _scratch[_count - 1];

            var smoothPercent = 100f * _smoothFrames / _totalFrames;
            var slowPercent = 100f * _slowFrames / _totalFrames;
            var predictedAverage = (float)_predictedSum / _totalFrames;
            var managedHeapBytes = GC.GetTotalMemory(false);
            var targetFrameRate = Application.targetFrameRate;
            var culture = CultureInfo.InvariantCulture;

            _builder.Length = 0;
            var qualityLevel = QualitySettings.GetQualityLevel();
            var qualityNames = QualitySettings.names;
            var qualityName = qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "?";
            _builder.Append(Screen.width).Append('x').Append(Screen.height)
                .Append("  quality ").Append(qualityLevel).Append(' ').Append(qualityName).Append("  ")
                .Append(SystemInfo.graphicsDeviceName).Append('\n');
            _builder.Append("frame ms  p50 ").Append(frameP50.ToString("0.0", culture))
                .Append("  p95 ").Append(frameP95.ToString("0.0", culture))
                .Append("  p99 ").Append(frameP99.ToString("0.0", culture))
                .Append("  worst ").Append(_worstFrameMs.ToString("0", culture)).Append('\n');
            _builder.Append("under 20ms ").Append(smoothPercent.ToString("0.0", culture))
                .Append("%  over 40ms ").Append(slowPercent.ToString("0.0", culture))
                .Append("%  over 100ms ").Append(_hitchFrames)
                .Append("  of ").Append(_totalFrames).Append(" frames in ")
                .Append(((int)_sessionSeconds).ToString(culture)).Append(" s\n");
            if (_hasBusy)
            {
                _builder.Append("busy ms   p50 ").Append(busyP50.ToString("0.0", culture))
                    .Append("  p95 ").Append(busyP95.ToString("0.0", culture))
                    .Append("   other p50 ").Append(otherP50.ToString("0.0", culture))
                    .Append("  p95 ").Append(otherP95.ToString("0.0", culture)).Append('\n');
            }
            else
            {
                _builder.Append("busy ms   not available\n");
            }
            _builder.Append("sim ms    p50 ").Append(simulationP50.ToString("0.0", culture))
                .Append("  p95 ").Append(simulationP95.ToString("0.0", culture))
                .Append("  max ").Append(simulationMax.ToString("0.0", culture)).Append('\n');
            _builder.Append("predicted avg ").Append(predictedAverage.ToString("0.0", culture))
                .Append("  max ").Append(_predictedMax);
            if (online)
                _builder.Append("  ping ").Append(ping).Append("  offset ").Append(inputOffset);
            else
                _builder.Append("  offline");
            if (stalling)
                _builder.Append("  STALLING");
            _builder.Append('\n');
            _builder.Append("session ").Append(simulationRate).Append(" Hz  rollback ").Append(rollbackWindow)
                .Append("  checksum ").Append(checksumInterval)
                .Append("  target fps ").Append(targetFrameRate)
                .Append("  heap ").Append((managedHeapBytes / 1048576f).ToString("0", culture)).Append(" MB");
            foreach (var statusLine in StatusLines)
                _builder.Append('\n').Append(statusLine());
            SetText(_builder.ToString());

            if (!writeReport)
                return;

            // One machine-readable line per minute, readable from the browser console.
            _builder.Length = 0;
            _builder.Append(ReportPrefix).Append('{')
                .Append("\"seconds\":").Append(((int)_sessionSeconds).ToString(culture))
                .Append(",\"frames\":").Append(_totalFrames)
                .Append(",\"windowFrames\":").Append(_count)
                .Append(",\"frameP50\":").Append(frameP50.ToString("0.00", culture))
                .Append(",\"frameP95\":").Append(frameP95.ToString("0.00", culture))
                .Append(",\"frameP99\":").Append(frameP99.ToString("0.00", culture))
                .Append(",\"frameWorst\":").Append(_worstFrameMs.ToString("0.00", culture))
                .Append(",\"under20Percent\":").Append(smoothPercent.ToString("0.00", culture))
                .Append(",\"over40Percent\":").Append(slowPercent.ToString("0.00", culture))
                .Append(",\"over100Frames\":").Append(_hitchFrames)
                .Append(",\"busyAvailable\":").Append(_hasBusy ? "true" : "false")
                .Append(",\"busyP50\":").Append(busyP50.ToString("0.00", culture))
                .Append(",\"busyP95\":").Append(busyP95.ToString("0.00", culture))
                .Append(",\"otherP50\":").Append(otherP50.ToString("0.00", culture))
                .Append(",\"otherP95\":").Append(otherP95.ToString("0.00", culture))
                .Append(",\"simulationP50\":").Append(simulationP50.ToString("0.00", culture))
                .Append(",\"simulationP95\":").Append(simulationP95.ToString("0.00", culture))
                .Append(",\"simulationMax\":").Append(simulationMax.ToString("0.00", culture))
                .Append(",\"predictedAverage\":").Append(predictedAverage.ToString("0.00", culture))
                .Append(",\"predictedMax\":").Append(_predictedMax)
                .Append(",\"online\":").Append(online ? "true" : "false")
                .Append(",\"ping\":").Append(ping)
                .Append(",\"inputOffset\":").Append(inputOffset)
                .Append(",\"simulationRate\":").Append(simulationRate)
                .Append(",\"rollbackWindow\":").Append(rollbackWindow)
                .Append(",\"checksumInterval\":").Append(checksumInterval)
                .Append(",\"targetFrameRate\":").Append(targetFrameRate)
                .Append(",\"qualityLevel\":").Append(qualityLevel)
                .Append(",\"quality\":\"").Append(Escape(qualityName)).Append('"')
                .Append(",\"managedHeapBytes\":").Append(managedHeapBytes)
                .Append(",\"width\":").Append(Screen.width)
                .Append(",\"height\":").Append(Screen.height)
                .Append(",\"gpu\":\"").Append(Escape(SystemInfo.graphicsDeviceName)).Append('"')
                .Append(",\"device\":\"").Append(Escape(SystemInfo.deviceModel)).Append('"')
                .Append(",\"os\":\"").Append(Escape(SystemInfo.operatingSystem)).Append('"')
                .Append(",\"mobile\":").Append(Application.isMobilePlatform ? "true" : "false")
                .Append(",\"page\":\"").Append(Escape(Application.absoluteURL)).Append('"')
                .Append(",\"status\":\"");
            for (var i = 0; i < StatusLines.Count; i++)
                _builder.Append(i == 0 ? "" : " | ").Append(Escape(StatusLines[i]().Replace('\n', ' ')));
            _builder.Append("\"}");
            Debug.Log(_builder.ToString());
            StartCoroutine(SendReport(_builder.ToString().Substring(ReportPrefix.Length)));
        }

        // The local test server keeps these reports (tools/unity/serve_webgl.py, "/perf-report"),
        // so a run on a phone can be read without a screenshot. Anywhere else the request simply
        // fails, and nothing depends on it.
        private static System.Collections.IEnumerator SendReport(string json)
        {
            var page = Application.absoluteURL;
            if (string.IsNullOrEmpty(page) || !page.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
                yield break;

            var hostEnd = page.IndexOf('/', page.IndexOf("://", System.StringComparison.Ordinal) + 3);
            var origin = hostEnd < 0 ? page : page.Substring(0, hostEnd);
            var request = new UnityEngine.Networking.UnityWebRequest(origin + "/perf-report", "POST")
            {
                uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");

            UnityEngine.Networking.UnityWebRequestAsyncOperation sending = null;
            try
            {
                sending = request.SendWebRequest();
            }
            catch (System.InvalidOperationException)
            {
                // The build refuses plain HTTP: there is no local test server to report to.
            }

            if (sending != null)
                yield return sending;
            request.Dispose();
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private void SetText(string text)
        {
            if (_content.text == text)
                return;

            _content.text = text;
            _layoutDirty = true;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { richText = false, wordWrap = false };
                _style.normal.textColor = Color.white;
                _style.padding = new RectOffset(6, 6, 4, 4);
                _background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                _background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
                _background.Apply();
            }

            var fontSize = Mathf.Max(11, Screen.height / 40);
            if (_style.fontSize != fontSize)
            {
                _style.fontSize = fontSize;
                _layoutDirty = true;
            }

            if (_layoutDirty)
            {
                _size = _style.CalcSize(_content);
                _layoutDirty = false;
            }

            var area = new Rect(8f, Screen.height * 0.28f, _size.x, _size.y);
            GUI.DrawTexture(area, _background);
            GUI.Label(area, _content, _style);
        }
    }
}
