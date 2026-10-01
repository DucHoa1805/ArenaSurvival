using System;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvival.DevTools
{
    /// <summary>
    /// Đo hiệu năng bằng ProfilerRecorder trong N frame rồi in bảng trung bình / tệ nhất ra Console và ghi vào
    /// persistentDataPath/perf-log.txt. Đọc số bằng code thay vì nhìn biểu đồ Profiler: lặp lại được,
    /// so trước/sau chính xác, chép thẳng vào README được.
    ///
    /// Chỉ chạy trong Editor và Development build. F10 bắt đầu 1 lần đo.
    /// </summary>
    public class PerfRecorder : MonoBehaviour
    {
        [SerializeField] private int _sampleFrames = 300;
        [Tooltip("Bỏ qua bấy nhiêu frame đầu: lúc vừa bù quân số, pool còn đang tạo object mới.")]
        [SerializeField] private int _warmupFrames = 60;
        [Tooltip("Để đếm số quái trung bình trong lúc đo.")]
        [SerializeField] private Enemies.EnemySpawner _spawner;

        private struct Metric
        {
            public string Label;
            public string Unit;
            public double Scale; // đổi đơn vị gốc (ns, byte) sang đơn vị hiển thị
            public ProfilerRecorder Recorder;
            public double Sum;
            public double Max;
        }

        private Metric[] _metrics;
        private bool _capturing;
        private int _frame;
        private string _label;
        private int _enemySum;

        public bool IsCapturing => _capturing;

        private void Awake()
        {
            if (!Debug.isDebugBuild) Destroy(this);
        }

        private void Update()
        {
            if (!_capturing && Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                BeginCapture("F10");
            }

            if (!_capturing) return;

            // Bảng lên cấp / Pause dừng thời gian: frame đó không phản ánh gameplay, bỏ qua.
            if (Time.timeScale == 0f) return;

            _frame++;
            if (_frame <= _warmupFrames) return;

            for (int i = 0; i < _metrics.Length; i++)
            {
                if (!_metrics[i].Recorder.Valid) continue;
                // LastValue là giá trị của frame vừa xong (frame hiện tại chưa kết thúc).
                double v = _metrics[i].Recorder.LastValue * _metrics[i].Scale;
                _metrics[i].Sum += v;
                if (v > _metrics[i].Max) _metrics[i].Max = v;
            }

            // Đọc bộ đếm có sẵn, không dùng FindObjectsByType: hàm đó tạo mảng mới mỗi frame và sẽ cộng rác
            // vào chính chỉ số GC Alloc đang đo.
            if (_spawner != null) _enemySum += _spawner.ActiveCount;

            if (_frame >= _warmupFrames + _sampleFrames) EndCapture();
        }

        public void BeginCapture(string label) => BeginCapture(label, _sampleFrames);

        /// <summary>
        /// Số frame nên đủ dài theo THỜI GIAN chứ không chỉ theo số frame: build chạy không giới hạn fps (~800 fps)
        /// thì 300 frame chưa tới nửa giây — từng gặp: cửa sổ đo rơi đúng lúc pool còn đang tạo thêm object,
        /// ra các đỉnh GC không phản ánh lúc chơi ổn định.
        /// </summary>
        public void BeginCapture(string label, int sampleFrames)
        {
            if (_capturing) return;

            _sampleFrames = Mathf.Max(1, sampleFrames);

            _label = label;
            _frame = 0;
            _enemySum = 0;
            _metrics = new[]
            {
                Create("Main Thread", "ms", 1e-6, ProfilerCategory.Internal, "Main Thread"),
                Create("Scripts Update", "ms", 1e-6, ProfilerCategory.Scripts, "BehaviourUpdate"),
                Create("Scripts LateUpdate", "ms", 1e-6, ProfilerCategory.Scripts, "LateBehaviourUpdate"),
                Create("Scripts FixedUpdate", "ms", 1e-6, ProfilerCategory.Scripts, "FixedBehaviourUpdate"),
                Create("Physics2D.Simulate", "ms", 1e-6, ProfilerCategory.Physics, "Physics2D.Simulate"),
                Create("GC Alloc / frame", "B", 1, ProfilerCategory.Memory, "GC Allocated In Frame"),
                Create("GC Alloc count / frame", "", 1, ProfilerCategory.Memory, "GC Allocation In Frame Count"),
                Create("Batches", "", 1, ProfilerCategory.Render, "Batches Count"),
                // Có SRP Batcher thì Batches tăng theo số object là bình thường; SetPass (đổi trạng thái shader) mới là chi phí thật.
                Create("SetPass Calls", "", 1, ProfilerCategory.Render, "SetPass Calls Count"),
                Create("Draw Calls", "", 1, ProfilerCategory.Render, "Draw Calls Count"),
            };
            _capturing = true;
            StartProfilerLog(label);
            Debug.Log($"[PerfRecorder] Bắt đầu đo '{label}': {_warmupFrames} frame khởi động + {_sampleFrames} frame.");
        }

        private static Metric Create(string label, string unit, double scale, ProfilerCategory category, string stat)
        {
            return new Metric
            {
                Label = label,
                Unit = unit,
                Scale = scale,
                Recorder = ProfilerRecorder.StartNew(category, stat),
            };
        }

        /// <summary>
        /// Có tham số -profile-log thì ghi dữ liệu Profiler đầy đủ (.raw) trong đúng cửa sổ đo, để mở trong cửa sổ Profiler
        /// của Editor và xem hàm nào cấp phát / tốn thời gian. Chỉ trong cửa sổ đo: ghi cả trận thì file rất lớn.
        /// </summary>
        private static void StartProfilerLog(string label)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-profile-log") < 0) return;

            Profiler.logFile = Path.Combine(Application.persistentDataPath, $"profile-{label}");
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
        }

        private static void StopProfilerLog()
        {
            if (!Profiler.enableBinaryLog) return;

            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = "";
        }

        private void EndCapture()
        {
            _capturing = false;
            StopProfilerLog();

            var sb = new StringBuilder();
            sb.AppendLine($"[PerfRecorder] '{_label}' — {DateTime.Now:yyyy-MM-dd HH:mm:ss} — {_sampleFrames} frame" +
                          $" — quái TB: {(_spawner != null ? (_enemySum / (float)_sampleFrames).ToString("F0") : "?")}");
            sb.AppendLine($"{"Chỉ số",-24}{"Trung bình",14}{"Tệ nhất",14}");
            foreach (Metric m in _metrics)
            {
                string avg = m.Recorder.Valid ? Format(m.Sum / _sampleFrames, m.Unit) : "n/a";
                string max = m.Recorder.Valid ? Format(m.Max, m.Unit) : "n/a";
                sb.AppendLine($"{m.Label,-24}{avg,14}{max,14}");
                m.Recorder.Dispose();
            }

            string report = sb.ToString();
            Debug.Log(report);

            try
            {
                File.AppendAllText(Path.Combine(Application.persistentDataPath, "perf-log.txt"), report + Environment.NewLine);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[PerfRecorder] Không ghi được perf-log.txt: {e.Message}");
            }
        }

        private static string Format(double value, string unit) =>
            unit == "ms" ? $"{value:F2} ms" : unit == "B" ? $"{value:F0} B" : $"{value:F1}";

        private void OnDestroy()
        {
            if (_metrics == null) return;
            foreach (Metric m in _metrics) m.Recorder.Dispose();
        }
    }
}
