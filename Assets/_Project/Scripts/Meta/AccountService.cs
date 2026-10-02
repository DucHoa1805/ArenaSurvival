using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;

namespace ArenaSurvival.Meta
{
    /// <summary>1 lần lên cấp chờ Hub hiện popup.</summary>
    public class LevelUpInfo
    {
        public int level;
        public List<CurrencyCost> rewards = new List<CurrencyCost>(2);
        /// <summary>Tên tính năng vừa mở ở cấp này (để popup ghi "Mở khóa: Talent").</summary>
        public List<string> unlockedFeatures = new List<string>();
    }

    /// <summary>
    /// Level tài khoản: cộng XP (từ màn chơi), thưởng mỗi cấp qua EconomyService, mở khóa tính năng theo AccountLevelTable.
    /// Level suy ra từ PlayerProfile.accountXp (không lưu riêng). Các lần lên cấp xếp hàng đợi để Hub hiện popup khi người chơi
    /// quay về — vì lên cấp xảy ra lúc áp kết quả màn, khi đang ở scene Game.
    /// </summary>
    public class AccountService
    {
        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly AccountLevelTable _table;
        private readonly Queue<LevelUpInfo> _pending = new Queue<LevelUpInfo>();

        /// <summary>Tổng XP hoặc cấp vừa đổi (thanh trên cùng cập nhật).</summary>
        public event Action OnXpChanged;

        public AccountService(PlayerProfile profile, EconomyService economy, AccountLevelTable table)
        {
            _profile = profile;
            _economy = economy;
            _table = table;
        }

        // Tính lại từ XP mỗi lần đọc (tối đa 50 vòng, rẻ) thay vì giữ bản sao: xóa hồ sơ (menu dev) hay nạp lại save
        // không bao giờ làm level lệch với XP. XP có sẵn trong save cũ chỉ để xếp cấp, không trả thưởng hồi tố.
        private AccountProgress Progress => AccountRules.FromXp(_profile.accountXp);

        public int Level => Progress.level;
        public long XpInLevel => Progress.xpInLevel;
        public long XpForLevel => Progress.xpForLevel;
        public bool HasPendingLevelUp => _pending.Count > 0;

        /// <summary>Cộng XP; mỗi cấp vượt qua → cộng thưởng + xếp hàng popup. Trả số cấp đã lên. Không tự lưu (người gọi lưu).</summary>
        public int AddXp(long amount)
        {
            if (amount <= 0) return 0;

            int before = Level;
            _profile.accountXp = _profile.accountXp > long.MaxValue - amount ? long.MaxValue : _profile.accountXp + amount;
            int after = Level;

            for (int level = before + 1; level <= after; level++)
            {
                var info = new LevelUpInfo { level = level };
                AccountRules.RewardFor(level, info.rewards);
                foreach (CurrencyCost reward in info.rewards) _economy.Add(reward.id, reward.amount, Economy.EconomyReasons.AccountLevel);
                if (_table != null && _table.unlocks != null)
                    foreach (FeatureUnlock u in _table.unlocks)
                        if (u != null && u.level == level) info.unlockedFeatures.Add(string.IsNullOrEmpty(u.displayName) ? u.featureId : u.displayName);
                _pending.Enqueue(info);
            }

            OnXpChanged?.Invoke();
            return after - before;
        }

        public int UnlockLevel(string featureId)
        {
            FeatureUnlock unlock = _table != null ? _table.Find(featureId) : null;
            return unlock != null ? unlock.level : 1;
        }

        public bool IsUnlocked(string featureId) => Level >= UnlockLevel(featureId);

        public bool TakePendingLevelUp(out LevelUpInfo info)
        {
            if (_pending.Count == 0)
            {
                info = null;
                return false;
            }
            info = _pending.Dequeue();
            return true;
        }
    }
}
