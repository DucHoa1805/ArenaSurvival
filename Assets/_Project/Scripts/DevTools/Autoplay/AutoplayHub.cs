using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using ArenaSurvival.Stats;
using ArenaSurvival.Weapons;

namespace ArenaSurvival.DevTools
{
    /// <summary>
    /// Phần "ra Hub tiêu tiền" của bot: mặc đồ/cầm vũ khí mạnh nhất, phân rã đồ yếu lấy nguyên liệu, rồi lặp mua thứ tăng lực chiến
    /// nhiều nhất trên mỗi đồng vàng (AutoplayRules.PickBest) tới khi hết thứ mua được. Mọi thao tác đi qua đúng các dịch vụ mà UI dùng,
    /// nên bot chịu cùng luật kinh tế với người chơi. Chỉ chạy trong build dev (AutoplayRunner).
    /// </summary>
    public static class AutoplayHub
    {
        private const int MaxActions = 2000;

        private static readonly List<AutoplayOption> Options = new List<AutoplayOption>();
        private static readonly List<Func<bool>> Actions = new List<Func<bool>>();
        private static readonly List<CurrencyCost> Cost = new List<CurrencyCost>();

        /// <summary>Trả về số lần mua/nâng đã làm.</summary>
        public static int SpendAll(TalentCatalog talents, HeroBaseStats heroBase)
        {
            UnlockWeapons();
            EquipBestWeapon();
            EquipBestItems();
            DismantleWeakItems();

            int done = 0;
            for (; done < MaxActions; done++)
            {
                BuildOptions(talents, heroBase);
                int best = AutoplayRules.PickBest(Options);
                if (best < 0 || !Actions[best]()) break;
                // Nâng sao có thể khiến khẩu khác mạnh hơn khẩu đang cầm.
                EquipBestWeapon();
            }
            return done;
        }

        private static void UnlockWeapons()
        {
            WeaponService weapons = GameServices.Weapons;
            foreach (WeaponData data in weapons.Database.weapons)
            {
                if (data != null && !weapons.IsOwned(data.id)) weapons.TryUnlock(data.id);
            }
        }

        /// <summary>
        /// Bot né bằng cách giữ khoảng cách nên chỉ dùng được súng bắn xa: cầm Shotgun (tầm 3.5) thì phải lao vào giữa đám quái —
        /// bot kẹt cả chục lượt ở 1-4 vì vậy. Đây là giới hạn của bot, ghi rõ trong báo cáo chơi thử.
        /// </summary>
        public const float MinWeaponRange = 5f;

        private static void EquipBestWeapon()
        {
            WeaponService weapons = GameServices.Weapons;
            string bestId = WeaponService.DefaultWeaponId;
            int bestPower = WeaponRules.Power(weapons.GetEffective(bestId));
            foreach (WeaponData data in weapons.Database.weapons)
            {
                if (data == null || !weapons.IsOwned(data.id) || weapons.GetEffective(data.id).range < MinWeaponRange) continue;
                int power = WeaponRules.Power(weapons.GetEffective(data.id));
                if (power > bestPower) { bestPower = power; bestId = data.id; }
            }
            weapons.Equip(bestId);
        }

        private static int ItemPower(OwnedItem item) => StatCalculator.Power(GameServices.Inventory.ItemStats(item));

        private static void EquipBestItems()
        {
            InventoryService inventory = GameServices.Inventory;
            foreach (EquipSlot slot in new[] { EquipSlot.Armor, EquipSlot.Accessory })
            {
                OwnedItem best = inventory.GetEquipped(slot);
                int bestPower = best != null ? ItemPower(best) : -1;
                foreach (OwnedItem item in inventory.Items)
                {
                    ItemData data = inventory.GetData(item);
                    if (data == null || data.slot != slot) continue;
                    int power = ItemPower(item);
                    if (power > bestPower) { best = item; bestPower = power; }
                }
                if (best != null && !inventory.IsEquipped(best.uid)) inventory.Equip(best.uid);
            }
        }

