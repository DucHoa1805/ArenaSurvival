using System;
using System.Collections.Generic;
using ArenaSurvival.Core;
using ArenaSurvival.Economy;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using ArenaSurvival.Services;
using ArenaSurvival.Stages;

namespace ArenaSurvival.Missions
{
    /// <summary>
    /// Nhiệm vụ ngày + mốc điểm + quà đăng nhập 7 ngày (Phase 12). Ngày theo GameClock (UTC, không lùi được khi chỉnh giờ).
    /// Tự nghe sự kiện của các dịch vụ khác (tiền tệ có lý do, quảng cáo, rương) nên chỗ khác không phải gọi Track rải rác;
    /// riêng kết quả màn do GameServices gọi TrackStage sau khi áp thưởng.
    /// Thưởng đi qua EconomyService / ShopService (nguyên tắc B2).
    /// </summary>
    public class MissionService
    {
        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly ArenaSurvival.Shop.ShopService _shop;
        private readonly ITimeSource _clock;
        private readonly Action _save;
        private readonly Action _markDirty;

        public MissionCatalog Catalog { get; }

        /// <summary>Tiến độ / trạng thái nhận đổi — UI vẽ lại, Hub cập nhật chấm đỏ.</summary>
        public event Action OnChanged;
        /// <summary>(id nhiệm vụ, điểm) khi nhận nhiệm vụ; ("milestone_N", điểm mốc) khi nhận mốc — analytics.</summary>
        public event Action<string, int> OnClaimed;
        /// <summary>(ngày 1..7) khi nhận quà đăng nhập.</summary>
        public event Action<int> OnLoginClaimed;

        public MissionService(PlayerProfile profile, EconomyService economy, ArenaSurvival.Shop.ShopService shop, MissionCatalog catalog,
            ITimeSource clock, Action save, Action markDirty)
        {
            _profile = profile;
            _economy = economy;
            _shop = shop;
            Catalog = catalog;
            _clock = clock;
            _save = save;
            _markDirty = markDirty;
        }

        /// <summary>Nối vào sự kiện của các dịch vụ (gọi 1 lần lúc khởi tạo). Tách khỏi constructor để test chọn nối hay không.</summary>
        public void Connect(AdsService ads)
        {
            _economy.OnTransaction += HandleTransaction;
            _economy.OnSpent += HandleSpent;
            if (_shop != null) _shop.OnProductGranted += HandleProductGranted;
            if (ads != null) ads.OnRewarded += _ => Track(MissionMetric.AdWatched, 1);
        }

        public int Today => AdRules.DayKey(_clock.UtcNow);
        public TimeSpan UntilReset => MissionRules.UntilReset(_clock.UtcNow);

        // ---------- Nhiệm vụ ngày ----------

        public void Track(MissionMetric metric, long amount)
        {
            if (amount <= 0 || Catalog == null || Catalog.daily == null) return;
            RollDay();
            bool changed = false;
            foreach (MissionData m in Catalog.daily)
            {
                if (m == null || m.metric != metric || IsClaimed(m)) continue;
                long before = RawProgress(m);
                long after = MissionRules.Advance(before, amount, m.target);
                if (after == before) continue;
                _profile.daily.progress[m.id] = after;
                changed = true;
            }
            if (!changed) return;
            // Chỉ đánh dấu cần lưu: phần lớn lời gọi nằm giữa 1 thao tác khác (nâng cấp, áp thưởng màn) vốn sẽ tự lưu ngay sau đó.
            _markDirty?.Invoke();
            OnChanged?.Invoke();
        }

        public void TrackStage(StageResult result)
        {
            if (result == null) return;
            Track(MissionMetric.StagePlayed, 1);
            if (result.victory) Track(MissionMetric.StageWon, 1);
            Track(MissionMetric.EnemyKilled, result.killCount);
        }

        public long Progress(MissionData mission)
        {
            RollDay();
            return RawProgress(mission);
        }

        public bool IsComplete(MissionData mission) => mission != null && MissionRules.IsComplete(Progress(mission), mission.target);

        public bool IsClaimed(MissionData mission)
        {
            RollDay();
            return mission != null && _profile.daily.claimed.Contains(mission.id);
        }

        public bool CanClaim(MissionData mission) => mission != null && !IsClaimed(mission) && IsComplete(mission);

