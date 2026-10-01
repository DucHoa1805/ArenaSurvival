# Cấu trúc lại dự án — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đưa code và asset về cấu trúc của kế hoạch thương mại mà game vẫn chạy y hệt.

**Architecture:** Di chuyển file qua Unity Editor (`AssetDatabase.MoveAsset`, chạy bằng unity-cli `eval_file`) để giữ GUID; đổi namespace bằng sửa văn bản file `.cs`; kiểm tra bằng biên dịch, unit test, quét Missing Script và chơi thử.

**Tech Stack:** Unity 6000.3.13f1, unity-cli (`C:\Users\Admin\AppData\Local\Unity\bin\unity.exe`), Python (sửa văn bản), Git Bash.

**Spec:** `Docs/specs/2026-10-01-restructure-design.md`

## Global Constraints

- Không đổi logic, không đổi tên class, không thêm tính năng.
- Không di chuyển file trong `Assets/` bằng shell hay `git mv` khi Editor đang mở; chỉ qua `AssetDatabase`.
- Kiểm tra `scene.isDirty` trước khi mở/lưu scene; dirty thì dừng và hỏi người dùng.
- **Không tự `git commit`**: bước cuối mỗi task là kiểm tra, cuối kế hoạch đưa gợi ý commit.
- Mọi lệnh unity-cli kèm `--caller plugin --skill unity-cli`.

---

### Task 1: Di chuyển script và đổi namespace

**Files (di chuyển qua Editor, GUID giữ nguyên):**
- `Scripts/Progression/*.cs` (7 file) → `Scripts/RunUpgrades/`
- `Scripts/Enemies/EnemySpawner.cs`, `Scripts/Enemies/WaveData.cs`, `Scripts/Core/RunStats.cs` → `Scripts/Stages/`
- `Scripts/Combat/Playerweapon.cs` → `Scripts/Weapons/PlayerWeapon.cs`
- `Scripts/Combat/OrbitWeapon.cs`, `OrbitWeaponData.cs`, `SawBladeRig.cs`, `WeaponData.cs`, `WeaponDefinition.cs` → `Scripts/Weapons/`
- Xóa thư mục rỗng `Scripts/Progression/`.

**Files (sửa namespace / using):**
- Namespace `ArenaSurvival.Progression` → `ArenaSurvival.RunUpgrades`: 7 file trong `RunUpgrades/`; `using`/tên đầy đủ trong `DevTools/StressTest.cs`, `UI/LevelUpPanel.cs`, `UI/UpgradeOptionButton.cs`, `Tests/EditMode/RandomPickerTests.cs`, `Tests/EditMode/XpCurveTests.cs`.
- Namespace của 6 file vũ khí `ArenaSurvival.Combat` → `ArenaSurvival.Weapons`; thêm `using ArenaSurvival.Combat;` cho file vũ khí dùng `Health`/`Projectile`; thêm `using ArenaSurvival.Weapons;` cho `Player/PlayerStats.cs`, `RunUpgrades/UpgradeData.cs`, `RunUpgrades/UpgradeManager.cs`, `DevTools/StressTest.cs`, `Tests/EditMode/WeaponSpreadTests.cs`.
- Namespace của `EnemySpawner.cs`, `WaveData.cs` `ArenaSurvival.Enemies` → `ArenaSurvival.Stages` (thêm `using ArenaSurvival.Enemies;`); `RunStats.cs` `ArenaSurvival.Core` → `ArenaSurvival.Stages` (thêm `using ArenaSurvival.Core;`).
- Người dùng `RunStats`/`EnemySpawner`: `Save/HighScoreTracker.cs`, `UI/GameOverPanel.cs`, `UI/HudController.cs`, `DevTools/StressTest.cs`, `DevTools/PerfRecorder.cs` (đổi `Enemies.EnemySpawner` → `Stages.EnemySpawner`).

- [ ] **Step 1: Di chuyển bằng Editor**

Script `move_scripts.cs` (chạy bằng `eval_file`):

