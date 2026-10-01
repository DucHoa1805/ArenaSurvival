using ArenaSurvival.Stats;
using UnityEngine;

namespace ArenaSurvival.Items
{
    /// <summary>
    /// Công thức trang bị (hàm thuần, có unit test). Số liệu khởi điểm theo plan D1 / spec 8B, cân bằng lại ở Phase 10.
    /// </summary>
    public static class EquipmentRules
    {
        public const int InventoryCapacity = 100;
        /// <summary>Mỗi bấy nhiêu cấp phải đột phá 1 lần mới nâng tiếp.</summary>
        public const int LevelsPerAscension = 10;
        public const float UpgradeGoldBase = 100f;
        public const float UpgradeGoldGrowth = 1.15f;
        public const int AscendGoldPerStep = 500;
        // Phân rã hoàn 70% đã chi: nâng nhầm món không mất trắng, nhưng vẫn thiệt nên không thành mẹo "nâng rồi rã".
        private const int RefundPercent = 70;

        private static readonly float[] StatMultipliers = { 1f, 1.5f, 2.2f, 3.2f };
        private static readonly int[] RarityCaps = { 10, 20, 30, 40 };
        private static readonly float[] CostMultipliers = { 1f, 1.2f, 1.5f, 2f };
        private static readonly int[] DismantleBases = { 2, 5, 12, 30 };

        public static float StatMultiplier(Rarity rarity) => StatMultipliers[Index(rarity)];
        public static int RarityLevelCap(Rarity rarity) => RarityCaps[Index(rarity)];
        public static float CostMultiplier(Rarity rarity) => CostMultipliers[Index(rarity)];
        public static int DismantleBase(Rarity rarity) => DismantleBases[Index(rarity)];

        public static StatBlock Stats(StatBlock baseStats, StatBlock perLevel, Rarity rarity, int level)
        {
            StatBlock raw = baseStats + perLevel * Mathf.Max(0, level - 1);
            return raw * StatMultiplier(rarity);
        }

        /// <summary>Cấp tối đa hiện tại: bị chặn bởi số lần đột phá, và không bao giờ vượt trần của độ hiếm.</summary>
        public static int LevelCap(Rarity rarity, int ascension) =>
            Mathf.Min(RarityLevelCap(rarity), LevelsPerAscension * (Mathf.Max(0, ascension) + 1));

        public static bool CanAscend(Rarity rarity, int level, int ascension)
        {
            int cap = LevelCap(rarity, ascension);
            return level >= cap && cap < RarityLevelCap(rarity);
        }

        /// <summary>Vàng để nâng từ level lên level + 1.</summary>
        public static long UpgradeGold(Rarity rarity, int level) =>
            (long)System.Math.Floor(UpgradeGoldBase * System.Math.Pow(UpgradeGoldGrowth, Mathf.Max(0, level - 1)) * CostMultiplier(rarity) + 0.0001);

        /// <summary>Nguyên liệu để nâng từ level lên level + 1.</summary>
        public static long UpgradeMaterial(int level) => 1 + Mathf.Max(0, level) / 2;

        public static long AscendCores(int ascension) => Mathf.Max(0, ascension) + 1;
        public static long AscendGold(int ascension) => AscendGoldPerStep * (Mathf.Max(0, ascension) + 1L);

        public struct Refund
        {
            public long gold;
            public long material;
            public long cores;
        }

        /// <summary>Nhận lại khi phân rã: nguyên liệu gốc theo độ hiếm + 70% tổng đã chi để nâng cấp/đột phá (làm tròn xuống).</summary>
        public static Refund DismantleRefund(Rarity rarity, int level, int ascension)
        {
            long gold = 0, material = 0, cores = 0;
            for (int l = 1; l < level; l++)
            {
                gold += UpgradeGold(rarity, l);
                material += UpgradeMaterial(l);
            }
            for (int a = 0; a < ascension; a++)
            {
                gold += AscendGold(a);
                cores += AscendCores(a);
            }

            return new Refund
            {
                gold = gold * RefundPercent / 100,
                material = DismantleBase(rarity) + material * RefundPercent / 100,
                cores = cores * RefundPercent / 100,
            };
        }

        // Save hỏng/sửa tay có thể chứa độ hiếm ngoài khoảng: kẹp lại thay vì văng lỗi khi mở kho.
        private static int Index(Rarity rarity) => Mathf.Clamp((int)rarity, 0, StatMultipliers.Length - 1);
    }
}
