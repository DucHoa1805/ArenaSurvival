using ArenaSurvival.Combat;
using ArenaSurvival.Enemies;
using ArenaSurvival.RunUpgrades;
using UnityEngine;
using UnityEngine.InputSystem;
using ArenaSurvival.Weapons;
using ArenaSurvival.Stages;

namespace ArenaSurvival.DevTools
{
    /// <summary>
    /// Dựng 1 cảnh "đông quái" cố định để đo hiệu năng: giữ đúng N quái trên sân, Player bất tử, tắt XP
    /// để bảng lên cấp không chen ngang, tùy chọn mở hết vũ khí để có nhiều đạn/lưỡi cưa nhất.
    /// Cảnh phải giống nhau giữa các lần đo thì số liệu trước/sau tối ưu mới so được với nhau.
    ///
    /// Chỉ chạy trong Editor và Development build (tự gỡ ở bản build thật). F9 bật/tắt.
    /// </summary>
    public class StressTest : MonoBehaviour
    {
        [SerializeField] private WaveSpawner _spawner;
        [SerializeField] private StageSession _session;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private ExperienceSystem _experience;

        [SerializeField] private int _targetEnemyCount = 150;
        [Tooltip("Mở mọi vũ khí của Player khi bật, để cảnh có nhiều đạn và lưỡi cưa nhất.")]
        [SerializeField] private bool _unlockAllWeapons = true;
        [Tooltip("Sinh tối đa bấy nhiêu quái mỗi frame khi bù quân số, tránh 1 frame giật lớn làm lệch số đo.")]
        [SerializeField] private int _maxSpawnPerFrame = 25;

        public bool IsRunning { get; private set; }
        public int TargetEnemyCount => _targetEnemyCount;

        private int _previousFrameRate;
        private int _previousVSync;

        private void Awake()
        {
            // Debug.isDebugBuild = true trong Editor và Development build: bản phát hành không bao giờ có công cụ này.
            if (!Debug.isDebugBuild) Destroy(this);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (IsRunning) Stop();
                else Begin();
            }

            if (!IsRunning) return;

            int missing = _targetEnemyCount - _spawner.ActiveCount;
            if (missing > 0) _spawner.SpawnImmediately(Mathf.Min(missing, _maxSpawnPerFrame));
        }

        /// <summary>Đổi kịch bản trước khi Begin (BenchmarkRunner đọc từ dòng lệnh).</summary>
        public void Configure(int targetEnemyCount, bool unlockAllWeapons)
        {
            _targetEnemyCount = Mathf.Max(0, targetEnemyCount);
            _unlockAllWeapons = unlockAllWeapons;
        }

        public void Begin()
        {
            if (IsRunning) return;
            IsRunning = true;

            _playerHealth.Invulnerable = true;
            // Tắt hệ XP chứ không tự chọn nâng cấp: tự chọn thì vũ khí/chỉ số đổi dần theo thời gian,
            // và lần đo sau không còn cùng điều kiện với lần đo trước.
            _experience.enabled = false;
            // Spawner tắt Update = ngừng sinh theo nhịp đợt; quân số do StressTest giữ cố định.
            _spawner.enabled = false;
            // Màn không được tự sang đợt cuối hay kết thúc giữa lúc đang đo.
            _session.PauseProgression(true);

            if (_unlockAllWeapons)
            {
                foreach (MonoBehaviour behaviour in _playerHealth.GetComponents<MonoBehaviour>())
                {
                    if (behaviour is IWeapon) behaviour.enabled = true;
                    // Lưỡi cưa ở cấp tối đa (8 lưỡi) — trường hợp nặng nhất cho phép đo.
                    if (behaviour is OrbitWeapon orbit) orbit.SetLevel(6);
                }
                // Kỹ năng trong màn (10C) ở cấp tối đa để benchmark đo cả chi phí của chúng.
                var go = _playerHealth.gameObject;
                go.GetComponent<Player.PlayerShield>()?.SetLevel(5);
                go.GetComponent<Player.PlayerAura>()?.SetLevel(5);
                go.GetComponent<Player.PlayerMissiles>()?.SetLevel(5);
                go.GetComponent<Player.PlayerLightning>()?.SetLevel(5);
                go.GetComponent<Player.PlayerDrones>()?.SetLevel(4);
                var stats = go.GetComponent<Player.PlayerStats>();
                if (stats != null) { stats.ExtraProjectiles = 4; stats.ExtraPierce = 3; }
            }

            // Bỏ giới hạn fps (tắt VSync, targetFrameRate = -1): nếu khóa 60 fps thì "Main Thread" luôn ~16.7 ms
            // vì gồm cả thời gian ngồi chờ, không cho thấy game thật sự tốn bao nhiêu.
            _previousFrameRate = Application.targetFrameRate;
            _previousVSync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            Debug.Log($"[StressTest] BẬT: giữ {_targetEnemyCount} quái, Player bất tử, tắt XP, mở hết vũ khí = {_unlockAllWeapons}.");
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;

            _playerHealth.Invulnerable = false;
            _experience.enabled = true;
            _spawner.enabled = true;
            _session.PauseProgression(false);
            QualitySettings.vSyncCount = _previousVSync;
            Application.targetFrameRate = _previousFrameRate;

            Debug.Log("[StressTest] TẮT.");
        }
    }
}
