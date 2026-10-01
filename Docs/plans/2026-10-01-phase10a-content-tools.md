# Phase 10A — Công cụ dữ liệu + Chương 1 + 4 quái mới — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CSV → ScriptableObject (menu Editor), mô phỏng tiến trình, Chương 1 đủ 10 màn với Charger/Kamikaze/Splitter/Healer, tab Chiến đấu 10 màn.
**Architecture:** phần đọc/kiểm tra CSV và mô phỏng là hàm thuần trong assembly game (test được); phần ghi asset nằm trong assembly Editor mới
`ArenaSurvival.Editor`. Hành vi quái mới là SO `EnemyBehaviour` + hook `OnDeath`.
**Spec:** `Docs/specs/2026-10-01-phase10a-content-tools-design.md`

## Global Constraints
- Không tự commit; sửa scene/prefab qua Editor (`isDirty`); scene sạch trước `run_tests`; đổi tên asset bằng `MoveAsset`.
- Không cấp phát trong vòng lặp màn chơi (buffer tĩnh, pool có giới hạn). Comment tiếng Việt giải thích vì sao.
- `Enemy_Tank.asset`: công cụ chỉ ghi cột CSV; nêu rõ khi gợi ý commit.

### Task 1 (TDD): CsvReader + BalanceRows
**Files:** `Scripts/Data/CsvReader.cs`, `Scripts/Data/BalanceRows.cs`; test `CsvReaderTests.cs`, `BalanceRowsTests.cs`.
**Produces:** `CsvReader.Parse(string) → List<Dictionary<string,string>>` (ném `CsvException` có số dòng khi lỗi cấu trúc);
`EnemyRow/WaveRow/StageRow.Parse(Dictionary<string,string>, int line, List<string> errors)`; `BalanceRows.SplitList(";")`, `ParseColor`.
- [ ] Test → code → đạt.

### Task 2 (TDD): ChapterData + StageCatalog.chapters
- [ ] Test `ChapterOf`, thứ tự phẳng → `ChapterData`, `StageCatalog.chapters`, `ChapterOf(StageData)` → đạt.

### Task 3 (TDD): ProgressionSim
- [ ] Test (seed cố định ổn định, lực chiến cao → ít lượt, xác suất thắng biên 75%/125%) → `Meta/ProgressionSim.cs` → đạt.

### Task 4 (TDD): 4 hành vi quái + OnDeath + hiệu ứng hồi máu
- [ ] Test `EnemyRules.HealAmount`, `KamikazeHit` → `EnemyBehaviour.OnDeath`, `Enemy.HandleDied` gọi hook, `ChargerBehaviour`,
  `KamikazeBehaviour`, `SplitterBehaviour`, `HealerBehaviour`, `GameEvents.EnemyHealPulse`, `VfxSpawner` vòng xanh → đạt.

### Task 5: Assembly Editor + nhập/xuất/mô phỏng
- [ ] `Editor/ArenaSurvival.Editor.asmdef`, `BalanceImporter`, `BalanceExporter`, `ProgressionSimMenu` → biên dịch 0 lỗi; xuất CSV từ asset hiện có.

### Task 6: Nội dung Chương 1
- [ ] Asset hành vi; viết `enemies.csv`/`waves.csv`/`stages.csv` cho 10 màn; nhập; nhập lần 2 = "không đổi"; mô phỏng → chốt lực chiến đề xuất; VFX hồi máu.

### Task 7: Tab Chiến đấu 10 màn
- [ ] 2 hàng × 5, dấu mini-boss/boss chương, ◀ ▶ đổi chương (ẩn khi 1 chương), tên chương từ `ChapterData`.

### Task 8: Kiểm tra, bàn giao
- [ ] Chơi thử (sao lưu/khôi phục save), Missing Script, toàn bộ test, benchmark `-stage 10`, dọn Temp, CLAUDE.md/plan/licenses, gợi ý commit.
