# Phase 6A — Màn chơi có đầu có cuối

Ngày: 2026-10-01
Trạng thái: chờ duyệt
Liên quan: `Docs/project-plan.md` (Phase 6, Phần D1), `CLAUDE.md` (nguyên tắc kiến trúc)

## 1. Mục tiêu

Thay chế độ wave vô tận bằng **màn chơi có điểm kết thúc**: các đợt quái theo thời gian, đợt cuối gồm quái tinh anh phải diệt hết, thắng/thua, hồi sinh 1 lần, chấm sao, tính phần thưởng, màn kết quả. Chơi được 3 màn liền nhau với độ khó tăng rõ rệt.

Phase 6 được chia hai đợt; spec này là **6A**. **6B** (tách hành vi quái, boss nhiều pha, mini-boss, đạn của quái, thanh máu boss) có spec riêng, sẽ thay đợt cuối bằng boss mà không phải đổi cấu trúc của 6A.

## 2. Ngoài phạm vi

- Boss, mini-boss, hành vi quái mới, đạn của quái (6B).
- Lưu tiến độ màn, sao, mở khóa màn; thưởng lần đầu cao hơn và giảm thưởng khi chơi lại (Phase 7, cần save v2).
- Cộng phần thưởng vào ví (Phase 7, `EconomyService`): 6A chỉ tính và hiển thị.
- Màn chọn màn, Hub (Phase 9).
- Hồi sinh tốn kim cương / quảng cáo (Phase 11): 6A hồi sinh miễn phí.
- Cân bằng số liệu (Phase 10): số liệu 3 màn mẫu chỉ để thử nghiệm.

## 3. Quyết định đã chốt

| Câu hỏi | Quyết định |
|---|---|
| Chia Phase 6 | 6A màn chơi → 6B boss |
| Luồng vào màn | Chơi nối tiếp: "Chơi" vào màn 1; kết quả thắng có [Màn tiếp]. Không lưu tiến độ |
| Cấu trúc màn | Đợt thường chạy theo thời gian; đợt cuối sinh nhóm tinh anh cố định, diệt hết sân là thắng |
| Chế độ vô tận, kỷ lục | Bỏ. Gỡ `HighScoreTracker`; giữ `SaveData`/`SaveSystem` cho Phase 7 |
| Phần thưởng | Tính bằng hàm thuần theo công thức D1 và hiển thị; chưa lưu |
| Kiến trúc | `StageSession` điều phối; logic tính toán là hàm thuần có unit test; `GameManager` chỉ thêm trạng thái |

## 4. Dữ liệu

Mọi class mới ở `Assets/_Project/Scripts/Stages/`, namespace `ArenaSurvival.Stages`, trừ khi ghi khác.

### 4.1. `StageData` (ScriptableObject, `CreateAssetMenu "ArenaSurvival/Stage Data"`)

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `id` | `string` | ID cố định, ví dụ `"c1_s01"`. Không bao giờ đổi sau khi phát hành (save Phase 7 lưu theo id) |
| `displayName` | `string` | Hiển thị, ví dụ `"1-1"` |
| `stageNumber` | `int` (≥ 1) | Số thứ tự toàn cục, dùng cho công thức độ khó và phần thưởng |
| `waves` | `WaveData[]` | Các đợt thường, chạy lần lượt theo `WaveData.duration` |
| `finalWave` | `EliteGroup[]` | Các nhóm quái tinh anh của đợt cuối |
| `eliteSpawnInterval` | `float` (mặc định 0.6 s) | Khoảng cách giữa 2 con tinh anh khi rải dần |
| `targetTime` | `float` (giây) | Thắng trong thời gian này thì đạt sao thứ 3 |
| `baseGold` | `int` | Vàng gốc khi thắng màn có `stageNumber = 1` |
| `baseAccountXp` | `int` | XP tài khoản gốc tương tự |

`PlannedDuration` (property): tổng `duration` các đợt thường, dùng tính tiến độ.

### 4.2. `EliteGroup` (class `[Serializable]`)

| Trường | Kiểu | Mặc định |
|---|---|---|
| `enemy` | `EnemyData` | — |
| `count` | `int` | 1 |
| `healthMultiplier` | `float` | 5 |
| `scaleMultiplier` | `float` | 1.3 |
| `tint` | `Color` | cam nhạt `(1, 0.75, 0.45)` |

Hệ số tinh anh **nhân thêm** trên hệ số độ khó theo màn.

### 4.3. `StageCatalog` (ScriptableObject)

- `stages`: `StageData[]` theo thứ tự chơi.
- `IndexOf(StageData)`, `GetNext(StageData)` (trả `null` nếu là màn cuối), `First`.

