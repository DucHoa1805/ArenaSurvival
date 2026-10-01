using System;
using System.Collections;
using ArenaSurvival.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaSurvival.DevTools
{
    /// <summary>
    /// Chế độ benchmark tự động cho Development build, chạy bằng tham số dòng lệnh:
    ///   ArenaSurvival.exe -benchmark -enemies 150 -weapons 1 -label baseline -settle 12 -frames 3000 [-stage 2]
    /// Tự vào scene Game, bật StressTest, đo bằng PerfRecorder, ghi perf-log.txt rồi thoát.
    ///
    /// Vì sao cần: trong Editor, chính Editor tạo ~200 KB rác mỗi frame (đo cảnh 0 quái vẫn ra như vậy),
    /// nên số GC Alloc đo trong Editor vô nghĩa. Chạy bằng lệnh thì mỗi lần đo cùng điều kiện, lặp lại được.
    /// </summary>
    public class BenchmarkRunner : MonoBehaviour
    {
        [SerializeField] private StressTest _stressTest;
        [SerializeField] private PerfRecorder _recorder;
        [SerializeField] private Stages.StageSession _session;
        [Tooltip("Chờ bấy nhiêu giây (thời gian thực) sau khi bật StressTest rồi mới đo, cho quân số và pool ổn định.")]
        [SerializeField] private float _settleSeconds = 3f;

        private static bool IsBenchmark => Debug.isDebugBuild && HasArg("-benchmark");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SkipMainMenu()
        {
            // Build mở Boot trước; benchmark thì bảo Boot vào thẳng Game thay vì menu (không tự nạp scene ở đây,
            // để không tranh với việc nạp scene của Boot).
            if (IsBenchmark) SceneLoader.FirstSceneOverride = SceneLoader.GameScene;
        }

        private void Awake()
        {
            // Chọn màn ở Awake: StageSession đọc StageSelection trong Start, mà mọi Awake chạy trước mọi Start.
            if (!IsBenchmark) return;
            int stage = GetIntArg("-stage", 0);
            Stages.StageData[] stages = _session.Catalog.stages;
            if (stage >= 1 && stage <= stages.Length) Stages.StageSelection.Select(stages[stage - 1]);
        }

        private void Start()
        {
            if (!IsBenchmark)
            {
                // Bình thường (kể cả Editor) không làm gì: StressTest/PerfRecorder vẫn dùng tay bằng F9/F10.
                enabled = false;
                return;
            }

            // Player Settings để runInBackground = 0: cửa sổ mất focus là build ngừng chạy frame, benchmark treo
            // giữa chừng (đã gặp). Chỉ bật lúc chạy, trong chế độ benchmark, không đổi cài đặt của bản phát hành.
            Application.runInBackground = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            int enemies = GetIntArg("-enemies", _stressTest.TargetEnemyCount);
            bool weapons = GetIntArg("-weapons", 1) != 0;
            string label = GetStringArg("-label", $"benchmark-{enemies}");
            // Chờ đủ lâu để mọi pool đã tạo xong object (đo thấy ổn định sau ~8 giây ở 150 quái).
            float settle = GetIntArg("-settle", Mathf.RoundToInt(_settleSeconds));
            int frames = GetIntArg("-frames", 0);

            // Đo từng khẩu súng chính (Phóng lựu nổ vùng, Laser xuyên là 2 trường hợp nặng nhất).
            string mainWeapon = GetStringArg("-mainWeapon", null);
            if (!string.IsNullOrEmpty(mainWeapon))
            {
                var loadout = FindFirstObjectByType<Player.PlayerLoadout>();
                bool ok = loadout != null && loadout.EquipMainWeapon(mainWeapon);
                Debug.Log($"[Benchmark] Súng chính = {mainWeapon} ({(ok ? "ok" : "KHÔNG TÌM THẤY, giữ súng đang mặc")})");
                if (ok) label += $"-{mainWeapon}";
            }

            _stressTest.Configure(enemies, weapons);
            _stressTest.Begin();

            yield return new WaitForSecondsRealtime(settle);

            if (frames > 0) _recorder.BeginCapture(label, frames);
            else _recorder.BeginCapture(label);
            while (_recorder.IsCapturing) yield return null;

            Application.Quit();
        }

        private static bool HasArg(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        private static string GetStringArg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        private static int GetIntArg(string name, int fallback) =>
            int.TryParse(GetStringArg(name, null), out int value) ? value : fallback;
    }
}
