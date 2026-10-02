using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Missions;
using UnityEngine;

namespace ArenaSurvival.UI.Missions
{
    /// <summary>Chọn icon + số hiển thị cho 1 phần thưởng (thứ nổi bật nhất: rương > kim cương > thứ đầu tiên).</summary>
    public static class RewardIcons
    {
        public static Sprite Pick(MissionReward reward, Sprite gold, Sprite gem, out string amount)
        {
            amount = "";
            if (reward == null) return null;
            if (reward.chest != null)
            {
                amount = reward.chest.displayName;
                return reward.chest.icon;
            }
            if (reward.currencies == null || reward.currencies.Length == 0) return null;

            RewardAmount main = reward.currencies[0];
            foreach (RewardAmount r in reward.currencies) if (r != null && r.currencyId == CurrencyIds.Gem) main = r;
            amount = reward.currencies.Length > 1 ? $"{main.amount:N0}+" : main.amount.ToString("N0");
            if (main.currencyId == CurrencyIds.Gold) return gold;
            if (main.currencyId == CurrencyIds.Gem) return gem;
            Items.MaterialData m = GameServices.Inventory != null ? GameServices.Inventory.Database.GetMaterial(main.currencyId) : null;
            return m != null ? m.icon : null;
        }
    }
}
