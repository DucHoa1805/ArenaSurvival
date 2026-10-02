using System;

namespace ArenaSurvival.Services
{
    /// <summary>Vị trí quảng cáo có thưởng — id ổn định, lưu vào save (số lượt đã xem trong ngày).</summary>
    public static class AdPlacements
    {
        public const string DoubleReward = "double_reward";
        public const string Revive = "revive";
        public const string FreeChest = "free_chest";

        /// <summary>Giới hạn lượt/ngày (chỉnh được bằng Remote Config) — đủ để người chơi thấy có lợi, không đủ để quảng cáo thay hẳn cửa hàng.</summary>
        public static int DailyLimit(string placement)
        {
            switch (placement)
            {
                case DoubleReward: return Core.Tuning.AdLimitDouble;
                case Revive: return Core.Tuning.AdLimitRevive;
                case FreeChest: return Core.Tuning.AdLimitFreeChest;
                default: return 0;
            }
        }
    }

    /// <summary>Luật quảng cáo (hàm thuần): ngày tính theo UTC để đổi múi giờ không lấy thêm lượt.</summary>
    public static class AdRules
    {
        public static int DayKey(DateTime utc) => utc.Year * 10000 + utc.Month * 100 + utc.Day;

        /// <summary>Sang ngày mới thì bộ đếm cũ hết hiệu lực.</summary>
        public static bool IsNewDay(int storedDay, int today) => storedDay != today;

        public static int Remaining(int viewsToday, int limit) => Math.Max(0, limit - viewsToday);
    }
}
