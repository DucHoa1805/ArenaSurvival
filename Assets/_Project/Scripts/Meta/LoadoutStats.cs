using ArenaSurvival.Stats;

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
    }
}
