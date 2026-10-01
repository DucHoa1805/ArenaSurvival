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
        }

        private void Start()
        {
            // Vào màn chưa thắng đầu tiên (thắng hết thì màn cuối) — tạm thời tới khi có màn chọn màn ở Hub (Phase 9).
            _nextStage = GameServices.StageProgress.NextStage(_catalog);
            if (_playLabel != null && _nextStage != null) _playLabel.text = $"Chơi {_nextStage.displayName}";
            if (_goldText != null) _goldText.SetText("{0}", GameServices.Economy.GetBalance(CurrencyIds.Gold));
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(HandlePlayClicked);
            _quitButton.onClick.RemoveListener(HandleQuitClicked);
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _closeSettingsButton.onClick.RemoveListener(CloseSettings);
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
