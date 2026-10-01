using ArenaSurvival.Core;
using ArenaSurvival.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Màn kết quả của 1 màn, dùng chung cho thắng và thua. Chỉ HIỂN THỊ StageResult mà StageSession tạo ra —
    /// không tự tính sao hay phần thưởng, để quy tắc nằm một chỗ (StageRules) và có unit test.
    /// </summary>
    public class StageResultPanel : MonoBehaviour
    {
        [SerializeField] private StageSession _session;
        [SerializeField] private GameObject _content;

        [Header("Tiêu đề")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _stageText;
        [Tooltip("Viền khung: vàng khi thắng, đỏ khi thua.")]
        [SerializeField] private Image _frame;

        [Header("Sao")]
        [SerializeField] private Image[] _stars = new Image[3];
        [SerializeField] private TMP_Text[] _starLabels = new TMP_Text[3];

        [Header("Thống kê & phần thưởng")]
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _killText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _xpText;

        [Header("Nút")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;

        private static readonly Color VictoryColor = new Color(0.949f, 0.710f, 0.267f);
        private static readonly Color DefeatColor = new Color(0.878f, 0.275f, 0.235f);
        private static readonly Color StarOn = Color.white;
        private static readonly Color StarOff = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color LabelOn = new Color(0.957f, 0.945f, 0.918f);
        private static readonly Color LabelOff = new Color(0.663f, 0.706f, 0.769f, 0.6f);

        private void Awake()
        {
            _content.SetActive(false);
            _nextButton.onClick.AddListener(HandleNextClicked);
            _retryButton.onClick.AddListener(HandleRetryClicked);
            _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }

        private void OnDestroy()
        {
            _nextButton.onClick.RemoveListener(HandleNextClicked);
            _retryButton.onClick.RemoveListener(HandleRetryClicked);
            _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        private void OnEnable() => GameEvents.OnGameStateChanged += HandleGameStateChanged;

        private void OnDisable() => GameEvents.OnGameStateChanged -= HandleGameStateChanged;

        private void HandleGameStateChanged(GameState state)
        {
            bool ended = state == GameState.Victory || state == GameState.GameOver;
            // StageSession tạo Result TRƯỚC khi đổi trạng thái, nên lúc này đã đọc được.
            if (!ended || _session.Result == null)
            {
                _content.SetActive(false);
                return;
            }

            Show(_session.Result, _session.Stage);
        }

        private void Show(StageResult result, StageData stage)
        {
            Color accent = result.victory ? VictoryColor : DefeatColor;
            _titleText.text = result.victory ? "CHIẾN THẮNG!" : "THẤT BẠI";
            _titleText.color = accent;
            _stageText.text = $"Màn {stage.displayName}";
            if (_frame != null) _frame.color = accent;

            string[] labels = { "Thắng màn", "Còn trên 50% máu", $"Dưới {TimeFormatter.ToClock(stage.targetTime)}" };
            for (int i = 0; i < _stars.Length; i++)
            {
                bool on = result.starFlags != null && i < result.starFlags.Length && result.starFlags[i];
                _stars[i].color = on ? StarOn : StarOff;
                _starLabels[i].text = labels[i];
                _starLabels[i].color = on ? LabelOn : LabelOff;
            }

            _timeText.text = $"Thời gian: {TimeFormatter.ToClock(result.clearTime)}";
            _killText.text = $"Quái đã hạ: {result.killCount}";
            _goldText.text = $"+{result.gold}";
            _xpText.text = $"+{result.accountXp} XP";

            // Màn cuối danh sách thì không có "Màn tiếp"; thua thì phải chơi lại chứ không được đi tiếp.
            _nextButton.gameObject.SetActive(result.victory && _session.Catalog.GetNext(stage) != null);
            _content.SetActive(true);
        }

        private void HandleNextClicked()
        {
            StageData next = _session.Catalog.GetNext(_session.Stage);
            if (next == null) return;
            StageSelection.Select(next);
            SceneLoader.LoadGame();
        }

        private void HandleRetryClicked()
        {
            // Chọn lại đúng màn đang chơi: nạp lại scene Game là cách reset màn sạch nhất.
            StageSelection.Select(_session.Stage);
            SceneLoader.LoadGame();
        }

        private void HandleMainMenuClicked() => SceneLoader.LoadMainMenu();
    }
}
