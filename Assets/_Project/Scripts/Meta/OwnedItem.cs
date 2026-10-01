using ArenaSurvival.Items;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// 1 món trang bị người chơi đang có (lưu trong save). Mỗi lần rơi là 1 món riêng nên phải có uid: 2 cái "Áo Da"
    /// có thể khác cấp, khác độ hiếm. Chỉ lưu id của ItemData, không lưu tham chiếu asset.
    /// </summary>
    public class OwnedItem
    {
        public string uid;
        public string id;
        /// <summary>Lưu thành số trong JSON (giá trị enum cố định, xem Rarity).</summary>
        public Rarity rarity;
        public int level = 1;
        public int ascension;
    }
}
