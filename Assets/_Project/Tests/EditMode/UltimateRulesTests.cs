using ArenaSurvival.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class UltimateRulesTests
    {
        [Test]
        public void Regen_FillsInTwentyFiveSecondsAndCaps()
        {
            Assert.AreEqual(40f, UltimateRules.Regen(0f, 10f), 1e-4f);
            Assert.AreEqual(UltimateRules.MaxMana, UltimateRules.Regen(90f, 10f), 1e-4f);
            Assert.AreEqual(5f, UltimateRules.Regen(5f, -1f), 1e-4f, "deltaTime âm không trừ mana");
        }

        [Test]
        public void Kills_AddManaAndCap()
        {
            Assert.AreEqual(15f, UltimateRules.AddKills(10f, 5), 1e-4f);
            Assert.AreEqual(UltimateRules.MaxMana, UltimateRules.AddKills(99f, 50), 1e-4f);
            Assert.IsTrue(UltimateRules.IsFull(UltimateRules.MaxMana));
            Assert.IsFalse(UltimateRules.IsFull(99f));
        }

        [Test]
        public void RingDirection_EvenlySpacedUnitVectors()
        {
            Vector2 a = UltimateRules.RingDirection(0, 4, 0f);
            Vector2 b = UltimateRules.RingDirection(1, 4, 0f);
            Assert.AreEqual(1f, a.magnitude, 1e-4f);
            Assert.AreEqual(90f, Vector2.Angle(a, b), 1e-3f);
            Assert.AreEqual(45f, Vector2.Angle(a, UltimateRules.RingDirection(0, 4, 45f)), 1e-3f);
        }
    }
}
