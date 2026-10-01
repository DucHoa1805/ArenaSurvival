using System;

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
        public int gold;
        public int accountXp;
    }
}
