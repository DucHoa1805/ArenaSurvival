# Phase 12A — Giữ chân + dịch vụ (cục bộ) — Kế hoạch & kết quả

**Spec:** `Docs/specs/2026-10-02-phase12a-retention-services-design.md`

- [x] `EconomyService`: lý do (`EconomyReasons`), `OnTransaction`, `OnSpent`; truyền lý do ở mọi chỗ cộng/trừ.
- [x] `Tuning` (Core) + `RemoteConfigService` (file cục bộ ở bản dev, bản tạm `remote-cache.json`); `StageRules`, `AdPlacements`,
      `RevivePanel`, `StageProgressService` (hệ số vàng), `MissionService` (hệ số thưởng) đọc Tuning.
- [x] `GameClock` (không lùi về trước mốc đã thấy; chỗ cho độ lệch máy chủ ở 12B); `AdsService` dùng chung.
- [x] `Missions/`: dữ liệu, `MissionRules`, `MissionService` (nhiệm vụ ngày, mốc, quà 7 ngày); 5 asset `Mission_*`, `Resources/MissionCatalog`.
- [x] `AnalyticsService` (chỉ khi đồng ý) + `AnalyticsTracker` + `LocalAnalyticsSink`; `StageEvents.OnStageStarted`, `StageResult.waveReached`.
- [x] Save v7 + migration; `ResetToNew` xóa nhiệm vụ/quà (giữ mốc giờ).
- [x] UI: tab Sự kiện → Nhiệm vụ (prefab `UI/LoginDay`, `UI/MissionRow`, 3 mốc, đồng hồ làm mới, popup nhận), chấm đỏ; DevMenu.
- [x] Test 329/329 (24 test mới), 0 Missing Script, build Development thành công.
- [x] Chơi thử Editor: quà ngày 1, nâng Talent → nhiệm vụ nâng cấp + tiêu vàng tăng, nhận 5 nhiệm vụ + 3 mốc (mốc 100 mở rương),
      Remote Config từ file (khóa sai bị bỏ qua), sang ngày mới reset + quà ngày 2, màn 1-1 → stage_start/stage_end + nhiệm vụ chơi/hạ quái.
      Save thật khôi phục, file thử đã xóa.
