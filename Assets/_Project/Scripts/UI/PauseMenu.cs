using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Menu tạm dừng. Tự hiện/ẩn theo GameEvents.OnGameStateChanged nên không ai phải gọi nó —
    /// dù Pause đến từ phím Esc hay từ nơi khác, menu vẫn phản ứng đúng.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _content;
        [SerializeField] private UnityEngine.UI.Button _resumeButton;
        [SerializeField] private UnityEngine.UI.Button _mainMenuButton;

        private void Awake()
        {
            _content.SetActive(false);
            _resumeButton.onClick.AddListener(HandleResumeClicked);
            _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }

        private void OnDestroy()
        {
            _resumeButton.onClick.RemoveListener(HandleResumeClicked);
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
            _content.SetActive(newState == GameState.Paused);
        }

        private void HandleResumeClicked()
        {
            GameManager.Instance.ResumeGame();
        }

        private void HandleMainMenuClicked()
        {
            SceneLoader.LoadMainMenu();
        }
    }
}
