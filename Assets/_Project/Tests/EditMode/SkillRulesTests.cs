using ArenaSurvival.Combat;
using ArenaSurvival.RunUpgrades;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class SkillRulesTests
    {
        [TestCase(0, 5, 0, true)]   // kỹ năng mới, còn chỗ
        [TestCase(5, 5, 3, false)]  // đã tối đa
        [TestCase(2, 5, 8, true)]   // đủ 8 kỹ năng nhưng đây là kỹ năng đã có → vẫn nâng được
        [TestCase(0, 5, 8, false)]  // đủ 8 kỹ năng, kỹ năng mới → không đưa ra
        [TestCase(7, 0, 8, true)]   // không giới hạn cấp (Hồi máu) và không tính vào giới hạn
        public void IsOfferable(int level, int maxLevel, int distinctOwned, bool expected)
        {
            Assert.AreEqual(expected, SkillRules.IsOfferable(level, maxLevel, distinctOwned, 8));
        }

        [Test]
        public void CountsAsSkill_OnlyLimitedOnes()
        {
            Assert.IsTrue(SkillRules.CountsAsSkill(5));
            Assert.IsFalse(SkillRules.CountsAsSkill(0), "Hồi máu không chiếm chỗ kỹ năng");
        }

        [TestCase(1, 3)]
        [TestCase(2, 4)]
        [TestCase(6, 8)]
        [TestCase(9, 8)]
        public void SawBladeCount(int level, int expected) => Assert.AreEqual(expected, SkillRules.SawBladeCount(level));

        [Test]
        public void SawBladeDamage_Plus10PercentPerExtraLevel()
        {
            Assert.AreEqual(8f, SkillRules.SawBladeDamage(8f, 1), 1e-4);
            Assert.AreEqual(8f * 1.3f, SkillRules.SawBladeDamage(8f, 4), 1e-4);
        }

        [TestCase(1, 8f, 1)]
        [TestCase(4, 5f, 1)]
        [TestCase(5, 4f, 2)]
        public void Shield(int level, float cooldown, int layers)
        {
            Assert.AreEqual(cooldown, SkillRules.ShieldCooldown(level), 1e-4);
            Assert.AreEqual(layers, SkillRules.ShieldLayers(level));
        }

        [Test]
        public void Aura_GrowsWithLevel()
        {
            Assert.AreEqual(1.85f, SkillRules.AuraRadius(1), 1e-4);
            Assert.AreEqual(6f, SkillRules.AuraDamage(1), 1e-4);
            Assert.AreEqual(14f, SkillRules.AuraDamage(5), 1e-4);
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(5, 3)]
        public void MissileCount(int level, int expected) => Assert.AreEqual(expected, SkillRules.MissileCount(level));

        [Test]
        public void Multishot_SpreadWidensPerExtraBullet()
        {
            Assert.AreEqual(0f, SkillRules.SpreadWithExtra(0f, 0), 1e-4, "không có viên thêm thì giữ nguyên");
            Assert.AreEqual(20f, SkillRules.SpreadWithExtra(0f, 2), 1e-4);
            Assert.AreEqual(70f, SkillRules.SpreadWithExtra(50f, 2), 1e-4, "Shotgun đã tỏa sẵn thì nới thêm");
        }

        [Test]
        public void Health_ShieldLayerBlocksWholeHit()
        {
            var go = new GameObject("p");
            try
            {
                var hp = go.AddComponent<Health>();
                hp.SetMaxHealth(100f, true);
                int blocked = 0;
                hp.OnShieldBlocked += () => blocked++;
                hp.ShieldLayers = 1;
                hp.TakeDamage(40f);
                Assert.AreEqual(100f, hp.CurrentHealth, 1e-4, "lá chắn ăn trọn đòn");
                Assert.AreEqual(0, hp.ShieldLayers);
                Assert.AreEqual(1, blocked);
                hp.TakeDamage(40f);
                Assert.AreEqual(60f, hp.CurrentHealth, 1e-4, "hết lớp thì mất máu như thường");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
