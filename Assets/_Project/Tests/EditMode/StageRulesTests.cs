using ArenaSurvival.Stages;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class StageRulesTests
    {
        [TestCase(1, 1f)]
        [TestCase(2, 1.075f)]
        [TestCase(11, 1.75f)]
        public void DifficultyMultiplier_Grows7_5PercentPerStage(int stage, float expected)
        {
            Assert.AreEqual(expected, StageRules.DifficultyMultiplier(stage), 0.0001f);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void DifficultyMultiplier_StageBelowOne_TreatedAsOne(int stage)
        {
            Assert.AreEqual(1f, StageRules.DifficultyMultiplier(stage), 0.0001f);
        }

        [Test]
        public void EvaluateStars_Defeat_NoStars()
        {
            CollectionAssert.AreEqual(new[] { false, false, false },
                StageRules.EvaluateStars(false, 1f, 10f, 100f));
        }

        [Test]
        public void EvaluateStars_PerfectWin_AllStars()
        {
            CollectionAssert.AreEqual(new[] { true, true, true },
                StageRules.EvaluateStars(true, 0.9f, 90f, 100f));
        }

        [Test]
        public void EvaluateStars_LowHealth_MissesSecondStar()
        {
            CollectionAssert.AreEqual(new[] { true, false, true },
                StageRules.EvaluateStars(true, 0.3f, 90f, 100f));
        }

        [Test]
        public void EvaluateStars_TooSlow_MissesThirdStar()
        {
            CollectionAssert.AreEqual(new[] { true, true, false },
                StageRules.EvaluateStars(true, 0.9f, 150f, 100f));
        }

        [Test]
        public void EvaluateStars_ExactlyHalfHealth_DoesNotCount()
        {
            // "Còn TRÊN 50% máu": đúng 50% chưa đủ.
            Assert.IsFalse(StageRules.EvaluateStars(true, 0.5f, 90f, 100f)[1]);
        }

        [Test]
        public void EvaluateStars_ExactlyTargetTime_Counts()
        {
            Assert.IsTrue(StageRules.EvaluateStars(true, 0.9f, 100f, 100f)[2]);
        }

        [Test]
        public void CalculateRewards_WinStageOne_GivesBase()
        {
            Assert.AreEqual((100, 20), StageRules.CalculateRewards(100, 20, 1, true, 1f));
        }

        [Test]
        public void CalculateRewards_WinStageThree_Scales30Percent()
        {
            Assert.AreEqual((130, 26), StageRules.CalculateRewards(100, 20, 3, true, 1f));
        }

        [Test]
        public void CalculateRewards_DefeatHalfway_GivesQuarter()
        {
            // thua: base × tiến độ (0.5) × 50%
            Assert.AreEqual((25, 5), StageRules.CalculateRewards(100, 20, 1, false, 0.5f));
        }

        [Test]
        public void CalculateRewards_DefeatAtFullProgress_CappedAt90Percent()
        {
            // Thua mà vẫn nhận như đã đi hết màn thì không còn lý do cố thắng: tiến độ tối đa 0.9.
            Assert.AreEqual((45, 9), StageRules.CalculateRewards(100, 20, 1, false, 1f));
        }

        [Test]
        public void CalculateRewards_ZeroBaseOrNegativeProgress_NeverNegative()
        {
            Assert.AreEqual((0, 0), StageRules.CalculateRewards(0, 0, 5, true, 1f));
            Assert.AreEqual((0, 0), StageRules.CalculateRewards(100, 20, 1, false, -1f));
        }

        [TestCase(3, 3, 0, true)]   // đã ra hết tinh anh và không còn con nào → quái thường còn lại nổ, thắng
        [TestCase(3, 3, 1, false)]  // còn 1 tinh anh sống
        [TestCase(3, 2, 0, false)]  // còn tinh anh chưa ra sân (đang rải dần)
        [TestCase(0, 0, 0, true)]   // màn không có tinh anh: hết đợt thường là xong
        public void EliteWaveCleared_OnlyWhenAllElitesSpawnedAndDead(int queued, int spawned, int alive, bool expected)
        {
            Assert.AreEqual(expected, StageRules.EliteWaveCleared(queued, spawned, alive));
        }

        [TestCase(30f, 120f, 0.25f)]
        [TestCase(200f, 120f, 1f)]
        [TestCase(10f, 0f, 0f)]
        public void Progress_IsClampedRatio(float survival, float planned, float expected)
        {
            Assert.AreEqual(expected, StageRules.Progress(survival, planned), 0.0001f);
        }
    }
}