### 4.4. `StageSelection` (lớp tĩnh)

- `StageData Current { get; }`, `Select(StageData)`, `Clear()`.
- Sống qua chuyển scene (static). `SceneLoader` không đổi.
- Nếu `Current == null` khi vào scene Game (bấm Play thẳng ở scene Game trong Editor, hoặc benchmark), `StageSession` dùng `catalog.First`.

### 4.5. `EnemyModifiers` (struct, namespace `ArenaSurvival.Enemies`)

`health`, `damage`, `scale` (hệ số, mặc định 1) và `tint` (mặc định trắng). `static EnemyModifiers Default`.
`Enemy.Initialize(EnemyData data, EnemyModifiers mods)` thay cho `Initialize(EnemyData data, float healthMultiplier)`:
máu = `data.maxHealth × mods.health`, sát thương chạm = `data.contactDamage × mods.damage`, scale = `data.scale × mods.scale`, màu sprite = `mods.tint` (đặt lại mỗi lần lấy từ pool, nên quái thường luôn về màu trắng).

### 4.6. `StageResult` (class `[Serializable]`, dữ liệu thuần — không tham chiếu asset Unity)

`stageId`, `victory`, `starFlags` (`bool[3]`), `starCount`, `clearTime`, `killCount`, `hpFraction`, `usedRevive`, `gold`, `accountXp`.

## 5. Logic thuần: `StageRules` (lớp tĩnh)

| Hàm | Quy tắc |
|---|---|
| `DifficultyMultiplier(int stageNumber)` | `1 + 0.12 × (stageNumber − 1)`; `stageNumber < 1` coi như 1. Áp cho máu và sát thương quái thường |
| `EvaluateStars(bool victory, float hpFraction, float clearTime, float targetTime)` → `bool[3]` | Thua: cả 3 `false`. Thắng: `[0] = true`; `[1] = hpFraction > 0.5`; `[2] = clearTime <= targetTime` |
| `RewardScale(int stageNumber)` | `1 + 0.15 × (stageNumber − 1)` |
| `CalculateRewards(int baseGold, int baseXp, int stageNumber, bool victory, float progress)` → `(int gold, int xp)` | Thắng: `floor(base × RewardScale)`. Thua: `floor(base × RewardScale × clamp(progress, 0, 0.9) × 0.5)`. Không bao giờ âm |
| `Progress(float survivalTime, float plannedDuration)` | `clamp01(survivalTime / plannedDuration)`; `plannedDuration <= 0` → 0 |

Hằng số (0.12, 0.15, 0.9, 0.5, 0.5 máu cho sao 2) khai báo tập trung trong `StageRules` để Phase 10/12 thay bằng bảng cân bằng hoặc Remote Config.

## 6. Luồng màn chơi

### 6.1. `StageSession` (MonoBehaviour, trong scene Game)

Tham chiếu: `StageCatalog`, `WaveSpawner`, `RunStats`, `Health` của Player.

```
Start:
  stage = StageSelection.Current ?? catalog.First
  difficulty = StageRules.DifficultyMultiplier(stage.stageNumber)
  chạy đợt thường i = 1..N:
    spawner.SetWave(stage.waves[i], EnemyModifiers{health = difficulty, damage = difficulty})
    OnWaveStarted(i, N + 1)     // đợt cuối được tính là đợt N+1
    chờ wave.duration (thời gian game, dừng khi timeScale = 0)
  đợt cuối:
    spawner.StopSpawning()
    OnFinalWaveStarted()
    rải từng con tinh anh cách nhau eliteSpawnInterval (hệ số = độ khó × hệ số nhóm)
    khi đã rải xong và spawner.ActiveCount == 0 → chờ 1 giây (thời gian thực) → Kết thúc(thắng)
Player chết (Health.OnDied):
  còn lượt hồi sinh → GameManager.BeginRevivePrompt(), OnReviveOffered()
  hết lượt         → Kết thúc(thua)
Revive(): dùng lượt, Health.Revive(0.5), bất tử 2 giây, đẩy quái trong bán kính 3 ra mép bán kính, GameManager.Revive()
GiveUp(): Kết thúc(thua)
Kết thúc(victory):
  stars = StageRules.EvaluateStars(...), rewards = StageRules.CalculateRewards(..., Progress(...))
  tạo StageResult → GameManager.EndStage(victory) → OnStageEnded(result)
```

