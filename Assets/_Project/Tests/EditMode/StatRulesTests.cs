using System.Collections.Generic;
using ArenaSurvival.Combat;
using ArenaSurvival.Meta;
using ArenaSurvival.Stats;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class StatRulesTests
    {
        [Test]
        public void Add_PutsValueIntoMatchingField()
        {
            StatBlock s = default;
            s = StatRules.Add(s, StatKind.MaxHealth, 10);
            s = StatRules.Add(s, StatKind.Armor, 4);
            s = StatRules.Add(s, StatKind.DamagePercent, 6);
            s = StatRules.Add(s, StatKind.MoveSpeedPercent, 2);
            s = StatRules.Add(s, StatKind.HealthRegen, 0.4f);

            Assert.AreEqual(10, s.maxHealth, 0.0001f);
            Assert.AreEqual(4, s.armor, 0.0001f);
            Assert.AreEqual(6, s.damagePercent, 0.0001f);
            Assert.AreEqual(2, s.moveSpeedPercent, 0.0001f);
            Assert.AreEqual(0.4f, s.healthRegen, 0.0001f);
        }

        [Test]
        public void StatBlock_PlusAddsEveryField()
        {
            var a = new StatBlock { maxHealth = 100, armor = 1, damagePercent = 2, moveSpeedPercent = 3, healthRegen = 0.5f };
            var b = new StatBlock { maxHealth = 20, armor = 4, damagePercent = 6, moveSpeedPercent = 1, healthRegen = 0.25f };
            StatBlock c = a + b;
            Assert.AreEqual(120, c.maxHealth, 0.0001f);
            Assert.AreEqual(5, c.armor, 0.0001f);
            Assert.AreEqual(8, c.damagePercent, 0.0001f);
            Assert.AreEqual(4, c.moveSpeedPercent, 0.0001f);
            Assert.AreEqual(0.75f, c.healthRegen, 0.0001f);
        }

        [Test]
        public void Compute_AddsTalentLevelsOnTopOfHeroBase()
        {
            var hp = ScriptableObject.CreateInstance<TalentData>();
            hp.id = "talent_hp"; hp.stat = StatKind.MaxHealth; hp.valuePerLevel = 10;
            var dmg = ScriptableObject.CreateInstance<TalentData>();
            dmg.id = "talent_damage"; dmg.stat = StatKind.DamagePercent; dmg.valuePerLevel = 3;
            var catalog = ScriptableObject.CreateInstance<TalentCatalog>();
            catalog.talents = new[] { hp, dmg };
            var levels = new Dictionary<string, int> { ["talent_hp"] = 5, ["talent_damage"] = 2 };

            StatBlock s = StatCalculator.Compute(new StatBlock { maxHealth = 100 }, catalog,
                id => levels.TryGetValue(id, out int l) ? l : 0);

            Assert.AreEqual(150, s.maxHealth, 0.0001f);
            Assert.AreEqual(6, s.damagePercent, 0.0001f);
            Assert.AreEqual(0, s.armor, 0.0001f);

            Object.DestroyImmediate(hp); Object.DestroyImmediate(dmg); Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Compute_NoCatalog_ReturnsHeroBase()
        {
            StatBlock s = StatCalculator.Compute(new StatBlock { maxHealth = 100 }, null, _ => 3);
            Assert.AreEqual(100, s.maxHealth, 0.0001f);
        }

        [Test]
        public void Power_GrowsWhenAnyStatGrows()
        {
            var baseStats = new StatBlock { maxHealth = 100 };
            int p0 = StatCalculator.Power(baseStats);
            foreach (StatKind kind in System.Enum.GetValues(typeof(StatKind)))
            {
                Assert.Greater(StatCalculator.Power(StatRules.Add(baseStats, kind, 1)), p0, kind.ToString());
            }
        }

        [Test]
        public void Power_BaseHeroIs100()
        {
            Assert.AreEqual(100, StatCalculator.Power(new StatBlock { maxHealth = 100 }));
        }

        [TestCase(0f, 20f)]
        [TestCase(100f, 10f)]   // 100 giáp = giảm một nửa
        [TestCase(-50f, 20f)]   // giáp âm không làm tăng sát thương
        public void ApplyArmor_ReducesDamage(float armor, float expected)
        {
            Assert.AreEqual(expected, CombatRules.ApplyArmor(20f, armor), 0.0001f);
        }

        [Test]
        public void ApplyArmor_NeverBlocksEverything()
        {
            Assert.Greater(CombatRules.ApplyArmor(20f, 100000f), 0f);
        }

        [TestCase(100, 1.12f, 0, 100)]
        [TestCase(100, 1.12f, 1, 112)]
        [TestCase(100, 1.12f, 10, 310)]  // 100 × 1.12^10 = 310.58
        public void TalentCost_GrowsGeometrically(int baseCost, float growth, int level, int expected)
        {
            Assert.AreEqual(expected, TalentRules.Cost(baseCost, growth, level));
        }
    }
}
