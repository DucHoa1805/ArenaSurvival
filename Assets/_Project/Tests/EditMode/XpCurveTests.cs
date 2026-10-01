using ArenaSurvival.RunUpgrades;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class XpCurveTests
    {
        [TestCase(1, 5)]
        [TestCase(2, 10)]
        [TestCase(3, 15)]
        [TestCase(10, 50)]
        public void GetXpRequired_GrowsByFivePerLevel(int level, int expectedXp)
        {
            Assert.AreEqual(expectedXp, XpCurve.GetXpRequired(level));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void GetXpRequired_LevelBelowOne_TreatedAsLevelOne(int level)
        {
            Assert.AreEqual(XpCurve.GetXpRequired(1), XpCurve.GetXpRequired(level));
        }

        [Test]
        public void GetXpRequired_IsStrictlyIncreasing()
        {
            for (int level = 1; level < 50; level++)
            {
                Assert.Less(XpCurve.GetXpRequired(level), XpCurve.GetXpRequired(level + 1), $"level {level}");
            }
        }
    }
}
