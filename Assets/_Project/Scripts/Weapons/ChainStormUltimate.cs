using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Lưới điện (Tesla): vài đợt sét liên tiếp, mỗi đợt đánh từ người tới từng quái trên màn hình (gần trước) rồi nảy tiếp.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Ultimates/Chain Storm", fileName = "Ultimate_ChainStorm")]
    public class ChainStormUltimate : WeaponUltimate
    {
        [Min(1)] public int waves = 4;
        [Min(0f)] public float interval = 0.3f;
        [Min(1f)] public float radius = 9f;
        [Min(1)] public int targetsPerWave = 12;
        [Min(1)] public int jumps = 3;
        [Min(0.5f)] public float jumpRange = 3f;
        [Min(0f)] public float damageMultiplier = 1.6f;
        public Color boltColor = new Color(0.55f, 0.85f, 1f, 1f);

        public override float Duration => waves * interval;

        public override IEnumerator Run(UltimateContext ctx)
        {
            if (ctx.Chain == null) yield break;
            var wait = new WaitForSeconds(interval);
            for (int w = 0; w < waves; w++)
            {
                Vector2 center = ctx.Player.position;
                float damage = ctx.BaseDamage * damageMultiplier;
                List<Collider2D> hits = ctx.EnemiesAround(center, radius);
                int n = Mathf.Min(hits.Count, targetsPerWave);
                // Chép ra trước vì Strike dùng chung bộ đệm tìm quái của ChainBolts, không phải của ctx — vẫn an toàn,
                // nhưng quái có thể chết giữa chừng nên kiểm tra còn bật.
                for (int i = 0; i < n; i++)
                {
                    Transform t = hits[i].transform;
                    if (!t.gameObject.activeInHierarchy) continue;
                    ctx.Chain.Strike(center, t, jumps, jumpRange, damage, boltColor, 1.6f);
                }
                if (w < waves - 1) yield return wait;
            }
        }
    }
}
