using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvival.Shop
{
    public enum FakeStoreMode { Success, Fail, Cancel, SuccessNoConfirm }

    /// <summary>
    /// Store giả lập (11A, Editor/bản dev): mô phỏng đúng hợp đồng IStore để thử cả luồng trước khi có tài khoản Google Play.
    /// Giao dịch chưa xác nhận + gói mua-1-lần đã sở hữu lưu trong PlayerPrefs (thuộc "store", không thuộc save của game) —
    /// nên xóa hồ sơ game rồi Khôi phục vẫn lấy lại được gói như store thật.
    /// Mode SuccessNoConfirm giả lập game tắt sau khi trả tiền nhưng trước khi trao đồ.
    /// </summary>
    public class FakeStore : IStore
    {
        private const string PendingKey = "fakestore.pending";
        private const string OwnedKey = "fakestore.owned";

        public static FakeStoreMode Mode = FakeStoreMode.Success;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Mode = FakeStoreMode.Success;

        private readonly HashSet<string> _nonConsumables = new HashSet<string>();
        private bool _suppressConfirm;

        public bool IsReady { get; private set; }
        public event Action<StorePurchase> OnPurchased;
        public event Action<string, string> OnPurchaseFailed;

        public void Initialize(IReadOnlyList<StoreProductInfo> products)
        {
            _nonConsumables.Clear();
            foreach (StoreProductInfo p in products) if (p.nonConsumable) _nonConsumables.Add(p.storeProductId);
            IsReady = true;
            // Store thật giao lại giao dịch chưa xác nhận ngay khi khởi tạo.
            foreach (var (tx, product) in ReadPending()) Deliver(tx, product);
        }

        public void Purchase(string storeProductId)
        {
            switch (Mode)
            {
                case FakeStoreMode.Fail: OnPurchaseFailed?.Invoke(storeProductId, "failed"); return;
                case FakeStoreMode.Cancel: OnPurchaseFailed?.Invoke(storeProductId, "cancelled"); return;
            }

            string tx = "fake-" + Guid.NewGuid().ToString("N");
            var pending = ReadPending();
            pending.Add((tx, storeProductId));
            WritePending(pending);
            if (_nonConsumables.Contains(storeProductId)) AddOwned(storeProductId);

            if (Mode == FakeStoreMode.SuccessNoConfirm)
            {
                // Tiền đã trừ (giao dịch nằm trong pending) nhưng game "tắt" trước khi xử lý: không giao ngay.
                Debug.Log($"[FakeStore] {storeProductId}: đã thanh toán, giả lập game tắt trước khi trao — sẽ giao lại lần khởi tạo sau.");
                return;
            }
            Deliver(tx, storeProductId);
        }

        public void Restore()
        {
            foreach (string product in ReadOwned()) Deliver("restore-" + product, product);
        }

        public void Confirm(string transactionId)
        {
            var pending = ReadPending();
            if (pending.RemoveAll(p => p.tx == transactionId) > 0) WritePending(pending);
        }

        public string GetLocalizedPrice(string storeProductId) => null;

        private void Deliver(string tx, string product) =>
            OnPurchased?.Invoke(new StorePurchase { transactionId = tx, storeProductId = product, receipt = "fake:" + product });

        private static List<(string tx, string product)> ReadPending()
        {
            var list = new List<(string, string)>();
            string raw = PlayerPrefs.GetString(PendingKey, "");
            foreach (string part in raw.Split(';'))
            {
                int bar = part.IndexOf('|');
                if (bar > 0) list.Add((part.Substring(0, bar), part.Substring(bar + 1)));
            }
            return list;
        }

        private static void WritePending(List<(string tx, string product)> list)
        {
            var parts = new List<string>();
            foreach (var (tx, product) in list) parts.Add(tx + "|" + product);
            PlayerPrefs.SetString(PendingKey, string.Join(";", parts));
            PlayerPrefs.Save();
        }

        private static List<string> ReadOwned()
        {
            var list = new List<string>();
            foreach (string s in PlayerPrefs.GetString(OwnedKey, "").Split(';')) if (s.Length > 0) list.Add(s);
            return list;
        }

        private static void AddOwned(string product)
        {
            var owned = ReadOwned();
            if (owned.Contains(product)) return;
            owned.Add(product);
            PlayerPrefs.SetString(OwnedKey, string.Join(";", owned));
            PlayerPrefs.Save();
        }

        /// <summary>DevMenu: xóa sạch trạng thái store giả (như đổi tài khoản Google).</summary>
        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(PendingKey);
            PlayerPrefs.DeleteKey(OwnedKey);
            PlayerPrefs.Save();
        }

        public static int PendingCount => ReadPending().Count;
    }

    /// <summary>11A: chấp nhận biên lai do FakeStore phát. 11B thay bằng xác minh Google Play.</summary>
    public class FakeReceiptValidator : IReceiptValidator
    {
        public bool Validate(StorePurchase purchase) => purchase.receipt != null && purchase.receipt.StartsWith("fake:");
    }

    /// <summary>Bản phát hành khi chưa gắn store thật (trước 11B): mọi giao dịch báo "unavailable", không bao giờ trao đồ.</summary>
    public class UnavailableStore : IStore
    {
        public bool IsReady => false;
        public event Action<StorePurchase> OnPurchased { add { } remove { } }
        public event Action<string, string> OnPurchaseFailed;
        public void Initialize(IReadOnlyList<StoreProductInfo> products) { }
        public void Purchase(string storeProductId) => OnPurchaseFailed?.Invoke(storeProductId, "unavailable");
        public void Restore() { }
        public void Confirm(string transactionId) { }
        public string GetLocalizedPrice(string storeProductId) => null;
    }
}
