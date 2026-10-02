using System;
using System.Collections.Generic;

namespace ArenaSurvival.Loot
{
    /// <summary>1 dòng trong bảng "Tỷ lệ rơi": thứ gì, bao nhiêu, xác suất mỗi lượt quay (1 = chắc chắn).</summary>
    public struct LootOdds
    {
        public LootKind kind;
        public string id;
        public int min, max;
        /// <summary>Xác suất ra mục này trong 1 lượt quay của bảng (mục chắc chắn = 1).</summary>
        public float chance;
        /// <summary>Chỉ với trang bị: xác suất từng độ hiếm (đã chuẩn hóa), null với nguyên liệu.</summary>
        public float[] rarityChances;
    }

    public static class LootOddsRules
    {
        /// <summary>
        /// Liệt kê tỷ lệ rơi của bảng (kèm bảng alsoRoll). Tính thẳng từ cùng dữ liệu mà LootRules.Roll dùng nên con số hiển thị
        /// cho người chơi luôn đúng với thứ được quay — Google Play/App Store yêu cầu công bố tỷ lệ rương.
        /// </summary>
        public static void Odds(LootTable table, List<LootOdds> output) => Odds(table, output, 0);

        private static void Odds(LootTable table, List<LootOdds> output, int depth)
        {
            if (table == null || output == null || depth > LootRules.MaxAlsoRollDepth) return;

            float[] rarity = Normalize(table.rarityWeights);
            if (table.guaranteed != null)
                foreach (LootEntry e in table.guaranteed) Add(e, 1f, rarity, output);

            LootEntry[] weighted = table.weighted;
            if (weighted != null && weighted.Length > 0 && table.rolls > 0)
            {
                float total = Math.Max(0f, table.nothingWeight);
                foreach (LootEntry e in weighted) if (e != null) total += Math.Max(0f, e.weight);
                if (total > 0f)
                    foreach (LootEntry e in weighted)
                        if (e != null && e.weight > 0f) Add(e, e.weight / total, rarity, output);
            }

            if (table.alsoRoll != null)
                foreach (LootTable sub in table.alsoRoll) Odds(sub, output, depth + 1);
        }

        private static void Add(LootEntry e, float chance, float[] rarity, List<LootOdds> output)
        {
            if (e == null) return;
            if (e.item != null)
                output.Add(new LootOdds { kind = LootKind.Item, id = e.item.id, min = 1, max = 1, chance = chance, rarityChances = rarity });
            else if (e.material != null)
                output.Add(new LootOdds { kind = LootKind.Material, id = e.material.id, min = Math.Max(1, e.min), max = Math.Max(Math.Max(1, e.min), e.max), chance = chance });
        }

        public static float[] Normalize(float[] weights)
        {
            if (weights == null || weights.Length == 0) return new[] { 1f };
            float total = 0f;
            foreach (float w in weights) total += Math.Max(0f, w);
            var result = new float[weights.Length];
            if (total <= 0f) { result[0] = 1f; return result; }
            for (int i = 0; i < weights.Length; i++) result[i] = Math.Max(0f, weights[i]) / total;
            return result;
        }
    }
}
