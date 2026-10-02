using System;
using System.Collections.Generic;
using ArenaSurvival.Loot;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Kết quả một màn — thứ DUY NHẤT scene Game trả ra ngoài. Chỉ chứa dữ liệu thuần (id là chuỗi, không tham chiếu asset),
    /// để Phase 7 nhận và lưu/cộng thưởng mà scene Game không bao giờ ghi thẳng vào save.
    /// </summary>
    [Serializable]
    public class StageResult
    {
        public string stageId;
        public bool victory;
        public bool[] starFlags = new bool[3];
        public int starCount;
        public float clearTime;
        public int killCount;
        public float hpFraction;
        public bool usedRevive;
        /// <summary>Đợt đang chơi khi màn kết thúc (analytics: người chơi thua ở đâu).</summary>
        public int waveReached;
        public int gold;
        public int accountXp;

        /// <summary>Đồ đã nhặt trên sân (8B). Tầng meta quyết định giữ gì: nguyên liệu luôn giữ, trang bị chỉ giữ khi thắng.</summary>
        public List<LootDrop> collected = new List<LootDrop>();
        /// <summary>Thưởng quay từ bảng thắng màn (chỉ có khi thắng).</summary>
        public List<LootDrop> bonusLoot = new List<LootDrop>();
    }
}
