using System;
using System.Collections.Generic;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using UnityEngine;

namespace ArenaSurvival.Shop
{
    /// <summary>
    /// Luồng mua thật (Phase 11): store giao giao dịch → xác minh biên lai → trao vật phẩm + ghi mã giao dịch → LƯU save → xác nhận
    /// với store. Thứ tự này đảm bảo: tắt game trước khi lưu thì store giao lại (trao bù); đã lưu thì giao lại chỉ được xác nhận,
    /// không trao lần 2. Gói mua-1-lần khôi phục (cài lại game) chỉ trao nếu hồ sơ chưa có.
    /// </summary>
    public class IAPService
    {
        // Giữ đủ dài để nhận ra giao dịch giao lại, không để danh sách phình mãi trong save.
        public const int MaxRememberedTransactions = 200;

        private readonly IStore _store;
        private readonly IReceiptValidator _validator;
        private readonly ShopService _shop;
        private readonly PlayerProfile _profile;
        private readonly Action _save;
        private readonly List<LootDrop> _granted = new List<LootDrop>();

        /// <summary>Trao xong (sản phẩm, danh sách thứ đã nhận) — UI hiện popup.</summary>
        public event Action<ShopProductData, IReadOnlyList<LootDrop>> OnGranted;
        /// <summary>Mua không thành (sản phẩm có thể null, lý do).</summary>
        public event Action<ShopProductData, string> OnFailed;

        public bool IsReady => _store.IsReady;

        public IAPService(IStore store, IReceiptValidator validator, ShopService shop, PlayerProfile profile, Action save)
        {
            _store = store;
            _validator = validator;
            _shop = shop;
            _profile = profile;
            _save = save;
            _store.OnPurchased += HandlePurchased;
            _store.OnPurchaseFailed += HandleFailed;
        }

        public void Initialize()
        {
            var products = new List<StoreProductInfo>();
            ShopCatalog c = _shop.Catalog;
            if (c != null)
                foreach (var group in new[] { c.offers, c.gems, c.chests, c.gold })
                    if (group != null)
                        foreach (ShopProductData p in group)
                            if (p != null && p.IsIap && !string.IsNullOrEmpty(p.storeProductId))
                                products.Add(new StoreProductInfo { storeProductId = p.storeProductId, nonConsumable = p.purchaseLimit == 1 });
            _store.Initialize(products);
        }

        public string PriceOf(ShopProductData product) =>
            product == null ? "" : _store.GetLocalizedPrice(product.storeProductId) ?? product.priceLabel;

        public void Buy(ShopProductData product)
        {
            if (product == null || !product.IsIap) { OnFailed?.Invoke(product, "invalid"); return; }
            if (!_shop.CanPurchase(product)) { OnFailed?.Invoke(product, "limit"); return; }
            if (!_store.IsReady) { OnFailed?.Invoke(product, "unavailable"); return; }
            _store.Purchase(product.storeProductId);
        }

        public void Restore() => _store.Restore();

        private void HandlePurchased(StorePurchase purchase)
        {
            ShopProductData product = _shop.Catalog != null ? _shop.Catalog.FindByStoreId(purchase.storeProductId) : null;

            if (_profile.processedTransactions.Contains(purchase.transactionId))
            {
                // Đã trao ở lần trước nhưng store chưa kịp nhận xác nhận: chỉ xác nhận lại.
                _store.Confirm(purchase.transactionId);
                return;
            }

            if (product == null)
            {
                // Sản phẩm đã gỡ khỏi cửa hàng: không trao được gì, xác nhận để store không giao lại mãi. Ghi log để hỗ trợ khách.
                Debug.LogWarning($"IAPService: giao dịch {purchase.transactionId} cho sản phẩm không còn trong catalog '{purchase.storeProductId}'.");
                _store.Confirm(purchase.transactionId);
                return;
            }

            if (!_validator.Validate(purchase))
            {
                // Không xác nhận: biên lai giả/lỗi thì không trao, store/máy chủ tự hoàn tiền nếu là giao dịch thật bị lỗi.
                Debug.LogWarning($"IAPService: biên lai không hợp lệ cho {purchase.storeProductId} ({purchase.transactionId}).");
                OnFailed?.Invoke(product, "invalid_receipt");
                return;
            }

            _granted.Clear();
            bool alreadyOwned = product.purchaseLimit > 0 && !_shop.CanPurchase(product);
            if (!alreadyOwned) _shop.Grant(product, _granted);
            Remember(purchase.transactionId);
            _save?.Invoke();
            _store.Confirm(purchase.transactionId);
            if (!alreadyOwned) OnGranted?.Invoke(product, _granted);
        }

        private void HandleFailed(string storeProductId, string reason)
        {
            ShopProductData product = _shop.Catalog != null ? _shop.Catalog.FindByStoreId(storeProductId) : null;
            OnFailed?.Invoke(product, reason);
        }

        private void Remember(string transactionId)
        {
            _profile.processedTransactions.Add(transactionId);
            int extra = _profile.processedTransactions.Count - MaxRememberedTransactions;
            if (extra > 0) _profile.processedTransactions.RemoveRange(0, extra);
        }
    }
}
