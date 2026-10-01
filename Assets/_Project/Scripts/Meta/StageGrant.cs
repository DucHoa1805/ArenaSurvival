using System.Collections.Generic;
using ArenaSurvival.Loot;

namespace ArenaSurvival.Meta
{
    /// <summary>Thứ người chơi THỰC SỰ nhận được sau 1 màn (đã áp hệ số lần đầu / chơi lại) — màn kết quả hiển thị cái này.</summary>
    public class StageGrant
    {
        public string stageId;
        public int gold;
        public int accountXp;
        public bool firstClear;
        public float multiplier;
        public int starsBefore;
        public int starsAfter;

        /// <summary>id nguyên liệu → tổng số đã cộng (nhặt trên sân + thưởng thắng màn).</summary>
        public Dictionary<string, int> materials = new Dictionary<string, int>();
        /// <summary>uid các món trang bị mới vào kho.</summary>
        public List<string> newItemUids = new List<string>();
        /// <summary>Trang bị đã nhặt nhưng mất vì thua — màn kết quả hiện mờ để người chơi thấy cái giá của việc thua.</summary>
        public List<LootDrop> lostItems = new List<LootDrop>();
        /// <summary>Số món tự đổi thành nguyên liệu vì kho đầy.</summary>
        public int autoDismantled;
    }
}
