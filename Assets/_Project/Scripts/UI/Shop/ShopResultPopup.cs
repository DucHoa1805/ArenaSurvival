using System;
using System.Collections.Generic;
using System.Text;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Shop
{
    /// <summary>Popup "Nhận được": hiện từng thứ vừa trao bằng ô vật phẩm có sẵn (ItemSlotView).</summary>
    public class ShopResultPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Transform _container;
        [SerializeField] private ItemSlotView _slotPrefab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Sprite _goldIcon;
        [SerializeField] private Sprite _gemIcon;
        private readonly List<ItemSlotView> _views = new List<ItemSlotView>();

        private void Awake() => _closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        public void Show(string title, IReadOnlyList<LootDrop> drops)
        {
            _title.text = title;
            ItemDatabase db = GameServices.Inventory != null ? GameServices.Inventory.Database : null;
            int used = 0;
            foreach (LootDrop d in drops)
            {
                if (used >= _views.Count) _views.Add(Instantiate(_slotPrefab, _container));
                ItemSlotView v = _views[used++];
                v.gameObject.SetActive(true);
                v.SetDimmed(false);
                if (d.kind == LootKind.Item) v.ShowItemPreview(db != null ? db.GetItem(d.id) : null, d.rarity, "");
                else if (d.id == Economy.CurrencyIds.Gold) v.ShowCurrency(_goldIcon, d.amount);
                else if (d.id == Economy.CurrencyIds.Gem) v.ShowCurrency(_gemIcon, d.amount);
                else v.ShowMaterial(db != null ? db.GetMaterial(d.id) : null, d.amount);
            }
            for (int i = used; i < _views.Count; i++) _views[i].gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
    }
}