        public bool TryClaim(MissionData mission)
        {
            if (!CanClaim(mission)) return false;
            _profile.daily.claimed.Add(mission.id);
            _save?.Invoke();
            OnClaimed?.Invoke(mission.id, mission.points);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Điểm hôm nay = tổng điểm nhiệm vụ đã nhận (tính lại, không lưu riêng — không bao giờ lệch).</summary>
        public int Points
        {
            get
            {
                RollDay();
                int sum = 0;
                if (Catalog == null || Catalog.daily == null) return 0;
                foreach (MissionData m in Catalog.daily)
                    if (m != null && _profile.daily.claimed.Contains(m.id)) sum += m.points;
                return sum;
            }
        }

        public bool IsMilestoneClaimed(int index)
        {
            RollDay();
            return _profile.daily.milestones.Contains(index);
        }

        public bool CanClaimMilestone(int index)
        {
            MissionMilestone milestone = Milestone(index);
            return milestone != null && !IsMilestoneClaimed(index) && MissionRules.MilestoneReached(Points, milestone.points);
        }

        public bool TryClaimMilestone(int index, List<LootDrop> granted)
        {
            if (!CanClaimMilestone(index)) return false;
            MissionMilestone milestone = Milestone(index);
            _profile.daily.milestones.Add(index);
            GrantReward(milestone.reward, granted, EconomyReasons.Mission);
            _save?.Invoke();
            OnClaimed?.Invoke("milestone_" + (index + 1), milestone.points);
            OnChanged?.Invoke();
            return true;
        }

        private MissionMilestone Milestone(int index) =>
            Catalog != null && Catalog.milestones != null && index >= 0 && index < Catalog.milestones.Length ? Catalog.milestones[index] : null;

        // ---------- Quà đăng nhập ----------

        public int LoginDays => Catalog != null && Catalog.loginRewards != null ? Catalog.loginRewards.Length : 0;

        /// <summary>Ô quà kế tiếp (0..6). Đã nhận hôm nay thì là ô của ngày mai.</summary>
        public int LoginIndex => MissionRules.LoginIndex(_profile.login.count, LoginDays);

        /// <summary>Số ô đã nhận trong vòng hiện tại (để tô ✓).</summary>
        public int LoginClaimedInCycle => LoginIndex;

        public bool CanClaimLogin => LoginDays > 0 && MissionRules.CanClaimLogin(_profile.login.lastDay, Today);

        public bool TryClaimLogin(List<LootDrop> granted)
        {
            if (!CanClaimLogin) return false;
            int index = LoginIndex;
            _profile.login.lastDay = Today;
            _profile.login.count++;
            GrantReward(Catalog.loginRewards[index], granted, EconomyReasons.Login);
            _save?.Invoke();
            OnLoginClaimed?.Invoke(index + 1);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Có gì nhận được ngay (chấm đỏ tab Nhiệm vụ).</summary>
        public bool HasClaimable
        {
            get
            {
                if (CanClaimLogin) return true;
                if (Catalog == null) return false;
                if (Catalog.daily != null) foreach (MissionData m in Catalog.daily) if (CanClaim(m)) return true;
                if (Catalog.milestones != null) for (int i = 0; i < Catalog.milestones.Length; i++) if (CanClaimMilestone(i)) return true;
                return false;
            }
        }

        // ---------- Nội bộ ----------

        private void GrantReward(MissionReward reward, List<LootDrop> granted, string reason)
        {
            if (reward == null) return;
            if (reward.currencies != null)
            {
                foreach (RewardAmount r in reward.currencies)
                {
                    if (r == null || string.IsNullOrEmpty(r.currencyId)) continue;
                    long amount = MissionRules.ScaleReward(r.amount, Tuning.MissionRewardMultiplier);
                    if (!_economy.Add(r.currencyId, amount, reason)) continue;
                    granted?.Add(LootDrop.Material(r.currencyId, (int)Math.Min(int.MaxValue, amount)));
                }
            }
            if (reward.chest != null && _shop != null) _shop.GrantFree(reward.chest, granted, reason);
        }

        private long RawProgress(MissionData mission) =>
            mission != null && _profile.daily.progress.TryGetValue(mission.id, out long p) ? p : 0;

        private void RollDay()
        {
            int today = Today;
            DailyMissionState d = _profile.daily;
            if (!MissionRules.NeedsReset(d.day, today)) return;
            d.day = today;
            d.progress.Clear();
            d.claimed.Clear();
            d.milestones.Clear();
            _markDirty?.Invoke();
        }

        private void HandleTransaction(CurrencyTransaction t)
        {
            if (t.delta >= 0) return;
            if (t.id == CurrencyIds.Gold) Track(MissionMetric.GoldSpent, -t.delta);
        }

        private void HandleSpent(string reason)
        {
            if (EconomyReasons.IsUpgrade(reason)) Track(MissionMetric.Upgrade, 1);
        }

        private void HandleProductGranted(ArenaSurvival.Shop.ShopProductData product, string reason)
        {
            if (product != null && product.chestTable != null) Track(MissionMetric.ChestOpened, 1);
        }
    }
}
