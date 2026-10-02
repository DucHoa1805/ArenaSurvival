using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;
using ArenaSurvival.Services;
using ArenaSurvival.Shop;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArenaSurvival.Tests
{
    /// <summary>Store giả trong bộ nhớ cho test: điều khiển tay việc giao giao dịch (giao lại, khôi phục).</summary>
    public class MemoryStore : IStore
    {
        public readonly List<string> Confirmed = new List<string>();
        public bool IsReady => true;
        public event Action<StorePurchase> OnPurchased;
        public event Action<string, string> OnPurchaseFailed;
        public string LastPurchase;
        public void Initialize(IReadOnlyList<StoreProductInfo> products) { }
        public void Purchase(string id) => LastPurchase = id;
        public void Restore() { }
        public void Confirm(string tx) => Confirmed.Add(tx);
        public string GetLocalizedPrice(string id) => null;
        public void Deliver(string tx, string id, string receipt = null) =>
            OnPurchased?.Invoke(new StorePurchase { transactionId = tx, storeProductId = id, receipt = receipt ?? "fake:" + id });
        public void Fail(string id, string reason) => OnPurchaseFailed?.Invoke(id, reason);
    }

    public class ShopTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private InventoryService _inventory;
        private ItemDatabase _db;
        private ItemData _armor;
        private ShopCatalog _catalog;
        private ShopProductData _gems, _removeAds, _gold, _chest;
        private LootTable _table;
        private MaterialData _ore;
        private ShopService _shop;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            _ore = ScriptableObject.CreateInstance<MaterialData>(); _ore.id = CurrencyIds.Ore;
            _armor = ScriptableObject.CreateInstance<ItemData>(); _armor.id = "armor_iron"; _armor.slot = EquipSlot.Armor;
            _db = ScriptableObject.CreateInstance<ItemDatabase>(); _db.items = new[] { _armor }; _db.materials = new[] { _ore };
            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _inventory = new InventoryService(_profile, _economy, _db, null);

            _gems = Product("gems_80", ShopProductKind.Gems, "com.test.gems80", 0, 0, 2f, new ShopContent { currencyId = CurrencyIds.Gem, amount = 80 });
            _removeAds = Product("remove_ads", ShopProductKind.RemoveAds, "com.test.noads", 0, 1, 1f);
            _gold = Product("gold_2000", ShopProductKind.Gold, null, 100, 0, 1f, new ShopContent { currencyId = CurrencyIds.Gold, amount = 2000 });
            _table = ScriptableObject.CreateInstance<LootTable>();
            _table.guaranteed = new[] { new LootEntry { material = _ore, min = 5, max = 5 } };
            _table.weighted = new[] { new LootEntry { item = _armor, weight = 1 } };
            _table.nothingWeight = 3;
            _table.rarityWeights = new[] { 50f, 30f, 15f, 5f };
            _chest = Product("chest_common", ShopProductKind.Chest, null, 60, 0, 1f);
            _chest.chestTable = _table;

            _catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            _catalog.gems = new[] { _gems };
            _catalog.offers = new[] { _removeAds };
            _catalog.gold = new[] { _gold };
            _catalog.chests = new[] { _chest };
            _saves = 0;
            _shop = new ShopService(_profile, _economy, _inventory, _catalog, () => _saves++, () => 0f);
        }

        private static ShopProductData Product(string id, ShopProductKind kind, string storeId, long gemCost, int limit, float first, params ShopContent[] contents)
        {
            var p = ScriptableObject.CreateInstance<ShopProductData>();
            p.id = id; p.kind = kind; p.storeProductId = storeId; p.gemCost = gemCost; p.purchaseLimit = limit;
            p.firstPurchaseMultiplier = first; p.contents = contents;
            return p;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in new Object[] { _ore, _armor, _db, _gems, _removeAds, _gold, _chest, _table, _catalog }) Object.DestroyImmediate(o);
        }

        [Test]
        public void ShopRules_LimitAndFirstPurchaseBonus()
        {
            Assert.IsTrue(ShopRules.CanPurchase(5, 0), "0 = không giới hạn");
            Assert.IsFalse(ShopRules.CanPurchase(1, 1));
            Assert.AreEqual(2f, ShopRules.ContentMultiplier(0, 2f));
            Assert.AreEqual(1f, ShopRules.ContentMultiplier(1, 2f));
        }

        [Test]
        public void BuyWithGems_NotEnough_ChangesNothing()
        {
            _economy.Add(CurrencyIds.Gem, 50);
            Assert.IsFalse(_shop.TryBuyWithGems(_gold, new List<LootDrop>()));
            Assert.AreEqual(50, _economy.GetBalance(CurrencyIds.Gem));
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(0, _shop.PurchaseCount(_gold.id));
        }

        [Test]
        public void BuyWithGems_SpendsAndGrants()
        {
            _economy.Add(CurrencyIds.Gem, 150);
            var granted = new List<LootDrop>();
            Assert.IsTrue(_shop.TryBuyWithGems(_gold, granted));
            Assert.AreEqual(50, _economy.GetBalance(CurrencyIds.Gem));
            Assert.AreEqual(2000, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(1, granted.Count);
            Assert.AreEqual(1, _saves);
        }

        [Test]
        public void Chest_RollsTableIntoInventoryAndEconomy()
        {
            _economy.Add(CurrencyIds.Gem, 60);
            var granted = new List<LootDrop>();
            Assert.IsTrue(_shop.TryBuyWithGems(_chest, granted)); // random 0 → chọn mục đầu (trang bị), độ hiếm Thường
            Assert.AreEqual(5, _economy.GetBalance(CurrencyIds.Ore));
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreEqual(2, granted.Count);
        }

        [Test]
        public void ChestOdds_SumToOneAndRarityNormalized()
        {
            var odds = new List<LootOdds>();
            LootOddsRules.Odds(_table, odds);
            Assert.AreEqual(2, odds.Count);
            Assert.AreEqual(1f, odds[0].chance, 1e-5, "mục chắc chắn");
            Assert.AreEqual(0.25f, odds[1].chance, 1e-5, "1 / (1 + 3 không gì)");
            float r = 0f; foreach (float x in odds[1].rarityChances) r += x;
            Assert.AreEqual(1f, r, 1e-5);
            Assert.AreEqual(0.05f, odds[1].rarityChances[3], 1e-5);
        }

        [Test]
        public void Iap_GrantsOnce_EvenIfStoreDeliversAgain()
        {
            var store = new MemoryStore();
            var iap = new IAPService(store, new FakeReceiptValidator(), _shop, _profile, () => _saves++);
            int granted = 0;
            iap.OnGranted += (_, __) => granted++;

            store.Deliver("tx1", "com.test.gems80");
            store.Deliver("tx1", "com.test.gems80"); // store giao lại (chưa nhận xác nhận)
            Assert.AreEqual(160, _economy.GetBalance(CurrencyIds.Gem), "lần đầu ×2, và chỉ trao 1 lần");
            Assert.AreEqual(1, granted);
            Assert.AreEqual(2, store.Confirmed.Count, "lần giao lại vẫn được xác nhận");

            store.Deliver("tx2", "com.test.gems80");
            Assert.AreEqual(240, _economy.GetBalance(CurrencyIds.Gem), "lần mua thứ 2 không còn ×2");
        }

        [Test]
        public void Iap_InvalidReceipt_NotGrantedNotConfirmed()
        {
            var store = new MemoryStore();
            var iap = new IAPService(store, new FakeReceiptValidator(), _shop, _profile, null);
            string fail = null;
            iap.OnFailed += (_, reason) => fail = reason;
            store.Deliver("tx9", "com.test.gems80", "forged");
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gem));
            Assert.IsEmpty(store.Confirmed);
            Assert.AreEqual("invalid_receipt", fail);
        }

        [Test]
        public void Iap_RestoreRemoveAds_OnlyIfNotOwned()
        {
            var store = new MemoryStore();
            var iap = new IAPService(store, new FakeReceiptValidator(), _shop, _profile, null);
            int granted = 0;
            iap.OnGranted += (_, __) => granted++;
            store.Deliver("restore-noads", "com.test.noads");
            Assert.IsTrue(_profile.removeAds);
            store.Deliver("restore-noads-2", "com.test.noads"); // khôi phục lần nữa
            Assert.AreEqual(1, granted, "đã sở hữu thì không trao/báo lại");
            Assert.AreEqual(1, _shop.PurchaseCount(_removeAds.id));
        }

        [Test]
        public void Iap_BuyBlockedWhenLimitReached()
        {
            var store = new MemoryStore();
            var iap = new IAPService(store, new FakeReceiptValidator(), _shop, _profile, null);
            _profile.removeAds = true;
            string fail = null;
            iap.OnFailed += (_, reason) => fail = reason;
            iap.Buy(_removeAds);
            Assert.AreEqual("limit", fail);
            Assert.IsNull(store.LastPurchase);
        }

        private class TestAdProvider : IAdProvider
        {
            public bool Reward = true;
            public int Shown;
            public bool IsReady => true;
            public void Show(string placement, Action<bool> onComplete) { Shown++; onComplete(Reward); }
            public void SetConsent(bool personalized) { }
        }

        [Test]
        public void Ads_DailyLimitResetsNextUtcDay()
        {
            var now = new DateTime(2026, 10, 2, 23, 0, 0, DateTimeKind.Utc);
            var provider = new TestAdProvider();
            var ads = new AdsService(provider, _profile, null, () => now);
            int rewards = 0;
            for (int i = 0; i < 4; i++) ads.Show(AdPlacements.Revive, ok => { if (ok) rewards++; });
            Assert.AreEqual(3, rewards, "hồi sinh tối đa 3 lượt/ngày");
            Assert.AreEqual(0, ads.Remaining(AdPlacements.Revive));
            now = now.AddHours(2); // sang ngày UTC mới
            Assert.AreEqual(3, ads.Remaining(AdPlacements.Revive));
        }

        [Test]
        public void Ads_ClosedEarly_NoRewardNoCount()
        {
            var provider = new TestAdProvider { Reward = false };
            var ads = new AdsService(provider, _profile, null);
            bool? result = null;
            ads.Show(AdPlacements.DoubleReward, ok => result = ok);
            Assert.AreEqual(false, result);
            Assert.AreEqual(5, ads.Remaining(AdPlacements.DoubleReward));
        }

        [Test]
        public void Ads_RemoveAds_RewardsWithoutShowingButStillCounts()
        {
            var provider = new TestAdProvider();
            var ads = new AdsService(provider, _profile, null);
            _profile.removeAds = true;
            bool rewarded = false;
            ads.Show(AdPlacements.FreeChest, ok => rewarded = ok);
            Assert.IsTrue(rewarded);
            Assert.AreEqual(0, provider.Shown, "không phải xem quảng cáo");
            Assert.AreEqual(1, ads.Remaining(AdPlacements.FreeChest));
        }

        [Test]
        public void MigrationV5ToV6_AddsShopFields()
        {
            var v5 = new JObject { ["saveVersion"] = 5, ["accountXp"] = 10, ["currencies"] = new JObject(), ["stageStars"] = new JObject(),
                ["talents"] = new JObject(), ["items"] = new JArray(), ["weapons"] = new JObject(), ["equipped"] = new JObject() };
            JObject v6 = SaveMigrations.Migrate(v5, 5);
            var profile = v6.ToObject<PlayerProfile>();
            Assert.AreEqual(PlayerProfile.CurrentVersion, profile.saveVersion);
            Assert.AreEqual(-1, profile.consent, "người chơi cũ chưa được hỏi");
            Assert.IsFalse(profile.removeAds);
            Assert.IsNotNull(profile.processedTransactions);
            Assert.AreEqual(10, profile.accountXp);
        }
    }
}
