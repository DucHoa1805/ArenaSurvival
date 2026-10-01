using ArenaSurvival.Stats;
using UnityEngine;

namespace ArenaSurvival.Items
{
    /// <summary>
    /// 1 loại trang bị. Độ hiếm KHÔNG nằm ở đây mà quay theo từng món khi rơi (spec 8B), nên cùng 1 asset
    /// ra được cả bản Thường lẫn Huyền thoại — ít asset, chỉ số tự nhân theo EquipmentRules.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Items/Item Data", fileName = "Item_")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("ID cố định, save lưu theo chuỗi này. KHÔNG đổi sau khi phát hành.")]
        public string id;
        public string displayName;
        public Sprite icon;
        public EquipSlot slot;

        [Tooltip("Chỉ số ở cấp 1, độ hiếm Thường.")]
        public StatBlock baseStats;
        [Tooltip("Cộng thêm mỗi cấp (trước khi nhân hệ số độ hiếm).")]
        public StatBlock statsPerLevel;

        [Tooltip("Nguyên liệu tốn khi nâng cấp và nhận lại khi phân rã.")]
        public MaterialData upgradeMaterial;
    }
}