- Có cờ chặn kết thúc hai lần (ví dụ con tinh anh cuối và Player chết cùng frame: kết thúc đầu tiên thắng).
- Property cho UI: `Stage`, `CurrentWave`, `TotalWaves`, `IsFinalWave`, `Progress` (theo thời gian các đợt thường; đợt cuối = 1), `Catalog`.
- Instance event: `OnWaveStarted(int wave, int total)`, `OnFinalWaveStarted`, `OnReviveOffered`, `OnStageEnded(StageResult)`.
- Chỉ Editor/Development build: `DebugSkipToFinalWave()`, `PauseProgression(bool)` (StressTest dùng để màn không tự chuyển đợt/kết thúc).

### 6.2. `WaveSpawner` (đổi tên từ `EnemySpawner`, đổi tên file qua Editor để giữ GUID)

- Bỏ: danh sách wave riêng, tự chuyển wave, tăng độ khó vô tận (`_healthGrowthPerLoop`, `_intervalMultiplierPerLoop`, `_minSpawnInterval`), phát `GameEvents.WaveStarted`.
- Giữ: pool quái, chọn vị trí sinh trong sân ngoài tầm camera, `ActiveCount`, cache delegate trả về pool.
- API: `SetWave(WaveData wave, EnemyModifiers mods)`, `StopSpawning()`, `Spawn(EnemyData data, EnemyModifiers mods)`, `SpawnImmediately(int count)` (StressTest; dùng wave hiện tại), `ActiveCount`.

### 6.3. `GameManager` và `GameState`

- `GameState` thêm `RevivePrompt`, `Victory`.
- `GameManager` **bỏ** việc tự nghe `GameEvents.OnPlayerDied` để vào `GameOver`.
- Thêm: `BeginRevivePrompt()` (Playing → RevivePrompt, `timeScale = 0`), `Revive()` (RevivePrompt → Playing, `timeScale = 1`), `EndStage(bool victory)` (→ `Victory` hoặc `GameOver`, `timeScale = 0`).
- `PauseInput` chỉ có tác dụng ở `Playing`/`Paused` (giữ như hiện tại, vì `TogglePause` đã kiểm tra trạng thái).

### 6.4. `Health`

Thêm `Revive(float fraction)`: chỉ chạy khi `IsDead`; đặt `CurrentHealth = MaxHealth × clamp01(fraction)` (tối thiểu 1), phát `OnHealthChanged`.

### 6.5. `GameEvents`

Không thêm event mang kiểu của Stages (Core không phụ thuộc Stages). Âm thanh nghe `OnGameStateChanged` để phát tiếng thắng/thua/hồi sinh. Bỏ `OnWaveStarted` toàn cục nếu không còn ai dùng (HUD chuyển sang nghe `StageSession`).

## 7. Giao diện

Sửa tại chỗ những gì đã có (quy ước dự án), giữ phong cách hiện tại (sprite `ui_panel`/`ui_frame`, font Bungee + Chakra Petch, màu vàng `#F2B544`).

### 7.1. HUD (`HudController`)
- Ô "WAVE n" → `"{displayName} · Đợt {i}/{tổng}"`; đợt cuối → `"{displayName} · ĐỢT CUỐI"`.
- Thanh tiến độ màn mảnh dưới ô đó (`StageSession.Progress`).
- Banner cảnh báo giữa màn hình khi vào đợt cuối ("ĐỢT TINH ANH!"), hiện khoảng 2 giây có hoạt ảnh phóng/mờ.
- Đồng hồ giữ nguyên (thời gian đã chơi).

### 7.2. Bảng hồi sinh (mới, `RevivePanel`)
Khung như menu ESC: tiêu đề "HỒI SINH?", dòng phụ "Hồi sinh 1 lần mỗi màn", nút [Hồi sinh (miễn phí)] và [Bỏ cuộc]. Hiện khi `GameState.RevivePrompt`.

### 7.3. Màn kết quả (`GameOverPanel` → đổi tên `StageResultPanel`, đổi tên file qua Editor)
- Tiêu đề "CHIẾN THẮNG!" (vàng) hoặc "THẤT BẠI" (đỏ), kèm tên màn.
- 3 ngôi sao: đạt sáng, trượt mờ; dưới mỗi ngôi một dòng điều kiện ngắn ("Thắng màn", "Còn trên 50% máu", "Dưới m:ss").
- Thời gian, số quái hạ.
- Phần thưởng: icon vàng + số, "XP tài khoản" + số.
- Nút: thắng → [Màn tiếp] (ẩn nếu `GetNext == null`) [Chơi lại] [Về menu]; thua → [Chơi lại] [Về menu].
- [Màn tiếp]: `StageSelection.Select(next)` rồi `SceneLoader.LoadGame()`. [Chơi lại]: giữ màn hiện tại.

