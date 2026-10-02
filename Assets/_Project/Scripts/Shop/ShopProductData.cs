using System;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using UnityEngine;

namespace ArenaSurvival.Shop
{
    public enum ShopProductKind
    {
        Gems,       // IAP: kim cương
        Bundle,     // IAP: gói nhiều thứ (gói khởi đầu, gói theo mốc)
        RemoveAds,  // IAP: xóa quảng cáo (mua 1 lần, khôi phục được)
        Gold,       // mua bằng kim cương
        Chest,      // mua bằng kim cương, quay LootTable
    }

    /// <summary>1 dòng nội dung gói: tiền tệ/nguyên liệu/mảnh (currencyId + amount) hoặc 1 món trang bị (itemId + độ hiếm).</summary>
    [Serializable]
    public class ShopContent
    {
        public string currencyId;
        public long amount;
        public ItemData item;
        public Rarity rarity;
    }

    /// <summary>
    /// 1 sản phẩm trong cửa hàng. id là thứ lưu vào save (số lần đã mua) — đừng đổi id của gói đã phát hành.
    /// storeProductId là mã khai báo trên Google Play/App Store (11B). Giá hiển thị tạm tới khi store trả giá thật theo vùng.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Shop Product", fileName = "Shop_")]
    public class ShopProductData : ScriptableObject
    {
        public string id;
        public ShopProductKind kind;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Giá")]
        [Tooltip("Mã sản phẩm trên store (chỉ gói IAP).")]
        public string storeProductId;
        [Tooltip("Giá hiển thị khi store chưa trả giá thật (vd \"0,99 $\").")]
        public string priceLabel;
        [Tooltip("Giá bằng kim cương (gói Gold/Chest).")]
        public long gemCost;

        [Header("Nội dung")]
        public ShopContent[] contents = new ShopContent[0];
        [Tooltip("Chỉ rương: bảng quay khi mở.")]
        public LootTable chestTable;

        [Header("Giới hạn")]
        [Tooltip("Số lần mua tối đa mỗi tài khoản (0 = không giới hạn).")]
        [Min(0)] public int purchaseLimit;
        [Tooltip("Lần mua đầu nhân nội dung tiền tệ lên (gói kim cương: ×2). 1 = không thưởng thêm.")]
        [Min(1f)] public float firstPurchaseMultiplier = 1f;

        public bool IsIap => kind == ShopProductKind.Gems || kind == ShopProductKind.Bundle || kind == ShopProductKind.RemoveAds;
    }
}
