using UnityEngine;

namespace ArenaSurvival.RunUpgrades
{
    /// <summary>
    /// Công thức kỹ năng trong màn (hàm thuần, xem spec "run skills" 10C). Mọi con số theo cấp nằm ở đây để test được
    /// và để bảng lên cấp, component kỹ năng, bot dùng cùng một nguồn.
    /// </summary>
    public static class SkillRules
    {
        public const int MaxSawBlades = 8;
        public const float SpreadPerExtraBullet = 10f;

        /// <summary>maxLevel = 0: kỹ năng lặp vô hạn (Hồi máu), không chiếm chỗ trong giới hạn kỹ năng.</summary>
        public static bool CountsAsSkill(int maxLevel) => maxLevel > 0;

        /// <summary>
        /// Còn nâng được và (đã có rồi, hoặc còn chỗ cho kỹ năng mới). Giới hạn số kỹ năng buộc người chơi chọn hướng đánh
        /// thay vì nhặt đủ mọi thứ mỗi màn.
        /// </summary>
        public static bool IsOfferable(int level, int maxLevel, int distinctOwned, int maxSkills)
        {
            if (!CountsAsSkill(maxLevel)) return true;
            if (level >= maxLevel) return false;
            return level > 0 || distinctOwned < maxSkills;
        }

        public static int SawBladeCount(int level) => Mathf.Clamp(2 + level, 3, MaxSawBlades);

        public static float SawBladeDamage(float baseDamage, int level) => baseDamage * (1f + 0.1f * Mathf.Max(0, level - 1));

        public static float ShieldCooldown(int level) => Mathf.Max(4f, 9f - Mathf.Max(1, level));

        public static int ShieldLayers(int level) => level >= 5 ? 2 : 1;

        public static float AuraRadius(int level) => 1.6f + 0.25f * Mathf.Max(1, level);

        public static float AuraDamage(int level) => 4f + 2f * Mathf.Max(1, level);

        public static int MissileCount(int level) => 1 + (Mathf.Max(1, level) - 1) / 2;

        public static float MissileDamage(int level) => 10f + 4f * Mathf.Max(1, level);

        public static float MagnetMultiplier(int level) => 1f + 0.5f * Mathf.Max(0, level);

        /// <summary>Đa nòng: mỗi viên thêm nới quạt 10° (súng bắn thẳng thì từ 0°, Shotgun nới thêm từ góc sẵn có).</summary>
        public static float SpreadWithExtra(float baseSpread, int extraBullets) =>
            extraBullets <= 0 ? baseSpread : baseSpread + SpreadPerExtraBullet * extraBullets;
    }
}
