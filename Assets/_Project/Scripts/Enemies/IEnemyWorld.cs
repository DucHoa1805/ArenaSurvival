using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Những gì hành vi quái cần từ "thế giới" (triệu hồi quái con, giữ trong sân), tách thành interface để
    /// Enemies không phụ thuộc ngược vào Stages (WaveSpawner nằm ở Stages và implement interface này).
    /// </summary>
    public interface IEnemyWorld
    {
        Enemy Summon(EnemyData data, EnemyModifiers modifiers, Vector2 position);
        int CountAlive(EnemyData data);
        Vector2 ClampToArena(Vector2 position);
        /// <summary>Quái chết vì trúng đòn (không gọi khi nổ theo boss) — để rơi đồ theo bảng của chính con đó.</summary>
        void NotifyKilled(Enemy enemy);
    }
}
