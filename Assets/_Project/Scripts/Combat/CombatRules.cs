using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>Công thức chiến đấu dạng hàm thuần (test được, đổi số ở một chỗ).</summary>
    public static class CombatRules
    {
        /// <summary>
        /// Giáp giảm sát thương theo plan D1: damage × 100 / (100 + giáp). 100 giáp = giảm một nửa; giáp càng cao càng ít lời thêm
        /// và không bao giờ chặn 100% — tránh build "bất tử" làm mất thử thách.
        /// </summary>
        public static float ApplyArmor(float damage, float armor) => damage * 100f / (100f + Mathf.Max(0f, armor));
    }
}
