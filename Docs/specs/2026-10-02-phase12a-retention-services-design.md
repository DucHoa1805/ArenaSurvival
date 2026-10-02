# Phase 12A — Giữ chân người chơi + dịch vụ (bản cục bộ) — Thiết kế

Ngày: 2026-10-02. Người dùng đã chọn: 12A trước (offline, interface + bản cục bộ), 12B sau (nối Unity Gaming Services);
backend = UGS; nhiệm vụ ngày + quà 7 ngày nằm trong tab **Nhiệm vụ** (thay tab Sự kiện đang khóa).

## Mục tiêu

- Người chơi có lý do quay lại mỗi ngày: 5 nhiệm vụ ngày + 3 mốc thưởng, quà đăng nhập 7 ngày.
- Có số liệu để ra quyết định: `AnalyticsService` ghi sự kiện màn/tiền tệ/nâng cấp/mua/quảng cáo (12A: ghi file cục bộ ở bản dev).
- Chỉnh số liệu không cần build lại: `RemoteConfigService` + `Tuning` (12A: đọc file `remote-config.json` cạnh save ở bản dev).
- Tính năng theo ngày không bị lợi dụng khi chỉnh lùi giờ: `GameClock`.

Không làm ở 12A: cloud save, đăng nhập (12B); thông báo đẩy (Phase 13, cần build Android).

## 1. Đồng hồ (`Services/GameClock`)

- `ITimeSource.UtcNow`. `GameClock` = giờ máy + độ lệch máy chủ (12B đặt `SetServerOffset`), và **không bao giờ lùi** so với
  `profile.lastSeenUtcTicks` (lưu khi ghi save). Chỉnh lùi giờ → đồng hồ đứng ở mốc cũ, không lấy lại lượt/quà.
  Chỉnh tiến giờ chỉ chặn được khi có giờ máy chủ (12B). Hàm thuần `ClockRules.Trusted(device, lastSeen, offset)`.
- `AdsService`, `MissionService` dùng chung đồng hồ này. Ngày = `AdRules.DayKey` (UTC).

## 2. Nhiệm vụ ngày (`Missions/`)

- `MissionData` (SO): `id`, `description` (có `{0}` = mục tiêu), `metric` (`MissionMetric`), `target`, `points`.
- `MissionCatalog` (Resources `MissionCatalog`): `daily[]` (5 nhiệm vụ cố định), `milestones[]` (điểm + phần thưởng tiền tệ + rương tùy chọn),
  `loginRewards[7]`.
- Chỉ số đếm (`MissionMetric`, id chuỗi lưu save): `stage_played`, `stage_won`, `enemy_killed`, `upgrade`, `ad_watched`, `gold_spent`, `chest_opened`.
- Nguồn đếm — **không thêm hook rải rác**:
  - Kết quả màn (GameServices nối `StageEvents.OnStageCompleted`): chơi, thắng, số quái hạ.
  - `EconomyService.OnTransaction` (mới, xem mục 4): tiêu vàng; lý do `*_upgrade`/`*_ascend`/`weapon_star` = 1 lần nâng cấp; `shop_chest`/`ad_chest` = mở rương.
  - `AdsService.OnRewarded(placement)` (mới): xem quảng cáo.
- Nhiệm vụ ngày 1 (tổng 100 điểm): Chơi 3 màn (20), Hạ 300 quái (20), Nâng cấp 2 lần (20), Xem 1 quảng cáo (20), Tiêu 1000 vàng (20).
- Mốc: 40 điểm → 500 vàng; 70 → 15 KC + 1000 vàng; 100 → 25 KC + 1 rương thường.
- Hoàn thành thì bấm **Nhận** để cộng điểm; mốc đủ điểm thì bấm nhận. Sang ngày UTC mới: tiến độ, đã nhận, mốc về 0.
- Hàm thuần `MissionRules`: `Clamp`, `IsComplete`, `Points`, `MilestoneReached`, `NeedsReset`.

## 3. Quà đăng nhập 7 ngày

- Mỗi ngày UTC nhận được 1 quà, theo thứ tự ngày 1→7; **bỏ ngày không mất chuỗi** (thân thiện người chơi bận). Hết ngày 7 thì quay lại ngày 1.
- Quà: N1 500 vàng · N2 10 KC · N3 1000 vàng · N4 20 da + 20 quặng · N5 20 KC · N6 2000 vàng · N7 50 KC + rương cao cấp.
- `MissionRules.LoginIndex(claimCount)`, `CanClaimLogin(lastDay, today)`.

