using System;
using ArenaSurvival.Meta;
using ArenaSurvival.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Shop
{
    /// <summary>1 thẻ sản phẩm trong cửa hàng: icon, tên, mô tả, nhãn (x2 lần đầu / đã mua), nút giá, nút Tỷ lệ (rương).</summary>
    public class ShopCardView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private GameObject _badge;
        [SerializeField] private TMP_Text _badgeText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Image _priceIcon;
        [SerializeField] private Button _oddsButton;
        [SerializeField] private Sprite _gemSprite;
        [SerializeField] private Sprite _adSprite;

        public ShopProductData Product { get; private set; }
        public bool IsFreeChest { get; private set; }

        public event Action<ShopCardView> OnBuy;
        public event Action<ShopCardView> OnOdds;

        private void Awake()
        {
            _buyButton.onClick.AddListener(() => OnBuy?.Invoke(this));
            if (_oddsButton != null) _oddsButton.onClick.AddListener(() => OnOdds?.Invoke(this));
        }

        /// <param name="freeChest">Thẻ "rương miễn phí": mở bằng quảng cáo, không trừ kim cương.</param>
        public void Bind(ShopProductData product, bool freeChest = false)
        {
            Product = product;
            IsFreeChest = freeChest;
            _icon.sprite = product.icon;
            _icon.enabled = product.icon != null;
            _name.text = freeChest ? product.displayName + " miễn phí" : product.displayName;
            _description.text = freeChest ? "Xem quảng cáo để mở." : product.description;
            if (_oddsButton != null) _oddsButton.gameObject.SetActive(product.kind == ShopProductKind.Chest);
            Refresh();
        }

        /// <summary>Vẽ lại giá/nhãn/khả năng bấm (sau khi mua, đổi số dư, hết lượt quảng cáo).</summary>
        public void Refresh()
        {
            if (Product == null || !GameServices.IsInitialized) return;
            ShopService shop = GameServices.Shop;
            bool canBuy = shop.CanPurchase(Product);
            string badge = null;

            if (IsFreeChest)
            {
                int left = GameServices.Ads.Remaining(Services.AdPlacements.FreeChest);
                SetPrice(left > 0 ? $"MỞ ({left})" : "HẾT LƯỢT", GameServices.Ads.RemoveAds ? null : _adSprite);
                _buyButton.interactable = GameServices.Ads.CanShow(Services.AdPlacements.FreeChest);
            }
            else if (Product.IsIap)
            {
                SetPrice(canBuy ? GameServices.Iap.PriceOf(Product) : "ĐÃ MUA", null);
                _buyButton.interactable = canBuy;
                if (canBuy && Product.firstPurchaseMultiplier > 1f && shop.PurchaseCount(Product.id) == 0)
                    badge = $"x{Product.firstPurchaseMultiplier:0} LẦN ĐẦU";
            }
            else
            {
                SetPrice(Product.gemCost.ToString(), _gemSprite);
                // Vẫn cho bấm khi thiếu kim cương: bấm vào sẽ báo "không đủ" — rõ hơn nút xám không lý do.
                _buyButton.interactable = canBuy;
            }

            if (!canBuy && !IsFreeChest) badge = "ĐÃ MUA";
            if (_badge != null)
            {
                _badge.SetActive(badge != null);
                if (badge != null) _badgeText.text = badge;
            }
        }

        private void SetPrice(string text, Sprite icon)
        {
            _priceText.text = text;
            if (_priceIcon != null)
            {
                _priceIcon.sprite = icon;
                _priceIcon.gameObject.SetActive(icon != null);
            }
        }
    }
}
