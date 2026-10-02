using System;

namespace ArenaSurvival.Shop
{
    /// <summary>Luật cửa hàng (hàm thuần).</summary>
    public static class ShopRules
    {
        public static bool CanPurchase(int purchasedCount, int limit) => limit <= 0 || purchasedCount < limit;

        /// <summary>Hệ số nội dung tiền tệ: lần đầu mua gói có thưởng (gói kim cương ×2), các lần sau ×1.</summary>
        public static float ContentMultiplier(int purchasedBefore, float firstPurchaseMultiplier) =>
            purchasedBefore <= 0 ? Math.Max(1f, firstPurchaseMultiplier) : 1f;

        public static long ScaledAmount(long amount, float multiplier) => (long)Math.Round(amount * (double)multiplier);
    }
}
