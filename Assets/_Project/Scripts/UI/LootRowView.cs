using System.Collections.Generic;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Hàng "Vật phẩm nhận được" ở màn kết quả. Đọc StageGrant (thứ THỰC SỰ đã vào hồ sơ) chứ không đọc StageResult,
    /// để hiện đúng cả trường hợp thua mất trang bị hay kho đầy tự phân rã.
    /// </summary>
    public class LootRowView : MonoBehaviour
    {
        [SerializeField] private ItemSlotView _slotPrefab;
        [SerializeField] private Transform _container;
        [Tooltip("Dòng ghi chú: mất đồ khi thua / kho đầy.")]
        [SerializeField] private TMP_Text _noteText;

        private readonly List<ItemSlotView> _views = new List<ItemSlotView>();
        private int _used;

        public void Show(StageGrant grant)
        {
            _used = 0;
            InventoryService inventory = GameServices.Inventory;
            ItemDatabase db = inventory != null ? inventory.Database : null;

            if (grant != null && db != null)
            {
                foreach (KeyValuePair<string, int> pair in grant.materials) Next().ShowMaterial(db.GetMaterial(pair.Key), pair.Value);

                foreach (string uid in grant.newItemUids)
                {
                    OwnedItem item = inventory.Get(uid);
                    if (item != null) Next().ShowItem(item, inventory.GetData(item), false);
                }

                foreach (LootDrop lost in grant.lostItems)
                {
                    ItemSlotView view = Next();
                    view.ShowItemPreview(db.GetItem(lost.id), lost.rarity, "Mất");
                    view.SetDimmed(true);
                }
            }

            for (int i = _used; i < _views.Count; i++) _views[i].gameObject.SetActive(false);

            string note = "";
            if (grant != null && grant.lostItems.Count > 0) note = "Thua trận: trang bị đã nhặt bị mất (nguyên liệu vẫn giữ).";
            if (grant != null && grant.autoDismantled > 0)
                note += (note.Length > 0 ? "\n" : "") + $"Kho đầy: {grant.autoDismantled} món tự phân rã thành nguyên liệu.";
            if (_noteText != null)
            {
                _noteText.text = note;
                _noteText.gameObject.SetActive(note.Length > 0);
            }

            gameObject.SetActive(_used > 0 || note.Length > 0);
        }

        private ItemSlotView Next()
        {
            if (_used >= _views.Count) _views.Add(Instantiate(_slotPrefab, _container));
            ItemSlotView view = _views[_used++];
            view.gameObject.SetActive(true);
            return view;
        }
    }
}
