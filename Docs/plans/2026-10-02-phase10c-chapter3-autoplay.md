# Phase 10C — Chương 3 + bot tự chơi + cân bằng — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chương 3 "Căn cứ" chơi được (Lính Khiên, Mưa Pháo, 2 boss), bot `-autoplay` chơi 30 màn từ tài khoản mới, cân bằng theo nhật ký.
**Architecture:** `EnemyBehaviour.ModifyIncomingDamage` + `Enemy.TakeHit`; `ArtilleryAttack` + pool `ArtilleryShell` trong `EnemyProjectiles`;
nội dung qua CSV (10A); bot trong `DevTools/Autoplay/` (save riêng, `GameManager.PlayingTimeScale`, `PlayerController.MoveOverride`).
**Spec:** `Docs/specs/2026-10-02-phase10c-chapter3-autoplay-design.md`

## Global Constraints
- Không tự commit (trừ khi người dùng yêu cầu); sửa scene/prefab qua Editor (`isDirty`); scene sạch trước `run_tests`.
- Không cấp phát trong vòng lặp màn chơi (bot được phép cấp phát ít vì chỉ chạy ở build dev, nhưng dùng lại buffer).
- Comment tiếng Việt giải thích vì sao. Sao lưu/khôi phục save khi chơi thử trong Editor. Bot không bao giờ ghi vào `save.json`.

### Task 1 (TDD): Quy tắc + khiên + Mưa Pháo
- [x] Test `ShieldBlocks`, `InBlast` → `EnemyRules`; `EnemyBehaviour.ModifyIncomingDamage`, `Enemy.TakeHit`, `Projectile` truyền hướng;
  `DamageNumberSource` số xám khi bị chặn; `ShieldBehaviour`; `ArtilleryShell` + `EnemyProjectiles.PlaceShell`; `ArtilleryAttack`.
### Task 2: Asset & scene Chương 3
- [x] Sprite khiên, prefab vòng pháo, con `Shield` trong prefab `Enemy`, pool pháo trong `Game.unity`; tile + `Arena_Base.prefab` (render soi tile).
### Task 3: Nội dung Chương 3
- [x] Asset hành vi/đòn/BossBehaviour Fortress/General, bảng rơi C3; CSV (enemies/waves/stages c3, number 21–30); nhập ×2;
  `Chapter_C3.arenaPrefab`; mô phỏng → lực chiến đề xuất tạm.
### Task 4 (TDD): Bot
- [x] Test `AutoplayRules.PickBest`, `BotSteering`, `AutoplayPaths.SavePath` → `GameManager.PlayingTimeScale`, `PlayerController.MoveOverride`,
  `GameServices` chọn save autoplay, `AutoplayRunner` (khởi chạy, vòng Hub ↔ Game), `AutoplayHub`, `BotPilot`, nhật ký CSV + tóm tắt.
### Task 5: Chơi thử Chương 3 trong Editor + chạy bot lần đầu
- [x] Lính Khiên, Mưa Pháo, 3-5, 3-10, nền Hub; build dev; bot chạy 30 màn; kiểm tra save thật không đổi.
### Task 6: Cân bằng
- [x] Đọc nhật ký, chỉnh CSV/đề xuất, chạy lại bot 2–3 vòng; `Docs/Balance/playtest-report.md`.
### Task 7: Kiểm tra, bàn giao
- [x] Missing Script, toàn bộ test, benchmark `-stage 30`, dọn Temp, CLAUDE.md/plan/project-plan/asset-licenses, báo cáo.

## Kết quả (2026-10-02)
- Test 274/274, 0 Missing Script, CSV nhập lại "không đổi 151". Bot v13 thắng cả 30 màn (55 lượt, ~199 phút) — chi tiết `Docs/Balance/playtest-report.md`.
- Ngoài kế hoạch (người dùng duyệt giữa chừng): luật đợt cuối mới, kỹ năng trong màn có cấp (spec `2026-10-02-phase10c-run-skills-design.md`), Player mass 10, độ khó +9%/màn.
- Benchmark màn 30, 300 quái, mọi kỹ năng tối đa: Main Thread 2.39 ms, GC TB 4 B/frame (đỉnh 3.4 KB — để 10D tìm nguồn).