```csharp
var moves = new (string from, string to)[] {
    ("Assets/_Project/Scripts/Combat/Playerweapon.cs", "Assets/_Project/Scripts/Weapons/PlayerWeapon.cs"),
    ("Assets/_Project/Scripts/Combat/OrbitWeapon.cs", "Assets/_Project/Scripts/Weapons/OrbitWeapon.cs"),
    ("Assets/_Project/Scripts/Combat/OrbitWeaponData.cs", "Assets/_Project/Scripts/Weapons/OrbitWeaponData.cs"),
    ("Assets/_Project/Scripts/Combat/SawBladeRig.cs", "Assets/_Project/Scripts/Weapons/SawBladeRig.cs"),
    ("Assets/_Project/Scripts/Combat/WeaponData.cs", "Assets/_Project/Scripts/Weapons/WeaponData.cs"),
    ("Assets/_Project/Scripts/Combat/WeaponDefinition.cs", "Assets/_Project/Scripts/Weapons/WeaponDefinition.cs"),
    ("Assets/_Project/Scripts/Enemies/EnemySpawner.cs", "Assets/_Project/Scripts/Stages/EnemySpawner.cs"),
    ("Assets/_Project/Scripts/Enemies/WaveData.cs", "Assets/_Project/Scripts/Stages/WaveData.cs"),
    ("Assets/_Project/Scripts/Core/RunStats.cs", "Assets/_Project/Scripts/Stages/RunStats.cs"),
    ("Assets/_Project/Scripts/Progression/ExperienceSystem.cs", "Assets/_Project/Scripts/RunUpgrades/ExperienceSystem.cs"),
    ("Assets/_Project/Scripts/Progression/RandomPicker.cs", "Assets/_Project/Scripts/RunUpgrades/RandomPicker.cs"),
    ("Assets/_Project/Scripts/Progression/UpgradeData.cs", "Assets/_Project/Scripts/RunUpgrades/UpgradeData.cs"),
    ("Assets/_Project/Scripts/Progression/UpgradeManager.cs", "Assets/_Project/Scripts/RunUpgrades/UpgradeManager.cs"),
    ("Assets/_Project/Scripts/Progression/XpCurve.cs", "Assets/_Project/Scripts/RunUpgrades/XpCurve.cs"),
    ("Assets/_Project/Scripts/Progression/XpOrb.cs", "Assets/_Project/Scripts/RunUpgrades/XpOrb.cs"),
    ("Assets/_Project/Scripts/Progression/XpOrbSpawner.cs", "Assets/_Project/Scripts/RunUpgrades/XpOrbSpawner.cs"),
};
foreach (var f in new[]{"RunUpgrades","Stages","Weapons"})
    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/_Project/Scripts/" + f))
        UnityEditor.AssetDatabase.CreateFolder("Assets/_Project/Scripts", f);
var sb = new System.Text.StringBuilder();
foreach (var (from, to) in moves) {
    string err = UnityEditor.AssetDatabase.MoveAsset(from, to);
    sb.AppendLine(string.IsNullOrEmpty(err) ? $"OK {to}" : $"FAIL {from}: {err}");
}
return sb.ToString();
```

Kết quả mong đợi: 16 dòng `OK`, không dòng `FAIL`.

- [ ] **Step 2: Đổi namespace bằng script Python** (thay thế chính xác theo bảng ở trên, giữ nguyên kiểu xuống dòng của từng file).

- [ ] **Step 3: Biên dịch** — `recompile`, đọc `console`. Mong đợi `compilationFailed: false`, 0 lỗi. Có lỗi `CS0246` (không tìm thấy kiểu) thì thêm `using` còn thiếu vào đúng file, biên dịch lại.

- [ ] **Step 4: Unit test** — `run_tests --mode EditMode`. Mong đợi 40/40 đạt.

---

### Task 2: Di chuyển scene và asset dữ liệu, dọn file rác

**Files:**
- `Assets/Scenes/MainMenu.unity`, `Game.unity` → `Assets/_Project/Scenes/`; xóa `Assets/Scenes/` rỗng.
- `Data/Enemy_*` → `Data/Enemies/`; `Data/Weapon_*`, `Data/Orbit_SawBlade` → `Data/Weapons/`; `Data/Upgrade_*` → `Data/RunUpgrades/`; `Data/Wave_*` → `Data/Stages/`; `Data/SoundBank` → `Data/Audio/`.
- Xóa `Audio/Impact/desktop.ini`, `Audio/Digital/desktop.ini`.