### 7.4. Menu chính
- Bỏ ô "Kỷ lục" (`BestPill`) và code đọc kỷ lục trong `MainMenu`.
- Nút "Chơi": `StageSelection.Select(catalog.First)` rồi vào Game (`MainMenu` thêm tham chiếu `StageCatalog`).

### 7.5. Sprite mới
`icon_star.png` và `icon_coin.png` tạo bằng script (tự làm, giống các icon hiện có), ghi vào `Docs/asset-licenses.md`.

## 8. Âm thanh

`SoundBank` thêm `stageVictory` và `revive` (chọn từ các gói Kenney đã có trong `Audio/`). `AudioManager` phát `stageVictory` khi vào `Victory`, `revive` khi từ `RevivePrompt` về `Playing`. `gameOver` giữ nguyên.

## 9. Gỡ bỏ

- `Save/HighScoreTracker.cs` và object/component của nó trong `Game.unity`.
- Phần kỷ lục trong `MainMenu` và màn kết quả.
- Logic vô tận trong spawner (mục 6.2).
- Giữ: `SaveData`, `SaveSystem`, `SaveSystemTests`.

## 10. DevTools

- `StressTest`: tham chiếu `WaveSpawner`; khi bật gọi `StageSession.PauseProgression(true)`, khi tắt gọi `false`.
- `PerfRecorder`: tham chiếu `WaveSpawner` thay cho `EnemySpawner`.
- `BenchmarkRunner`: tham số `-stage N` (1-based, chọn `catalog.stages[N-1]` trước khi vào Game).

## 11. Dữ liệu mẫu (`Data/Stages/`)

| Asset | stageNumber | Đợt thường | Đợt cuối | targetTime | baseGold / baseXp |
|---|---|---|---|---|---|
| `Stage_C1_01` (`c1_s01`, "1-1") | 1 | `Wave_01`, `Wave_02`, `Wave_03` | 3 × Tank tinh anh | 210 s | 100 / 20 |
| `Stage_C1_02` (`c1_s02`, "1-2") | 2 | như trên | 5 × Tank tinh anh | 210 s | 100 / 20 |
| `Stage_C1_03` (`c1_s03`, "1-3") | 3 | như trên | 4 × Tank tinh anh + 6 × Fast tinh anh (máu × 3) | 225 s | 100 / 20 |
| `StageCatalog_Main` | — | 3 màn trên theo thứ tự | — | — | — |

`Wave_01..03`: đặt `duration` = 60 s mỗi đợt (hiện 30 s) để màn dài khoảng 3–4 phút. Đây là thay đổi dữ liệu, sẽ cân bằng lại ở Phase 10.

## 12. Kiểm tra

1. **Unit test** (`Tests/EditMode/StageRulesTests.cs`, `StageCatalogTests.cs`):
   - `DifficultyMultiplier`: màn 1 = 1, màn 2 = 1.12, màn 11 = 2.2, màn ≤ 0 coi như 1.
   - `EvaluateStars`: thua → 0 sao; thắng đủ điều kiện → 3; thiếu máu → thiếu sao 2; quá giờ → thiếu sao 3; đúng biên (máu = 0.5 → không đạt; thời gian = target → đạt).
   - `CalculateRewards`: thắng màn 1 = base; màn 3 = floor(base × 1.3); thua với progress 0.5 = floor(base × 0.25); progress 1 khi thua bị giới hạn 0.9; base 0 → 0.
   - `Progress`: chia đúng, `plannedDuration` 0 → 0, vượt 1 → 1.
   - `StageCatalog`: `GetNext` màn giữa, màn cuối → `null`, màn không có trong danh sách → `null`.
   - Toàn bộ test cũ vẫn đạt.
2. Biên dịch không lỗi; quét Missing Script trên prefab và 2 scene = 0.
3. Chơi thử bằng unity-cli, chụp ảnh từng bước:
   1. Menu → "Chơi" → HUD "1-1 · Đợt 1/4".
   2. `DebugSkipToFinalWave()` → banner đợt tinh anh, quái tinh anh to hơn và có màu.
   3. Hạ hết quái → `Victory` → màn kết quả có sao, thưởng, nút [Màn tiếp].
   4. [Màn tiếp] → HUD "1-2"; quái thường có máu ×1.12 (đọc bằng eval).
   5. Player chết → bảng hồi sinh → [Hồi sinh] → máu 50%, bất tử 2 giây → chết lần nữa → `GameOver` → màn kết quả thua, chỉ [Chơi lại] [Về menu].
   6. Console không lỗi.
4. Benchmark Development build 300 quái (`-stage 1`): chạy hết, GC trung bình vẫn 0 B/frame.
