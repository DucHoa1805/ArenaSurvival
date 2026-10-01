# Phase 7 — Hồ sơ, save v2, tiền tệ — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** PlayerProfile + SaveService an toàn (tmp/bak/HMAC/migration), EconomyService, StageProgressService áp kết quả màn, scene Boot.

**Architecture:** class C# thuần (test được) + `GameServices` static; Game phát `StageEvents.OnStageCompleted`, Meta áp + lưu.

**Tech Stack:** Unity 6000.3.13f1, Newtonsoft Json (com.unity.nuget.newtonsoft-json), NUnit, unity-cli.

**Spec:** `Docs/specs/2026-10-01-phase7-profile-save-economy-design.md`

## Global Constraints
- Không tự commit; sửa scene qua Editor, kiểm tra `isDirty`, ForceUpdateCanvases + lưu lại sau khi sửa UI.
- Trừ tiền luôn qua `TrySpend` trả `bool`; số dư không âm. Save chỉ lưu ID ổn định.
- Comment tiếng Việt, giải thích vì sao.

### Task 1: Package + Economy (TDD)
- [ ] Thêm `com.unity.nuget.newtonsoft-json` vào manifest (qua Editor `package_add`), xác nhận biên dịch.
- [ ] `Tests/EditMode/EconomyServiceTests.cs` → chạy thấy lỗi → `Economy/CurrencyIds.cs`, `Economy/EconomyService.cs`, `Meta/PlayerProfile.cs` → test đạt.

### Task 2: SaveService (TDD)
- [ ] `Tests/EditMode/SaveServiceTests.cs` (spec 9.1) → lỗi → `Save/SaveService.cs`, `SaveSecurity.cs`, `SaveMigrations.cs`, `SaveLoadResult.cs` → đạt.
- [ ] Gỡ `Save/SaveData.cs`, `Save/SaveSystem.cs`, `Tests/EditMode/SaveSystemTests.cs` (qua `AssetDatabase.DeleteAsset`).

### Task 3: Tiến độ màn + GameServices (TDD)
- [ ] `Tests/EditMode/StageProgressTests.cs` → lỗi → `Meta/StageProgressRules.cs`, `StageGrant.cs`, `ProfileService.cs`, `StageProgressService.cs`, `GameServices.cs`, `ServicesHost.cs`; `Stages/StageEvents.cs`; sửa `StageSession.EndStage` theo thứ tự spec 6 → đạt.

### Task 4: Boot, UI
- [ ] `Core/BootLoader.cs`, `SceneLoader.FirstSceneOverride`, `BenchmarkRunner` hook `BeforeSceneLoad`.
- [ ] Scene `Boot.unity` + Build Settings (Boot 0, MainMenu 1, Game 2).
- [ ] `MainMenu`: ô vàng + "Chơi {màn}"; `StageResultPanel`: số thực nhận + dòng thưởng phụ. Dựng UI tại chỗ, lưu.

### Task 5: Kiểm tra, bàn giao
- [ ] Chơi thử theo spec 9.3 (đọc `save.json` trên đĩa), sửa tay file → mở lại phát hiện.
- [ ] Benchmark GC 0 B/frame; Missing Script = 0.
- [ ] CLAUDE.md, tick ô Phase 7 trong plan, gợi ý commit.
