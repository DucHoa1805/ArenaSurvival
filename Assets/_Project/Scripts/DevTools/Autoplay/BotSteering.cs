using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvival.DevTools
{
    public enum ThreatKind { Enemy, Bullet, Blast, Beam }

    /// <summary>
    /// 1 mối nguy bot nhìn thấy. vector: vận tốc (Bullet) hoặc hướng tia (Beam, đã chuẩn hóa); radius: bán kính nổ (Blast)
    /// hoặc chiều dài tia (Beam); weight: hệ số né (boss, quái đang gồng nặng hơn).
    /// </summary>
    public struct Threat
    {
        public ThreatKind kind;
        public Vector2 position;
        public Vector2 vector;
        public float radius;
        public float weight;
    }

    public struct SteeringSettings
    {
        public float enemyRange;
        public float bulletRange;
        public float blastMargin;
        public float beamWidth;
        public float wallMargin;
        public float attractWeight;
        public float safeThreshold;

        public static SteeringSettings Default => new SteeringSettings
        {
            enemyRange = 5f, bulletRange = 3f, blastMargin = 0.5f, beamWidth = 0.8f,
            wallMargin = 4f, attractWeight = 0.6f, safeThreshold = 0.3f,
        };
    }

    /// <summary>
    /// Bot né bằng "chấm điểm hướng": thử 16 hướng, mỗi hướng đo mức nguy hiểm ở 2 điểm phía trước (quái, đường đạn, vòng pháo,
    /// tia ngắm, mép sân) rồi chọn hướng ít nguy hiểm nhất. Ban đầu dùng cộng lực, nhưng khi bị cả đám dồn vào góc thì tổng lực
    /// của đám đông lấn át lực đẩy khỏi tường và bot tự chạy vào góc chết (đã thấy khi chơi thử); chấm điểm thì tìm được khe hở.
    /// Hàm thuần (đầu vào là danh sách đã thu thập) để test từng loại mối nguy riêng.
    /// </summary>
    public static class BotSteering
    {
        private const int Directions = 16;
        private const float NearLook = 0.75f;
        private const float FarLook = 1.5f;
        private const float Epsilon = 0.0001f;

        public static Vector2 Compute(Vector2 self, IReadOnlyList<Threat> threats, int count, Rect arena, Vector2? attractor,
            SteeringSettings s, Vector2 previous = default)
        {
            bool attract = attractor.HasValue && Danger(self, threats, count, arena, s) < s.safeThreshold;
            Vector2 toAttractor = attract ? SafeNormal(attractor.Value - self) : Vector2.zero;

            Vector2 best = Vector2.zero;
            float bestCost = float.MaxValue, worstCost = 0f;
            for (int i = 0; i < Directions; i++)
            {
                float angle = i * (360f / Directions) * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float cost = Danger(self + d * NearLook, threats, count, arena, s) + Danger(self + d * FarLook, threats, count, arena, s);
                worstCost = Mathf.Max(worstCost, cost);
                // Hút về orb chỉ là phần thưởng nhỏ; giữ hướng cũ một chút để không giật qua lại giữa 2 hướng gần bằng điểm.
                cost -= Vector2.Dot(d, toAttractor) * s.attractWeight + Vector2.Dot(d, previous) * 0.02f;
                if (cost < bestCost) { bestCost = cost; best = d; }
            }

            // Không có gì nguy hiểm ở bất kỳ hướng nào và không có gì để nhặt: đứng yên (súng vẫn tự bắn).
            if (worstCost < Epsilon && !attract) return Vector2.zero;
            return best;
        }

        /// <summary>Mức nguy hiểm nếu đứng ở điểm p.</summary>
        private static float Danger(Vector2 p, IReadOnlyList<Threat> threats, int count, Rect arena, SteeringSettings s)
        {
            float danger = 0f;
            for (int i = 0; i < count; i++)
            {
                Threat t = threats[i];
                float w = t.weight > 0f ? t.weight : 1f;
                switch (t.kind)
                {
                    case ThreatKind.Enemy:
                    {
                        float dist = (p - t.position).magnitude;
                        if (dist >= s.enemyRange) break;
                        // Bình phương nghịch: quái sát người nặng hơn hẳn quái ở xa.
                        float d = Mathf.Max(dist, 0.3f);
                        danger += w / (d * d);
                        break;
                    }
                    case ThreatKind.Bullet:
                    {
                        Vector2 rel = p - t.position;
                        if (rel.magnitude >= s.bulletRange + FarLook) break;
                        Vector2 dir = SafeNormal(t.vector);
                        float along = Vector2.Dot(rel, dir);
                        if (along <= 0f) break; // điểm này nằm sau viên đạn: đạn đã bay qua
                        float off = (rel - dir * along).magnitude;
                        // Đứng trên đường đạn mới nguy hiểm; né ngang 0.6 là đủ (đạn nhỏ).
                        if (off < 0.6f) danger += 1.5f * w * (0.6f - off) / 0.6f / Mathf.Max(along, 0.3f);
                        break;
                    }
                    case ThreatKind.Blast:
                    {
                        float dist = (p - t.position).magnitude, edge = t.radius + s.blastMargin;
                        if (dist < edge) danger += 4f * w * (edge - dist);
                        break;
                    }
                    case ThreatKind.Beam:
                    {
                        Vector2 rel = p - t.position;
                        Vector2 dir = SafeNormal(t.vector);
                        float along = Vector2.Dot(rel, dir);
                        if (along <= 0f || along >= t.radius) break;
                        float off = (rel - dir * along).magnitude;
                        if (off < s.beamWidth) danger += 3f * w * (s.beamWidth - off) / s.beamWidth;
                        break;
                    }
                }
            }

            // Mép sân: phạt phần lấn vào dải sát tường — góc bị phạt cả 2 chiều nên bot không tự chui vào góc.
            danger += Wall(p.x - arena.xMin, s.wallMargin) + Wall(arena.xMax - p.x, s.wallMargin)
                    + Wall(p.y - arena.yMin, s.wallMargin) + Wall(arena.yMax - p.y, s.wallMargin);
            return danger;
        }

        // Bình phương và nặng: bản tuyến tính nhẹ (2.5 đơn vị, hệ số 2) vẫn để bot dạt ra mép rồi bị đám quái (collider cứng) chặn kín
        // trong góc — đo trong Editor: 23 quái → mất ~130 máu/giây khi kẹt góc. Ưu tiên chạy vòng quanh khoảng trống giữa sân.
        private static float Wall(float distance, float margin)
        {
            if (distance >= margin) return 0f;
            float t = (margin - distance) / margin;
            return 6f * t * t;
        }

        private static Vector2 SafeNormal(Vector2 v) => v.sqrMagnitude < 0.0001f ? Vector2.zero : v.normalized;
    }
}
