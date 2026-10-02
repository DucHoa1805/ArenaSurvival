using UnityEngine;

namespace ArenaSurvival.Shop
{
    /// <summary>Danh sách sản phẩm cửa hàng theo mục hiển thị. Nằm trong Resources vì dịch vụ tự khởi tạo trước mọi scene.</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Shop Catalog", fileName = "ShopCatalog")]
    public class ShopCatalog : ScriptableObject
    {
        public const string ResourcePath = "ShopCatalog";

        public ShopProductData[] offers = new ShopProductData[0];
        public ShopProductData[] gems = new ShopProductData[0];
        public ShopProductData[] chests = new ShopProductData[0];
        public ShopProductData[] gold = new ShopProductData[0];

        public ShopProductData Find(string id)
        {
            foreach (var group in new[] { offers, gems, chests, gold })
            {
                if (group == null) continue;
                foreach (ShopProductData p in group) if (p != null && p.id == id) return p;
            }
            return null;
        }

        public ShopProductData FindByStoreId(string storeProductId)
        {
            foreach (var group in new[] { offers, gems, chests, gold })
            {
                if (group == null) continue;
                foreach (ShopProductData p in group) if (p != null && p.IsIap && p.storeProductId == storeProductId) return p;
            }
            return null;
        }
    }
}
