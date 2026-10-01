using System;
using System.Collections.Generic;
using ArenaSurvival.Items;

namespace ArenaSurvival.Loot
{
    /// <summary>
    /// Quay bảng rơi đồ (hàm thuần). Nguồn ngẫu nhiên truyền vào: trong màn dùng UnityEngine.Random, test dùng seed cố định
    /// để kiểm tra phân phối. Ghi vào list có sẵn để gọi trong màn (mỗi lần quái chết) không cấp phát.
    /// </summary>
    public static class LootRules
    {
        // Bộ đệm trọng số dùng lại (chỉ chạy trên main thread).
        private static float[] _weights = new float[16];

        /// <summary>Chỉ số được chọn theo trọng số cộng dồn, r01 trong [0, 1). Tổng trọng số ≤ 0 → -1.</summary>
        public static int PickWeighted(float[] weights, int count, float r01)
        {
            float total = 0f;
            for (int i = 0; i < count; i++) total += Math.Max(0f, weights[i]);
            if (total <= 0f) return -1;

            float target = r01 * total;
            float acc = 0f;
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                float w = Math.Max(0f, weights[i]);
                if (w <= 0f) continue;
                acc += w;
                last = i;
                if (target < acc) return i;
            }
            // r01 sát 1 + sai số float: rơi về mục cuối có trọng số.
            return last;
        }

        public static void Roll(LootTable table, Func<float> random01, List<LootDrop> output)
        {
            if (table == null || random01 == null || output == null) return;

            if (table.guaranteed != null)
                foreach (LootEntry entry in table.guaranteed) AddEntry(entry, table, random01, output);

            LootEntry[] weighted = table.weighted;
            int count = weighted != null ? weighted.Length : 0;
            if (count == 0 || table.rolls <= 0) return;

            // Mục cuối của bộ đệm là "không rơi gì".
            if (_weights.Length < count + 1) _weights = new float[count + 1];
            for (int i = 0; i < count; i++) _weights[i] = weighted[i] != null ? weighted[i].weight : 0f;
            _weights[count] = table.nothingWeight;

            for (int r = 0; r < table.rolls; r++)
            {
                int pick = PickWeighted(_weights, count + 1, random01());
                if (pick >= 0 && pick < count) AddEntry(weighted[pick], table, random01, output);
            }
        }

        private static void AddEntry(LootEntry entry, LootTable table, Func<float> random01, List<LootDrop> output)
        {
            if (entry == null) return;

            if (entry.item != null)
            {
                output.Add(LootDrop.Item(entry.item.id, RollRarity(table.rarityWeights, random01())));
                return;
            }

            if (entry.material != null)
            {
                int min = Math.Max(1, entry.min);
                int max = Math.Max(min, entry.max);
                // Số nguyên đều trong [min, max]; kẹp vì r01 có thể đúng bằng 1 ở vài nguồn ngẫu nhiên.
                int amount = Math.Min(max, min + (int)(random01() * (max - min + 1)));
                output.Add(LootDrop.Material(entry.material.id, amount));
            }
        }

        private static Rarity RollRarity(float[] rarityWeights, float r01)
        {
            if (rarityWeights == null || rarityWeights.Length == 0) return Rarity.Common;
            int pick = PickWeighted(rarityWeights, rarityWeights.Length, r01);
            return pick < 0 ? Rarity.Common : (Rarity)pick;
        }
    }
}
