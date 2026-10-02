using System;

namespace ArenaSurvival.Missions
{
    /// <summary>Luật nhiệm vụ ngày và quà đăng nhập (hàm thuần, có unit test).</summary>
    public static class MissionRules
    {
        /// <summary>Cộng tiến độ, dừng ở mục tiêu (save không phình số vô nghĩa). amount ≤ 0 không đổi gì.</summary>
        public static long Advance(long progress, long amount, int target) =>
            amount <= 0 ? progress : Math.Min(target, progress + amount);

        public static bool IsComplete(long progress, int target) => progress >= target;

        public static bool MilestoneReached(int points, int required) => points >= required;

        /// <summary>Ngày đã lưu khác hôm nay = sang ngày mới, tiến độ cũ hết hiệu lực.</summary>
        public static bool NeedsReset(int storedDay, int today) => storedDay != today;

        /// <summary>Ô quà hôm nay trong vòng 7 ngày: theo số lần đã nhận, bỏ ngày không mất chuỗi.</summary>
        public static int LoginIndex(int claimCount, int days) => days <= 0 ? 0 : Math.Max(0, claimCount) % days;

        public static bool CanClaimLogin(int lastClaimDay, int today) => lastClaimDay != today;

        /// <summary>Thưởng nhân hệ số Remote Config, làm tròn xuống nhưng không về 0 (đã ghi thưởng thì phải nhận được gì đó).</summary>
        public static long ScaleReward(int amount, float multiplier) =>
            amount <= 0 ? 0 : Math.Max(1L, (long)Math.Floor(amount * (double)multiplier + 0.0001));

        /// <summary>Thời gian tới 0h UTC kế tiếp — mốc làm mới nhiệm vụ.</summary>
        public static TimeSpan UntilReset(DateTime utcNow) => utcNow.Date.AddDays(1) - utcNow;
    }
}
