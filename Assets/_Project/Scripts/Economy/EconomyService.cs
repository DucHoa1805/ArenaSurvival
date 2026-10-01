using System;
using ArenaSurvival.Meta;

namespace ArenaSurvival.Economy
{
    /// <summary>
    /// Đường DUY NHẤT để thay đổi tiền tệ. Gom về một chỗ để: không bao giờ âm, không cộng/trừ số âm lén,
    /// mọi thay đổi đều phát sự kiện (UI, analytics Phase 12) và đánh dấu hồ sơ cần lưu.
    /// Class thuần (không MonoBehaviour) nên unit test được.
    /// </summary>
    public class EconomyService
    {
        private readonly PlayerProfile _profile;
        private readonly Action _markDirty;

        /// <summary>(id tiền tệ, số dư mới)</summary>
        public event Action<string, long> OnCurrencyChanged;

        public EconomyService(PlayerProfile profile, Action markDirty)
        {
            _profile = profile;
            _markDirty = markDirty;
        }

        public long GetBalance(string currencyId) =>
            _profile.currencies.TryGetValue(currencyId, out long balance) ? balance : 0;

        /// <summary>Cộng tiền. amount ≤ 0 bị từ chối (trả false).</summary>
        public bool Add(string currencyId, long amount)
        {
            if (amount <= 0) return false;

            long current = GetBalance(currencyId);
            // Chặn tràn số: long.MaxValue + x quay về số âm.
            long next = current > long.MaxValue - amount ? long.MaxValue : current + amount;
            SetBalance(currencyId, next);
            return true;
        }

        /// <summary>Trừ tiền nếu đủ. Không đủ (hoặc amount ≤ 0) thì trả false và không thay đổi gì.</summary>
        public bool TrySpend(string currencyId, long amount)
        {
            if (amount <= 0) return false;

            long current = GetBalance(currencyId);
            if (current < amount) return false;

            SetBalance(currencyId, current - amount);
            return true;
        }

        private void SetBalance(string currencyId, long balance)
        {
            _profile.currencies[currencyId] = balance;
            _markDirty?.Invoke();
            OnCurrencyChanged?.Invoke(currencyId, balance);
        }
    }
}
