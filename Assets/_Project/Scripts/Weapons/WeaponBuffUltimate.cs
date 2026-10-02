using System.Collections;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Cường hóa súng trong vài giây (Cuồng nộ của Súng máy: bắn ×3; Đạn chùm của Shotgun: đạn nổ khi trúng), có thể kèm 1 vòng đạn
    /// mở màn để người chơi thấy chiêu tác dụng ngay.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Ultimates/Weapon Buff", fileName = "Ultimate_Buff")]
    public class WeaponBuffUltimate : WeaponUltimate
    {
        [Min(0.1f)] public float duration = 4f;
        [Min(1f)] public float fireRateMultiplier = 1f;
        [Min(0f)] public float explosionRadius;
        [Range(0f, 1f)] public float explosionFraction = 0.6f;
        [Min(0)] public int extraPierce;
        [Header("Vòng đạn mở màn (0 = không)")]
        [Min(0)] public int openingRing;
        [Min(0f)] public float openingDamageMultiplier = 1f;
        public float openingRange = 6f;

        public override float Duration => duration;

        public override IEnumerator Run(UltimateContext ctx)
        {
            if (openingRing > 0) ctx.Weapon.FireRing(openingRing, 0f, ctx.BaseDamage * openingDamageMultiplier, extraPierce, openingRange);
            ctx.Weapon.SetBuff(fireRateMultiplier, explosionRadius, explosionFraction, extraPierce);
            yield return new WaitForSeconds(duration);
            ctx.Weapon.ClearBuff();
        }
    }
}
