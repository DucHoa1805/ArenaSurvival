using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using ArenaSurvival.Stats;
using ArenaSurvival.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class RedDotRulesTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private TalentService _talents;
        private InventoryService _inventory;
        private WeaponService _weapons;
        private TalentCatalog _catalog;
        private TalentData _hp;
        private ItemDatabase _db;
        private ItemData _armor;
        private WeaponData _pistol;
        private WeaponData _shotgun;

        [SetUp]
        public void SetUp()
        {
            _hp = ScriptableObject.CreateInstance<TalentData>();
            _hp.id = "talent_hp"; _hp.stat = StatKind.MaxHealth; _hp.valuePerLevel = 10; _hp.maxLevel = 5; _hp.baseCost = 100; _hp.costGrowth = 1.12f;
            _catalog = ScriptableObject.CreateInstance<TalentCatalog>();
            _catalog.talents = new[] { _hp };
            _armor = ScriptableObject.CreateInstance<ItemData>(); _armor.id = "armor_leather"; _armor.slot = EquipSlot.Armor;
            _pistol = ScriptableObject.CreateInstance<WeaponData>(); _pistol.id = WeaponService.DefaultWeaponId;
            _shotgun = ScriptableObject.CreateInstance<WeaponData>(); _shotgun.id = "weapon_shotgun";
            _db = ScriptableObject.CreateInstance<ItemDatabase>();
            _db.items = new[] { _armor };
            _db.weapons = new[] { _pistol, _shotgun };

            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _talents = new TalentService(_profile, _economy, null);
            _inventory = new InventoryService(_profile, _economy, _db, null);
            _weapons = new WeaponService(_profile, _economy, _db, null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in new Object[] { _hp, _catalog, _armor, _pistol, _shotgun, _db }) Object.DestroyImmediate(o);
        }

        [Test]
        public void Talent_OnlyWhenAffordable()
        {
            Assert.IsFalse(RedDotRules.TalentAvailable(_catalog, _talents));
            _economy.Add(CurrencyIds.Gold, 100);
            Assert.IsTrue(RedDotRules.TalentAvailable(_catalog, _talents));
        }

        [Test]
        public void Equipment_NothingToDo_NoDot()
        {
            Assert.IsFalse(RedDotRules.EquipmentAvailable(_inventory, _weapons, _economy));
        }

        [Test]
        public void Equipment_EmptySlotWithMatchingItem_Dot()
        {
            _inventory.Add("armor_leather", Rarity.Common, out OwnedItem item);
            Assert.IsTrue(RedDotRules.EquipmentAvailable(_inventory, _weapons, _economy));
            _inventory.Equip(item.uid);
            Assert.IsFalse(RedDotRules.EquipmentAvailable(_inventory, _weapons, _economy), "đã mặc rồi thì hết việc");
        }

        [Test]
        public void Equipment_WeaponUnlockableOrStarUp_Dot()
        {
            _economy.Add(WeaponRules.ShardId("weapon_shotgun"), WeaponRules.ShardsFor(1));
            Assert.IsTrue(RedDotRules.EquipmentAvailable(_inventory, _weapons, _economy), "đủ mảnh mở khóa");
            _weapons.TryUnlock("weapon_shotgun");
            Assert.IsFalse(RedDotRules.EquipmentAvailable(_inventory, _weapons, _economy));
            _economy.Add(WeaponRules.ShardId(WeaponService.DefaultWeaponId), WeaponRules.ShardsFor(2));
            Assert.IsTrue(RedDotRules.EquipmentAvailable(_inventory, _weapons, _economy), "đủ mảnh lên sao");
        }
    }
}
