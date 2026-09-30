using System;
using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Save
{
    /// <summary>
    /// Khi ván chơi kết thúc: so kết quả trong RunStats với kỷ lục đã lưu, cập nhật và ghi file nếu phá kỷ lục.
    /// Logic này nằm ở đây chứ không nằm trong GameOverPanel, vì UI chỉ hiển thị, không quyết định dữ liệu.
    /// </summary>
    public class HighScoreTracker : MonoBehaviour
    {
        [SerializeField] private RunStats _runStats;

        public SaveData Best { get; private set; }
        public bool IsNewBestTime { get; private set; }

        /// <summary>Phát sau khi đã so sánh và lưu kỷ lục của ván vừa kết thúc.</summary>
        public event Action OnRecordsUpdated;

        private void Awake()
        {
            // Đọc 1 lần lúc vào scene, không đọc file mỗi lần cần so sánh.
            Best = SaveSystem.Load();
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
            if (newState != GameState.GameOver) return;

            IsNewBestTime = _runStats.SurvivalTime > Best.bestSurvivalTime;
            bool isNewBestKills = _runStats.KillCount > Best.bestKillCount;

            if (IsNewBestTime) Best.bestSurvivalTime = _runStats.SurvivalTime;
            if (isNewBestKills) Best.bestKillCount = _runStats.KillCount;

            // Chỉ ghi file khi thật sự có gì mới.
            if (IsNewBestTime || isNewBestKills)
            {
                SaveSystem.Save(Best);
            }

            OnRecordsUpdated?.Invoke();
        }
    }
}
