# Phase 10B — Chương 2 — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chương 2 "Thành phố bỏ hoang" chơi được: sân riêng, 10 màn, biến thể + Sniper, mini-boss/boss mới, bảng rơi C2.
**Architecture:** `ChapterData.arenaPrefab` + `ArenaLoader`; `SniperBehaviour` + tia ngắm `LineRenderer`; `SpiralAttack`; nội dung qua CSV (10A).
**Spec:** `Docs/specs/2026-10-02-phase10b-chapter2-design.md`

## Global Constraints
- Không tự commit (trừ khi người dùng yêu cầu); sửa scene/prefab qua Editor (`isDirty`); scene sạch trước `run_tests`.
- Không cấp phát trong vòng lặp màn chơi. Comment tiếng Việt giải thích vì sao. Sao lưu/khôi phục save khi chơi thử.

### Task 1 (TDD): Quy tắc + ArenaLoader + Sniper + SpiralAttack
- [x] Test `AimLocked`, góc xoắn ốc, `NeedsSwap`, catalog 2 chương → `EnemyRules`, `ArenaLoader`, `ChapterData.arenaPrefab`,
  `EnemyContext.Telegraph`, `SniperBehaviour`, `SpiralAttack` → đạt.
### Task 2: Sân thành phố
- [x] Tile asset mới, `Arena_City.prefab` (dựng bằng script), `ArenaLoader` trong Game + MainMenu, `Telegraph` trong prefab `Enemy`.
### Task 3: Nội dung Chương 2
- [x] Asset hành vi Sniper, đòn Spiral, BossBehaviour Armored/Commander, bảng rơi C2; CSV (enemies/waves/stages); nhập ×2; mô phỏng → chốt lực chiến đề xuất.
### Task 4: Kiểm tra, bàn giao
- [x] Chơi thử (đổi chương ở Hub, sân, Sniper, 2-5, 2-10), Missing Script, toàn bộ test, benchmark `-stage 20`, dọn Temp, CLAUDE.md/plan, báo cáo.

## Kết quả (2026-10-02)
- Test 236/236. Nhập CSV lần 2: "không đổi 102". 0 Missing Script. Benchmark 300 quái màn 20: Main Thread 1.54 ms, GC 0 B/frame
  (lần chạy đầu sau build có 1 frame 1 KB — chạy lại 2 lần đều 0 B, màn 10 cũng 0 B).
- Chơi thử trong Editor: Hub ◀ ▶ đổi chương; nền Hub thành phố khi màn nên chơi tiếp thuộc C2; 2-2 tia ngắm Sniper (vạch đỏ mờ dần, chốt rồi bắn);
  2-5 hai pha; 2-10 ba pha, gọi Kamikaze, thắng màn: 3 sao, ×2 lần đầu, 20 mảnh Phóng lựu, lõi boss ×3.
- Lực chiến đề xuất C2 (mô phỏng): 565, 605, 645, 685, 800, 760, 800, 835, 870, 1115 → màn thường 1.7–2.3 lượt, 2-5 2.5, 2-10 3.9; 20 màn ~108 phút.
- Lỗi phát hiện khi chơi thử và đã sửa: cột `number` của C2 ghi 1–10 (phải là số màn toàn cục 11–20) → boss chỉ có hệ số độ khó của màn 5.

