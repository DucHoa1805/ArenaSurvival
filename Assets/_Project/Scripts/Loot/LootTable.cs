using System;
using ArenaSurvival.Items;
using UnityEngine;

namespace ArenaSurvival.Loot
{
    /// <summary>1 mục trong bảng rơi đồ: ĐÚNG MỘT trong material / item.</summary>
    [Serializable]
    public class LootEntry
    {
        public MaterialData material;
        public ItemData item;
        [Min(1)] public int min = 1;
        [Min(1)] public int max = 1;
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>
    /// Bảng rơi đồ dùng chung cho quái, tinh anh, boss và thưởng thắng màn. Tách phần "luôn rơi" khỏi phần "quay"
    /// để boss chắc chắn cho lõi mà vẫn có món ngẫu nhiên, không phải chỉnh trọng số cho khéo.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Loot/Loot Table", fileName = "Loot_")]
    public class LootTable : ScriptableObject
    {
        [Tooltip("Luôn rơi, mỗi mục 1 lần.")]
        public LootEntry[] guaranteed = new LootEntry[0];

        [Tooltip("Số lần quay phần weighted.")]
        [Min(0)] public int rolls = 1;
        public LootEntry[] weighted = new LootEntry[0];
        [Tooltip("Trọng số của kết quả 'không rơi gì' khi quay.")]
        [Min(0f)] public float nothingWeight;

        [Tooltip("Trọng số độ hiếm khi kết quả là trang bị: Thường, Hiếm, Sử thi, Huyền thoại.")]
        public float[] rarityWeights = { 1f, 0f, 0f, 0f };

        private void OnValidate()
        {
            Validate(guaranteed);
            Validate(weighted);
            if (rarityWeights == null || rarityWeights.Length != 4) Array.Resize(ref rarityWeights, 4);
        }

        private void Validate(LootEntry[] entries)
        {
            if (entries == null) return;
            foreach (LootEntry e in entries)
            {
                if (e == null) continue;
                if (e.max < e.min) e.max = e.min;
                if ((e.material != null) == (e.item != null))
                    Debug.LogWarning($"{name}: mỗi mục rơi đồ phải gán đúng 1 trong material / item.", this);
            }
        }
    }
}
