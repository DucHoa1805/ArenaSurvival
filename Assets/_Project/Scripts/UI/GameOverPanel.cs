using ArenaSurvival.Core;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Màn hình thua: hiện kết quả ván chơi (đọc từ RunStats) và cho Retry hoặc về menu chính.
    /// Tự hiện khi game chuyển sang trạng thái GameOver.
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private RunStats _runStats;
        [SerializeField] private GameObject _content;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _killText;
        [SerializeField] private UnityEngine.UI.Button _retryButton;
        [SerializeField] private UnityEngine.UI.Button _mainMenuButton;

        private void Awake()
        {
            _content.SetActive(false);
            _retryButton.onClick.AddListener(HandleRetryClicked);
            _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }

        private void OnDestroy()
        {
            _retryButton.onClick.RemoveListener(HandleRetryClicked);
            _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState != GameState.GameOver)
            {
                _content.SetActive(false);
                return;
            }

            _timeText.text = $"Thời gian sống sót: {TimeFormatter.ToClock(_runStats.SurvivalTime)}";
            _killText.text = $"Quái đã hạ: {_runStats.KillCount}";
            _content.SetActive(true);
        }

        private void HandleRetryClicked()
        {
            SceneLoader.LoadGame();
        }

        private void HandleMainMenuClicked()
        {
            SceneLoader.LoadMainMenu();
        }
    }
}
