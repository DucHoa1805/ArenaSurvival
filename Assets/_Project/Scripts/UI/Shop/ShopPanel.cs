using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using ArenaSurvival.Services;
using ArenaSurvival.Shop;
using ArenaSurvival.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Shop
{
    /// <summary>
    /// Tab Cửa hàng (Phase 11A): dựng thẻ từ ShopCatalog, điều phối mua (IAP qua IAPService, gói kim cương qua ShopService),
    /// rương miễn phí bằng quảng cáo, khôi phục giao dịch. Chỉ hiển thị + chuyển lệnh — luật nằm ở dịch vụ.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private ShopCardView _cardPrefab;
        [SerializeField] private Transform _offersRow;
        [SerializeField] private Transform _gemsRow;
        [SerializeField] private Transform _chestsRow;
        [SerializeField] private Transform _goldRow;
        [SerializeField] private Button _restoreButton;
        [SerializeField] private ShopConfirmPopup _confirm;
        [SerializeField] private ShopResultPopup _result;
        [SerializeField] private ShopOddsPopup _odds;
        [SerializeField] private HubToast _toast;

        private readonly List<ShopCardView> _cards = new List<ShopCardView>();
        private readonly List<LootDrop> _granted = new List<LootDrop>();
        private bool _built;
        private Action _refresh;
        private Action<string, long> _onCurrency;
        private Action<ShopProductData, IReadOnlyList<LootDrop>> _onGranted;
        private Action<ShopProductData, string> _onFailed;

        private void Awake()
        {
            _refresh = RefreshAll;
            _onCurrency = (_, __) => RefreshAll();
            _onGranted = HandleGranted;
            _onFailed = HandleFailed;
            if (_restoreButton != null) _restoreButton.onClick.AddListener(Restore);
        }

        private void OnEnable()
        {
            if (!GameServices.IsInitialized) return;
            if (!_built) Build();
            GameServices.Shop.OnChanged += _refresh;
            GameServices.Ads.OnChanged += _refresh;
            GameServices.Economy.OnCurrencyChanged += _onCurrency;
            GameServices.Iap.OnGranted += _onGranted;
            GameServices.Iap.OnFailed += _onFailed;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (!GameServices.IsInitialized) return;
            GameServices.Shop.OnChanged -= _refresh;
            GameServices.Ads.OnChanged -= _refresh;
            GameServices.Economy.OnCurrencyChanged -= _onCurrency;
            GameServices.Iap.OnGranted -= _onGranted;
            GameServices.Iap.OnFailed -= _onFailed;
        }

        private void Build()
        {
            _built = true;
            ShopCatalog c = GameServices.Shop.Catalog;
            AddCards(c.offers, _offersRow);
            AddCards(c.gems, _gemsRow);
            // Rương miễn phí đứng đầu mục Rương: thứ người chơi quay lại mỗi ngày.
            if (c.chests != null && c.chests.Length > 0 && c.chests[0] != null) AddCard(c.chests[0], _chestsRow, true);
            AddCards(c.chests, _chestsRow);
            AddCards(c.gold, _goldRow);
        }

        private void AddCards(ShopProductData[] products, Transform row)
        {
            if (products == null || row == null) return;
            foreach (ShopProductData p in products) if (p != null) AddCard(p, row, false);
        }

        private void AddCard(ShopProductData p, Transform row, bool freeChest)
        {
            ShopCardView card = Instantiate(_cardPrefab, row);
            card.Bind(p, freeChest);
            card.OnBuy += HandleBuy;
            card.OnOdds += v => _odds.Show("Tỷ lệ rơi — " + v.Product.displayName, v.Product.chestTable);
            _cards.Add(card);
        }

        private void RefreshAll()
        {
            foreach (ShopCardView card in _cards) card.Refresh();
        }

        private void HandleBuy(ShopCardView card)
        {
            ShopProductData p = card.Product;
            if (card.IsFreeChest)
            {
                GameServices.Ads.Show(AdPlacements.FreeChest, ok =>
                {
                    if (!ok) { Toast("Xem hết quảng cáo để nhận rương."); return; }
                    _granted.Clear();
                    GameServices.Shop.GrantFree(p, _granted);
                    _result.Show(p.displayName, _granted);
                });
                return;
            }

            if (p.IsIap)
            {
                GameServices.Iap.Buy(p);
                return;
            }

            if (GameServices.Economy.GetBalance(CurrencyIds.Gem) < p.gemCost)
            {
                Toast("Không đủ kim cương.");
                return;
            }
            _confirm.Show($"Mua {p.displayName} với {p.gemCost} kim cương?", () =>
            {
                _granted.Clear();
                if (GameServices.Shop.TryBuyWithGems(p, _granted)) _result.Show(p.displayName, _granted);
                else Toast("Không mua được.");
            });
        }

        private void HandleGranted(ShopProductData product, IReadOnlyList<LootDrop> granted)
        {
            if (product.kind == ShopProductKind.RemoveAds || granted.Count == 0) Toast($"Đã nhận: {product.displayName}");
            else _result.Show(product.displayName, granted);
        }

        private void HandleFailed(ShopProductData product, string reason)
        {
            switch (reason)
            {
                case "cancelled": return; // người chơi tự hủy: không làm phiền
                case "limit": Toast("Gói này đã mua rồi."); break;
                case "unavailable": Toast("Cửa hàng chưa sẵn sàng, thử lại sau."); break;
                default: Toast("Mua không thành công. Bạn chưa bị trừ tiền."); break;
            }
        }

        private void Restore()
        {
            GameServices.Iap.Restore();
            Toast("Đã gửi yêu cầu khôi phục giao dịch.");
        }

        private void Toast(string message)
        {
            if (_toast != null) _toast.Show(message);
            else Debug.Log("[Shop] " + message);
        }
    }
}
