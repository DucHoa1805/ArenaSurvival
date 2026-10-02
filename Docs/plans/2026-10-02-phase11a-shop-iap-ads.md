# Phase 11A — Cửa hàng, IAP, quảng cáo (giả lập) — Kế hoạch & kết quả

**Spec:** `Docs/specs/2026-10-02-phase11a-shop-iap-ads-design.md`

- [x] Hàm thuần + test: `ShopRules`, `AdRules`, `LootOddsRules`; IAP không trao trùng, biên lai sai, khôi phục, giới hạn mua; migration v6.
- [x] Dữ liệu: `ShopProductData`, `ShopCatalog` (Resources), 11 sản phẩm, 2 bảng rơi rương.
- [x] Dịch vụ: `ShopService`, `IStore` + `FakeStore` (PlayerPrefs), `IAPService`, `AdsService` + `FakeAdProvider`; nối `GameServices`.
- [x] UI: tab Cửa hàng (prefab `UI/ShopCard`), popup xác nhận/kết quả/tỷ lệ; popup quyền riêng tư + nút Cài đặt;
      hồi sinh bằng QC/20 kim cương; nút Nhân đôi vàng; gộp Chơi lại/Về Hub 1 hàng; DevMenu điều khiển store giả.
- [x] Chơi thử Editor: mua kim cương (x2 lần đầu), gói khởi đầu, chặn mua lần 2, giao dịch dở dang được trao bù khi khởi tạo lại,
      thất bại/hủy không đổi gì, xóa QC + khôi phục sau khi xóa hồ sơ, rương (xác nhận), tỷ lệ, rương miễn phí bằng QC, hồi sinh bằng QC,
      nhân đôi vàng. Save thật khôi phục, store giả đã xóa.
- [x] Test 305/305, 0 Missing Script, build Development thành công.
