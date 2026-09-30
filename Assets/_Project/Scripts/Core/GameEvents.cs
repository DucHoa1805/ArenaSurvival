using System;
using UnityEngine;

namespace ArenaSurvival.Core
{
    /// <summary>
    /// Hub sự kiện tĩnh dùng chung cho toàn bộ game — nơi các hệ thống "hét lên" khi có chuyện
    /// xảy ra, mà không cần biết ai đang lắng nghe. Ví dụ: Enemy chết chỉ cần gọi EnemyKilled(),
    /// không cần biết ProgressionSystem hay UI có tồn tại hay không.
    ///
    /// Đánh đổi: dùng static event dễ setup nhưng cũng dễ lạm dụng nếu để mọi thứ đi qua đây.
    /// Chỉ nên dùng cho sự kiện thực sự mang tính "toàn cục" (global), không dùng để thay thế
    /// giao tiếp giữa 2 script có quan hệ cha-con rõ ràng (lúc đó cứ gọi trực tiếp cho đơn giản).
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int, Vector3> OnEnemyKilled; // (xpReward, vị trí chết)
        public static event Action<int> OnXpGained;
        public static event Action<int> OnLevelUp;
        public static event Action OnPlayerDied;
        public static event Action<GameState> OnGameStateChanged;

        public static void EnemyKilled(int xpReward, Vector3 position) => OnEnemyKilled?.Invoke(xpReward, position);
        public static void XpGained(int amount) => OnXpGained?.Invoke(amount);
        public static void LevelUp(int newLevel) => OnLevelUp?.Invoke(newLevel);
        public static void PlayerDied() => OnPlayerDied?.Invoke();
        public static void GameStateChanged(GameState newState) => OnGameStateChanged?.Invoke(newState);
    }
}