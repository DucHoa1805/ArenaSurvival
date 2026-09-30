using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// HUD trong lúc chơi: máu, XP, cấp độ, wave, thời gian sống sót, số quái đã hạ.
    /// Chỉ hiển thị — mọi số liệu đến từ event của gameplay; gameplay không biết HUD tồn tại.
    /// Text chỉ được gán lại khi giá trị thật sự đổi, không gán mỗi frame (tránh tạo rác string).
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private Health _playerHealth;
        [SerializeField] private RunStats _runStats;

        [Header("Health")]
        [SerializeField] private RectTransform _healthFill;
        [SerializeField] private TMP_Text _healthText;

        [Header("Experience")]
        [SerializeField] private RectTransform _xpFill;
        [SerializeField] private TMP_Text _levelText;

        [Header("Run Info")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _waveText;
        [SerializeField] private TMP_Text _killText;

        private int _shownSeconds = -1;

        private void OnEnable()
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _runStats.OnKillCountChanged += HandleKillCountChanged;
            GameEvents.OnXpChanged += HandleXpChanged;
            GameEvents.OnLevelUp += HandleLevelUp;
            GameEvents.OnWaveStarted += HandleWaveStarted;
        }

        private void OnDisable()
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
            _runStats.OnKillCountChanged -= HandleKillCountChanged;
            GameEvents.OnXpChanged -= HandleXpChanged;
            GameEvents.OnLevelUp -= HandleLevelUp;
            GameEvents.OnWaveStarted -= HandleWaveStarted;
        }

        private void Start()
        {
            // Giá trị ban đầu: Health không phát event lúc khởi tạo nên đọc trực tiếp 1 lần.
            HandleHealthChanged(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
            HandleKillCountChanged(_runStats.KillCount);
            HandleLevelUp(1);
        }

        private void Update()
        {
            // Đồng hồ đổi mỗi frame nhưng chữ chỉ cần đổi mỗi giây.
            int seconds = (int)_runStats.SurvivalTime;
            if (seconds == _shownSeconds) return;

            _shownSeconds = seconds;
            _timerText.text = TimeFormatter.ToClock(seconds);
        }

        private void HandleHealthChanged(float current, float max)
        {
            SetFill(_healthFill, max > 0f ? current / max : 0f);
            _healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        private void HandleXpChanged(int currentXp, int xpToNextLevel)
        {
            SetFill(_xpFill, xpToNextLevel > 0 ? (float)currentXp / xpToNextLevel : 0f);
        }

        private void HandleLevelUp(int newLevel)
        {
            // Chỉ số: chữ "CẤP" là nhãn cố định nằm cạnh trong huy hiệu.
            _levelText.text = newLevel.ToString();
        }

        private void HandleWaveStarted(int waveNumber)
        {
            _waveText.text = $"Wave {waveNumber}";
        }

        private void HandleKillCountChanged(int killCount)
        {
            // Chỉ số: icon đầu lâu bên cạnh đã nói lên đây là số quái đã hạ.
            _killText.text = killCount.ToString();
        }

        // Thanh được vẽ bằng cách kéo neo phải của phần fill: 0 = rỗng, 1 = đầy.
        // Không cần sprite nào, và tự co giãn đúng theo kích thước thanh.
        private static void SetFill(RectTransform fill, float ratio)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }
    }
}
