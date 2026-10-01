using ArenaSurvival.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class EnemyRulesTests
    {
        [TestCase(10f, 1)]   // xa → tiến lại
        [TestCase(2f, -1)]   // gần → lùi ra
        [TestCase(5.5f, 0)]  // vừa → đứng bắn
        [TestCase(6.5f, 0)]  // đúng biên trên vẫn tính là vừa
        [TestCase(4.5f, 0)]  // đúng biên dưới vẫn tính là vừa
        public void RangeIntent_KeepsPreferredDistance(float distance, int expected)
        {
            Assert.AreEqual(expected, EnemyRules.RangeIntent(distance, 5.5f, 1f));
        }

        [Test]
        public void RadialDirection_IsUnitLengthAndEvenlySpaced()
        {
            const int count = 8;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = EnemyRules.RadialDirection(i, count, 0f);
                Vector2 b = EnemyRules.RadialDirection((i + 1) % count, count, 0f);
                Assert.AreEqual(1f, a.magnitude, 0.0001f);
                Assert.AreEqual(360f / count, Vector2.Angle(a, b), 0.01f);
            }
        }

        [Test]
        public void RadialDirection_FirstBulletPointsRightThenRotatesByOffset()
        {
            Vector2 noOffset = EnemyRules.RadialDirection(0, 4, 0f);
            Vector2 rotated = EnemyRules.RadialDirection(0, 4, 90f);
            Assert.AreEqual(1f, noOffset.x, 0.0001f);
            Assert.AreEqual(1f, rotated.y, 0.0001f);
        }

        [Test]
        public void RadialDirection_ZeroCount_ReturnsRight()
        {
            Assert.AreEqual(Vector2.right, EnemyRules.RadialDirection(0, 0, 0f));
        }

        private static readonly float[] ThreePhases = { 1f, 0.66f, 0.33f };

        [TestCase(1f, 0)]
        [TestCase(0.7f, 0)]
        [TestCase(0.66f, 1)]  // đúng ngưỡng là vào pha
        [TestCase(0.5f, 1)]
        [TestCase(0.2f, 2)]
        [TestCase(0f, 2)]
        public void PhaseForHealth_PicksDeepestPhaseReached(float hp, int expected)
        {
            Assert.AreEqual(expected, EnemyRules.PhaseForHealth(ThreePhases, hp, 0));
        }

        [Test]
        public void PhaseForHealth_NeverGoesBack()
        {
            // Boss hồi máu (sau này có thể có) cũng không quay lại pha dễ hơn.
            Assert.AreEqual(2, EnemyRules.PhaseForHealth(ThreePhases, 0.9f, 2));
        }

        [Test]
        public void PhaseForHealth_NoThresholds_StaysAtCurrent()
        {
            Assert.AreEqual(0, EnemyRules.PhaseForHealth(new float[0], 0.1f, 0));
            Assert.AreEqual(0, EnemyRules.PhaseForHealth(null, 0.1f, 0));
        }
    }
}
