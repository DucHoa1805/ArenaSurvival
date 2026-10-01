using ArenaSurvival.Stats;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// 1 Talent (chỉ số gốc nâng vĩnh viễn bằng vàng) — vòi xả vàng chính của nền kinh tế (plan A4).
    /// Là dữ liệu: thêm Talent mới = tạo asset, không sửa code.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Talent", fileName = "Talent_")]
    public class TalentData : ScriptableObject
    {
        [Tooltip("ID cố định, ví dụ \"talent_hp\". KHÔNG đổi sau khi phát hành (save lưu cấp theo id này).")]
        public string id;
        public string displayName;
        public Sprite icon;
        public StatKind stat;
        [Tooltip("Chỉ số cộng thêm mỗi cấp.")]
        public float valuePerLevel = 1f;
        [Min(1)] public int maxLevel = 50;
        [Min(1)] public int baseCost = 100;
        [Tooltip("Giá cấp kế = baseCost × costGrowth^(cấp hiện tại). Plan D1: 1.12.")]
        [Min(1f)] public float costGrowth = 1.12f;
    }
}
