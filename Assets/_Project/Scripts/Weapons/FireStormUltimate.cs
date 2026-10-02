using System.Collections;
using System.Collections.Generic;
using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Bão lửa (Phun lửa): vòng lửa xoáy quanh người vài giây — quái trong vòng bị đốt cháy và trúng sát thương theo nhịp.
    /// Hình vòng lửa dựng bằng các vụ nổ nhỏ xoay quanh (dùng lại pool hiệu ứng nổ, không thêm hệ hạt riêng).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Ultimates/Fire Storm", fileName = "Ultimate_FireStorm")]
    public class FireStormUltimate : WeaponUltimate
    {
        [Min(0.5f)] public float duration = 4f;
        [Min(0.5f)] public float radius = 3.5f;
        [Min(0.05f)] public float tickInterval = 0.3f;
        [Min(0f)] public float damageMultiplier = 1.2f;
        [Min(0f)] public float burnDpsMultiplier = 2f;
        [Min(0f)] public float burnDuration = 3f;
        [Min(1)] public int flamesPerTick = 5;
        public float spinDegreesPerSecond = 220f;

        public override float Duration => duration;

        public override IEnumerator Run(UltimateContext ctx)
        {
            var wait = new WaitForSeconds(tickInterval);
            float t = 0f;
            while (t < duration)
            {
                Vector2 center = ctx.Player.position;
                float baseDamage = ctx.BaseDamage;
                List<Collider2D> hits = ctx.EnemiesAround(center, radius);
                for (int i = 0; i < hits.Count; i++)
                {
                    if (hits[i].TryGetComponent(out Health health)) health.TakeDamage(baseDamage * damageMultiplier);
                    if (hits[i].TryGetComponent(out IBurnable burn)) burn.ApplyBurn(baseDamage * burnDpsMultiplier, burnDuration);
                }
                float spin = t * spinDegreesPerSecond;
                for (int f = 0; f < flamesPerTick; f++)
                {
                    float a = (spin + 360f * f / flamesPerTick) * Mathf.Deg2Rad;
                    Core.GameEvents.Explosion(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * 0.8f, 1.4f);
                }
                yield return wait;
                t += tickInterval;
            }
        }
    }
}
