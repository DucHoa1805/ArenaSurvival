using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Hệ số áp lên 1 con quái lúc sinh ra, trên nền EnemyData. Dùng chung cho độ khó theo màn và quái tinh anh,
    /// nên không phải tạo asset riêng cho "Tank màn 3" hay "Tank tinh anh".
    /// </summary>
    public struct EnemyModifiers
    {
        public float health;
        public float damage;
        public float scale;
        public Color tint;

        public static EnemyModifiers Default => new EnemyModifiers { health = 1f, damage = 1f, scale = 1f, tint = Color.white };

        public static EnemyModifiers ForDifficulty(float multiplier) =>
            new EnemyModifiers { health = multiplier, damage = multiplier, scale = 1f, tint = Color.white };
    }
}
