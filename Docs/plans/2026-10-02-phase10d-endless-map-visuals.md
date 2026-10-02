# Phase 10D — Bản đồ vô tận, nhân vật, hiệu ứng — Kế hoạch & kết quả

**Spec:** `Docs/specs/2026-10-02-phase10d-endless-map-visuals-design.md`

- [x] `GroundRules` (hash, nhiễu, khối) + test; `InfiniteGround` (chunk quanh camera); 3 prefab `Ground_*`; `ChapterData.arenaPrefab` → nền mới.
- [x] Bỏ `Walls` (Game.unity), thay instance `Arena` bằng `Ground_Desert` ở Game + MainMenu.
- [x] `WaveSpawner` vô tận: vòng sinh ngoài màn hình, không kẹp, kéo quái xa lại gần; bot dùng `PlayArea`.
- [x] Nhân vật tự vẽ (`hero.png`) + bóng + vòng sáng + vệt chạy + lóe nòng; vệt đạn (đạn người chơi, lựu, đạn quái).
- [x] Hiệu ứng kỹ năng: lưỡi cưa quầng/vệt + thức tỉnh cấp 6, vòng điện xoay/nảy, lá chắn lấp lánh/chớp.
- [x] Kỹ năng mới Sét lan, Drone hộ vệ (+ icon, asset, UpgradeManager, bot, StressTest).
- [x] Độ khó +7.5%/màn.
- [x] Test 292/292, 0 Missing Script, benchmark màn 30 (300 quái + mọi kỹ năng): Main Thread 2.05 ms, GC TB 5 B/frame.
- [ ] Chạy lại bot sau 10D (người dùng yêu cầu dừng test — để lúc cần số liệu).
