using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using TMPro;
using UnityEngine;
using ArenaSurvival.Stages;

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
        [SerializeField] private StageSession _session;

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

        [Header("Stage")]
        [Tooltip("Phần fill của thanh tiến độ màn (kéo neo phải giống thanh máu).")]
        [SerializeField] private RectTransform _stageProgressFill;
        [Tooltip("Banner \"ĐỢT TINH ANH!\" giữa màn hình, tắt sẵn.")]
        [SerializeField] private RectTransform _finalWaveBanner;
        [SerializeField] private float _bannerDuration = 2f;
        [Tooltip("Chữ trong banner: đổi theo tình huống (đợt tinh anh / boss xuất hiện / boss đổi pha).")]
        [SerializeField] private TMP_Text _bannerText;

        private int _shownSeconds = -1;
        private CanvasGroup _bannerGroup;
        private float _bannerTimer = -1f;

        private void OnEnable()
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _runStats.OnKillCountChanged += HandleKillCountChanged;
            GameEvents.OnXpChanged += HandleXpChanged;
            GameEvents.OnLevelUp += HandleLevelUp;
            _session.OnWaveStarted += HandleWaveStarted;
            _session.OnFinalWaveStarted += HandleFinalWaveStarted;
            _session.OnBossSpawned += HandleBossSpawned;
            _session.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
            _runStats.OnKillCountChanged -= HandleKillCountChanged;
            GameEvents.OnXpChanged -= HandleXpChanged;
            GameEvents.OnLevelUp -= HandleLevelUp;
            _session.OnWaveStarted -= HandleWaveStarted;
            _session.OnFinalWaveStarted -= HandleFinalWaveStarted;
            _session.OnBossSpawned -= HandleBossSpawned;
            _session.OnBossDefeated -= HandleBossDefeated;
            DetachBoss();
        }

        private void Awake()
        {
            if (_finalWaveBanner != null)
            {
                _bannerGroup = _finalWaveBanner.GetComponent<CanvasGroup>();
                _finalWaveBanner.gameObject.SetActive(false);
            }
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
            if (_stageProgressFill != null) SetFill(_stageProgressFill, _session.Progress);
            UpdateBanner();

            // Đồng hồ đổi mỗi frame nhưng chữ chỉ cần đổi mỗi giây.
            int seconds = (int)_runStats.SurvivalTime;
            if (seconds == _shownSeconds) return;

            _shownSeconds = seconds;
            _timerText.text = TimeFormatter.ToClock(seconds);
        }

        private void HandleHealthChanged(float current, float max)
        {
            SetFill(_healthFill, max > 0f ? current / max : 0f);
            // SetText với tham số số không tạo chuỗi mới (đổi mỗi lần Player bị đánh / hồi máu).
            _healthText.SetText("{0}/{1}", Mathf.CeilToInt(current), Mathf.CeilToInt(max));
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

        private void HandleWaveStarted(int wave, int total)
        {
            // Đợt cuối không ghi số: "ĐỢT CUỐI" báo rõ hơn là phải dọn sạch sân.
            string name = _session.Stage != null ? _session.Stage.displayName : "";
            string final = _session.HasBoss ? "BOSS" : "ĐỢT CUỐI";
            _waveText.text = _session.IsFinalWave ? $"{name} · {final}" : $"{name} · Đợt {wave}/{total}";
        }

        // Màn có boss: banner để dành cho lúc boss thật sự xuất hiện, không báo "đợt tinh anh" (chỉ là quân hộ tống).
        private void HandleFinalWaveStarted()
        {
            if (!_session.HasBoss) ShowBanner("ĐỢT TINH ANH!");
        }

        private Enemies.Enemy _boss;
        private System.Action<int> _onBossPhase;

        private void HandleBossSpawned(Enemies.Enemy boss)
        {
            DetachBoss();
            _boss = boss;
            if (_onBossPhase == null) _onBossPhase = HandleBossPhaseChanged;
            _boss.OnBossPhaseChanged += _onBossPhase;
            ShowBanner("BOSS XUẤT HIỆN!");
        }

        private void HandleBossPhaseChanged(int phase) => ShowBanner("BOSS NỔI GIẬN!");

        private void HandleBossDefeated() => DetachBoss();

        private void DetachBoss()
        {
            // Boss là object trong pool: phải gỡ, nếu không lần sau object này làm quái thường vẫn còn nối vào HUD.
            if (_boss != null && _onBossPhase != null) _boss.OnBossPhaseChanged -= _onBossPhase;
            _boss = null;
        }

        private void ShowBanner(string text)
        {
            if (_finalWaveBanner == null) return;
            if (_bannerText != null) _bannerText.text = text;
            _bannerTimer = 0f;
            _finalWaveBanner.gameObject.SetActive(true);
        }

        private void UpdateBanner()
        {
            if (_bannerTimer < 0f) return;

            _bannerTimer += Time.deltaTime;
            float t = _bannerTimer / _bannerDuration;
            if (t >= 1f)
            {
                _bannerTimer = -1f;
                _finalWaveBanner.gameObject.SetActive(false);
                return;
            }

            // Phóng nhanh từ to về cỡ thật trong 15% đầu, giữ, rồi mờ dần ở 25% cuối.
            float scale = t < 0.15f ? Mathf.Lerp(1.6f, 1f, t / 0.15f) : 1f;
            _finalWaveBanner.localScale = Vector3.one * scale;
            if (_bannerGroup != null) _bannerGroup.alpha = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
        }

        private void HandleKillCountChanged(int killCount)
        {
            // Chỉ số: icon đầu lâu bên cạnh đã nói lên đây là số quái đã hạ.
            _killText.SetText("{0}", killCount); // đổi mỗi lần hạ quái; ToString() sẽ tạo 1 chuỗi mỗi lần
        }

        // Thanh được vẽ bằng cách kéo neo phải của phần fill: 0 = rỗng, 1 = đầy.
        // Không cần sprite nào, và tự co giãn đúng theo kích thước thanh.
        private static void SetFill(RectTransform fill, float ratio)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }
    }
}
