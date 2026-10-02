using System.Collections;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>Mưa lựu (Phóng lựu): nhiều vụ nổ rải ngẫu nhiên quanh người trong ~1 giây; ưu tiên vòng ngoài để không nổ đè lên nhau ở giữa.</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Ultimates/Grenade Rain", fileName = "Ultimate_GrenadeRain")]
    public class GrenadeRainUltimate : WeaponUltimate
    {
        [Min(1)] public int count = 12;
        [Min(0f)] public float totalTime = 1.2f;
        [Min(0.5f)] public float areaRadius = 5f;
        [Min(0.1f)] public float blastRadius = 1.8f;
        [Min(0f)] public float damageMultiplier = 2f;

        public override IEnumerator Run(UltimateContext ctx)
        {
            float step = count > 1 ? totalTime / (count - 1) : 0f;
            var wait = new WaitForSeconds(step);
            for (int i = 0; i < count; i++)
            {
                // sqrt cho mật độ đều theo diện tích (không dồn về tâm); 0.3 tối thiểu để không nổ ngay chân.
                float r = areaRadius * Mathf.Sqrt(Random.Range(0.09f, 1f));
                Vector2 offset = Random.insideUnitCircle.normalized * r;
                ctx.Explode((Vector2)ctx.Player.position + offset, blastRadius, ctx.BaseDamage * damageMultiplier);
                if (i < count - 1) yield return wait;
            }
        }
    }
}
