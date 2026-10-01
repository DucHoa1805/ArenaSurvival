using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class AccountRulesTests
    {
        [Test]
        public void XpToNext_FollowsPlanFormula()
        {
            Assert.AreEqual(100, AccountRules.XpToNext(1));
            Assert.AreEqual(283, AccountRules.XpToNext(2)); // 100 × 2^1.5 = 282.8
            Assert.AreEqual(520, AccountRules.XpToNext(3));
            Assert.AreEqual(0, AccountRules.XpToNext(AccountRules.MaxLevel), "cấp tối đa không lên nữa");
        }

        [Test]
        public void FromXp_HandlesBoundaries()
        {
            Assert.AreEqual(1, AccountRules.FromXp(0).level);
            Assert.AreEqual(1, AccountRules.FromXp(99).level);
            AccountProgress p = AccountRules.FromXp(100);
            Assert.AreEqual(2, p.level);
            Assert.AreEqual(0, p.xpInLevel);
            Assert.AreEqual(283, p.xpForLevel);
            AccountProgress q = AccountRules.FromXp(100 + 283 + 10);
            Assert.AreEqual(3, q.level);
            Assert.AreEqual(10, q.xpInLevel);
        }

        [Test]
        public void FromXp_CapsAtMaxLevel()
        {
            AccountProgress p = AccountRules.FromXp(long.MaxValue / 2);
            Assert.AreEqual(AccountRules.MaxLevel, p.level);
            Assert.AreEqual(0, p.xpForLevel);
        }

        [Test]
        public void RewardFor_GoldScalesGemBonusEveryFiveLevels()
        {
            var r = new List<CurrencyCost>();
            AccountRules.RewardFor(3, r);
            Assert.AreEqual(CurrencyIds.Gold, r[0].id);
            Assert.AreEqual(300, r[0].amount);
            Assert.AreEqual(CurrencyIds.Gem, r[1].id);
            Assert.AreEqual(5, r[1].amount);
            AccountRules.RewardFor(5, r);
            Assert.AreEqual(20, r[1].amount);
        }
    }

    public class AccountServiceTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private AccountLevelTable _table;
        private AccountService _account;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _table = ScriptableObject.CreateInstance<AccountLevelTable>();
            _table.unlocks = new[]
            {
                new FeatureUnlock { featureId = FeatureIds.Talents, level = 2, displayName = "Talent" },
                new FeatureUnlock { featureId = FeatureIds.Shop, level = 5, displayName = "Cửa hàng" },
            };
            _account = new AccountService(_profile, _economy, _table);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_table);

        [Test]
        public void NewAccount_Level1_TalentsLocked()
        {
            Assert.AreEqual(1, _account.Level);
            Assert.IsFalse(_account.IsUnlocked(FeatureIds.Talents));
            Assert.IsTrue(_account.IsUnlocked(FeatureIds.Equipment), "tính năng không có trong bảng = mở từ đầu");
            Assert.AreEqual(2, _account.UnlockLevel(FeatureIds.Talents));
        }

        [Test]
        public void AddXp_CrossingSeveralLevels_GrantsEachRewardAndQueuesInOrder()
        {
            int gained = _account.AddXp(100 + 283 + 520); // lên thẳng cấp 4
            Assert.AreEqual(3, gained);
            Assert.AreEqual(4, _account.Level);
            Assert.AreEqual(200 + 300 + 400, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(15, _economy.GetBalance(CurrencyIds.Gem));

            Assert.IsTrue(_account.TakePendingLevelUp(out LevelUpInfo first));
            Assert.AreEqual(2, first.level);
            CollectionAssert.Contains(first.unlockedFeatures, "Talent");
            Assert.IsTrue(_account.TakePendingLevelUp(out LevelUpInfo second));
            Assert.AreEqual(3, second.level);
            Assert.AreEqual(0, second.unlockedFeatures.Count);
            Assert.IsTrue(_account.TakePendingLevelUp(out _));
            Assert.IsFalse(_account.TakePendingLevelUp(out _));
        }

        [Test]
        public void AddXp_WithinLevel_NoReward()
        {
            Assert.AreEqual(0, _account.AddXp(50));
            Assert.AreEqual(50, _account.XpInLevel);
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gold));
            Assert.IsFalse(_account.TakePendingLevelUp(out _));
        }

        [Test]
        public void AddXp_ZeroOrNegative_Ignored()
        {
            Assert.AreEqual(0, _account.AddXp(0));
            Assert.AreEqual(0, _account.AddXp(-10));
            Assert.AreEqual(0, _profile.accountXp);
        }

        [Test]
        public void ExistingXp_IsNotRewardedRetroactively()
        {
            var profile = new PlayerProfile { accountXp = 1000 };
            var economy = new EconomyService(profile, null);
            var account = new AccountService(profile, economy, _table);
            Assert.AreEqual(4, account.Level);
            Assert.AreEqual(0, economy.GetBalance(CurrencyIds.Gold));
            Assert.IsFalse(account.TakePendingLevelUp(out _));
        }

        [Test]
        public void StageProgress_AddsXpThroughAccount()
        {
            var progress = new StageProgressService(_profile, _economy, null, null, _account);
            StageGrant grant = progress.Apply(new StageResult { stageId = "c1_s01", victory = true, starCount = 1, gold = 0, accountXp = 60 });
            Assert.AreEqual(120, _profile.accountXp, "lần thắng đầu ×2");
            Assert.AreEqual(1, grant.levelBefore);
            Assert.AreEqual(2, grant.levelAfter);
            Assert.IsTrue(_account.TakePendingLevelUp(out _));
        }
    }
}