- [ ] **Step 1:** Kiểm tra scene đang mở không dirty (dirty thì dừng, hỏi người dùng).
- [ ] **Step 2:** Di chuyển bằng `AssetDatabase.MoveAsset` (giống Task 1), xóa `desktop.ini` bằng `AssetDatabase.DeleteAsset`.
- [ ] **Step 3: Kiểm tra Build Settings** — `EditorBuildSettings.scenes` phải là `[0] Assets/_Project/Scenes/MainMenu.unity`, `[1] Assets/_Project/Scenes/Game.unity`, cả hai enabled.
- [ ] **Step 4: Quét Missing Script** — với mọi prefab trong `Assets/_Project/Prefabs` và mọi GameObject trong 2 scene (mở lần lượt bằng `EditorSceneManager.OpenScene`, chỉ đọc), cộng `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount`. Mong đợi tổng = 0.
- [ ] **Step 5: Kiểm tra ScriptableObject** — mọi asset trong `Data/` load bằng `AssetDatabase.LoadAssetAtPath<ScriptableObject>` khác `null` và in ra kiểu. Mong đợi 19 asset, đúng kiểu (`EnemyData`, `WeaponData`, `OrbitWeaponData`, `UpgradeData`, `WaveData`, `SoundBank`).

---

### Task 3: Chơi thử toàn vòng

- [ ] **Step 1:** Mở `MainMenu.unity`, `editor_play`, eval `Application.runInBackground = true`.
- [ ] **Step 2:** Bấm "Chơi" (gọi `onClick` của nút Play), chờ vào Game, chụp `capture_game_view`.
- [ ] **Step 3:** Chờ tới bảng lên cấp, chụp; chọn 1 nâng cấp (gọi `onClick` của `UpgradeOptionButton` đầu tiên); kiểm tra trạng thái về `Playing`.
- [ ] **Step 4:** Cho Player chết (gọi `Health.TakeDamage` lượng lớn trên Player), chụp màn Game Over.
- [ ] **Step 5:** `editor_stop`; console không có lỗi; xóa ảnh trong `Assets/Temp` và thư mục `Assets/Temp`.

---

### Task 4: Tài liệu

**Files:**
- Move (shell, ngoài `Assets/`): `unity-game-project-plan.md` → `Docs/project-plan.md`; `DESIGN.md` → `Docs/archive/core-design-v1.md` (thêm dòng ghi chú ở đầu).
- Create: `Docs/asset-licenses.md` (bảng ở spec mục 7.2).
- Modify: `CLAUDE.md` (viết lại theo spec mục 7.3).
- Modify: `Docs/project-plan.md` — chỉ tick 5 ô trong mục "Phase 0–5: Cần rà lại để tái sử dụng" (giữ ô "`WaveData` cũ trở thành một phần của `StageData`" và ô "Main Menu cũ sẽ được thay..." chưa tick vì thuộc Phase 6/7/9).
- Modify: bộ nhớ Claude `arenasurvival-internship-project.md` + `MEMORY.md`.

- [ ] **Step 1:** Di chuyển 2 file tài liệu, thêm ghi chú đầu `core-design-v1.md`.
- [ ] **Step 2:** Viết `Docs/asset-licenses.md`.
- [ ] **Step 3:** Viết lại `CLAUDE.md`.
- [ ] **Step 4:** Tick ô trong plan; kiểm tra bằng `git diff Docs/project-plan.md` chỉ có dòng `- [ ]` → `- [x]` (sau khi so với bản gốc qua rename).
- [ ] **Step 5:** Cập nhật bộ nhớ.

---

### Task 5: Bàn giao

- [ ] **Step 1:** `git status` — chỉ chứa thay đổi của đợt này; liệt kê riêng file Unity tự sinh (nếu có).
- [ ] **Step 2:** Đưa gợi ý commit `chore: cấu trúc lại dự án theo định hướng thương mại` và danh sách file; nhắc file lớn có thể xóa ngoài repo.
