using System.Collections;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>Tia hủy diệt (Laser): 1 tia lớn quét quanh người, gây sát thương theo nhịp cho mọi quái nằm trên tia.</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Ultimates/Beam Sweep", fileName = "Ultimate_BeamSweep")]
    public class BeamSweepUltimate : WeaponUltimate
    {
        [Min(0.1f)] public float sweepTime = 1.6f;
        public float sweepDegrees = 360f;
        [Min(1f)] public float length = 11f;
        [Min(0.05f)] public float width = 0.55f;
        [Min(0.02f)] public float tickInterval = 0.08f;
        [Min(0f)] public float damageMultiplier = 3f;
        public Gradient beamColor = new Gradient();

        public override float Duration => sweepTime;

        public override IEnumerator Run(UltimateContext ctx)
        {
            LineRenderer beam = ctx.Beam;
            if (beam == null) yield break;
            beam.colorGradient = beamColor;
            beam.widthMultiplier = width;
            beam.positionCount = 2;
            beam.enabled = true;

            float start = Random.Range(0f, 360f);
            float t = 0f, tick = 0f;
            while (t < sweepTime)
            {
                t += Time.deltaTime;
                tick += Time.deltaTime;
                float angle = (start + sweepDegrees * Mathf.Clamp01(t / sweepTime)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 origin = ctx.Player.position;
                beam.SetPosition(0, origin);
                beam.SetPosition(1, origin + dir * length);
                // Nhấp nháy độ dày cho tia có cảm giác năng lượng thay vì 1 que tĩnh.
                beam.widthMultiplier = width * (0.85f + 0.3f * Mathf.PerlinNoise(t * 20f, 0f));
                if (tick >= tickInterval)
                {
                    tick = 0f;
                    ctx.DamageAlongRay(origin, dir, length, ctx.BaseDamage * damageMultiplier);
                }
                yield return null;
            }
            beam.enabled = false;
        }
    }
}
