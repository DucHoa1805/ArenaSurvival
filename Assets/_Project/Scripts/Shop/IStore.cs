using System;
using System.Collections.Generic;

namespace ArenaSurvival.Shop
{
    /// <summary>1 giao dịch store giao cho game (mua mới, giao lại giao dịch dở dang, hoặc khôi phục).</summary>
    public struct StorePurchase
    {
        public string transactionId;
        public string storeProductId;
        public string receipt;
    }

    public struct StoreProductInfo
    {
        public string storeProductId;
        /// <summary>Mua 1 lần (xóa quảng cáo, gói khởi đầu): store nhớ và khôi phục được khi cài lại.</summary>
        public bool nonConsumable;
    }

    /// <summary>
    /// Cửa hàng ứng dụng (Google Play/App Store qua Unity IAP ở 11B; FakeStore ở 11A). Game không biết store cụ thể nào.
    /// Hợp đồng: giao dịch chưa được Confirm thì store giao lại ở lần khởi tạo sau — game tắt giữa chừng vẫn được trao bù.
    /// </summary>
    public interface IStore
    {
        bool IsReady { get; }
        event Action<StorePurchase> OnPurchased;
        event Action<string, string> OnPurchaseFailed; // (storeProductId, lý do: "cancelled", "failed", "unavailable"...)

        void Initialize(IReadOnlyList<StoreProductInfo> products);
        void Purchase(string storeProductId);
        void Restore();
        void Confirm(string transactionId);
        /// <summary>Giá theo vùng do store trả; null khi chưa có (hiện priceLabel trong dữ liệu).</summary>
        string GetLocalizedPrice(string storeProductId);
    }

    /// <summary>Xác minh biên lai trước khi trao vật phẩm (11B: Google Play qua máy chủ/Cloud Code).</summary>
    public interface IReceiptValidator
    {
        bool Validate(StorePurchase purchase);
    }
}
