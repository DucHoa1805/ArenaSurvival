using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;

namespace ArenaSurvival.Shop
{
    /// <summary>
    /// Trao nội dung sản phẩm và mua bằng kim cương. Mọi tiền tệ/nguyên liệu đi qua EconomyService, trang bị qua InventoryService
    /// (nguyên tắc B2). Không tự gọi store: gói IAP đi qua IAPService (xác minh biên lai rồi mới gọi Grant).
    /// </summary>
    public class ShopService
    {
        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly InventoryService _inventory;
        private readonly Action _save;
        private readonly Func<float> _random01;

        public ShopCatalog Catalog { get; }
        public bool RemoveAdsOwned => _profile.removeAds;

        /// <summary>Có gì đổi (số lần mua, cờ xóa quảng cáo) — cửa hàng vẽ lại.</summary>
        public event Action OnChanged;

        public ShopService(PlayerProfile profile, EconomyService economy, InventoryService inventory, ShopCatalog catalog, Action save,
            Func<float> random01 = null)
        {
            _profile = profile;
            _economy = economy;
            _inventory = inventory;
            Catalog = catalog;
            _save = save;
            _random01 = random01 ?? (() => UnityEngine.Random.value);
        }

        public int PurchaseCount(string productId) =>
            productId != null && _profile.purchaseCounts.TryGetValue(productId, out int n) ? n : 0;

        public bool CanPurchase(ShopProductData product)
        {
            if (product == null) return false;
            if (product.kind == ShopProductKind.RemoveAds && _profile.removeAds) return false;
            return ShopRules.CanPurchase(PurchaseCount(product.id), product.purchaseLimit);
        }

        /// <summary>Gói vàng/rương: trừ kim cương rồi trao. Không đủ kim cương hoặc hết lượt → false, không đổi gì.</summary>
        public bool TryBuyWithGems(ShopProductData product, List<LootDrop> granted)
        {
            if (product == null || product.IsIap || !CanPurchase(product)) return false;
            if (product.gemCost > 0 && !_economy.TrySpend(CurrencyIds.Gem, product.gemCost)) return false;
            Grant(product, granted);
            _save?.Invoke();
            return true;
        }

        /// <summary>Rương miễn phí (sau quảng cáo): trao như mua nhưng không trừ kim cương, không tính lượt mua.</summary>
        public void GrantFree(ShopProductData product, List<LootDrop> granted)
        {
            if (product == null) return;
            GrantContents(product, 1f, granted);
            _save?.Invoke();
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Trao nội dung + tăng số lần mua. KHÔNG lưu (IAPService lưu cùng lúc với mã giao dịch để 2 thứ không bao giờ lệch nhau).
        /// granted nhận danh sách thứ đã trao để popup hiện lại.
        /// </summary>
        public void Grant(ShopProductData product, List<LootDrop> granted)
        {
            if (product == null) return;
            int before = PurchaseCount(product.id);
            GrantContents(product, ShopRules.ContentMultiplier(before, product.firstPurchaseMultiplier), granted);
            if (product.kind == ShopProductKind.RemoveAds) _profile.removeAds = true;
            _profile.purchaseCounts[product.id] = before + 1;
            OnChanged?.Invoke();
        }

        private void GrantContents(ShopProductData product, float multiplier, List<LootDrop> granted)
        {
            if (product.contents != null)
            {
                foreach (ShopContent c in product.contents)
                {
                    if (c == null) continue;
                    if (c.item != null) AddItem(c.item.id, c.rarity, granted);
                    else if (!string.IsNullOrEmpty(c.currencyId) && c.amount > 0)
                    {
                        long amount = ShopRules.ScaledAmount(c.amount, multiplier);
                        _economy.Add(c.currencyId, amount);
                        granted?.Add(LootDrop.Material(c.currencyId, (int)Math.Min(int.MaxValue, amount)));
                    }
                }
            }

            if (product.chestTable != null)
            {
                var drops = new List<LootDrop>();
                LootRules.Roll(product.chestTable, _random01, drops);
                foreach (LootDrop d in drops)
                {
                    if (d.kind == LootKind.Item) AddItem(d.id, d.rarity, granted);
                    else { _economy.Add(d.id, d.amount); granted?.Add(d); }
                }
            }
        }

        private void AddItem(string itemId, Items.Rarity rarity, List<LootDrop> granted)
        {
            if (_inventory == null) return;
            if (_inventory.Add(itemId, rarity, out _) != InventoryService.AddOutcome.Unknown) granted?.Add(LootDrop.Item(itemId, rarity));
        }
    }
}
