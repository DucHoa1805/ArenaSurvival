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

        /// <summary>Nhân mọi trường (hệ số độ hiếm trang bị, chỉ số mỗi cấp × số cấp).</summary>
        public static StatBlock operator *(StatBlock a, float k) => new StatBlock
        {
            maxHealth = a.maxHealth * k,
            armor = a.armor * k,
            damagePercent = a.damagePercent * k,
            moveSpeedPercent = a.moveSpeedPercent * k,
            healthRegen = a.healthRegen * k,
        };
    }

    public static class StatRules
    {
        /// <summary>Đọc giá trị 1 trường theo kind (UI liệt kê chỉ số của trang bị mà không viết tay từng trường).</summary>
        public static float Get(StatBlock stats, StatKind kind)
        {
            switch (kind)
            {
                case StatKind.MaxHealth: return stats.maxHealth;
                case StatKind.Armor: return stats.armor;
                case StatKind.DamagePercent: return stats.damagePercent;
                case StatKind.MoveSpeedPercent: return stats.moveSpeedPercent;
                case StatKind.HealthRegen: return stats.healthRegen;
                default: return 0f;
            }
        }

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
