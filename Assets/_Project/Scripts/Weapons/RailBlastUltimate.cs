using System.Collections;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Phát bắn xuyên màn hình (Railgun): vài phát tia dày dài quá màn hình, mỗi phát nhắm quái gần nhất (không có thì xoay đều),
    /// sát thương mọi quái trên dải tia. Dùng chung LineRenderer với Tia hủy diệt.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Ultimates/Rail Blast", fileName = "Ultimate_RailBlast")]
    public class RailBlastUltimate : WeaponUltimate
    {
        [Min(1)] public int shots = 3;
        [Min(0.05f)] public float interval = 0.35f;
        [Min(1f)] public float length = 26f;
        [Min(0.1f)] public float width = 1.2f;
        [Min(0.05f)] public float fadeTime = 0.3f;
        [Min(0f)] public float damageMultiplier = 6f;
        public float searchRadius = 12f;
        public Gradient beamColor = new Gradient();

        public override float Duration => shots * interval;

        public override IEnumerator Run(UltimateContext ctx)
        {
            LineRenderer beam = ctx.Beam;
            float baseAngle = Random.Range(0f, 360f);
            for (int s = 0; s < shots; s++)
            {
                Vector2 origin = ctx.Player.position;
                Vector2 dir = AimDirection(ctx, origin, baseAngle + s * 360f / shots);
                ctx.DamageAlongThickRay(origin, dir, length, width, ctx.BaseDamage * damageMultiplier);
                Core.GameEvents.Explosion(origin + dir * 0.8f, 0.8f);

                if (beam != null)
                {
                    beam.colorGradient = beamColor;
                    beam.positionCount = 2;
                    beam.SetPosition(0, origin);
                    beam.SetPosition(1, origin + dir * length);
                    beam.enabled = true;
                }
                // Tia lóe dày rồi teo lại: cảm giác 1 phát bắn năng lượng cực mạnh, không phải tia liên tục.
                float t = 0f;
                while (t < interval)
                {
                    t += Time.deltaTime;
                    if (beam != null)
                    {
                        float k = Mathf.Clamp01(t / fadeTime);
                        beam.widthMultiplier = width * (1f - k) * (1f - k) + 0.02f;
                        if (k >= 1f) beam.enabled = false;
                    }
                    yield return null;
                }
            }
            if (beam != null) beam.enabled = false;
        }

        private Vector2 AimDirection(UltimateContext ctx, Vector2 origin, float fallbackDeg)
        {
            var hits = ctx.EnemiesAround(origin, searchRadius);
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < hits.Count; i++)
            {
                float d = ((Vector2)hits[i].transform.position - origin).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = hits[i].transform; }
            }
            if (best != null) return ((Vector2)best.position - origin).normalized;
            float a = fallbackDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
    }
}
