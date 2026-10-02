using System.Collections.Generic;
using System.IO;
using ArenaSurvival.DevTools;
using ArenaSurvival.RunUpgrades;
using ArenaSurvival.Save;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class AutoplayTests
    {
        private static readonly Rect Arena = new Rect(-10f, -10f, 20f, 20f);

        [Test]
        public void SavePath_SameFolderDifferentFileThanRealSave()
        {
            string real = SaveService.DefaultPath;
            string bot = AutoplayRules.SavePath(Path.GetDirectoryName(real));
            Assert.AreNotEqual(real, bot, "bot không bao giờ được ghi vào save thật");
            Assert.AreEqual(Path.GetDirectoryName(real), Path.GetDirectoryName(bot));
        }

        [Test]
        public void PickBest_PrefersFreeThenPowerPerGold()
        {
            var options = new List<AutoplayOption>
            {
                new AutoplayOption(10f, 100, true),   // 0.1/vàng
                new AutoplayOption(30f, 200, true),   // 0.15/vàng
                new AutoplayOption(50f, 100, false),  // tốt nhất nhưng chưa đủ tiền
            };
            Assert.AreEqual(1, AutoplayRules.PickBest(options));

            options.Add(new AutoplayOption(5f, 0, true)); // mặc đồ tốt hơn: không tốn vàng
            Assert.AreEqual(3, AutoplayRules.PickBest(options));
        }

        [Test]
        public void PickBest_NothingUseful_ReturnsMinusOne()
        {
            var options = new List<AutoplayOption> { new AutoplayOption(0f, 0, true), new AutoplayOption(20f, 50, false) };
            Assert.AreEqual(-1, AutoplayRules.PickBest(options));
        }

        [Test]
        public void ChooseStage_FarmsAfterLossStreak_StuckAfterLimit()
        {
            Assert.AreEqual(StageChoice.Target, AutoplayRules.ChooseStage(2, 2, true));
            Assert.AreEqual(StageChoice.Farm, AutoplayRules.ChooseStage(3, 3, true));
            Assert.AreEqual(StageChoice.Target, AutoplayRules.ChooseStage(3, 3, false), "chưa thắng màn nào thì không có gì để farm");
            Assert.AreEqual(StageChoice.Stuck, AutoplayRules.ChooseStage(AutoplayRules.StuckLimit, 0, true));
        }

        [Test]
        public void UpgradeRank_HealFirstOnlyWhenLow()
        {
            Assert.Less(AutoplayRules.UpgradeRank(UpgradeType.AddWeapon, 1f), AutoplayRules.UpgradeRank(UpgradeType.Damage, 1f));
            Assert.Greater(AutoplayRules.UpgradeRank(UpgradeType.Heal, 0.9f), AutoplayRules.UpgradeRank(UpgradeType.MoveSpeed, 0.9f));
            Assert.Less(AutoplayRules.UpgradeRank(UpgradeType.Heal, 0.3f), AutoplayRules.UpgradeRank(UpgradeType.AddWeapon, 0.3f));
        }

        private static Vector2 Steer(Vector2 self, params Threat[] threats) =>
            BotSteering.Compute(self, threats, threats.Length, Arena, null, SteeringSettings.Default);

        [Test]
        public void Steering_RunsAwayFromNearbyEnemy()
        {
            Vector2 v = Steer(Vector2.zero, new Threat { kind = ThreatKind.Enemy, position = new Vector2(1.5f, 0f) });
            Assert.Less(v.x, -0.9f);
        }

        [Test]
        public void Steering_DodgesBulletSideways()
        {
            var bullet = new Threat { kind = ThreatKind.Bullet, position = new Vector2(-2f, 0.1f), vector = new Vector2(6f, 0f) };
            Vector2 v = Steer(Vector2.zero, bullet);
            Assert.Greater(Mathf.Abs(v.y), Mathf.Abs(v.x), "né ngang đường đạn chứ không chạy dọc theo nó");

            var passed = new Threat { kind = ThreatKind.Bullet, position = new Vector2(2f, 0f), vector = new Vector2(6f, 0f) };
            Assert.AreEqual(Vector2.zero, Steer(Vector2.zero, passed), "đạn đã bay qua thì bỏ qua");
        }

        [Test]
        public void Steering_LeavesArtilleryCircle()
        {
            var blast = new Threat { kind = ThreatKind.Blast, position = new Vector2(0.5f, 0f), radius = 1.5f };
            Assert.Less(Steer(Vector2.zero, blast).x, -0.9f);
            Assert.AreEqual(Vector2.zero, Steer(new Vector2(0f, 4f), blast), "ngoài vòng (và xa tường) thì không cần chạy");
        }

        [Test]
        public void Steering_StepsOffSniperBeam()
        {
            var beam = new Threat { kind = ThreatKind.Beam, position = new Vector2(-6f, 0.2f), vector = Vector2.right, radius = 14f };
            Vector2 v = Steer(Vector2.zero, beam);
            Assert.Greater(Mathf.Abs(v.y), 0.9f);
        }

        [Test]
        public void Steering_PushedOffWallsAndCorners()
        {
            Assert.Greater(Steer(new Vector2(-9.5f, 0f)).x, 0.9f);
            Vector2 corner = Steer(new Vector2(9.5f, 9.5f));
            Assert.Less(corner.x, -0.5f);
            Assert.Less(corner.y, -0.5f);
        }

        [Test]
        public void Steering_AttractsToPickupOnlyWhenSafe()
        {
            Vector2? orb = new Vector2(0f, 3f);
            Vector2 safe = BotSteering.Compute(Vector2.zero, new Threat[0], 0, Arena, orb, SteeringSettings.Default);
            Assert.Greater(safe.y, 0.9f);

            var enemy = new[] { new Threat { kind = ThreatKind.Enemy, position = new Vector2(1f, 0f) } };
            Vector2 danger = BotSteering.Compute(Vector2.zero, enemy, 1, Arena, orb, SteeringSettings.Default);
            Assert.Less(danger.x, -0.9f, "có quái sát người thì không đi nhặt orb");
        }
    }
}
