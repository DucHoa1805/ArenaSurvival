using System;

namespace ArenaSurvival.Stats
{
    /// <summary>Loại chỉ số — chỉ để chọn TRƯỜNG nào của StatBlock; thêm Talent/trang bị mới không cần sửa code nào khác.</summary>
    public enum StatKind
    {
        MaxHealth,
        Armor,
        DamagePercent,
        MoveSpeedPercent,
        HealthRegen,
    }

    /// <summary>
    /// Bộ chỉ số của nhân vật ngoài trận. Cộng được với nhau, đúng công thức plan (A4):
    /// chỉ số = gốc nhân vật + Talent + trang bị (8B) — mỗi nguồn là 1 StatBlock.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public float maxHealth;
        public float armor;
        /// <summary>% sát thương cộng thêm (6 = +6%).</summary>
        public float damagePercent;
        /// <summary>% tốc chạy cộng thêm.</summary>
        public float moveSpeedPercent;
        /// <summary>Máu hồi mỗi giây.</summary>
        public float healthRegen;

        public static StatBlock operator +(StatBlock a, StatBlock b) => new StatBlock
        {
            maxHealth = a.maxHealth + b.maxHealth,
            armor = a.armor + b.armor,
            damagePercent = a.damagePercent + b.damagePercent,
            moveSpeedPercent = a.moveSpeedPercent + b.moveSpeedPercent,
            healthRegen = a.healthRegen + b.healthRegen,
        };
    }

    public static class StatRules
    {
        /// <summary>Cộng value vào đúng trường theo kind.</summary>
        public static StatBlock Add(StatBlock stats, StatKind kind, float value)
        {
            switch (kind)
            {
                case StatKind.MaxHealth: stats.maxHealth += value; break;
                case StatKind.Armor: stats.armor += value; break;
                case StatKind.DamagePercent: stats.damagePercent += value; break;
                case StatKind.MoveSpeedPercent: stats.moveSpeedPercent += value; break;
                case StatKind.HealthRegen: stats.healthRegen += value; break;
            }
            return stats;
        }
    }
}
