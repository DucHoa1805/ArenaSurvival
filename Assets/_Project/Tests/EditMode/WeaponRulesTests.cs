using ArenaSurvival.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class WeaponRulesTests
    {
        private WeaponData _gun;

        [SetUp]
        public void SetUp()
        {
            _gun = ScriptableObject.CreateInstance<WeaponData>();
            _gun.id = "weapon_shotgun";
            _gun.damage = 2.5f; _gun.damagePerLevel = 0.2f; _gun.fireRate = 0.9f; _gun.projectileCount = 6;
            _gun.spreadAngle = 60f; _gun.range = 3.5f; _gun.projectileSpeed = 11f; _gun.knockback = 0.35f;
            _gun.starBonus3 = new WeaponStarBonus { extraProjectiles = 2 };
            _gun.starBonus5 = new WeaponStarBonus { knockbackPercent = 100f };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_gun);

        [Test]
        public void ShardId_PrefixesWeaponId() => Assert.AreEqual("shard_weapon_laser", WeaponRules.ShardId("weapon_laser"));

        [Test]
        public void StarTables_MatchSpec()
        {
            int[] shards = { 10, 20, 40, 80, 150 };
            float[] mult = { 1f, 1.25f, 1.5f, 1.8f, 2.2f };
            for (int s = 1; s <= WeaponRules.MaxStars; s++)
            {
                Assert.AreEqual(shards[s - 1], WeaponRules.ShardsFor(s), $"mảnh cho {s}★");
                Assert.AreEqual(mult[s - 1], WeaponRules.StarMultiplier(s), 1e-4, $"hệ số {s}★");
                Assert.AreEqual(10 * s, WeaponRules.LevelCap(s), $"trần cấp {s}★");
            }
        }

        [Test]
        public void UpgradeCost_FollowsFormula()
        {
            Assert.AreEqual(100, WeaponRules.UpgradeGold(1));
            Assert.AreEqual(174, WeaponRules.UpgradeGold(5)); // 100 × 1.15^4 = 174.9
            Assert.AreEqual(1, WeaponRules.UpgradeOre(1));
            Assert.AreEqual(4, WeaponRules.UpgradeOre(6));
        }

        [Test]
        public void Effective_Level1Star1_IsBaseData()
        {
            WeaponStats s = WeaponRules.Effective(_gun, 1, 1);
            Assert.AreEqual(2.5f, s.damage, 1e-4);
            Assert.AreEqual(6, s.projectileCount);
            Assert.AreEqual(0.35f, s.knockback, 1e-4);
            Assert.AreEqual(3.5f, s.range, 1e-4);
        }

        [Test]
        public void Effective_LevelAndStarsScaleDamage()
        {
            WeaponStats s = WeaponRules.Effective(_gun, 6, 2);
            Assert.AreEqual((2.5f + 0.2f * 5) * 1.25f, s.damage, 1e-4);
        }

        [Test]
        public void Effective_StarBonusesApplyAtThreeAndFive()
        {
            Assert.AreEqual(6, WeaponRules.Effective(_gun, 1, 2).projectileCount);
            Assert.AreEqual(8, WeaponRules.Effective(_gun, 1, 3).projectileCount, "3★: +2 viên");
            Assert.AreEqual(0.35f, WeaponRules.Effective(_gun, 1, 4).knockback, 1e-4);
            WeaponStats five = WeaponRules.Effective(_gun, 1, 5);
            Assert.AreEqual(8, five.projectileCount, "bonus 3★ vẫn giữ ở 5★");
            Assert.AreEqual(0.7f, five.knockback, 1e-4, "5★: đẩy lùi +100%");
        }

        [Test]
        public void Effective_PercentBonuses()
        {
            _gun.explosionRadius = 1.6f; _gun.explosionDamageFraction = 0.7f;
            _gun.starBonus3 = new WeaponStarBonus { explosionRadiusPercent = 30f, fireRatePercent = 25f, extraPierce = 2 };
            WeaponStats s = WeaponRules.Effective(_gun, 1, 3);
            Assert.AreEqual(1.6f * 1.3f, s.explosionRadius, 1e-4);
            Assert.AreEqual(0.9f * 1.25f, s.fireRate, 1e-4);
            Assert.AreEqual(2, s.pierce);
        }

        [Test]
        public void Effective_ClampsInvalidLevelAndStars()
        {
            WeaponStats s = WeaponRules.Effective(_gun, 0, 9);
            Assert.AreEqual(2.5f * 2.2f, s.damage, 1e-4, "cấp < 1 coi như 1, sao > 5 coi như 5");
        }

        [Test]
        public void Power_IncreasesWithLevelStarsPierceAndExplosion()
        {
            int p1 = WeaponRules.Power(WeaponRules.Effective(_gun, 1, 1));
            int p2 = WeaponRules.Power(WeaponRules.Effective(_gun, 10, 1));
            int p3 = WeaponRules.Power(WeaponRules.Effective(_gun, 10, 3));
            Assert.Less(p1, p2);
            Assert.Less(p2, p3);

            WeaponStats basic = new WeaponStats { damage = 5, fireRate = 1.5f, projectileCount = 1 };
            WeaponStats pierce = basic; pierce.pierce = 2;
            WeaponStats boom = basic; boom.explosionRadius = 1f; boom.explosionDamageFraction = 0.5f;
            Assert.AreEqual(30, WeaponRules.Power(basic), "4 × 5 × 1.5 × 1");
            Assert.Greater(WeaponRules.Power(pierce), WeaponRules.Power(basic));
            Assert.Greater(WeaponRules.Power(boom), WeaponRules.Power(basic));
        }
    }
}
