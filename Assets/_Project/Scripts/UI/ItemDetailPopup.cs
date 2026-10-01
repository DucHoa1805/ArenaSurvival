using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Chi tiết 1 món: chỉ số hiện tại → cấp sau, giá; Mặc/Tháo, Nâng cấp (tới mốc thì Đột phá), Phân rã (có hỏi lại).
    /// Mọi thao tác đi qua InventoryService; popup chỉ hiển thị và tự làm mới theo sự kiện của dịch vụ.
    /// </summary>
    public class ItemDetailPopup : MonoBehaviour
    {
        [Header("Thông tin")]
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _rarityText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _costText;

        [Header("Nút")]
        [SerializeField] private Button _equipButton;
        [SerializeField] private TMP_Text _equipLabel;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TMP_Text _upgradeLabel;
        [SerializeField] private Button _dismantleButton;
        [SerializeField] private Button _closeButton;

        [Header("Xác nhận phân rã")]
        [SerializeField] private GameObject _confirm;
        [SerializeField] private TMP_Text _confirmText;
        [SerializeField] private Button _confirmYes;
        [SerializeField] private Button _confirmNo;

        private enum UpgradeMode { Upgrade, Ascend, Maxed }

        private readonly List<CurrencyCost> _cost = new List<CurrencyCost>(3);
        private string _uid;
        private UpgradeMode _mode;
        private Action _onInventoryChanged;
        private Action<string, long> _onCurrencyChanged;

        private static InventoryService Inventory => GameServices.Inventory;

        private void Awake()
        {
            _onInventoryChanged = Refresh;
            _onCurrencyChanged = (_, __) => Refresh();
            _equipButton.onClick.AddListener(HandleEquip);
            _upgradeButton.onClick.AddListener(HandleUpgrade);
            _dismantleButton.onClick.AddListener(AskDismantle);
            _closeButton.onClick.AddListener(Close);
            _confirmYes.onClick.AddListener(HandleDismantle);
            _confirmNo.onClick.AddListener(HideConfirm);
        }

        private void OnDestroy()
        {
            _equipButton.onClick.RemoveListener(HandleEquip);
            _upgradeButton.onClick.RemoveListener(HandleUpgrade);
            _dismantleButton.onClick.RemoveListener(AskDismantle);
            _closeButton.onClick.RemoveListener(Close);
            _confirmYes.onClick.RemoveListener(HandleDismantle);
            _confirmNo.onClick.RemoveListener(HideConfirm);
        }

        private void OnEnable()
        {
            // Dịch vụ sống lâu hơn popup: đăng ký khi hiện, gỡ khi ẩn.
            Inventory.OnInventoryChanged += _onInventoryChanged;
            Inventory.OnEquipmentChanged += _onInventoryChanged;
            GameServices.Economy.OnCurrencyChanged += _onCurrencyChanged;
        }

        private void OnDisable()
        {
            if (Inventory != null)
            {
                Inventory.OnInventoryChanged -= _onInventoryChanged;
                Inventory.OnEquipmentChanged -= _onInventoryChanged;
            }
            if (GameServices.Economy != null) GameServices.Economy.OnCurrencyChanged -= _onCurrencyChanged;
        }

        public void Show(string uid)
        {
            _uid = uid;
            gameObject.SetActive(true);
            HideConfirm();
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (!isActiveAndEnabled) return;

            OwnedItem item = Inventory.Get(_uid);
            ItemData data = Inventory.GetData(item);
            if (item == null || data == null)
            {
                // Món vừa bị phân rã (hoặc mất dữ liệu): không còn gì để hiện.
                Close();
                return;
            }

            Color rarityColor = RarityColors.Get(item.rarity);
            _frame.color = rarityColor;
            _icon.sprite = data.icon;
            _nameText.text = data.displayName;
            _rarityText.text = RarityColors.Name(item.rarity);
            _rarityText.color = rarityColor;

            int cap = EquipmentRules.LevelCap(item.rarity, item.ascension);
            int rarityCap = EquipmentRules.RarityLevelCap(item.rarity);
            _levelText.text = cap < rarityCap
                ? $"Cấp {item.level}/{cap}  (tối đa {rarityCap} sau đột phá)"
                : $"Cấp {item.level}/{cap}";

            var now = Inventory.ItemStats(item);
            bool canUpgrade = Inventory.GetUpgradeCost(item, _cost);
            if (canUpgrade)
            {
                _mode = UpgradeMode.Upgrade;
                var next = EquipmentRules.Stats(data.baseStats, data.statsPerLevel, item.rarity, item.level + 1);
                _statsText.text = ItemTextFormatter.Stats(now, next);
            }
            else
            {
                _statsText.text = ItemTextFormatter.Stats(now, null);
                _mode = Inventory.GetAscendCost(item, _cost) ? UpgradeMode.Ascend : UpgradeMode.Maxed;
            }

            _upgradeLabel.text = _mode == UpgradeMode.Upgrade ? "Nâng cấp" : _mode == UpgradeMode.Ascend ? "Đột phá" : "TỐI ĐA";
            _costText.text = _mode == UpgradeMode.Maxed ? "" : ItemTextFormatter.Cost(_cost, Inventory.Database);
            // Mờ nút khi không đủ tài nguyên: người chơi thấy ngay còn thiếu, thay vì bấm rồi không có gì xảy ra.
            _upgradeButton.interactable = _mode != UpgradeMode.Maxed && GameServices.Economy.CanAfford(_cost);

            bool equipped = Inventory.IsEquipped(item.uid);
            _equipLabel.text = equipped ? "Tháo" : "Mặc";
            // Đang mặc thì không cho phân rã (InventoryService cũng chặn); ẩn hẳn để khỏi bấm nhầm.
            _dismantleButton.gameObject.SetActive(!equipped);
        }

        private void HandleEquip()
        {
            OwnedItem item = Inventory.Get(_uid);
            ItemData data = Inventory.GetData(item);
            if (data == null) return;

            if (Inventory.IsEquipped(_uid)) Inventory.Unequip(data.slot);
            else Inventory.Equip(_uid);
        }

        private void HandleUpgrade()
        {
            if (_mode == UpgradeMode.Upgrade) Inventory.TryUpgrade(_uid);
            else if (_mode == UpgradeMode.Ascend) Inventory.TryAscend(_uid);
        }

        private void AskDismantle()
        {
            OwnedItem item = Inventory.Get(_uid);
            if (item == null) return;

            Inventory.GetDismantleRefund(item, _cost);
            string name = Inventory.GetData(item)?.displayName ?? item.id;
            _confirmText.text = $"Phân rã {name} (cấp {item.level})?\nNhận lại: {ItemTextFormatter.Cost(_cost, Inventory.Database)}";
            _confirm.SetActive(true);
        }

        private void HandleDismantle()
        {
            HideConfirm();
            Inventory.TryDismantle(_uid); // thành công → OnInventoryChanged → Refresh thấy món đã mất → tự đóng
        }

        private void HideConfirm()
        {
            if (_confirm != null) _confirm.SetActive(false);
        }
    }
}
