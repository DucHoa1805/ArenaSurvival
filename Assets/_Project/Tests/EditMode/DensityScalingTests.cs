using System.Collections.Generic;
using ArenaSurvival.Core;
using ArenaSurvival.RunUpgrades;
using ArenaSurvival.Stages;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class DensityScalingTests
    {
        [SetUp] public void SetUp() => Tuning.Reset();
        [TearDown] public void TearDown() => Tuning.Reset();

        [Test]
        public void SpawnDensity_HighEarly_FadesToOneByChapterThree()
        {
            Assert.AreEqual(1.8f, StageRules.SpawnDensity(1), 1e-4f);
            Assert.AreEqual(1f + 0.8f * (1f - 9f / 20f), StageRules.SpawnDensity(10), 1e-4f);
            Assert.AreEqual(1f, StageRules.SpawnDensity(21), 1e-4f);
            Assert.AreEqual(1f, StageRules.SpawnDensity(30), 1e-4f);
        }

        [Test]
        public void SpawnDensity_FollowsRemoteConfig()
        {
            Tuning.Apply(new Dictionary<string, string> { [Tuning.KeySpawnDensity] = "1.5", [Tuning.KeySpawnEarlyBoost] = "0" });
            Assert.AreEqual(1.5f, StageRules.SpawnDensity(1), 1e-4f);
        }

        [Test]
        public void SpawnCount_CarriesFractionAndAveragesCorrectly()
        {
            float carry = 0f;
            int total = 0;
            for (int i = 0; i < 10; i++) total += StageRules.SpawnCount(ref carry, 1.6f, 0, 999);
            Assert.AreEqual(16, total);
            Assert.Less(carry, 1f);
        }

        [Test]
        public void SpawnCount_RespectsAliveCap()
        {
            float carry = 0f;
            Assert.AreEqual(1, StageRules.SpawnCount(ref carry, 3f, 219, 220));
            Assert.AreEqual(0, StageRules.SpawnCount(ref carry, 3f, 220, 220));
        }

        [Test]
        public void StageScale_GrowsThreePercentPerStage()
        {
            Assert.AreEqual(1f, SkillRules.StageScale(1), 1e-5f);
            Assert.AreEqual(1.27f, SkillRules.StageScale(10), 1e-4f);
            Assert.AreEqual(1.87f, SkillRules.StageScale(30), 1e-4f);
        }

        [Test]
        public void StatDescription_ShowsScaledNumbers()
        {
            Assert.AreEqual("Mọi đòn gây thêm 47% sát thương.", SkillRules.StatDescription(UpgradeType.Damage, 0.25f * 1.87f));
            Assert.AreEqual("+37 máu tối đa (hồi luôn phần tăng).", SkillRules.StatDescription(UpgradeType.MaxHealth, 20f * 1.87f));
            Assert.IsNull(SkillRules.StatDescription(UpgradeType.Aura, 1f));
            Assert.IsTrue(SkillRules.ScalesSkillDamage(UpgradeType.Lightning));
            Assert.IsFalse(SkillRules.ScalesSkillDamage(UpgradeType.Multishot));
        }
    }
}
