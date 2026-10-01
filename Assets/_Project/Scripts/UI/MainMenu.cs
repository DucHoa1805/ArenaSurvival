using ArenaSurvival.Core;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button _playButton;
        [SerializeField] private UnityEngine.UI.Button _quitButton;
        [Tooltip("Nút \"Chơi\" vào màn đầu tiên của danh sách này (tạm thời, tới khi có màn chọn màn ở Hub - Phase 9).")]
        [SerializeField] private StageCatalog _catalog;
        [Tooltip("Chữ trên nút Chơi: ghi luôn màn sẽ vào.")]
        [SerializeField] private TMP_Text _playLabel;
        [Tooltip("Số vàng hiện có.")]
        [SerializeField] private TMP_Text _goldText;

        private StageData _nextStage;
        private System.Action<string, long> _onCurrencyChanged;

        public StageCatalog Catalog => _catalog;

        [Header("Nâng cấp (Talent)")]
        [SerializeField] private UnityEngine.UI.Button _talentButton;
        [Tooltip("Prefab TalentPanel trong scene, tắt sẵn; tự đóng bằng nút Đóng của nó.")]
        [SerializeField] private GameObject _talentPanel;

        [Header("Trang bị")]
        [SerializeField] private UnityEngine.UI.Button _equipmentButton;
        [Tooltip("Prefab EquipmentPanel trong scene, tắt sẵn; tự đóng bằng nút Đóng của nó.")]
        [SerializeField] private GameObject _equipmentPanel;

        [Header("Cài đặt")]
        [SerializeField] private UnityEngine.UI.Button _settingsButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private UnityEngine.UI.Button _closeSettingsButton;

        private void Awake()
        {
            _playButton.onClick.AddListener(HandlePlayClicked);
            _quitButton.onClick.AddListener(HandleQuitClicked);
            _settingsButton.onClick.AddListener(OpenSettings);
            _closeSettingsButton.onClick.AddListener(CloseSettings);
            _settingsPanel.SetActive(false);
            if (_talentButton != null) _talentButton.onClick.AddListener(OpenTalents);
            if (_talentPanel != null) _talentPanel.SetActive(false);
            if (_equipmentButton != null) _equipmentButton.onClick.AddListener(OpenEquipment);
            if (_equipmentPanel != null) _equipmentPanel.SetActive(false);
        }

        private void OpenTalents() => _talentPanel.SetActive(true);
        private void OpenEquipment() => _equipmentPanel.SetActive(true);

        private void Start()
        {
            // Vào màn chưa thắng đầu tiên (thắng hết thì màn cuối) — tạm thời tới khi có màn chọn màn ở Hub (Phase 9).
            _nextStage = GameServices.StageProgress.NextStage(_catalog);
            if (_playLabel != null && _nextStage != null) _playLabel.text = $"Chơi {_nextStage.displayName}";
            if (_goldText != null) _goldText.SetText("{0}", GameServices.Economy.GetBalance(CurrencyIds.Gold));

            // Số vàng đổi ngay khi ví đổi (menu dev cộng tiền; sau này Hub nhận quà) chứ không chỉ lúc mở menu.
            _onCurrencyChanged = HandleCurrencyChanged;
            GameServices.Economy.OnCurrencyChanged += _onCurrencyChanged;
        }

        private void HandleCurrencyChanged(string currencyId, long balance)
        {
            if (currencyId == CurrencyIds.Gold && _goldText != null) _goldText.SetText("{0}", balance);
        }

        private void OnDestroy()
        {
            // Dịch vụ sống lâu hơn scene: không gỡ thì ví giữ tham chiếu tới menu đã bị hủy.
            if (_onCurrencyChanged != null && GameServices.Economy != null) GameServices.Economy.OnCurrencyChanged -= _onCurrencyChanged;
            _playButton.onClick.RemoveListener(HandlePlayClicked);
            _quitButton.onClick.RemoveListener(HandleQuitClicked);
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _closeSettingsButton.onClick.RemoveListener(CloseSettings);
            if (_talentButton != null) _talentButton.onClick.RemoveListener(OpenTalents);
            if (_equipmentButton != null) _equipmentButton.onClick.RemoveListener(OpenEquipment);
        }

        // Tắt bảng là đủ để lưu: VolumeSlider tự ghi xuống đĩa trong OnDisable.
        private void OpenSettings() => _settingsPanel.SetActive(true);
        private void CloseSettings() => _settingsPanel.SetActive(false);

        private void HandlePlayClicked()
        {
            // Nạp scene là bất đồng bộ: khóa nút để bấm 2 lần không nạp scene Game 2 lần.
            _playButton.interactable = false;
            StageSelection.Select(_nextStage != null ? _nextStage : _catalog.First);
            SceneLoader.LoadGame();
        }

        private void HandleQuitClicked()
        {
            // Application.Quit() không có tác dụng trong Editor (và trên WebGL), nên trong Editor
            // thoát Play mode để nút vẫn thử được.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
