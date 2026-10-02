using System.Collections;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>Bắn loạn (Súng lục): vài vòng đạn 360° quanh người, mỗi vòng xoay lệch nửa khoảng cách để lấp chỗ hở.</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Ultimates/Bullet Ring", fileName = "Ultimate_BulletRing")]
    public class BulletRingUltimate : WeaponUltimate
    {
        [Min(1)] public int rings = 3;
        [Min(4)] public int bulletsPerRing = 24;
        [Min(0f)] public float interval = 0.25f;
        [Min(0f)] public float damageMultiplier = 1.5f;
        [Min(0)] public int extraPierce = 2;
        public float range = 9f;

        public override IEnumerator Run(UltimateContext ctx)
        {
            float half = 180f / bulletsPerRing;
            for (int r = 0; r < rings; r++)
            {
                ctx.Weapon.FireRing(bulletsPerRing, r * half, ctx.BaseDamage * damageMultiplier, extraPierce, range);
                if (r < rings - 1) yield return new WaitForSeconds(interval);
            }
        }
    }
}
