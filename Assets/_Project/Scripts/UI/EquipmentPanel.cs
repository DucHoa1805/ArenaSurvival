using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using ArenaSurvival.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Bảng Trang bị: 2 ô đang mặc, tài nguyên, lực chiến, lưới kho đồ, phân rã nhanh. Prefab độc lập để Phase 9 đặt vào Hub.
    /// Ô đồ được DÙNG LẠI giữa các lần làm mới (tạo thêm khi thiếu, ẩn khi thừa): kho tới 100 món, hủy/tạo lại mỗi lần
    /// nâng cấp là giật và sinh rác.
    /// </summary>
    public class EquipmentPanel : MonoBehaviour
    {
        [Header("Dữ liệu (để tính lực chiến)")]
        [SerializeField] private HeroBaseStats _heroBase;
        [SerializeField] private TalentCatalog _talents;

        [Header("Ô đồ")]
        [SerializeField] private ItemSlotView _slotPrefab;
        [SerializeField] private Transform _grid;
        [SerializeField] private ItemSlotView _armorSlot;
        [SerializeField] private ItemSlotView _accessorySlot;
        [SerializeField] private ItemDetailPopup _detail;
        [Tooltip("Hiện khi kho trống (không bắt buộc).")]
        [SerializeField] private GameObject _emptyHint;

        [Header("Chữ")]
        [SerializeField] private TMP_Text _powerText;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _leatherText;
        [SerializeField] private TMP_Text _oreText;
        [SerializeField] private TMP_Text _coreText;

        [Header("Nút")]
        [SerializeField] private Button _quickDismantleButton;
        [SerializeField] private Button _closeButton;

        private readonly List<ItemSlotView> _views = new List<ItemSlotView>();
        private readonly List<OwnedItem> _sorted = new List<OwnedItem>();
        private Action _onChanged;
        private Action<string, long> _onCurrencyChanged;
        private Action<ItemSlotView> _onSlotClicked;
        private Comparison<OwnedItem> _compare;

        private static InventoryService Inventory => GameServices.Inventory;

        private void Awake()
        {
            _onChanged = Refresh;
            _onCurrencyChanged = (_, __) => RefreshResources();
            _onSlotClicked = HandleSlotClicked;
            _compare = CompareItems;
            _armorSlot.OnClicked += _onSlotClicked;
            _accessorySlot.OnClicked += _onSlotClicked;
            _quickDismantleButton.onClick.AddListener(HandleQuickDismantle);
            _closeButton.onClick.AddListener(Close);
            if (_detail != null) _detail.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _quickDismantleButton.onClick.RemoveListener(HandleQuickDismantle);
            _closeButton.onClick.RemoveListener(Close);
        }

        private void OnEnable()
        {
            Inventory.OnInventoryChanged += _onChanged;
            Inventory.OnEquipmentChanged += _onChanged;
            GameServices.Economy.OnCurrencyChanged += _onCurrencyChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (Inventory != null)
            {
                Inventory.OnInventoryChanged -= _onChanged;
                Inventory.OnEquipmentChanged -= _onChanged;
            }
            if (GameServices.Economy != null) GameServices.Economy.OnCurrencyChanged -= _onCurrencyChanged;
            if (_detail != null) _detail.Close();
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            ShowEquipped(_armorSlot, EquipSlot.Armor);
            ShowEquipped(_accessorySlot, EquipSlot.Accessory);

            _sorted.Clear();
            foreach (OwnedItem item in Inventory.Items)
            {
                if (Inventory.GetData(item) != null) _sorted.Add(item);
            }
            _sorted.Sort(_compare);

            for (int i = 0; i < _sorted.Count; i++)
            {
                if (i >= _views.Count)
                {
                    ItemSlotView view = Instantiate(_slotPrefab, _grid);
                    view.OnClicked += _onSlotClicked;
                    _views.Add(view);
                }
                OwnedItem item = _sorted[i];
                _views[i].gameObject.SetActive(true);
                _views[i].ShowItem(item, Inventory.GetData(item), Inventory.IsEquipped(item.uid));
            }
            for (int i = _sorted.Count; i < _views.Count; i++) _views[i].gameObject.SetActive(false);

            if (_emptyHint != null) _emptyHint.SetActive(_sorted.Count == 0);
            _countText.SetText("Kho đồ {0}/{1}", Inventory.Items.Count, EquipmentRules.InventoryCapacity);
            _quickDismantleButton.interactable = HasQuickDismantleTargets();

            StatBlock stats = LoadoutStats.Compute(_heroBase, _talents);
            _powerText.SetText("Lực chiến: {0}", StatCalculator.Power(stats));
            RefreshResources();
        }

        private void RefreshResources()
        {
            SetAmount(_goldText, CurrencyIds.Gold);
            SetAmount(_leatherText, CurrencyIds.Leather);
            SetAmount(_oreText, CurrencyIds.Ore);
            SetAmount(_coreText, CurrencyIds.BossCore);
        }

        private static void SetAmount(TMP_Text text, string currencyId)
        {
            if (text != null) text.SetText("{0}", GameServices.Economy.GetBalance(currencyId));
        }

        private void ShowEquipped(ItemSlotView slot, EquipSlot equipSlot)
        {
            OwnedItem item = Inventory.GetEquipped(equipSlot);
            ItemData data = Inventory.GetData(item);
            if (data == null) slot.ShowEmpty();
            else slot.ShowItem(item, data, equipped: false); // ô "đang mặc" thì không cần dấu đang mặc
        }

        private void HandleSlotClicked(ItemSlotView view)
        {
            if (!string.IsNullOrEmpty(view.Uid) && _detail != null) _detail.Show(view.Uid);
        }

        private void HandleQuickDismantle() => Inventory.DismantleQuick();

        private bool HasQuickDismantleTargets()
        {
            foreach (OwnedItem item in Inventory.Items)
            {
                if (item.rarity == Rarity.Common && item.level <= 1 && item.ascension == 0 && !Inventory.IsEquipped(item.uid)) return true;
            }
            return false;
        }

        // Đang mặc lên đầu → độ hiếm cao → cấp cao → món nhặt trước (uid nhỏ) — thứ tự ổn định, không nhảy lung tung khi làm mới.
        private int CompareItems(OwnedItem a, OwnedItem b)
        {
            bool ea = Inventory.IsEquipped(a.uid), eb = Inventory.IsEquipped(b.uid);
            if (ea != eb) return ea ? -1 : 1;
            if (a.rarity != b.rarity) return b.rarity.CompareTo(a.rarity);
            if (a.level != b.level) return b.level.CompareTo(a.level);
            return string.CompareOrdinal(a.uid.PadLeft(12), b.uid.PadLeft(12));
        }
    }
}