## 4. Tiền tệ có lý do (`EconomyService`)

- `Add`/`TrySpend`/`TrySpendMany` thêm tham số tùy chọn `reason` (hằng số `EconomyReasons`), phát `OnTransaction(CurrencyTransaction)`
  {id, delta, balance, reason}. Gọi cũ không truyền lý do vẫn chạy (`"unknown"`). Analytics lấy "nguồn và nơi tiêu" từ đây.

## 5. Analytics (`Services/AnalyticsService`)

- `IAnalyticsSink.Send(name, params)`. 12A: `LocalAnalyticsSink` (bản dev: nối dòng JSON vào `analytics-log.jsonl` cạnh save),
  `NullAnalyticsSink` (bản phát hành tới khi có UGS ở 12B).
- **Chỉ gửi khi `consent == 1`** (đã đồng ý trong popup quyền riêng tư).
- Sự kiện: `session_start`, `stage_start` (màn, chương, lực chiến, lần đầu?), `stage_end` (thắng, sao, thời gian, quái, hồi sinh, % máu),
  `currency` (id, delta, số dư, lý do), `purchase` (sản phẩm, loại, giá), `ad_reward` (vị trí), `level_up`, `mission_claim`, `login_claim`.
  Không có sự kiện mỗi frame (không ảnh hưởng GC trong màn).
- `StageEvents.OnStageStarted` mới (StageSession phát khi bắt đầu) để ghi `stage_start` mà scene Game không biết tới analytics.

## 6. Remote Config (`Core/Tuning` + `Services/RemoteConfigService`)

- `Tuning` (static, giá trị mặc định = số hiện tại): `difficulty_growth` 0.075, `reward_growth` 0.15, `revive_gem_cost` 20,
  `ad_limit_double_reward` 5, `ad_limit_revive` 3, `ad_limit_free_chest` 2, `stage_gold_multiplier` 1, `mission_reward_multiplier` 1.
  `StageRules`, `AdPlacements`, `RevivePanel`, `StageProgressService`, `MissionService` đọc từ `Tuning` thay cho hằng số.
- `IRemoteConfigProvider.Fetch(callback(dict))`. 12A: `FileRemoteConfigProvider` (bản dev đọc `remote-config.json` cạnh save).
  Giá trị lấy được lưu vào `remote-cache.json` để lần mở offline sau vẫn dùng; khóa lạ/kiểu sai bị bỏ qua (giữ mặc định).
- Áp lúc khởi tạo dịch vụ (trước khi vào màn) — đổi file rồi mở lại game là có giá trị mới, không cần build.

## 7. Save v7

`daily` {day, progress, claimed, milestones}, `login` {lastDay, count}, `lastSeenUtcTicks`. Migration v6→v7 chỉ tăng version (trường mới có mặc định).

## 8. UI

- Tab Sự kiện → **Nhiệm vụ** (bỏ `_comingSoon`, `featureId` `missions`, không có trong bảng level = mở từ đầu), chấm đỏ khi có thứ nhận được.
- Nội dung: hàng quà 7 ngày (ngày đã nhận ✓, hôm nay sáng + nút Nhận), thanh điểm + 3 mốc, danh sách 5 nhiệm vụ (thanh tiến độ, Nhận),
  đồng hồ "Làm mới sau hh:mm:ss". Phần thưởng có rương dùng lại `ShopResultPopup`.
- DevMenu: "Sang ngày mới" (dịch đồng hồ +1 ngày), "+ tiến độ nhiệm vụ", "Nạp lại Remote Config", mở file log analytics.

## Kiểm tra

Unit test: `MissionRules`, `MissionService` (đếm, reset ngày, nhận, mốc, quà đăng nhập, không nhận 2 lần), `ClockRules`,
`Tuning.Apply` (khóa lạ, kiểu sai), `EconomyService.OnTransaction`, `AnalyticsService` (chặn khi chưa đồng ý), migration v7.
Chơi thử Editor: nhiệm vụ tăng sau màn/nâng cấp/quảng cáo, nhận điểm + mốc, sang ngày mới reset, quà 7 ngày, file remote-config đổi độ khó.
