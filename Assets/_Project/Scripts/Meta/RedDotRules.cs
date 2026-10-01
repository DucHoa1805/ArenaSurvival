using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Weapons;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Khi nào hiện chấm đỏ ở tab Hub (hàm thuần trên các dịch vụ, có unit test). Chỉ báo việc người chơi LÀM ĐƯỢC NGAY
    /// (đủ tiền/mảnh, có đồ để mặc) — chấm đỏ lúc nào cũng sáng thì người chơi sẽ lờ nó đi.
    /// </summary>
    public static class RedDotRules
    {
        private static readonly List<CurrencyCost> Cost = new List<CurrencyCost>(2);

        public static bool TalentAvailable(TalentCatalog catalog, TalentService talents)
        {
            if (catalog == null || catalog.talents == null || talents == null) return false;
            foreach (TalentData talent in catalog.talents)
                if (talent != null && talents.CanUpgrade(talent)) return true;
            return false;
        }

        public static bool EquipmentAvailable(InventoryService inventory, WeaponService weapons, EconomyService economy)
        {
            if (weapons != null && weapons.Database != null && weapons.Database.weapons != null)
            {
                // Mở khóa hoặc lên sao được: mảnh là thứ người chơi hay tích mà quên dùng.
                foreach (WeaponData weapon in weapons.Database.weapons)
                    if (weapon != null && weapons.GetStarCost(weapon.id, Cost) && economy.CanAfford(Cost)) return true;
            }

            if (inventory != null)
            {
                // Ô còn trống mà kho có món đúng ô — người mới nhặt được đồ nhưng chưa biết phải mặc.
                if (SlotFillable(inventory, EquipSlot.Armor) || SlotFillable(inventory, EquipSlot.Accessory)) return true;
            }
            return false;
        }

        private static bool SlotFillable(InventoryService inventory, EquipSlot slot)
        {
            if (inventory.GetEquipped(slot) != null) return false;
            foreach (OwnedItem item in inventory.Items)
            {
                ItemData data = inventory.GetData(item);
                if (data != null && data.slot == slot) return true;
            }
            return false;
        }
    }
}
