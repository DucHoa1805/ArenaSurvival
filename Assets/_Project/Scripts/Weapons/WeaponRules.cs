using System;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>Phần thưởng mở ra ở mốc 3★ / 5★ — riêng từng khẩu, cộng dồn (5★ vẫn giữ bonus 3★).</summary>
    [Serializable]
    public struct WeaponStarBonus
    {
        public int extraProjectiles;
        public int extraPierce;
        public float fireRatePercent;
        public float explosionRadiusPercent;
        public float knockbackPercent;
        [Tooltip("Chữ hiện ở bảng Vũ khí, vd \"+2 viên\".")]
        public string text;
    }

    /// <summary>Chỉ số thực của 1 khẩu ở cấp/sao cụ thể — thứ PlayerWeapon dùng để bắn (struct, không cấp phát).</summary>
    public struct WeaponStats
    {
        public float damage;
        public float fireRate;
        public int projectileCount;
        public float spreadAngle;
        public float aimJitter;
        public float range;
        public float projectileSpeed;
        public int pierce;
        public float explosionRadius;
        public float explosionDamageFraction;
        public float knockback;
    }

    /// <summary>Công thức vũ khí meta (hàm thuần, có unit test). Số liệu khởi điểm theo spec 8C, cân bằng ở Phase 10.</summary>
    public static class WeaponRules
    {
        public const int MaxStars = 5;
        public const string ShardPrefix = "shard_";

        private static readonly int[] Shards = { 10, 20, 40, 80, 150 };
        private static readonly float[] Multipliers = { 1f, 1.25f, 1.5f, 1.8f, 2.2f };
        private const int LevelsPerStar = 10;

        /// <summary>Id nguyên liệu mảnh của 1 khẩu — sinh theo quy tắc nên thêm súng mới không phải khai báo thêm hằng số.</summary>
        public static string ShardId(string weaponId) => ShardPrefix + weaponId;

        /// <summary>Số mảnh cần để LÊN sao `stars` (1★ = mở khóa).</summary>
        public static int ShardsFor(int stars) => Shards[ClampStars(stars) - 1];
        public static float StarMultiplier(int stars) => Multipliers[ClampStars(stars) - 1];
        public static int LevelCap(int stars) => LevelsPerStar * ClampStars(stars);

        /// <summary>Vàng để nâng từ level lên level + 1 (cùng dạng công thức trang bị 8B).</summary>
        public static long UpgradeGold(int level) => (long)Math.Floor(100.0 * Math.Pow(1.15, Mathf.Max(0, level - 1)) + 0.0001);
        public static long UpgradeOre(int level) => 1 + Mathf.Max(0, level) / 2;

        public static WeaponStats Effective(WeaponData data, int level, int stars)
        {
            level = Mathf.Max(1, level);
            stars = ClampStars(stars);

            var s = new WeaponStats
            {
                damage = (data.damage + data.damagePerLevel * (level - 1)) * StarMultiplier(stars),
                fireRate = data.fireRate,
                projectileCount = Mathf.Max(1, data.projectileCount),
                spreadAngle = data.spreadAngle,
                aimJitter = data.aimJitter,
                range = data.range,
                projectileSpeed = data.projectileSpeed,
                pierce = data.pierce,
                explosionRadius = data.explosionRadius,
                explosionDamageFraction = data.explosionDamageFraction,
                knockback = data.knockback,
            };

            if (stars >= 3) Apply(ref s, data.starBonus3);
            if (stars >= 5) Apply(ref s, data.starBonus5);
            return s;
        }

        /// <summary>
        /// Phần lực chiến của vũ khí: ước lượng sát thương/giây × 4. Xuyên và nổ được cộng thêm vì cùng 1 phát trúng nhiều quái.
        /// Chỉ để hiển thị, cân bằng lại ở Phase 10.
        /// </summary>
        public static int Power(WeaponStats s)
        {
            float dps = s.damage * s.fireRate * Mathf.Max(1, s.projectileCount);
            dps *= 1f + 0.25f * s.pierce;
            if (s.explosionRadius > 0f) dps *= 1f + 2f * s.explosionDamageFraction;
            return Mathf.RoundToInt(4f * dps);
        }

        private static void Apply(ref WeaponStats s, WeaponStarBonus b)
        {
            s.projectileCount += b.extraProjectiles;
            s.pierce += b.extraPierce;
            s.fireRate *= 1f + b.fireRatePercent / 100f;
            s.explosionRadius *= 1f + b.explosionRadiusPercent / 100f;
            s.knockback *= 1f + b.knockbackPercent / 100f;
        }

        private static int ClampStars(int stars) => Mathf.Clamp(stars, 1, MaxStars);
    }
}
