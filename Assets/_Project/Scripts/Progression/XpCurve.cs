using System;

namespace ArenaSurvival.Progression
{
    /// <summary>
    /// Công thức XP cần để lên cấp — logic thuần, không phụ thuộc UnityEngine,
    /// nên viết unit test EditMode được mà không cần dựng scene.
    /// </summary>
    public static class XpCurve
    {
        public const int BaseXp = 5;          // XP cần để từ cấp 1 lên cấp 2
        public const int XpGrowthPerLevel = 5; // mỗi cấp sau cần thêm bấy nhiêu

        /// <summary>XP cần có để từ <paramref name="level"/> lên cấp kế tiếp (cấp bắt đầu từ 1).</summary>
        public static int GetXpRequired(int level)
        {
            return BaseXp + XpGrowthPerLevel * (Math.Max(1, level) - 1);
        }
    }
}
