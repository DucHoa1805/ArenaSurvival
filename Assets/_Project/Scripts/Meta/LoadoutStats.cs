using ArenaSurvival.Stats;
using ArenaSurvival.Weapons;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Chỉ số ngoài trận của người chơi hiện tại = gốc + Talent + trang bị. Một chỗ duy nhất để PlayerLoadout (vào trận),
    /// bảng Talent và bảng Trang bị hiển thị cùng một lực chiến.
    /// </summary>
    public static class LoadoutStats
    {
        public static StatBlock Compute(HeroBaseStats heroBase, TalentCatalog talents)
        {
            StatBlock baseStats = heroBase != null ? heroBase.baseStats : new StatBlock { maxHealth = 100f };
            if (!GameServices.IsInitialized) return baseStats;

            StatBlock equipment = GameServices.Inventory != null ? GameServices.Inventory.EquippedStats() : default;
            return StatCalculator.Compute(baseStats, talents, GameServices.Talents.GetLevel, equipment);
        }

        /// <summary>
        /// Lực chiến hiển thị = phần chỉ số (StatCalculator.Power) + phần vũ khí đang mặc (WeaponRules.Power).
        /// Mọi bảng gọi đây để cùng 1 con số.
        /// </summary>
        public static int Power(HeroBaseStats heroBase, TalentCatalog talents)
        {
            int power = StatCalculator.Power(Compute(heroBase, talents));
            WeaponService weapons = GameServices.IsInitialized ? GameServices.Weapons : null;
            if (weapons != null && weapons.EquippedData != null) power += WeaponRules.Power(weapons.GetEffective(weapons.EquippedId));
            return power;
        }
    }
}