        /// <summary>Đồ không mặc mà yếu hơn đồ đang mặc cùng ô thì phân rã lấy nguyên liệu nâng cấp — như người chơi dọn kho.</summary>
        private static void DismantleWeakItems()
        {
            InventoryService inventory = GameServices.Inventory;
            var junk = new List<string>();
            foreach (OwnedItem item in inventory.Items)
            {
                ItemData data = inventory.GetData(item);
                if (data == null || inventory.IsEquipped(item.uid)) continue;
                OwnedItem equipped = inventory.GetEquipped(data.slot);
                if (equipped != null && ItemPower(item) <= ItemPower(equipped)) junk.Add(item.uid);
            }
            foreach (string uid in junk) inventory.TryDismantle(uid);
        }

        private static long GoldIn(List<CurrencyCost> costs)
        {
            long gold = 0;
            foreach (CurrencyCost c in costs) if (c.id == CurrencyIds.Gold) gold += c.amount;
            return gold;
        }

        private static void Add(float gain, long gold, bool affordable, Func<bool> action)
        {
            Options.Add(new AutoplayOption(gain, gold, affordable));
            Actions.Add(action);
        }

        private static void BuildOptions(TalentCatalog talents, HeroBaseStats heroBase)
        {
            Options.Clear();
            Actions.Clear();
            EconomyService economy = GameServices.Economy;
            InventoryService inventory = GameServices.Inventory;
            WeaponService weapons = GameServices.Weapons;

            // Talent: lực chiến sau khi +1 cấp, tính bằng đúng công thức hiển thị.
            StatBlock baseStats = heroBase != null ? heroBase.baseStats : new StatBlock { maxHealth = 100f };
            StatBlock equipment = inventory.EquippedStats();
            int now = StatCalculator.Power(StatCalculator.Compute(baseStats, talents, GameServices.Talents.GetLevel, equipment));
            foreach (TalentData talent in talents.talents)
            {
                if (talent == null) continue;
                int cost = GameServices.Talents.GetCost(talent);
                if (cost < 0) continue;
                string id = talent.id;
                int after = StatCalculator.Power(StatCalculator.Compute(baseStats, talents,
                    t => GameServices.Talents.GetLevel(t) + (t == id ? 1 : 0), equipment));
                TalentData captured = talent;
                Add(after - now, cost, GameServices.Talents.CanUpgrade(talent), () => GameServices.Talents.TryUpgrade(captured));
            }

            // Vũ khí đang cầm: lên cấp (vàng + quặng) và lên sao (mảnh, không tốn vàng).
            string weaponId = weapons.EquippedId;
            WeaponData weaponData = weapons.GetData(weaponId);
            if (weaponData != null)
            {
                int level = weapons.GetLevel(weaponId), stars = weapons.GetStars(weaponId);
                int weaponNow = WeaponRules.Power(WeaponRules.Effective(weaponData, level, stars));
                if (weapons.GetUpgradeCost(weaponId, Cost))
                {
                    int after = WeaponRules.Power(WeaponRules.Effective(weaponData, level + 1, stars));
                    Add(after - weaponNow, GoldIn(Cost), economy.CanAfford(Cost), () => weapons.TryUpgrade(weaponId));
                }
                if (weapons.GetStarCost(weaponId, Cost))
                {
                    int after = WeaponRules.Power(WeaponRules.Effective(weaponData, level, stars + 1));
                    Add(after - weaponNow, GoldIn(Cost), economy.CanAfford(Cost), () => weapons.TryStarUp(weaponId));
                }
            }

            // Đồ đang mặc: nâng cấp, hoặc đột phá khi chạm trần (giá trị = cấp kế tiếp mà nó mở ra).
            foreach (EquipSlot slot in new[] { EquipSlot.Armor, EquipSlot.Accessory })
            {
                OwnedItem item = inventory.GetEquipped(slot);
                ItemData data = item != null ? inventory.GetData(item) : null;
                if (data == null) continue;
                int itemNow = StatCalculator.Power(EquipmentRules.Stats(data.baseStats, data.statsPerLevel, item.rarity, item.level));
                int itemNext = StatCalculator.Power(EquipmentRules.Stats(data.baseStats, data.statsPerLevel, item.rarity, item.level + 1));
                string uid = item.uid;
                if (inventory.GetUpgradeCost(item, Cost))
                    Add(itemNext - itemNow, GoldIn(Cost), economy.CanAfford(Cost), () => inventory.TryUpgrade(uid));
                else if (inventory.GetAscendCost(item, Cost))
                    Add(itemNext - itemNow, GoldIn(Cost), economy.CanAfford(Cost), () => inventory.TryAscend(uid));
            }
        }
    }
}
