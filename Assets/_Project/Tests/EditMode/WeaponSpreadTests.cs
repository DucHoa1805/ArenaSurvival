using ArenaSurvival.Combat;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class WeaponSpreadTests
    {
        [Test]
        public void SingleProjectile_HasNoOffset()
        {
            Assert.AreEqual(0f, PlayerWeapon.GetSpreadOffset(0, 1, 50f));
        }

        [Test]
        public void FiveProjectiles_SpreadEvenlyAcrossTheAngle()
        {
            float[] expected = { -25f, -12.5f, 0f, 12.5f, 25f };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], PlayerWeapon.GetSpreadOffset(i, 5, 50f), 0.0001f, $"index {i}");
            }
        }

        [Test]
        public void EvenCount_IsSymmetricAndHasNoCenterShot()
        {
            Assert.AreEqual(-30f, PlayerWeapon.GetSpreadOffset(0, 2, 60f), 0.0001f);
            Assert.AreEqual(30f, PlayerWeapon.GetSpreadOffset(1, 2, 60f), 0.0001f);
        }

        [Test]
        public void ZeroSpread_AllProjectilesGoStraight()
        {
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(0f, PlayerWeapon.GetSpreadOffset(i, 4, 0f), 0.0001f);
            }
        }
    }
}
