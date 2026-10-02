using System;
using ArenaSurvival.Economy;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Nâng Talent bằng vàng. Trừ tiền chỉ qua EconomyService.TrySpend: không đủ tiền hay đã tối đa thì không có gì thay đổi
    /// (không có trạng thái "trừ tiền rồi mà chưa lên cấp"). Thành công thì lưu ngay — mua bằng tiền thì không được mất.
    /// </summary>
    public class TalentService
    {
        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly Action _save;

        /// <summary>(id Talent, cấp mới)</summary>
        public event Action<string, int> OnTalentChanged;

        public TalentService(PlayerProfile profile, EconomyService economy, Action save)
        {
            _profile = profile;
            _economy = economy;
            _save = save;
        }

        public int GetLevel(string talentId) =>
            talentId != null && _profile.talents.TryGetValue(talentId, out int level) ? level : 0;

        /// <summary>Giá lên cấp kế; -1 nếu đã tối đa.</summary>
        public int GetCost(TalentData talent)
        {
            int level = GetLevel(talent.id);
            return TalentRules.IsMaxed(level, talent.maxLevel) ? -1 : TalentRules.Cost(talent.baseCost, talent.costGrowth, level);
        }

        public bool CanUpgrade(TalentData talent)
        {
            if (talent == null) return false;
            int cost = GetCost(talent);
            return cost >= 0 && _economy.GetBalance(CurrencyIds.Gold) >= cost;
        }

        public bool TryUpgrade(TalentData talent)
        {
            if (talent == null) return false;
            int cost = GetCost(talent);
            if (cost < 0 || !_economy.TrySpend(CurrencyIds.Gold, cost, EconomyReasons.TalentUpgrade)) return false;

            int level = GetLevel(talent.id) + 1;
            _profile.talents[talent.id] = level;
            _save?.Invoke();
            OnTalentChanged?.Invoke(talent.id, level);
            return true;
        }
    }
}
