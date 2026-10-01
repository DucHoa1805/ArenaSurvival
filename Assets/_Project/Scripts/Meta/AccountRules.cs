using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;

namespace ArenaSurvival.Meta
{
    /// <summary>Tiến độ trong cấp hiện tại (suy ra từ tổng XP).</summary>
    public struct AccountProgress
    {
        public int level;
        public long xpInLevel;
        /// <summary>XP cần để lên cấp kế; 0 khi đã ở cấp tối đa.</summary>
        public long xpForLevel;
    }

    /// <summary>
    /// Công thức level tài khoản (hàm thuần, có unit test). Level KHÔNG lưu trong save mà suy ra từ tổng XP: không bao giờ lệch nhau,
    /// và đổi công thức ở Phase 10 thì mọi người chơi tự xếp lại cấp đúng.
    /// </summary>
    public static class AccountRules
    {
        public const int MaxLevel = 50;

        /// <summary>XP để lên từ level → level + 1 (plan D1: 100 × level^1.5). 0 ở cấp tối đa.</summary>
        public static long XpToNext(int level)
        {
            if (level >= MaxLevel) return 0;
            return (long)Math.Round(100.0 * Math.Pow(Math.Max(1, level), 1.5));
        }

        public static AccountProgress FromXp(long totalXp)
        {
            long remaining = Math.Max(0, totalXp);
            int level = 1;
            while (level < MaxLevel)
            {
                long need = XpToNext(level);
                if (remaining < need) break;
                remaining -= need;
                level++;
            }
            return new AccountProgress { level = level, xpInLevel = level >= MaxLevel ? 0 : remaining, xpForLevel = XpToNext(level) };
        }

        /// <summary>Thưởng khi ĐẠT cấp `level`: vàng 100 × cấp, kim cương 5 (mỗi 5 cấp: 20). Ghi vào list có sẵn.</summary>
        public static void RewardFor(int level, List<CurrencyCost> output)
        {
            output.Clear();
            output.Add(new CurrencyCost(CurrencyIds.Gold, 100L * level));
            output.Add(new CurrencyCost(CurrencyIds.Gem, level % 5 == 0 ? 20 : 5));
        }
    }
}
