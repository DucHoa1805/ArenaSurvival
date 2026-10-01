using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Trừ nhiều loại cùng lúc (vàng + nguyên liệu khi nâng đồ): kiểm tra ĐỦ TẤT CẢ rồi mới trừ, để không bao giờ có cảnh
        /// trừ vàng xong mới phát hiện thiếu da. Thiếu 1 thứ / số âm → false, không thay đổi gì. Dòng amount = 0 bỏ qua.
        /// </summary>
        public bool TrySpendMany(IReadOnlyList<CurrencyCost> costs)
        {
            if (!CanAfford(costs)) return false;

            for (int i = 0; i < costs.Count; i++)
            {
                if (costs[i].amount > 0) SetBalance(costs[i].id, GetBalance(costs[i].id) - costs[i].amount);
            }
            return true;
        }

        /// <summary>Đủ tất cả chi phí trong danh sách không (UI dùng để làm mờ nút trước khi người chơi bấm).</summary>
        public bool CanAfford(IReadOnlyList<CurrencyCost> costs)
        {
            if (costs == null) return false;

            for (int i = 0; i < costs.Count; i++)
            {
                if (costs[i].amount < 0) return false;

                // Cộng dồn các dòng cùng id (danh sách ngắn nên duyệt lồng là đủ, không cấp phát từ điển).
                long required = 0;
                for (int j = 0; j < costs.Count; j++)
                {
                    if (costs[j].id == costs[i].id) required += costs[j].amount;
                }
                if (GetBalance(costs[i].id) < required) return false;
            }
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
