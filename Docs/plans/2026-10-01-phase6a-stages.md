# Phase 6A — Màn chơi — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay wave vô tận bằng màn chơi có kết thúc (đợt theo thời gian → đợt tinh anh → thắng/thua, hồi sinh, sao, phần thưởng, màn kết quả), chơi nối tiếp 3 màn.

**Architecture:** `StageSession` (MonoBehaviour) điều phối `WaveSpawner`; mọi phép tính ở `StageRules` (hàm thuần, unit test); `GameManager` chỉ thêm trạng thái `RevivePrompt`/`Victory`; UI nghe instance event của `StageSession`.

**Tech Stack:** Unity 6000.3.13f1, C#, NUnit (EditMode), unity-cli `eval_file`, Python (sửa văn bản, tạo PNG).

**Spec:** `Docs/specs/2026-10-01-phase6a-stages-design.md` — đọc cùng kế hoạch; tên class, trường, hằng số và hành vi lấy đúng từ spec.

## Global Constraints

- Không tự `git commit`; cuối kế hoạch đưa gợi ý commit.
- Sửa scene/prefab qua Editor; kiểm tra `isDirty` trước khi lưu/đổi scene; không sửa object trong scene lúc Play qua `SerializedObject`.
- Đổi tên class/file qua `AssetDatabase.MoveAsset` để giữ GUID.
- Sửa UI tại chỗ, giữ phong cách hiện tại.
- Comment tiếng Việt, giải thích vì sao.
- Không tạo delegate mới / không `+=` trong `OnEnable` của object nằm trong pool (bài học hiệu năng).

---

### Task 1: Dữ liệu màn và logic thuần (TDD)

**Files:**
- Create: `Scripts/Stages/StageRules.cs`, `StageData.cs` (gồm `EliteGroup`), `StageCatalog.cs`, `StageSelection.cs`, `StageResult.cs`
- Test: `Tests/EditMode/StageRulesTests.cs`, `Tests/EditMode/StageCatalogTests.cs`

**Interfaces — Produces:**
- `static float StageRules.DifficultyMultiplier(int stageNumber)`
- `static bool[] StageRules.EvaluateStars(bool victory, float hpFraction, float clearTime, float targetTime)`
- `static float StageRules.RewardScale(int stageNumber)`
- `static (int gold, int xp) StageRules.CalculateRewards(int baseGold, int baseXp, int stageNumber, bool victory, float progress)`
- `static float StageRules.Progress(float survivalTime, float plannedDuration)`
- `StageData { string id; string displayName; int stageNumber; WaveData[] waves; EliteGroup[] finalWave; float eliteSpawnInterval; float targetTime; int baseGold; int baseAccountXp; float PlannedDuration }`
- `StageCatalog { StageData[] stages; StageData First; int IndexOf(StageData); StageData GetNext(StageData) }`
- `static class StageSelection { StageData Current; void Select(StageData); void Clear(); }`
- `StageResult` (các trường theo spec 4.6)

- [ ] Step 1: Viết `StageRulesTests` + `StageCatalogTests` theo spec mục 12.1 (test catalog tạo `StageCatalog`/`StageData` bằng `ScriptableObject.CreateInstance`, hủy trong TearDown).
- [ ] Step 2: Chạy test → biên dịch lỗi / FAIL (chưa có class).
- [ ] Step 3: Viết các class ở Files.
- [ ] Step 4: Chạy toàn bộ EditMode test → tất cả PASS.

### Task 2: Luồng màn chơi (code)

**Files:**
- Create: `Scripts/Enemies/EnemyModifiers.cs`, `Scripts/Stages/StageSession.cs`
- Rename (Editor): `Scripts/Stages/EnemySpawner.cs` → `WaveSpawner.cs` (class `WaveSpawner`)
- Modify: `Enemies/Enemy.cs` (`Initialize(EnemyData, EnemyModifiers)`, sát thương theo hệ số, tint), `Combat/Health.cs` (`Revive`), `Core/GameState.cs`, `Core/GameManager.cs`, `Core/GameEvents.cs` (bỏ `WaveStarted` nếu không còn dùng), `DevTools/StressTest.cs`, `DevTools/PerfRecorder.cs`, `DevTools/BenchmarkRunner.cs`

