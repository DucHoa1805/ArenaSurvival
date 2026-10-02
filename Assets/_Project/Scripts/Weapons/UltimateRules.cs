using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>Luật mana của chiêu vũ khí (10E C, hàm thuần, có unit test).</summary>
    public static class UltimateRules
    {
        public const float MaxMana = 100f;
        // ~25 s nếu không hạ quái; quái chương đầu chết ~1 con/s nên thực tế ~15–20 s một chiêu.
        public const float RegenPerSecond = 4f;
        public const float ManaPerKill = 1f;

        public static float Regen(float mana, float deltaTime) => Mathf.Min(MaxMana, mana + RegenPerSecond * Mathf.Max(0f, deltaTime));

        public static float AddKills(float mana, int kills) => Mathf.Min(MaxMana, mana + ManaPerKill * Mathf.Max(0, kills));

        public static bool IsFull(float mana) => mana >= MaxMana - 0.001f;

        /// <summary>Hướng thứ i trong vòng count tia đều 360°, lệch thêm offsetDeg (các vòng sau xoay lệch cho kín kẽ).</summary>
        public static Vector2 RingDirection(int index, int count, float offsetDeg)
        {
            float a = (offsetDeg + 360f * index / Mathf.Max(1, count)) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
    }
}
