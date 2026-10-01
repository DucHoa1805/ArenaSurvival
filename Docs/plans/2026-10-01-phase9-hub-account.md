# Phase 9 — Hub và level tài khoản — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** level tài khoản (XP, thưởng, mở khóa) + Hub (thanh trên, 5 tab, chọn màn, chấm đỏ, tiền bay, Safe Area) thay menu chính.
**Architecture:** `AccountRules` (hàm thuần) + `AccountService` (Meta, `GameServices.Account`, level suy ra từ `accountXp`);
Hub = `HubController` + các view nhỏ; dùng lại `TalentPanel`/`EquipmentPanel` ở chế độ nhúng.
**Spec:** `Docs/specs/2026-10-01-phase9-hub-account-design.md`

## Global Constraints
- Không tự commit; sửa scene qua Editor, kiểm tra `isDirty`, scene sạch trước `run_tests`; sửa UI tại chỗ khi có thể.
- Mọi thay đổi tiền tệ qua `EconomyService`. Không đổi cấu trúc save (level suy ra từ XP).
- Comment tiếng Việt giải thích vì sao.

### Task 1 (TDD): AccountRules + AccountLevelTable + AccountService + nối StageProgressService
- [ ] Test (`AccountRulesTests`, `AccountServiceTests`, cộng XP qua Apply) → code → đạt.

### Task 2 (TDD): RedDotRules
- [ ] Test (Talent đủ vàng; vũ khí mở khóa/lên sao được; ô trống có món đúng ô) → code → đạt.

### Task 3: Script Hub
- [ ] `HubController`, `HubTopBar`, `HubTabBar`/`HubTab`, `BattleTab`/`StageNodeView`, `EmbeddedPanel`, `LevelUpPopup`, `RewardFly`,
  `HubToast`, `SafeAreaFitter`; `StageData.recommendedPower`; màn kết quả hiện "Lên cấp N!" → biên dịch 0 lỗi.

### Task 4: Nội dung + dựng Hub trong MainMenu.unity
- [ ] Icon (kim cương, bánh răng, ổ khóa, cửa hàng, hộp quà, ba lô); `AccountLevelTable` (Resources); `recommendedPower` 3 màn;
  dựng Hub, nhúng TalentPanel/EquipmentPanel, chuyển bảng Cài đặt + nút Thoát; gỡ khung menu cũ + `MainMenu.cs`; SafeArea cho HUD.

### Task 5: Kiểm tra, bàn giao
- [ ] Chơi thử theo spec 6.3 (sao lưu/khôi phục save), Missing Script, toàn bộ test, benchmark, dọn Temp, CLAUDE.md/plan/licenses, báo cáo + gợi ý commit.
