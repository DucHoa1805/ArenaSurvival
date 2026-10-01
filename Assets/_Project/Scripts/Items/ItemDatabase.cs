using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvival.Items
{
    /// <summary>
    /// Tra cứu trang bị / nguyên liệu theo id (save chỉ lưu id). Đặt ở Resources/ItemDatabase vì GameServices
    /// tự khởi tạo trước mọi scene, không có chỗ nào gán tham chiếu qua Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Items/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public const string ResourcePath = "ItemDatabase";

        public ItemData[] items = new ItemData[0];
        public MaterialData[] materials = new MaterialData[0];

        private Dictionary<string, ItemData> _items;
        private Dictionary<string, MaterialData> _materials;

        public ItemData GetItem(string id)
        {
            if (_items == null) Build();
            return id != null && _items.TryGetValue(id, out ItemData item) ? item : null;
        }

        public MaterialData GetMaterial(string id)
        {
            if (_materials == null) Build();
            return id != null && _materials.TryGetValue(id, out MaterialData material) ? material : null;
        }

        // Sửa asset trong Editor (kể cả lúc đang chạy): dựng lại từ điển cho khớp.
        private void OnValidate()
        {
            _items = null;
            _materials = null;
        }

        private void Build()
        {
            _items = new Dictionary<string, ItemData>();
            _materials = new Dictionary<string, MaterialData>();
            if (items != null)
                foreach (ItemData item in items)
                    if (item != null && !string.IsNullOrEmpty(item.id)) _items[item.id] = item;
            if (materials != null)
                foreach (MaterialData material in materials)
                    if (material != null && !string.IsNullOrEmpty(material.id)) _materials[material.id] = material;
        }
    }
}
