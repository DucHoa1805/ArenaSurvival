using System;
using ArenaSurvival.Stats;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Tính chỉ số cuối và lực chiến từ các nguồn ngoài trận (hàm thuần). 8B thêm trang bị: chỉ cần cộng thêm 1 StatBlock ở đây.
    /// </summary>
    public static class StatCalculator
    {
        // Trọng số lực chiến khởi điểm: gần đúng "1 điểm chỉ số đáng giá bao nhiêu máu". Cân bằng lại ở Phase 10.
        public const float PowerPerHealth = 1f;
        public const float PowerPerArmor = 6f;
        public const float PowerPerDamagePercent = 12f;
        public const float PowerPerMoveSpeedPercent = 4f;
        public const float PowerPerRegen = 40f;

        /// <summary>Gốc nhân vật + Talent. levelOf trả về cấp của Talent theo id (0 nếu chưa nâng).</summary>
        public static StatBlock Compute(StatBlock heroBase, TalentCatalog talents, Func<string, int> levelOf)
        {
            StatBlock total = heroBase;
            if (talents == null || talents.talents == null || levelOf == null) return total;

            foreach (TalentData talent in talents.talents)
            {
                if (talent == null) continue;
                int level = levelOf(talent.id);
                if (level > 0) total = StatRules.Add(total, talent.stat, talent.valuePerLevel * level);
            }
            return total;
        }

        /// <summary>Gốc + Talent + trang bị (tổng chỉ số các món đang mặc, 8B).</summary>
        public static StatBlock Compute(StatBlock heroBase, TalentCatalog talents, Func<string, int> levelOf, StatBlock equipment) =>
            Compute(heroBase, talents, levelOf) + equipment;

        /// <summary>Lực chiến: tổng có trọng số, để người chơi so sức mạnh bằng 1 con số (plan A4).</summary>
        public static int Power(StatBlock s) => Mathf.RoundToInt(
            s.maxHealth * PowerPerHealth +
            s.armor * PowerPerArmor +
            s.damagePercent * PowerPerDamagePercent +
            s.moveSpeedPercent * PowerPerMoveSpeedPercent +
            s.healthRegen * PowerPerRegen);
    }
}
