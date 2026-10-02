using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Stats;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Kho trang bị: thêm, mặc/tháo, nâng cấp, đột phá, phân rã. Class thuần (unit test được), nằm trong GameServices.
    /// Mọi chi phí trừ qua EconomyService.TrySpendMany; công thức ở EquipmentRules. Thao tác của người chơi lưu ngay;
    /// riêng Add không lưu vì nó được gọi hàng loạt lúc áp thưởng cuối màn (người gọi lưu 1 lần).
    /// </summary>
    public class InventoryService
    {
        public enum AddOutcome
        {
            Added,
            /// <summary>Kho đầy: món mới đã đổi ngay thành nguyên liệu.</summary>
            AutoDismantled,
            /// <summary>id không có trong ItemDatabase (dữ liệu bảng rơi đồ sai): bỏ qua.</summary>
            Unknown,
        }

        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly ItemDatabase _database;
        private readonly Action _save;
        private readonly List<CurrencyCost> _costBuffer = new List<CurrencyCost>(3);
        // Chỉ cảnh báo 1 lần mỗi id lạ, không spam console mỗi lần tính chỉ số.
        private readonly HashSet<string> _warnedIds = new HashSet<string>();

        public event Action OnInventoryChanged;
        public event Action OnEquipmentChanged;

        public InventoryService(PlayerProfile profile, EconomyService economy, ItemDatabase database, Action save)
        {
            _profile = profile;
            _economy = economy;
            _database = database;
            _save = save;
        }

        public IReadOnlyList<OwnedItem> Items => _profile.items;
        public bool IsFull => _profile.items.Count >= EquipmentRules.InventoryCapacity;
        public ItemDatabase Database => _database;

        public OwnedItem Get(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            foreach (OwnedItem item in _profile.items)
            {
                if (item.uid == uid) return item;
            }
            return null;
        }

        /// <summary>Dữ liệu của món; null nếu id không còn trong database (asset đã bị xóa).</summary>
        public ItemData GetData(OwnedItem item)
        {
            if (item == null || _database == null) return null;
            ItemData data = _database.GetItem(item.id);
            if (data == null && _warnedIds.Add(item.id))
                Debug.LogWarning($"InventoryService: không tìm thấy trang bị '{item.id}' trong ItemDatabase; giữ trong save nhưng bỏ qua.");
            return data;
        }

        public OwnedItem GetEquipped(EquipSlot slot) =>
            _profile.equipped.TryGetValue(EquipSlotIds.Key(slot), out string uid) ? Get(uid) : null;

        public bool IsEquipped(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return false;
            foreach (string equippedUid in _profile.equipped.Values)
            {
                if (equippedUid == uid) return true;
            }
            return false;
        }

        public StatBlock ItemStats(OwnedItem item)
        {
            ItemData data = GetData(item);
            return data == null ? default : EquipmentRules.Stats(data.baseStats, data.statsPerLevel, item.rarity, item.level);
        }

        /// <summary>Tổng chỉ số các món đang mặc — nguồn "trang bị" trong công thức chỉ số (plan A4).</summary>
        public StatBlock EquippedStats()
        {
            StatBlock total = default;
            foreach (string uid in _profile.equipped.Values)
            {
                OwnedItem item = Get(uid);
                if (item != null) total += ItemStats(item);
            }
            return total;
        }

        public AddOutcome Add(string itemId, Rarity rarity, out OwnedItem added)
        {
            added = null;
            ItemData data = _database != null ? _database.GetItem(itemId) : null;
            if (data == null)
            {
                Debug.LogWarning($"InventoryService: bỏ qua trang bị không có trong ItemDatabase: '{itemId}'.");
                return AddOutcome.Unknown;
            }

            if (IsFull)
            {
                // Không bao giờ âm thầm làm mất phần thưởng: đổi thành nguyên liệu, màn kết quả báo lại cho người chơi.
                GiveRefund(EquipmentRules.DismantleRefund(rarity, 1, 0), data);
                return AddOutcome.AutoDismantled;
            }

            added = new OwnedItem { uid = "i" + _profile.nextItemUid++, id = itemId, rarity = rarity, level = 1 };
            _profile.items.Add(added);
            OnInventoryChanged?.Invoke();
            return AddOutcome.Added;
        }

        public bool Equip(string uid)
        {
            OwnedItem item = Get(uid);
            ItemData data = GetData(item);
            if (data == null) return false;

            string key = EquipSlotIds.Key(data.slot);
            if (_profile.equipped.TryGetValue(key, out string current) && current == uid) return false;
            _profile.equipped[key] = uid;
            CommitEquipment();
            return true;
        }

        public bool Unequip(EquipSlot slot)
        {
            if (!_profile.equipped.Remove(EquipSlotIds.Key(slot))) return false;
            CommitEquipment();
            return true;
        }

        /// <summary>Chi phí nâng lên cấp kế. false nếu không nâng được (đang ở trần cấp, hoặc món không còn dữ liệu).</summary>
        public bool GetUpgradeCost(OwnedItem item, List<CurrencyCost> output)
        {
            output.Clear();
            ItemData data = GetData(item);
            if (data == null || item.level >= EquipmentRules.LevelCap(item.rarity, item.ascension)) return false;

            output.Add(new CurrencyCost(CurrencyIds.Gold, EquipmentRules.UpgradeGold(item.rarity, item.level)));
            if (data.upgradeMaterial != null)
                output.Add(new CurrencyCost(data.upgradeMaterial.id, EquipmentRules.UpgradeMaterial(item.level)));
            return true;
        }

        /// <summary>Chi phí đột phá. false nếu chưa tới mốc hoặc đã tới trần độ hiếm.</summary>
        public bool GetAscendCost(OwnedItem item, List<CurrencyCost> output)
        {
            output.Clear();
            if (GetData(item) == null || !EquipmentRules.CanAscend(item.rarity, item.level, item.ascension)) return false;

            output.Add(new CurrencyCost(CurrencyIds.Gold, EquipmentRules.AscendGold(item.ascension)));
            output.Add(new CurrencyCost(CurrencyIds.BossCore, EquipmentRules.AscendCores(item.ascension)));
            return true;
        }

        /// <summary>Thứ nhận lại khi phân rã (để popup ghi rõ trước khi người chơi xác nhận).</summary>
        public void GetDismantleRefund(OwnedItem item, List<CurrencyCost> output)
        {
            output.Clear();
            if (item == null) return;
            EquipmentRules.Refund refund = EquipmentRules.DismantleRefund(item.rarity, item.level, item.ascension);
            ItemData data = GetData(item);
            if (refund.gold > 0) output.Add(new CurrencyCost(CurrencyIds.Gold, refund.gold));
            if (refund.material > 0 && data != null && data.upgradeMaterial != null)
                output.Add(new CurrencyCost(data.upgradeMaterial.id, refund.material));
            if (refund.cores > 0) output.Add(new CurrencyCost(CurrencyIds.BossCore, refund.cores));
        }

        public bool TryUpgrade(string uid)
        {
            OwnedItem item = Get(uid);
            if (item == null || !GetUpgradeCost(item, _costBuffer) || !_economy.TrySpendMany(_costBuffer, EconomyReasons.ItemUpgrade)) return false;

            item.level++;
            CommitInventory(equipmentChanged: IsEquipped(uid));
            return true;
        }

        public bool TryAscend(string uid)
        {
            OwnedItem item = Get(uid);
            if (item == null || !GetAscendCost(item, _costBuffer) || !_economy.TrySpendMany(_costBuffer, EconomyReasons.ItemAscend)) return false;

            item.ascension++;
            CommitInventory(equipmentChanged: false);
            return true;
        }

        public bool TryDismantle(string uid)
        {
            OwnedItem item = Get(uid);
            if (item == null || IsEquipped(uid)) return false;

            DismantleWithoutSave(item);
            CommitInventory(equipmentChanged: false);
            return true;
        }

        /// <summary>Phân rã mọi món Thường cấp 1 chưa đột phá, chưa mặc — đồ chắc chắn không ai muốn giữ. Trả số món đã rã.</summary>
        public int DismantleQuick()
        {
            int count = 0;
            for (int i = _profile.items.Count - 1; i >= 0; i--)
            {
                OwnedItem item = _profile.items[i];
                if (item.rarity != Rarity.Common || item.level > 1 || item.ascension > 0 || IsEquipped(item.uid)) continue;
                DismantleWithoutSave(item);
                count++;
            }

            if (count > 0) CommitInventory(equipmentChanged: false);
            return count;
        }

        private void DismantleWithoutSave(OwnedItem item)
        {
            GiveRefund(EquipmentRules.DismantleRefund(item.rarity, item.level, item.ascension), GetData(item));
            _profile.items.Remove(item);
        }

        private void GiveRefund(EquipmentRules.Refund refund, ItemData data)
        {
            if (refund.gold > 0) _economy.Add(CurrencyIds.Gold, refund.gold, EconomyReasons.Dismantle);
            if (refund.material > 0 && data != null && data.upgradeMaterial != null) _economy.Add(data.upgradeMaterial.id, refund.material, EconomyReasons.Dismantle);
            if (refund.cores > 0) _economy.Add(CurrencyIds.BossCore, refund.cores, EconomyReasons.Dismantle);
        }

        private void CommitEquipment()
        {
            _save?.Invoke();
            OnEquipmentChanged?.Invoke();
        }

        private void CommitInventory(bool equipmentChanged)
        {
            _save?.Invoke();
            OnInventoryChanged?.Invoke();
            // Nâng cấp món đang mặc làm đổi chỉ số/lực chiến: báo cả kênh trang bị.
            if (equipmentChanged) OnEquipmentChanged?.Invoke();
        }
    }
}
