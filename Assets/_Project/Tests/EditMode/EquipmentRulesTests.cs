using ArenaSurvival.Items;
using ArenaSurvival.Stats;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class EquipmentRulesTests
    {
        private static readonly StatBlock Base = new StatBlock { maxHealth = 20f, armor = 3f };
        private static readonly StatBlock PerLevel = new StatBlock { maxHealth = 4f, armor = 0.6f };

        [Test]
        public void StatBlock_MultiplyByFloat_ScalesEveryField()
        {
            var s = new StatBlock { maxHealth = 10, armor = 2, damagePercent = 4, moveSpeedPercent = 1, healthRegen = 0.5f } * 2f;
            Assert.AreEqual(20f, s.maxHealth, 1e-4);
            Assert.AreEqual(4f, s.armor, 1e-4);
            Assert.AreEqual(8f, s.damagePercent, 1e-4);
            Assert.AreEqual(2f, s.moveSpeedPercent, 1e-4);
            Assert.AreEqual(1f, s.healthRegen, 1e-4);
        }

        [Test]
        public void Stats_RareLevel3_IsBasePlusTwoLevelsTimesOnePointFive()
        {
            StatBlock s = EquipmentRules.Stats(Base, PerLevel, Rarity.Rare, 3);
            Assert.AreEqual((20f + 8f) * 1.5f, s.maxHealth, 1e-4);
            Assert.AreEqual((3f + 1.2f) * 1.5f, s.armor, 1e-4);
        }

        [Test]
        public void Stats_Level1Common_IsBase()
        {
            StatBlock s = EquipmentRules.Stats(Base, PerLevel, Rarity.Common, 1);
            Assert.AreEqual(20f, s.maxHealth, 1e-4);
            Assert.AreEqual(3f, s.armor, 1e-4);
        }

        [TestCase(Rarity.Common, 10)]
        [TestCase(Rarity.Rare, 20)]
        [TestCase(Rarity.Epic, 30)]
        [TestCase(Rarity.Legendary, 40)]
        public void RarityLevelCap_MatchesSpec(Rarity rarity, int cap) => Assert.AreEqual(cap, EquipmentRules.RarityLevelCap(rarity));

        [Test]
        public void LevelCap_LimitedByAscension()
        {
            Assert.AreEqual(10, EquipmentRules.LevelCap(Rarity.Legendary, 0));
            Assert.AreEqual(20, EquipmentRules.LevelCap(Rarity.Legendary, 1));
            Assert.AreEqual(20, EquipmentRules.LevelCap(Rarity.Rare, 5), "không vượt trần độ hiếm");
            Assert.AreEqual(10, EquipmentRules.LevelCap(Rarity.Common, 0));
        }

        [Test]
        public void CanAscend_OnlyAtCapAndBelowRarityCap()
        {
            Assert.IsTrue(EquipmentRules.CanAscend(Rarity.Rare, 10, 0));
            Assert.IsFalse(EquipmentRules.CanAscend(Rarity.Rare, 9, 0), "chưa tới mốc");
            Assert.IsFalse(EquipmentRules.CanAscend(Rarity.Rare, 20, 1), "đã tới trần độ hiếm");
            Assert.IsFalse(EquipmentRules.CanAscend(Rarity.Common, 10, 0), "đồ Thường không đột phá");
        }

        [Test]
        public void UpgradeCost_FollowsFormula()
        {
            Assert.AreEqual(100, EquipmentRules.UpgradeGold(Rarity.Common, 1));
            // 100 × 1.15^4 × 1.5 = 262.35...
            Assert.AreEqual(262, EquipmentRules.UpgradeGold(Rarity.Epic, 5));
            Assert.AreEqual(1, EquipmentRules.UpgradeMaterial(1));
            Assert.AreEqual(3, EquipmentRules.UpgradeMaterial(4));
            Assert.AreEqual(3, EquipmentRules.UpgradeMaterial(5));
        }

        [Test]
        public void AscendCost_GrowsWithAscension()
        {
            Assert.AreEqual(1, EquipmentRules.AscendCores(0));
            Assert.AreEqual(3, EquipmentRules.AscendCores(2));
            Assert.AreEqual(500, EquipmentRules.AscendGold(0));
            Assert.AreEqual(1000, EquipmentRules.AscendGold(1));
        }

        [Test]
        public void DismantleRefund_NewItem_IsOnlyBaseMaterial()
        {
            EquipmentRules.Refund r = EquipmentRules.DismantleRefund(Rarity.Epic, 1, 0);
            Assert.AreEqual(0, r.gold);
            Assert.AreEqual(12, r.material);
            Assert.AreEqual(0, r.cores);
        }

        [Test]
        public void DismantleRefund_UpgradedItem_Returns70PercentRoundedDown()
        {
            // Hiếm cấp 11, đã đột phá 1 lần: đã chi nâng cấp cấp 1..10 + 1 lần đột phá.
            long gold = 0, material = 0;
            for (int level = 1; level <= 10; level++)
            {
                gold += EquipmentRules.UpgradeGold(Rarity.Rare, level);
                material += EquipmentRules.UpgradeMaterial(level);
            }
            gold += EquipmentRules.AscendGold(0);
            long cores = EquipmentRules.AscendCores(0);

            EquipmentRules.Refund r = EquipmentRules.DismantleRefund(Rarity.Rare, 11, 1);
            Assert.AreEqual(gold * 7 / 10, r.gold);
            Assert.AreEqual(5 + material * 7 / 10, r.material);
            Assert.AreEqual(cores * 7 / 10, r.cores, "1 lõi × 70% làm tròn xuống = 0");
        }
    }
}
