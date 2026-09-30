using UnityEngine;

namespace ArenaSurvival.Core
{
    /// <summary>
    /// Singleton có kiểm soát — chỉ dùng cho đúng 1 việc: quản lý trạng thái toàn cục của game.
    /// Nhược điểm của Singleton (khó test, trạng thái ẩn toàn cục) được chấp nhận ở quy mô
    /// dự án này, nhưng cần biết rõ đánh đổi đó khi giải thích lúc phỏng vấn.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Menu;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // TODO (Phase 4): bỏ dòng này khi có Main Menu thật gọi StartGame() qua nút bấm.
            StartGame();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        public void StartGame()
        {
            SetState(GameState.Playing);
            Time.timeScale = 1f;
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.Paused);
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            SetState(GameState.Playing);
            Time.timeScale = 1f;
        }

        private void HandlePlayerDied()
        {
            SetState(GameState.GameOver);
            Time.timeScale = 0f;
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            GameEvents.GameStateChanged(newState);
        }
    }
}