using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Phần "quyết định" của hành vi quái/boss viết thành hàm thuần: test được mà không cần chạy scene,
    /// còn các asset hành vi chỉ việc gọi và áp kết quả lên Rigidbody.
    /// </summary>
    public static class EnemyRules
    {
        /// <summary>Quái bắn xa nên tiến (+1), lùi (-1) hay đứng bắn (0) để giữ khoảng cách preferred ± tolerance.</summary>
        public static int RangeIntent(float distance, float preferred, float tolerance)
        {
            if (distance > preferred + tolerance) return 1;
            if (distance < preferred - tolerance) return -1;
            return 0;
        }

        /// <summary>Hướng của viên thứ index trong vòng đạn count viên, xoay thêm offsetDeg (để các vòng sau lệch nhau, khó đứng một chỗ né).</summary>
        public static Vector2 RadialDirection(int index, int count, float offsetDeg)
        {
            if (count <= 0) return Vector2.right;
            float angle = (offsetDeg + 360f * index / count) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// Pha sâu nhất mà % máu hiện tại đã chạm tới (máu ≤ ngưỡng của pha). Chỉ tiến, không lùi: boss đã nổi giận
        /// thì không quay về pha dễ dù sau này có cơ chế hồi máu.
        /// </summary>
        /// <summary>Lượng máu Healer hồi cho 1 quái: phần trăm máu tối đa nhưng không vượt phần còn thiếu; quái đã chết thì 0.</summary>
        public static float HealAmount(float maxHealth, float currentHealth, float fraction)
        {
            if (currentHealth <= 0f) return 0f;
            return Mathf.Max(0f, Mathf.Min(maxHealth * fraction, maxHealth - currentHealth));
        }

        /// <summary>Player có trúng vụ nổ của Kamikaze không (tính cả đúng mép bán kính).</summary>
        public static bool KamikazeHit(float distance, float radius) => distance <= radius;

        public static int PhaseForHealth(float[] thresholds, float hpFraction, int current)
        {
            if (thresholds == null || thresholds.Length == 0) return current;

            int phase = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hpFraction <= thresholds[i]) phase = i;
            }
            return Mathf.Max(current, phase);
        }
    }
}
