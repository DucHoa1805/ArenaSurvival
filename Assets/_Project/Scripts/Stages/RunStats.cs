using System;
using UnityEngine;
using ArenaSurvival.Core;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Số liệu của ván chơi hiện tại: thời gian sống sót và số quái đã hạ.
    /// Tách khỏi HUD vì đây là dữ liệu của game chứ không phải của giao diện — màn Game Over
    /// và phần lưu điểm cao cũng cần đọc cùng số liệu này.
    /// </summary>
    public class RunStats : MonoBehaviour
    {
        public float SurvivalTime { get; private set; }
        public int KillCount { get; private set; }

        public event Action<int> OnKillCountChanged;

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void Update()
        {
            // Chỉ tính giờ khi đang chơi — không tính lúc ở menu, tạm dừng, chọn nâng cấp hay đã thua.
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            SurvivalTime += Time.deltaTime;
        }

        private void HandleEnemyKilled(int xpReward, Vector3 position)
        {
            KillCount++;
            OnKillCountChanged?.Invoke(KillCount);
        }
    }
}
