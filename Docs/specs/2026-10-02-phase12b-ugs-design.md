# Phase 12B — Nối Unity Gaming Services — Thiết kế

Ngày: 2026-10-02. Người dùng đã chọn: lệch save thì **hiện popup cho người chơi chọn** (một bên rỗng thì tự lấy bên kia);
bản dev/Editor **mặc định cục bộ**, bật UGS bằng công tắc DevMenu; code trước, hướng dẫn bật dashboard sau.

## Gói

`com.unity.services.authentication` 3.8.0, `com.unity.services.cloudsave` 3.4.1, `com.unity.remote-config` 4.2.5,
`com.unity.services.analytics` 6.3.0 (kéo theo `com.unity.services.core` 1.18).

## Kiến trúc (giữ nguyên interface 11A/12A, chỉ thêm adapter)

- `UgsBootstrap` (static): `UnityServices.InitializeAsync` với môi trường `production` (bản phát hành) hoặc `development` (Editor/dev
  khi bật công tắc) → đăng nhập ẩn danh → lấy giờ máy chủ (header `Date` của 1 yêu cầu HEAD tới máy chủ Unity) → `GameClock.SetServerOffset`.
  Lỗi/mất mạng: game chạy bản cục bộ, `ServicesHost` thử lại sau 60 s và khi app quay lại từ nền.
- `UgsRemoteConfigProvider : IRemoteConfigProvider`: chờ bootstrap, `FetchConfigsAsync`, đọc mọi khóa thành chuỗi → `Tuning`. Lấy không được
  (origin ≠ Remote) → báo null, giữ bản tạm.
- `UgsAnalyticsSink : IAnalyticsSink`: `CustomEvent` + `RecordEvent`. Trước khi bootstrap xong thì xếp hàng tối đa 64 sự kiện.
  Đồng ý (consent == 1) → `EndUserConsent` Granted (hoặc `StartDataCollection` nếu SDK không có khung đồng ý mới); từ chối → Denied.
  `AdsService.OnConsentChanged` báo khi người chơi đổi ý trong Cài đặt.
- Cloud Save: `ICloudStorage` (`UgsCloudStorage` dùng `CloudSaveService.Instance.Data.Player`) + `CloudSyncService`:
  - Khóa `profile` = nguyên chuỗi save (envelope có HMAC, như file), khóa `profile_meta` = tóm tắt {accountXp, số màn đã qua, giờ lưu, syncId}.
  - `syncId` (Guid mỗi lần tải lên). "Bản đám mây máy này đồng bộ lần cuối" lưu ở **PlayerPrefs theo playerId** (trạng thái của lần cài, không phải
    của hồ sơ) → không đổi save, không tạo vòng lưu ↔ tải lên.
  - Quyết định (hàm thuần `CloudSaveRules.Decide`): không có bản mây → tải lên; syncId mây = syncId đã đồng bộ → tải lên; máy rỗng (XP 0, 0 màn)
    mà mây có tiến độ → lấy bản mây; mây rỗng → tải lên; còn lại → **hỏi người chơi**. Chưa quyết định thì không bao giờ tải lên (không đè bản mây).
  - Lấy bản mây: `ProfileService.ReplaceWith` chép vào **cùng object** hồ sơ (dịch vụ đang giữ tham chiếu), gộp quyền lợi đã trả tiền
    (`CloudSaveRules.MergePurchases`: xóa QC = hoặc, mã giao dịch hợp, số lần mua lấy lớn hơn), lưu, nạp lại scene Hub.
  - Tải lên: sau mỗi lần lưu thì đánh dấu, tối đa 1 lần/60 s, và ngay khi app chuyển nền/thoát.
  - Popup chọn bản (`CloudConflictPopup`, trong Hub): 2 cột "Trên máy" / "Đám mây" (cấp, số màn, giờ lưu). Việc lấy bản mây chỉ làm ở Hub.
- Chọn adapter trong `GameServices`: bản phát hành → UGS; Editor/dev → cục bộ, trừ khi `PlayerPrefs dev_use_ugs = 1` (DevMenu, khởi động lại để áp).

## Hạn chế

Đăng nhập ẩn danh gắn với lần cài: gỡ game là mất tài khoản → tới khi liên kết Google Play Games (cần Play Console), Cloud Save chủ yếu là bản
sao lưu trên cùng máy. Analytics 6.x chỉ nhận sự kiện tùy chỉnh đã khai báo trên dashboard (danh sách ở `Docs/ugs-setup.md`).

## Kiểm tra

Unit test: `CloudSaveRules` (mọi nhánh quyết định, gộp mua hàng), envelope save ↔ chuỗi (đúng/sai chữ ký), `ProfileService.ReplaceWith` giữ
object, `CloudSyncService` với kho giả (không tải lên khi chờ chọn, giới hạn 60 s, chọn bản mây/bản máy).
Kết nối thật: sau khi người dùng bật dịch vụ — đăng nhập, Remote Config đổi số, sự kiện hiện trên dashboard, bản mây xuất hiện.
