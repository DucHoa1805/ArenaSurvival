using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    /// <summary>Áp loot cuối màn (spec 8B mục 7.3): nguyên liệu luôn nhận, trang bị chỉ nhận khi thắng.</summary>
    public class StageLootApplyTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private InventoryService _inventory;
        private StageProgressService _progress;
        private ItemDatabase _db;
        private ItemData _ring;
        private MaterialData _leather;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            _leather = ScriptableObject.CreateInstance<MaterialData>();
            _leather.id = CurrencyIds.Leather;
            _ring = ScriptableObject.CreateInstance<ItemData>();
            _ring.id = "acc_ring_power"; _ring.slot = EquipSlot.Accessory; _ring.upgradeMaterial = _leather;
            _db = ScriptableObject.CreateInstance<ItemDatabase>();
            _db.items = new[] { _ring };
            _db.materials = new[] { _leather };

            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _saves = 0;
            _inventory = new InventoryService(_profile, _economy, _db, () => _saves++);
            _progress = new StageProgressService(_profile, _economy, () => _saves++, _inventory);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_leather);
            Object.DestroyImmediate(_ring);
            Object.DestroyImmediate(_db);
        }

        private static StageResult Result(bool victory, List<LootDrop> collected, List<LootDrop> bonus = null) => new StageResult
        {
            stageId = "c1_s01", victory = victory, starCount = victory ? 1 : 0, gold = 0, accountXp = 0,
            collected = collected, bonusLoot = bonus ?? new List<LootDrop>(),
        };

        [Test]
        public void Victory_GrantsMaterialsItemsAndBonus_SavesOnce()
        {
            var collected = new List<LootDrop> { LootDrop.Material(CurrencyIds.Leather, 3), LootDrop.Item("acc_ring_power", Rarity.Rare) };
            var bonus = new List<LootDrop> { LootDrop.Material(CurrencyIds.Leather, 2), LootDrop.Item("acc_ring_power", Rarity.Common) };

            StageGrant grant = _progress.Apply(Result(true, collected, bonus));

            Assert.AreEqual(5, _economy.GetBalance(CurrencyIds.Leather));
            Assert.AreEqual(5, grant.materials[CurrencyIds.Leather]);
            Assert.AreEqual(2, _inventory.Items.Count);
            Assert.AreEqual(2, grant.newItemUids.Count);
            Assert.AreEqual(0, grant.lostItems.Count);
            Assert.AreEqual(1, _saves, "áp cả màn chỉ lưu 1 lần");
        }

        [Test]
        public void Defeat_KeepsMaterials_LosesItems()
        {
            var collected = new List<LootDrop> { LootDrop.Material(CurrencyIds.Ore, 4), LootDrop.Item("acc_ring_power", Rarity.Epic) };

            StageGrant grant = _progress.Apply(Result(false, collected));

            Assert.AreEqual(4, _economy.GetBalance(CurrencyIds.Ore));
            Assert.AreEqual(0, _inventory.Items.Count);
            Assert.AreEqual(1, grant.lostItems.Count);
            Assert.AreEqual(Rarity.Epic, grant.lostItems[0].rarity);
        }

        [Test]
        public void Victory_InventoryFull_CountsAutoDismantled()
        {
            for (int i = 0; i < EquipmentRules.InventoryCapacity; i++) _inventory.Add("acc_ring_power", Rarity.Common, out _);

            StageGrant grant = _progress.Apply(Result(true, new List<LootDrop> { LootDrop.Item("acc_ring_power", Rarity.Rare) }));

            Assert.AreEqual(1, grant.autoDismantled);
            Assert.AreEqual(0, grant.newItemUids.Count);
            Assert.AreEqual(EquipmentRules.DismantleBase(Rarity.Rare), _economy.GetBalance(CurrencyIds.Leather));
        }

        [Test]
        public void NullLootLists_AreTreatedAsEmpty()
        {
            var result = new StageResult { stageId = "c1_s01", victory = true, starCount = 1, collected = null, bonusLoot = null };
            StageGrant grant = _progress.Apply(result);
            Assert.AreEqual(0, grant.materials.Count);
            Assert.AreEqual(0, grant.newItemUids.Count);
        }
    }
}