**Interfaces — Produces:**
- `WaveSpawner.SetWave(WaveData, EnemyModifiers)`, `StopSpawning()`, `Spawn(EnemyData, EnemyModifiers)`, `SpawnImmediately(int)`, `int ActiveCount`
- `GameManager.BeginRevivePrompt()`, `Revive()`, `EndStage(bool victory)`; `GameState.RevivePrompt`, `GameState.Victory`
- `Health.Revive(float fraction)`
- `StageSession`: `Stage`, `Catalog`, `CurrentWave`, `TotalWaves`, `IsFinalWave`, `Progress`, `ReviveAvailable`; event `OnWaveStarted(int,int)`, `OnFinalWaveStarted`, `OnReviveOffered`, `OnStageEnded(StageResult)`; `Revive()`, `GiveUp()`, `DebugSkipToFinalWave()`, `PauseProgression(bool)`

- [ ] Step 1: Đổi tên file spawner qua Editor; viết lại `WaveSpawner` theo spec 6.2.
- [ ] Step 2: `EnemyModifiers`, `Enemy.Initialize`, `Health.Revive`, `GameState`, `GameManager` theo spec 6.3–6.4.
- [ ] Step 3: `StageSession` theo spec 6.1.
- [ ] Step 4: DevTools theo spec mục 10.
- [ ] Step 5: Biên dịch → 0 lỗi (UI chưa sửa nên có thể còn lỗi ở `HudController`/`GameOverPanel`/`MainMenu`: sửa tối thiểu cho biên dịch được, phần UI đầy đủ làm ở Task 4).
- [ ] Step 6: Chạy toàn bộ test → PASS.

### Task 3: Dữ liệu mẫu và nối scene

- [ ] Step 1: `Wave_01..03.duration = 60`. Tạo `Data/Stages/Stage_C1_01..03.asset`, `StageCatalog_Main.asset` theo spec mục 11.
- [ ] Step 2: Scene `Game.unity` (kiểm tra `isDirty` trước): đổi tên object `EnemySpawner` → `WaveSpawner`; thêm object `StageSession` (gán catalog, spawner, RunStats, Health Player); gỡ component `HighScoreTracker`; nối `DevTools` (StressTest/PerfRecorder/BenchmarkRunner) tới spawner/session mới. Lưu scene.
- [ ] Step 3: Quét Missing Script trên prefab và 2 scene → 0.

### Task 4: Giao diện, sprite, âm thanh

**Files:**
- Rename (Editor): `UI/GameOverPanel.cs` → `UI/StageResultPanel.cs`
- Create: `UI/RevivePanel.cs`, `Art/UI/icon_star.png`, `Art/UI/icon_coin.png`
- Modify: `UI/HudController.cs`, `UI/MainMenu.cs`, `Audio/SoundBank.cs`, `Audio/AudioManager.cs`, `Data/Audio/SoundBank.asset`, `Docs/asset-licenses.md`
- Delete: `Save/HighScoreTracker.cs`

- [ ] Step 1: Tạo 2 PNG (Python/PIL, 128×128, trong suốt), import dạng Sprite.
- [ ] Step 2: Code UI theo spec mục 7; xóa `HighScoreTracker.cs`; âm thanh theo spec mục 8.
- [ ] Step 3: Biên dịch → 0 lỗi; test → PASS.
- [ ] Step 4: Sửa UI trong scene qua Editor (HUD: chữ đợt + thanh tiến độ + banner; `StageResultPanel`: tiêu đề, hàng 3 sao, thời gian, số quái, phần thưởng, 3 nút; `RevivePanel` mới; MainMenu: tắt `BestPill`, gán catalog). Lưu scene sau khi kiểm tra `isDirty`.

### Task 5: Kiểm tra và bàn giao

- [ ] Step 1: Chơi thử theo spec mục 12.3 (6 bước), chụp ảnh, console 0 lỗi; xóa ảnh tạm.
- [ ] Step 2: Build Development + benchmark 300 quái `-stage 1` → GC trung bình 0 B/frame.
- [ ] Step 3: Cập nhật `CLAUDE.md` (cấu trúc, tiến độ 6A xong, nội dung game) và tick ô Phase 6 tương ứng trong `Docs/project-plan.md`.
- [ ] Step 4: `git status`; gợi ý commit.
