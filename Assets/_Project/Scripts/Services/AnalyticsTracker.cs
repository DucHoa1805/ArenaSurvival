using System;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;

namespace ArenaSurvival.Services
{
    /// <summary>
    /// Nối sự kiện có sẵn của các dịch vụ thành sự kiện analytics — gom 1 chỗ để không hệ thống nào phải tự gọi analytics
    /// (thêm/bớt sự kiện chỉ sửa file này). Đăng ký 1 lần lúc khởi tạo GameServices, sống suốt game.
    /// </summary>
    public class AnalyticsTracker
    {
        private readonly AnalyticsService _analytics;
        private readonly PlayerProfile _profile;
        private readonly AccountService _account;
        private readonly WeaponService _weapons;
        private int _lastLevel;

        private readonly Action<StageData> _onStageStarted;
        private readonly Action<StageResult> _onStageCompleted;

        public AnalyticsTracker(AnalyticsService analytics, PlayerProfile profile, EconomyService economy, AccountService account,
            WeaponService weapons, ArenaSurvival.Shop.ShopService shop, AdsService ads, ArenaSurvival.Missions.MissionService missions)
        {
            _analytics = analytics;
            _profile = profile;
            _account = account;
            _weapons = weapons;
            _lastLevel = account != null ? account.Level : 0;

            economy.OnTransaction += HandleTransaction;
            if (account != null) account.OnXpChanged += HandleXpChanged;
            if (shop != null) shop.OnProductGranted += HandleProductGranted;
            if (ads != null) ads.OnRewarded += HandleAdRewarded;
            if (missions != null)
            {
                missions.OnClaimed += (id, points) => _analytics.Begin().Param("mission", id).Param("points", points).Send(AnalyticsEvents.MissionClaim);
                missions.OnLoginClaimed += day => _analytics.Begin().Param("day", day).Send(AnalyticsEvents.LoginClaim);
            }

            _onStageStarted = HandleStageStarted;
            _onStageCompleted = HandleStageCompleted;
            StageEvents.OnStageStarted += _onStageStarted;
            StageEvents.OnStageCompleted += _onStageCompleted;
        }

        /// <summary>Gỡ sự kiện static (StageEvents) khi GameServices khởi tạo lại trong Editor.</summary>
        public void Disconnect()
        {
            StageEvents.OnStageStarted -= _onStageStarted;
            StageEvents.OnStageCompleted -= _onStageCompleted;
        }

        public void SessionStart(string appVersion)
        {
            _analytics.Begin()
                .Param("version", appVersion)
                .Param("level", _lastLevel)
                .Param("stages_cleared", _profile.stageStars.Count)
                .Param("tampered", _profile.tampered)
                .Send(AnalyticsEvents.SessionStart);
        }

        private void HandleStageStarted(StageData stage)
        {
            if (stage == null) return;
            _analytics.Begin()
                .Param("stage", stage.id)
                .Param("stage_number", stage.stageNumber)
                .Param("first_try", !_profile.stageStars.ContainsKey(stage.id))
                .Param("level", _account != null ? _account.Level : 0)
                .Param("weapon", _weapons != null ? _weapons.EquippedId : "")
                .Send(AnalyticsEvents.StageStart);
        }

        private void HandleStageCompleted(StageResult result)
        {
            if (result == null) return;
            _analytics.Begin()
                .Param("stage", result.stageId)
                .Param("victory", result.victory)
                .Param("stars", result.starCount)
                .Param("wave", result.waveReached)
                .Param("time", (int)result.clearTime)
                .Param("kills", result.killCount)
                .Param("hp", result.hpFraction)
                .Param("revived", result.usedRevive)
                .Send(AnalyticsEvents.StageEnd);
        }

        private void HandleTransaction(CurrencyTransaction t)
        {
            _analytics.Begin()
                .Param("currency", t.id)
                .Param("delta", t.delta)
                .Param("balance", t.balance)
                .Param("reason", t.reason)
                .Send(AnalyticsEvents.Currency);
        }

        private void HandleXpChanged()
        {
            int level = _account.Level;
            if (level <= _lastLevel) return;
            _lastLevel = level;
            _analytics.Begin().Param("level", level).Send(AnalyticsEvents.LevelUp);
        }

        private void HandleProductGranted(ArenaSurvival.Shop.ShopProductData product, string reason)
        {
            if (product == null) return;
            _analytics.Begin()
                .Param("product", product.id)
                .Param("kind", product.kind.ToString())
                .Param("source", reason)
                .Param("gem_cost", product.gemCost)
                .Send(AnalyticsEvents.Purchase);
        }

        private void HandleAdRewarded(string placement) =>
            _analytics.Begin().Param("placement", placement).Param("remove_ads", _profile.removeAds).Send(AnalyticsEvents.AdReward);
    }
}
