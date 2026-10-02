# Phase 11A — Cửa hàng, IAP và quảng cáo có thưởng (giả lập, chưa cần tài khoản)

Ngày: 2026-10-02
Trạng thái: đã duyệt (11A trước, 11B gắn SDK thật; quảng cáo LevelPlay; giá theo mức phổ biến; hồi sinh cần quảng cáo/kim cương)
Liên quan: `Docs/project-plan.md` Phase 11, mục kinh tế (gói ra mắt, kim cương)

## 1. Mục tiêu
Toàn bộ luồng mua bán + quảng cáo chạy được trong Editor/bản dev với cửa hàng và quảng cáo giả lập, kiến trúc sẵn để 11B chỉ
thêm 2 lớp (`UnityIapStore`, `LevelPlayAdProvider`) mà không sửa phần còn lại.

## 2. Dữ liệu
- `ShopProductData` (SO): `id` (ổn định, lưu vào save), `kind` (Gems, Bundle, RemoveAds, Gold, Chest), `storeProductId`,
  `priceLabel` (hiển thị tới khi store trả giá thật), `gemCost` (gói mua bằng kim cương), `contents` (tiền tệ/nguyên liệu/mảnh: id + số;
  trang bị: itemId + độ hiếm), `chestTable` (LootTable, chỉ rương), `purchaseLimit` (0 = không giới hạn), `firstPurchaseMultiplier`.
- `ShopCatalog` (Resources): danh sách theo mục hiển thị.
- Gói: Kim cương 0.99$/80, 4.99$/450, 9.99$/1000, 19.99$/2200, 49.99$/6000 (lần đầu ×2); Khởi đầu 1.99$ (300 kim cương, 5000 vàng,
  30 mảnh Laser, 1 Giáp Sắt Hiếm; mua 1 lần); Xóa quảng cáo 2.99$ (1 lần); Vàng: 100 kim cương → 2000 vàng, 450 → 10000;
  Rương thường 60 kim cương, Rương cao cấp 280 kim cương; rương miễn phí mỗi ngày bằng quảng cáo.
- Tỷ lệ rơi của rương tính bằng hàm thuần từ `LootTable` (`LootRules.Odds`) — luôn khớp đúng thứ được quay.

## 3. Dịch vụ
- `IStore`: `Initialize(products, onReady)`, `Purchase(productId)`, `Restore()`, `Confirm(transactionId)`, sự kiện
  `OnPurchased(StorePurchase{transactionId, productId, receipt})`, `OnFailed(productId, reason)`.
  `FakeStore` (11A): giao dịch lưu vào PlayerPrefs tới khi game xác nhận — tắt game giữa chừng thì lần mở sau giao lại như store thật.
  Chế độ kết quả (thành công/thất bại/hủy) chỉnh ở DevMenu.
- `IReceiptValidator` (11A: `FakeReceiptValidator` chấp nhận biên lai của FakeStore).
- `IAPService`: mua → xác minh → trao (`ShopService.Grant`) + ghi `transactionId` vào `profile.processedTransactions` → lưu save →
  `Confirm`. Giao dịch đã có trong danh sách thì chỉ xác nhận, không trao lại (idempotent). `Restore` trao lại gói mua-1-lần chưa có.
- `ShopService`: mua bằng kim cương qua `EconomyService.TrySpend` (trả bool), mở rương (`LootRules.Roll`), trao nội dung
  (tiền tệ/nguyên liệu → `EconomyService`, trang bị → `InventoryService`), đếm `purchaseCounts`, cờ `removeAds`.
- `AdsService` + `IAdProvider` (`FakeAdProvider`: lớp phủ đếm ngược 3 s, có nút đóng sớm = không thưởng). Vị trí và giới hạn/ngày (UTC):
  `double_reward` 5, `revive` 3, `free_chest` 2. Đã mua Xóa quảng cáo → thưởng ngay không cần xem. Hàm thuần `AdRules`.
- Hồ sơ v6: `processedTransactions` (list), `purchaseCounts` (id → số lần), `removeAds`, `adViews` (vị trí → số lần), `adDay` (yyyyMMdd UTC),
  `consent` (−1 chưa hỏi, 0 không, 1 có). Migration v5→v6.

## 4. Giao diện & luật
- Tab Cửa hàng (Hub, mở ở level 5): các mục Ưu đãi (Khởi đầu, Xóa quảng cáo), Kim cương, Rương (+ nút Tỷ lệ, rương miễn phí), Vàng;
  nút Khôi phục giao dịch; popup kết quả (danh sách vật phẩm nhận). Gói đã mua hết lượt hiện "ĐÃ MUA".
- Màn kết quả: nút "Nhân đôi thưởng" (quảng cáo `double_reward`; nhân đôi vàng của lượt đó, 1 lần mỗi màn).
- Hồi sinh: 1 lần/màn, cần xem quảng cáo (`revive`) hoặc 20 kim cương; không đủ cả hai thì chỉ có "Bỏ cuộc".
- Đồng ý quyền riêng tư: lần đầu vào Hub hiện popup (Đồng ý / Không), đổi lại trong Cài đặt; lưu `consent` cho 11B truyền vào LevelPlay.
- Bot `-autoplay`: quảng cáo giả lập 0 s.

## 5. Kiểm tra
Unit test: thưởng lần đầu, giới hạn mua, AdRules (đếm/ngày, sang ngày mới đặt lại), Odds (tổng xác suất = 1), IAP idempotent
(trao 1 lần khi store giao lại), Restore, mua bằng kim cương không đủ tiền → false, migration v6. Chơi thử Editor: mua từng loại gói,
tắt Play giữa giao dịch → mở lại được trao bù, rương + tỷ lệ, quảng cáo hồi sinh/nhân đôi, consent.
