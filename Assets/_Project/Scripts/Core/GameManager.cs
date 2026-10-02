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

        /// <summary>
        /// timeScale khi đang chơi (mặc định 1). Bot tự chơi đặt cao hơn để chạy nhanh; mọi chỗ "chơi tiếp" dùng giá trị này
        /// chứ không đặt cứng 1, nếu không mỗi lần lên cấp/hồi sinh xong game lại về tốc độ thường.
        /// </summary>
        public static float PlayingTimeScale = 1f;

        // Editor tắt domain reload thì biến static còn giá trị từ lần Play trước.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => PlayingTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            // Instance là static nên sống qua lần nạp scene — dọn đi để scene Game nạp lại
            // (Retry) không nhìn thấy GameManager cũ đã bị hủy.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // GameManager chỉ tồn tại trong scene Game; scene này được nạp nghĩa là người chơi
            // đã bấm Play (hoặc Retry), nên vào chơi luôn.
            StartGame();
        }

        public void StartGame()
        {
            SetState(GameState.Playing);
            Time.timeScale = PlayingTimeScale;
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
            Time.timeScale = PlayingTimeScale;
        }

        /// <summary>Dùng cho phím Pause: đang chơi thì dừng, đang dừng thì chơi tiếp, trạng thái khác thì bỏ qua.</summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        public void BeginLevelUp()
        {
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.LevelUp);
            Time.timeScale = 0f;
        }

        public void EndLevelUp()
        {
            if (CurrentState != GameState.LevelUp) return;

            SetState(GameState.Playing);
            Time.timeScale = PlayingTimeScale;
        }

        // Không tự vào GameOver khi Player chết nữa: GameManager không biết còn lượt hồi sinh hay không.
        // StageSession quyết định rồi gọi một trong các hàm dưới.

        public void BeginRevivePrompt()
        {
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.RevivePrompt);
            Time.timeScale = 0f;
        }

        public void Revive()
        {
            if (CurrentState != GameState.RevivePrompt) return;

            SetState(GameState.Playing);
            Time.timeScale = PlayingTimeScale;
        }

        /// <summary>Kết thúc màn: thắng → Victory, thua → GameOver. Gọi được từ Playing, LevelUp hoặc RevivePrompt.</summary>
        public void EndStage(bool victory)
        {
            if (CurrentState == GameState.Victory || CurrentState == GameState.GameOver) return;

            SetState(victory ? GameState.Victory : GameState.GameOver);
            Time.timeScale = 0f;
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            GameEvents.GameStateChanged(newState);
        }
    }
}