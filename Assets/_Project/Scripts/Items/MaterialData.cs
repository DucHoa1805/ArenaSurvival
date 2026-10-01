using UnityEngine;

namespace ArenaSurvival.Items
{
    /// <summary>
    /// Nguyên liệu (quặng, da, lõi boss). Số lượng nằm trong EconomyService như 1 loại tiền phụ, nên id ở đây
    /// PHẢI trùng hằng số trong CurrencyIds. Asset này chỉ giữ phần hiển thị.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Items/Material Data", fileName = "Mat_")]
    public class MaterialData : ScriptableObject
    {
        [Tooltip("Trùng CurrencyIds, KHÔNG đổi sau khi phát hành (save lưu số lượng theo chuỗi này).")]
        public string id;
        public string displayName;
        public Sprite icon;
    }
}
