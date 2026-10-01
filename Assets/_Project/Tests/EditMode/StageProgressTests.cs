using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class StageProgressTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private StageProgressService _progress;
        private int _saves;
        private StageCatalog _catalog;
        private StageData _s1, _s2, _s3;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _saves = 0;
            _progress = new StageProgressService(_profile, _economy, () => _saves++);

            _s1 = Stage("c1_s01"); _s2 = Stage("c1_s02"); _s3 = Stage("c1_s03");
            _catalog = ScriptableObject.CreateInstance<StageCatalog>();
            _catalog.stages = new[] { _s1, _s2, _s3 };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in new Object[] { _s1, _s2, _s3, _catalog }) Object.DestroyImmediate(o);
        }

        private static StageData Stage(string id)
        {
            var s = ScriptableObject.CreateInstance<StageData>();
            s.id = id;
            return s;
        }

        private static StageResult Win(string id, int stars, int gold = 100, int xp = 20) => new StageResult
        {
            stageId = id, victory = true, starCount = stars, gold = gold, accountXp = xp,
        };

        private static StageResult Lose(string id, int gold = 10, int xp = 2) => new StageResult
        {
            stageId = id, victory = false, starCount = 0, gold = gold, accountXp = xp,
        };

        [TestCase(true, false, 2f)]
        [TestCase(true, true, 0.5f)]
        [TestCase(false, false, 1f)]
        [TestCase(false, true, 1f)]
        public void RewardMultiplier_FirstClearDoubleReplayHalf(bool victory, bool cleared, float expected)
        {
            Assert.AreEqual(expected, StageProgressRules.RewardMultiplier(victory, cleared), 0.0001f);
        }

        [Test]
        public void FirstWin_DoublesRewardsRecordsStarsAndSaves()
        {
            StageGrant g = _progress.Apply(Win("c1_s01", 2));

            Assert.IsTrue(g.firstClear);
            Assert.AreEqual(200, g.gold);
            Assert.AreEqual(40, g.accountXp);
            Assert.AreEqual(200, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(40, _profile.accountXp);
            Assert.AreEqual(2, _progress.GetStars("c1_s01"));
            Assert.IsTrue(_progress.IsCleared("c1_s01"));
            Assert.AreEqual(1, _saves, "lưu ngay khi áp kết quả: tắt app ở màn kết quả không mất thưởng");
            Assert.AreSame(g, _progress.LastGrant);
        }

        [Test]
        public void Replay_HalvesRewardsAndKeepsBestStars()
        {
            _progress.Apply(Win("c1_s01", 3));
            StageGrant g = _progress.Apply(Win("c1_s01", 1));

            Assert.IsFalse(g.firstClear);
            Assert.AreEqual(50, g.gold);
            Assert.AreEqual(3, _progress.GetStars("c1_s01"), "sao chỉ tăng, chơi lại kém hơn không làm mất sao");
            Assert.AreEqual(3, g.starsBefore);
            Assert.AreEqual(3, g.starsAfter);
        }

        [Test]
        public void Replay_WithMoreStars_RaisesBest()
        {
            _progress.Apply(Win("c1_s01", 1));
            StageGrant g = _progress.Apply(Win("c1_s01", 3));
            Assert.AreEqual(1, g.starsBefore);
            Assert.AreEqual(3, g.starsAfter);
        }

        [Test]
        public void Defeat_GivesBaseRewardsWithoutClearing()
        {
            StageGrant g = _progress.Apply(Lose("c1_s01"));

            Assert.AreEqual(10, g.gold);
            Assert.IsFalse(g.firstClear);
            Assert.IsFalse(_progress.IsCleared("c1_s01"));
            Assert.AreEqual(0, _progress.GetStars("c1_s01"));
        }

        [Test]
        public void ZeroReward_DoesNotFail()
        {
            StageGrant g = _progress.Apply(Lose("c1_s01", 0, 0));
            Assert.AreEqual(0, g.gold);
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gold));
        }

        [Test]
        public void Unlock_IsSequential()
        {
            Assert.IsTrue(_progress.IsUnlocked(_catalog, _s1));
            Assert.IsFalse(_progress.IsUnlocked(_catalog, _s2));

            _progress.Apply(Win("c1_s01", 1));
            Assert.IsTrue(_progress.IsUnlocked(_catalog, _s2));
            Assert.IsFalse(_progress.IsUnlocked(_catalog, _s3));
        }

        [Test]
        public void NextStage_IsFirstUncleared_ThenLastWhenAllCleared()
        {
            Assert.AreSame(_s1, _progress.NextStage(_catalog));
            _progress.Apply(Win("c1_s01", 1));
            Assert.AreSame(_s2, _progress.NextStage(_catalog));
            _progress.Apply(Win("c1_s02", 1));
            _progress.Apply(Win("c1_s03", 1));
            Assert.AreSame(_s3, _progress.NextStage(_catalog));
        }

        [Test]
        public void NullResult_IsIgnored()
        {
            Assert.IsNull(_progress.Apply(null));
            Assert.AreEqual(0, _saves);
        }
    }
}
