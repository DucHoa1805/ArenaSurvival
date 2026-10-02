using ArenaSurvival.Stages;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class GroundRulesTests
    {
        [Test]
        public void Hash_IsDeterministicAndSeedDependent()
        {
            Assert.AreEqual(GroundRules.Hash(12, -7, 3), GroundRules.Hash(12, -7, 3), "tô lại chunk phải ra y hệt");
            Assert.AreNotEqual(GroundRules.Hash(12, -7, 3), GroundRules.Hash(12, -7, 4));
            Assert.AreNotEqual(GroundRules.Hash(1, 0, 3), GroundRules.Hash(0, 1, 3), "đổi chỗ x/y không trùng");
        }

        [Test]
        public void Hash01_InRangeAndRoughlyUniform()
        {
            int below = 0;
            for (int x = -50; x < 50; x++)
            for (int y = -50; y < 50; y++)
            {
                float v = GroundRules.Hash01(x, y, 7);
                Assert.That(v, Is.InRange(0f, 1f));
                if (v < 0.5f) below++;
            }
            Assert.That(below / 10000f, Is.InRange(0.45f, 0.55f));
        }

        [Test]
        public void Noise_IsSmoothBetweenNeighbours()
        {
            for (int x = -20; x < 20; x++)
            {
                float a = GroundRules.Noise(x, 5, 0.08f, 1), b = GroundRules.Noise(x + 1, 5, 0.08f, 1);
                Assert.Less(Mathf.Abs(a - b), 0.2f, "nhiễu thấp tần: ô cạnh nhau gần giá trị — mảng liền khối");
            }
        }

        [TestCase(0, 16, 0)]
        [TestCase(15, 16, 0)]
        [TestCase(16, 16, 1)]
        [TestCase(-1, 16, -1)]
        [TestCase(-16, 16, -1)]
        [TestCase(-17, 16, -2)]
        public void FloorDiv_HandlesNegatives(int a, int b, int expected) => Assert.AreEqual(expected, GroundRules.FloorDiv(a, b));

        [Test]
        public void Pick_StaysInArray()
        {
            for (int i = 0; i < 200; i++) Assert.That(GroundRules.Pick(i, -i, 2, 5), Is.InRange(0, 4));
            Assert.AreEqual(-1, GroundRules.Pick(1, 1, 1, 0));
        }

        [Test]
        public void InBlockPatch_RespectsMarginAndDensity()
        {
            for (int x = -16; x < 16; x++)
            for (int y = -16; y < 16; y++)
            {
                int lx = x - GroundRules.FloorDiv(x, 8) * 8, ly = y - GroundRules.FloorDiv(y, 8) * 8;
                bool edge = lx < 1 || ly < 1 || lx >= 7 || ly >= 7;
                if (edge) Assert.IsFalse(GroundRules.InBlockPatch(x, y, 8, 1, 1f, 3), "lề khối luôn để trống");
                else Assert.IsTrue(GroundRules.InBlockPatch(x, y, 8, 1, 1f, 3), "density 1: mọi khối được phủ");
                Assert.IsFalse(GroundRules.InBlockPatch(x, y, 8, 1, 0f, 3), "density 0: không phủ");
            }
        }

        [Test]
        public void InRange_SquareRadius()
        {
            Assert.IsTrue(GroundRules.InRange(new Vector2Int(2, -2), Vector2Int.zero, 2));
            Assert.IsFalse(GroundRules.InRange(new Vector2Int(3, 0), Vector2Int.zero, 2));
        }
    }
}
