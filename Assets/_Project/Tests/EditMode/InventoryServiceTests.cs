using System.Collections.Generic;
using System.IO;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;
using ArenaSurvival.Stats;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class InventoryServiceTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private InventoryService _inventory;
        private ItemDatabase _db;
        private MaterialData _leather;
        private ItemData _armor;
        private ItemData _ring;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            _leather = ScriptableObject.CreateInstance<MaterialData>();
            _leather.id = CurrencyIds.Leather;
            _armor = ScriptableObject.CreateInstance<ItemData>();
            _armor.id = "armor_leather"; _armor.slot = EquipSlot.Armor; _armor.upgradeMaterial = _leather;
            _armor.baseStats = new StatBlock { maxHealth = 20, armor = 3 };
            _armor.statsPerLevel = new StatBlock { maxHealth = 4, armor = 0.5f };
            _ring = ScriptableObject.CreateInstance<ItemData>();
            _ring.id = "acc_ring_power"; _ring.slot = EquipSlot.Accessory; _ring.upgradeMaterial = _leather;
            _ring.baseStats = new StatBlock { damagePercent = 4 };
            _db = ScriptableObject.CreateInstance<ItemDatabase>();
            _db.items = new[] { _armor, _ring };
            _db.materials = new[] { _leather };

            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _saves = 0;
            _inventory = new InventoryService(_profile, _economy, _db, () => _saves++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_leather);
            Object.DestroyImmediate(_armor);
            Object.DestroyImmediate(_ring);
            Object.DestroyImmediate(_db);
        }

        private OwnedItem AddArmor(Rarity rarity = Rarity.Common)
        {
            Assert.AreEqual(InventoryService.AddOutcome.Added, _inventory.Add("armor_leather", rarity, out OwnedItem item));
            return item;
        }

        [Test]
        public void Add_AssignsIncreasingUids_DoesNotSave()
        {
            OwnedItem a = AddArmor();
            OwnedItem b = AddArmor(Rarity.Rare);
            Assert.AreEqual("i1", a.uid);
            Assert.AreEqual("i2", b.uid);
            Assert.AreEqual(1, b.level);
            Assert.AreEqual(Rarity.Rare, b.rarity);
            Assert.AreEqual(2, _inventory.Items.Count);
            Assert.AreEqual(0, _saves, "Add không tự lưu: người gọi lưu 1 lần sau khi cộng cả loạt");
        }

        [Test]
        public void Add_UnknownId_IsRejected()
        {
            Assert.AreEqual(InventoryService.AddOutcome.Unknown, _inventory.Add("no_such_item", Rarity.Common, out OwnedItem item));
            Assert.IsNull(item);
            Assert.AreEqual(0, _inventory.Items.Count);
        }

        [Test]
        public void Add_WhenFull_AutoDismantlesIntoMaterial()
        {
            for (int i = 0; i < EquipmentRules.InventoryCapacity; i++) AddArmor();
            Assert.IsTrue(_inventory.IsFull);

            Assert.AreEqual(InventoryService.AddOutcome.AutoDismantled, _inventory.Add("armor_leather", Rarity.Epic, out OwnedItem item));
            Assert.IsNull(item);
            Assert.AreEqual(EquipmentRules.InventoryCapacity, _inventory.Items.Count);
            Assert.AreEqual(EquipmentRules.DismantleBase(Rarity.Epic), _economy.GetBalance(CurrencyIds.Leather));
        }

        [Test]
        public void Equip_SetsSlot_ReplacesPreviousInSameSlot_Saves()
        {
            OwnedItem a = AddArmor();
            OwnedItem b = AddArmor();
            int events = 0;
            _inventory.OnEquipmentChanged += () => events++;

            Assert.IsTrue(_inventory.Equip(a.uid));
            Assert.IsTrue(_inventory.Equip(b.uid));
            Assert.AreSame(b, _inventory.GetEquipped(EquipSlot.Armor));
            Assert.IsFalse(_inventory.IsEquipped(a.uid));
            Assert.AreEqual(2, _saves);
            Assert.AreEqual(2, events);
        }

        [Test]
        public void Unequip_ClearsSlot()
        {
            OwnedItem a = AddArmor();
            _inventory.Equip(a.uid);
            Assert.IsTrue(_inventory.Unequip(EquipSlot.Armor));
            Assert.IsNull(_inventory.GetEquipped(EquipSlot.Armor));
            Assert.IsFalse(_inventory.Unequip(EquipSlot.Armor), "ô trống thì không có gì để tháo");
        }

        [Test]
        public void EquippedStats_SumsEquippedItemsOnly()
        {
            OwnedItem a = AddArmor(Rarity.Rare);
            _inventory.Add("acc_ring_power", Rarity.Common, out OwnedItem ring);
            AddArmor(); // không mặc
            _inventory.Equip(a.uid);
            _inventory.Equip(ring.uid);

            StatBlock s = _inventory.EquippedStats();
            Assert.AreEqual(30f, s.maxHealth, 1e-4, "20 × 1.5");
            Assert.AreEqual(4.5f, s.armor, 1e-4);
            Assert.AreEqual(4f, s.damagePercent, 1e-4);
        }

        [Test]
        public void EquippedStats_UnknownItemInSave_IsSkippedButKept()
        {
            _profile.items.Add(new OwnedItem { uid = "i99", id = "removed_item", level = 3 });
            _profile.equipped[EquipSlotIds.Armor] = "i99";

            StatBlock s = _inventory.EquippedStats();
            Assert.AreEqual(0f, s.maxHealth);
            Assert.AreEqual(1, _profile.items.Count, "không bao giờ xóa dữ liệu người chơi");
        }

        [Test]
        public void TryUpgrade_EnoughResources_LevelsUpAndSpends()
        {
            OwnedItem a = AddArmor();
            _economy.Add(CurrencyIds.Gold, 1000);
            _economy.Add(CurrencyIds.Leather, 10);

            Assert.IsTrue(_inventory.TryUpgrade(a.uid));
            Assert.AreEqual(2, a.level);
            Assert.AreEqual(1000 - EquipmentRules.UpgradeGold(Rarity.Common, 1), _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(10 - EquipmentRules.UpgradeMaterial(1), _economy.GetBalance(CurrencyIds.Leather));
            Assert.AreEqual(1, _saves);
        }

        [Test]
        public void TryUpgrade_MissingMaterial_ChangesNothing()
        {
            OwnedItem a = AddArmor();
            _economy.Add(CurrencyIds.Gold, 1000);

            Assert.IsFalse(_inventory.TryUpgrade(a.uid));
            Assert.AreEqual(1, a.level);
            Assert.AreEqual(1000, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(0, _saves);
        }

        [Test]
        public void TryUpgrade_AtLevelCap_Refused_ThenAscendUnlocksMore()
        {
            OwnedItem a = AddArmor(Rarity.Rare);
            a.level = 10;
            _economy.Add(CurrencyIds.Gold, 100000);
            _economy.Add(CurrencyIds.Leather, 100);

            Assert.IsFalse(_inventory.TryUpgrade(a.uid), "cấp 10 phải đột phá trước");
            Assert.IsFalse(_inventory.TryAscend(a.uid), "chưa có lõi boss");

            _economy.Add(CurrencyIds.BossCore, 1);
            Assert.IsTrue(_inventory.TryAscend(a.uid));
            Assert.AreEqual(1, a.ascension);
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.BossCore));
            Assert.IsTrue(_inventory.TryUpgrade(a.uid));
            Assert.AreEqual(11, a.level);
        }

        [Test]
        public void TryAscend_CommonItem_Refused()
        {
            OwnedItem a = AddArmor();
            a.level = 10;
            _economy.Add(CurrencyIds.Gold, 100000);
            _economy.Add(CurrencyIds.BossCore, 5);
            Assert.IsFalse(_inventory.TryAscend(a.uid));
        }

        [Test]
        public void TryDismantle_RefundsAndRemoves_EquippedRefused()
        {
            OwnedItem a = AddArmor(Rarity.Rare);
            OwnedItem b = AddArmor();
            _inventory.Equip(b.uid);

            Assert.IsFalse(_inventory.TryDismantle(b.uid), "món đang mặc không phân rã được");
            Assert.IsTrue(_inventory.TryDismantle(a.uid));
            Assert.IsNull(_inventory.Get(a.uid));
            Assert.AreEqual(EquipmentRules.DismantleBase(Rarity.Rare), _economy.GetBalance(CurrencyIds.Leather));
        }

        [Test]
        public void DismantleQuick_OnlyCommonLevel1Unequipped()
        {
            OwnedItem keepEquipped = AddArmor();
            OwnedItem keepRare = AddArmor(Rarity.Rare);
            OwnedItem keepUpgraded = AddArmor();
            keepUpgraded.level = 2;
            AddArmor();
            AddArmor();
            _inventory.Equip(keepEquipped.uid);
            int savesBefore = _saves;

            Assert.AreEqual(2, _inventory.DismantleQuick());
            Assert.AreEqual(3, _inventory.Items.Count);
            Assert.IsNotNull(_inventory.Get(keepRare.uid));
            Assert.AreEqual(savesBefore + 1, _saves, "lưu 1 lần cho cả loạt");
        }

        [Test]
        public void GetUpgradeCost_ListsGoldAndMaterial()
        {
            OwnedItem a = AddArmor(Rarity.Epic);
            a.level = 5;
            var cost = new List<CurrencyCost>();

            Assert.IsTrue(_inventory.GetUpgradeCost(a, cost));
            Assert.AreEqual(2, cost.Count);
            Assert.AreEqual(CurrencyIds.Gold, cost[0].id);
            Assert.AreEqual(EquipmentRules.UpgradeGold(Rarity.Epic, 5), cost[0].amount);
            Assert.AreEqual(CurrencyIds.Leather, cost[1].id);
            Assert.AreEqual(EquipmentRules.UpgradeMaterial(5), cost[1].amount);
        }
    }

    public class SaveMigrationV4Tests
    {
        [Test]
        public void V3File_IsMigratedToV4WithEmptyInventory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ArenaSurvivalMig_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "save.json");
            try
            {
                // Đúng định dạng file của Phase 8A (save v3, có Talent, chưa có kho đồ).
                string payload = "{\"saveVersion\":3,\"accountXp\":40,\"currencies\":{\"gold\":200},\"stageStars\":{\"c1_s01\":3}," +
                                 "\"talents\":{\"talent_hp\":4},\"tampered\":false}";
                string json = "{\"format\":2,\"payload\":" + Newtonsoft.Json.JsonConvert.ToString(payload) +
                              ",\"hmac\":\"" + SaveSecurity.Sign(payload) + "\"}";
                File.WriteAllText(path, json);

                SaveLoadResult r = new SaveService(path).Load();

                Assert.AreEqual(SaveLoadStatus.Migrated, r.Status);
                Assert.AreEqual(4, r.Profile.saveVersion);
                Assert.AreEqual(4, r.Profile.talents["talent_hp"], "dữ liệu cũ giữ nguyên");
                Assert.IsNotNull(r.Profile.items);
                Assert.AreEqual(0, r.Profile.items.Count);
                Assert.IsNotNull(r.Profile.equipped);
                Assert.AreEqual(1, r.Profile.nextItemUid);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void Inventory_RoundTripsThroughSave()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ArenaSurvivalInv_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "save.json");
            try
            {
                var profile = new PlayerProfile();
                profile.items.Add(new OwnedItem { uid = "i7", id = "armor_iron", rarity = Rarity.Epic, level = 12, ascension = 1 });
                profile.equipped[EquipSlotIds.Armor] = "i7";
                profile.nextItemUid = 8;
                var service = new SaveService(path);
                Assert.IsTrue(service.Save(profile));

                PlayerProfile loaded = service.Load().Profile;
                Assert.AreEqual(1, loaded.items.Count);
                Assert.AreEqual(Rarity.Epic, loaded.items[0].rarity);
                Assert.AreEqual(12, loaded.items[0].level);
                Assert.AreEqual(1, loaded.items[0].ascension);
                Assert.AreEqual("i7", loaded.equipped[EquipSlotIds.Armor]);
                Assert.AreEqual(8, loaded.nextItemUid);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
