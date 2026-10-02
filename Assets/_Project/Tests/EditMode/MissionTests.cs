using System;
using System.Collections.Generic;
using ArenaSurvival.Core;
using ArenaSurvival.Economy;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using ArenaSurvival.Missions;
using ArenaSurvival.Save;
using ArenaSurvival.Services;
using ArenaSurvival.Stages;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    /// <summary>Giờ giả: test đổi ngày bằng cách gán Now.</summary>
    public class FakeTime : ITimeSource
    {
        public DateTime Now = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc);
        public DateTime UtcNow => Now;
    }

    public class FakeAdProviderForTests : IAdProvider
    {
        public bool IsReady => true;
        public void Show(string placement, Action<bool> onComplete) => onComplete(true);
        public void SetConsent(bool personalized) { }
    }

    public class MemorySink : IAnalyticsSink
    {
        public readonly List<string> Events = new List<string>();
        public readonly List<Dictionary<string, object>> Params = new List<Dictionary<string, object>>();
        public void Send(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            Events.Add(eventName);
            var copy = new Dictionary<string, object>();
            foreach (var kv in parameters) copy[kv.Key] = kv.Value;
            Params.Add(copy);
        }
    }

    public class MissionTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private FakeTime _time;
        private MissionCatalog _catalog;
        private MissionData _play, _kill, _upgrade, _ad, _gold;
        private MissionService _missions;
        private AdsService _ads;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            Tuning.Reset();
            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _time = new FakeTime();
            _play = Mission("play_stages", MissionMetric.StagePlayed, 3, 20);
            _kill = Mission("kill_enemies", MissionMetric.EnemyKilled, 300, 20);
            _upgrade = Mission("upgrade", MissionMetric.Upgrade, 2, 20);
            _ad = Mission("watch_ad", MissionMetric.AdWatched, 1, 20);
            _gold = Mission("spend_gold", MissionMetric.GoldSpent, 1000, 20);
            _catalog = ScriptableObject.CreateInstance<MissionCatalog>();
            _catalog.daily = new[] { _play, _kill, _upgrade, _ad, _gold };
            _catalog.milestones = new[]
            {
                new MissionMilestone { points = 40, reward = Reward(CurrencyIds.Gold, 500) },
                new MissionMilestone { points = 100, reward = Reward(CurrencyIds.Gem, 25) },
            };
            _catalog.loginRewards = new MissionReward[7];
            for (int i = 0; i < 7; i++) _catalog.loginRewards[i] = Reward(CurrencyIds.Gold, 100 * (i + 1));
            _saves = 0;
            _missions = new MissionService(_profile, _economy, null, _catalog, _time, () => _saves++, null);
            _ads = new AdsService(new FakeAdProviderForTests(), _profile, null, () => _time.Now);
            _missions.Connect(_ads);
        }

        [TearDown]
        public void TearDown() => Tuning.Reset();

        private static MissionData Mission(string id, MissionMetric metric, int target, int points)
        {
            var m = ScriptableObject.CreateInstance<MissionData>();
            m.id = id; m.metric = metric; m.target = target; m.points = points; m.description = "x {0}";
            return m;
        }

        private static MissionReward Reward(string id, int amount) =>
            new MissionReward { currencies = new[] { new RewardAmount { currencyId = id, amount = amount } } };

        // ---------- Hàm thuần ----------

        [Test]
        public void Rules_AdvanceClampsAtTargetAndIgnoresNonPositive()
        {
            Assert.AreEqual(3, MissionRules.Advance(2, 5, 3));
            Assert.AreEqual(2, MissionRules.Advance(2, 0, 3));
            Assert.AreEqual(2, MissionRules.Advance(2, -4, 3));
        }

        [Test]
        public void Rules_LoginIndexWrapsAfterSevenDays()
        {
            Assert.AreEqual(0, MissionRules.LoginIndex(0, 7));
            Assert.AreEqual(6, MissionRules.LoginIndex(6, 7));
            Assert.AreEqual(0, MissionRules.LoginIndex(7, 7));
            Assert.AreEqual(0, MissionRules.LoginIndex(3, 0));
        }

        [Test]
        public void Rules_ScaleRewardNeverZero()
        {
            Assert.AreEqual(150, MissionRules.ScaleReward(100, 1.5f));
            Assert.AreEqual(1, MissionRules.ScaleReward(1, 0.1f));
            Assert.AreEqual(0, MissionRules.ScaleReward(0, 2f));
        }

        [Test]
        public void Rules_UntilResetIsTimeToNextUtcMidnight()
        {
            var t = new DateTime(2026, 10, 2, 22, 30, 0, DateTimeKind.Utc);
            Assert.AreEqual(TimeSpan.FromMinutes(90), MissionRules.UntilReset(t));
        }

        // ---------- Nhiệm vụ ngày ----------

        [Test]
        public void TrackStage_CountsPlayedWonAndKills()
        {
            _missions.TrackStage(new StageResult { stageId = "1-1", victory = true, killCount = 120 });
            _missions.TrackStage(new StageResult { stageId = "1-2", victory = false, killCount = 250 });
            Assert.AreEqual(2, _missions.Progress(_play));
            Assert.AreEqual(300, _missions.Progress(_kill), "dừng ở mục tiêu");
            Assert.IsTrue(_missions.IsComplete(_kill));
            Assert.IsFalse(_missions.IsComplete(_play));
        }

        [Test]
        public void Economy_UpgradeReasonCountsOncePerSpendEvenWithManyCurrencies()
        {
            _economy.Add(CurrencyIds.Gold, 5000);
            _economy.Add(CurrencyIds.Leather, 50);
            var costs = new List<CurrencyCost> { new CurrencyCost(CurrencyIds.Gold, 300), new CurrencyCost(CurrencyIds.Leather, 2) };
            Assert.IsTrue(_economy.TrySpendMany(costs, EconomyReasons.ItemUpgrade));
            Assert.AreEqual(1, _missions.Progress(_upgrade));
            Assert.AreEqual(300, _missions.Progress(_gold));
            // Tiêu không phải nâng cấp (mua rương) vẫn tính tiêu vàng nhưng không tính nâng cấp.
            Assert.IsTrue(_economy.TrySpend(CurrencyIds.Gold, 800, EconomyReasons.ShopPurchase));
            Assert.AreEqual(1, _missions.Progress(_upgrade));
            Assert.AreEqual(1000, _missions.Progress(_gold));
        }

        [Test]
        public void Economy_WeaponUnlockIsNotAnUpgrade()
        {
            _economy.Add("shard_x", 10);
            Assert.IsTrue(_economy.TrySpend("shard_x", 10, EconomyReasons.WeaponUnlock));
            Assert.AreEqual(0, _missions.Progress(_upgrade));
        }

        [Test]
        public void Ads_RewardedCountsAdMission()
        {
            _ads.Show(AdPlacements.DoubleReward, _ => { });
            Assert.IsTrue(_missions.IsComplete(_ad));
        }

        [Test]
        public void Claim_OnlyWhenCompleteAndOnce_AddsPoints()
        {
            Assert.IsFalse(_missions.TryClaim(_ad));
            _missions.Track(MissionMetric.AdWatched, 1);
            Assert.IsTrue(_missions.TryClaim(_ad));
            Assert.IsFalse(_missions.TryClaim(_ad), "không nhận 2 lần");
            Assert.AreEqual(20, _missions.Points);
            Assert.AreEqual(1, _saves);
        }

        [Test]
        public void Milestone_NeedsPointsAndGrantsOnce()
        {
            Assert.IsFalse(_missions.CanClaimMilestone(0));
            _missions.Track(MissionMetric.AdWatched, 1);
            _missions.Track(MissionMetric.StagePlayed, 3);
            _missions.TryClaim(_ad);
            _missions.TryClaim(_play);
            var granted = new List<LootDrop>();
            Assert.IsTrue(_missions.TryClaimMilestone(0, granted));
            Assert.AreEqual(500, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(1, granted.Count);
            Assert.IsFalse(_missions.TryClaimMilestone(0, granted));
            Assert.IsFalse(_missions.CanClaimMilestone(1), "chưa đủ 100 điểm");
        }

        [Test]
        public void Milestone_RewardUsesRemoteMultiplier()
        {
            Tuning.Apply(new Dictionary<string, string> { [Tuning.KeyMissionRewardMultiplier] = "2" });
            _missions.Track(MissionMetric.AdWatched, 1);
            _missions.Track(MissionMetric.StagePlayed, 3);
            _missions.TryClaim(_ad);
            _missions.TryClaim(_play);
            _missions.TryClaimMilestone(0, null);
            Assert.AreEqual(1000, _economy.GetBalance(CurrencyIds.Gold));
        }

        [Test]
        public void NewUtcDay_ResetsProgressClaimsAndMilestones()
        {
            _missions.Track(MissionMetric.AdWatched, 1);
            _missions.TryClaim(_ad);
            _missions.Track(MissionMetric.StagePlayed, 2);
            _time.Now = _time.Now.AddDays(1);
            Assert.AreEqual(0, _missions.Progress(_play));
            Assert.IsFalse(_missions.IsClaimed(_ad));
            Assert.AreEqual(0, _missions.Points);
        }

        [Test]
        public void HasClaimable_ReflectsLoginAndMissions()
        {
            Assert.IsTrue(_missions.HasClaimable, "quà đăng nhập ngày đầu");
            _missions.TryClaimLogin(null);
            Assert.IsFalse(_missions.HasClaimable);
            _missions.Track(MissionMetric.AdWatched, 1);
            Assert.IsTrue(_missions.HasClaimable);
        }

        // ---------- Quà đăng nhập ----------

        [Test]
        public void Login_OncePerDay_SkippingDaysKeepsStreak_WrapsAfterSeven()
        {
            Assert.IsTrue(_missions.TryClaimLogin(null));
            Assert.AreEqual(100, _economy.GetBalance(CurrencyIds.Gold));
            Assert.IsFalse(_missions.TryClaimLogin(null), "1 lần mỗi ngày");

            _time.Now = _time.Now.AddDays(5); // bỏ 4 ngày
            Assert.IsTrue(_missions.TryClaimLogin(null));
            Assert.AreEqual(100 + 200, _economy.GetBalance(CurrencyIds.Gold), "ngày 2, không mất chuỗi");

            for (int i = 0; i < 5; i++) { _time.Now = _time.Now.AddDays(1); _missions.TryClaimLogin(null); }
            Assert.AreEqual(0, _missions.LoginIndex, "nhận đủ 7 ô thì quay về ô 1");
        }

        // ---------- Đồng hồ ----------

        [Test]
        public void Clock_NeverGoesBackBeforeLastSeen()
        {
            var t1 = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
            var t0 = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(t1, ClockRules.Trusted(t0, t1.Ticks, TimeSpan.Zero));
            Assert.AreEqual(t1.AddHours(2), ClockRules.Trusted(t1, t0.Ticks, TimeSpan.FromHours(2)), "cộng độ lệch máy chủ");
        }

        [Test]
        public void GameClock_RecordsLastSeenAndResistsRollback()
        {
            DateTime device = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
            var profile = new PlayerProfile();
            var clock = new GameClock(() => profile.lastSeenUtcTicks, t => profile.lastSeenUtcTicks = t, () => device);
            Assert.AreEqual(device, clock.UtcNow);
            device = device.AddDays(-3); // chỉnh lùi giờ máy
            Assert.AreEqual(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc), clock.UtcNow);
        }

        // ---------- Tuning / Remote Config ----------

        [Test]
        public void Tuning_AppliesValidKeysIgnoresBadOnes()
        {
            int applied = Tuning.Apply(new Dictionary<string, string>
            {
                [Tuning.KeyDifficultyGrowth] = "0.05",
                [Tuning.KeyReviveGemCost] = "abc",
                [Tuning.KeyAdLimitRevive] = "999",
                ["khoa_la"] = "1",
            });
            Assert.AreEqual(1, applied);
            Assert.AreEqual(0.05f, Tuning.DifficultyGrowth, 1e-6f);
            Assert.AreEqual(Tuning.DefaultReviveGemCost, Tuning.ReviveGemCost);
            Assert.AreEqual(Tuning.DefaultAdLimitRevive, Tuning.AdLimitRevive);
            Assert.AreEqual(1f + 0.05f * 9, StageRules.DifficultyMultiplier(10), 1e-5f, "StageRules đọc Tuning");
        }

        [Test]
        public void Tuning_ApplyResetsKeysMissingFromNewSet()
        {
            Tuning.Apply(new Dictionary<string, string> { [Tuning.KeyReviveGemCost] = "30" });
            Tuning.Apply(new Dictionary<string, string>());
            Assert.AreEqual(Tuning.DefaultReviveGemCost, Tuning.ReviveGemCost);
        }

        [Test]
        public void RemoteConfig_ParseConvertsNumbersInvariant()
        {
            Dictionary<string, string> d = RemoteConfigService.Parse("{\"difficulty_growth\": 0.06, \"revive_gem_cost\": 25, \"x\": null}");
            Assert.AreEqual("0.06", d["difficulty_growth"]);
            Assert.AreEqual("25", d["revive_gem_cost"]);
            Assert.IsFalse(d.ContainsKey("x"));
        }

        [Test]
        public void AdLimit_FollowsTuning()
        {
            Tuning.Apply(new Dictionary<string, string> { [Tuning.KeyAdLimitFreeChest] = "4" });
            Assert.AreEqual(4, AdPlacements.DailyLimit(AdPlacements.FreeChest));
        }

        // ---------- Analytics ----------

        [Test]
        public void Analytics_SendsOnlyWithConsent()
        {
            var sink = new MemorySink();
            var analytics = new AnalyticsService(sink, _profile);
            _profile.consent = -1;
            Assert.IsFalse(analytics.Begin().Param("a", 1).Send("x"));
            _profile.consent = 0;
            Assert.IsFalse(analytics.Begin().Send("x"));
            _profile.consent = 1;
            Assert.IsTrue(analytics.Begin().Param("a", 1).Send("x"));
            Assert.AreEqual(1, sink.Events.Count);
            Assert.AreEqual(1, sink.Params[0]["a"]);
        }

        [Test]
        public void Analytics_TrackerLogsCurrencyWithReason()
        {
            var sink = new MemorySink();
            _profile.consent = 1;
            var analytics = new AnalyticsService(sink, _profile);
            var tracker = new AnalyticsTracker(analytics, _profile, _economy, null, null, null, _ads, _missions);
            try
            {
                _economy.Add(CurrencyIds.Gold, 50, EconomyReasons.StageReward);
                int i = sink.Events.IndexOf(AnalyticsEvents.Currency);
                Assert.GreaterOrEqual(i, 0);
                Assert.AreEqual(EconomyReasons.StageReward, sink.Params[i]["reason"]);
                Assert.AreEqual(50L, sink.Params[i]["delta"]);

                StageEvents.RaiseStageCompleted(new StageResult { stageId = "1-3", victory = false, waveReached = 2 });
                int j = sink.Events.IndexOf(AnalyticsEvents.StageEnd);
                Assert.AreEqual(2, sink.Params[j]["wave"]);
            }
            finally { tracker.Disconnect(); }
        }

        [Test]
        public void Economy_TransactionWithoutReasonIsUnknown()
        {
            CurrencyTransaction last = default;
            _economy.OnTransaction += t => last = t;
            _economy.Add(CurrencyIds.Gem, 5);
            Assert.AreEqual(EconomyReasons.Unknown, last.reason);
            Assert.AreEqual(5, last.delta);
        }

        // ---------- Save ----------

        [Test]
        public void MigrationV6ToV7_AddsDailyState()
        {
            var v6 = new JObject { ["saveVersion"] = 6, ["accountXp"] = 7, ["currencies"] = new JObject(), ["stageStars"] = new JObject(),
                ["consent"] = 1 };
            var profile = SaveMigrations.Migrate(v6, 6).ToObject<PlayerProfile>();
            Assert.AreEqual(PlayerProfile.CurrentVersion, profile.saveVersion);
            Assert.IsNotNull(profile.daily);
            Assert.IsNotNull(profile.daily.progress);
            Assert.AreEqual(0, profile.login.count);
            Assert.AreEqual(1, profile.consent);
            Assert.AreEqual(7, profile.accountXp);
        }
    }
}
