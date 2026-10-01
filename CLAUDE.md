# Arena Survival — ghi chú dự án

Game 2D top-down action survival **thương mại trên mobile** (Android trước, iOS sau), kiếm tiền từ **bán vật phẩm (IAP)**
và **quảng cáo có thưởng**. Tham khảo thể loại: Survivor.io, Archero. Có hai vòng lặp: trong màn (bắn, lên cấp, chọn kỹ năng,
reset sau màn) và ngoài màn (trang bị, Talent, level tài khoản, lưu vĩnh viễn).

- Kế hoạch: `Docs/project-plan.md` (Phase 0–14, kinh tế, pháp lý). **Đọc trước khi làm phase mới.**
- Spec/plan từng đợt việc: `Docs/specs/`, `Docs/plans/`. Thiết kế bản core cũ: `Docs/archive/core-design-v1.md`.
- Giấy phép asset: `Docs/asset-licenses.md` (asset mới phải cho phép dùng thương mại và được ghi vào đây).

Cập nhật lần cuối: 2026-10-01, sau Phase 6A (màn chơi có kết thúc: đợt quái, tinh anh, sao, hồi sinh, kết quả).

## Quy ước làm việc (quan trọng)

- **Trả lời bằng tiếng Việt.** Comment trong code viết tiếng Việt, giải thích **vì sao** chứ không giải thích **cái gì**.
- **Không tự `git commit` / `git push`.** Chuẩn bị thay đổi, kiểm tra xong thì đưa người dùng danh sách file và message để họ tự commit.
  Message theo kiểu `feat: …`, `fix: …`, `perf: …`, `test: …`, `polish: …`, `chore: …` (tiếng Việt có dấu). Xong mỗi đầu việc thì gợi ý commit.
- Tính năng mới: trình bày thiết kế ngắn và chờ đồng ý trước khi code, trừ khi yêu cầu đã rõ ràng.
  Việc lớn (hệ thống mới, đổi cấu trúc): viết spec vào `Docs/specs/`, kế hoạch vào `Docs/plans/`.
- Sửa scene/prefab **qua Unity Editor** (unity-cli `eval_file`), không sửa tay YAML khi Editor đang mở.
  Luôn kiểm tra `scene.isDirty` trước khi lưu hoặc đổi scene; dirty thì hỏi người dùng.
  Không sửa object trong scene lúc Play qua `SerializedObject` (để lại cờ dirty sau khi thoát Play).
  Scene có Layout Group / ContentSizeFitter thường **tự dirty lại ngay sau khi lưu** (layout tính lại ở lượt cập nhật kế tiếp):
  sau khi sửa UI, gọi `Canvas.ForceUpdateCanvases()` rồi lưu lần nữa; dirty mà không rõ nguồn thì vẫn hỏi người dùng.
- Di chuyển/đổi tên asset **chỉ qua `AssetDatabase.MoveAsset`** để giữ GUID; không `git mv` khi Editor đang mở.
- Sửa UI **tại chỗ** (đổi sprite, font, vị trí, thêm con), không xóa rồi dựng lại, để giữ liên kết script.
- `Enemy_Tank.asset` thường có thay đổi riêng của người dùng: không đưa vào commit của mình.
- **Đo trước khi tối ưu**, không sửa theo phỏng đoán (xem mục Hiệu năng).

## Nguyên tắc kiến trúc (theo plan, mục B2)

1. **Tách trạng thái trong màn và trạng thái lưu vĩnh viễn.** Scene Game không ghi trực tiếp vào save; nó chỉ trả về `StageResult`,
   Hub chịu trách nhiệm áp thưởng.
2. **Mọi thay đổi tiền tệ đi qua `EconomyService`** (cộng/trừ, kiểm tra đủ tiền, phát sự kiện, ghi analytics). Hàm trừ tiền trả về `bool`,
   không bao giờ để số dư âm.
3. **Dữ liệu tách khỏi logic** bằng ScriptableObject; số liệu cân bằng sau này có thể ghi đè bằng Remote Config.
4. **Giao tiếp qua event** (`GameEvents` static cho sự kiện toàn cục; instance event cho quan hệ cha–con rõ ràng).
5. **Object sinh hàng loạt phải pool** (`ObjectPool<T>`).
6. **Save có số version** và migration; **save chỉ lưu ID ổn định** (chuỗi `id`), không lưu tham chiếu asset hay tên hiển thị.
   ID vật phẩm/tiền tệ/màn là hằng số khai báo tập trung.
7. **Logic kinh tế và chỉ số viết dạng hàm thuần** để unit test được mà không cần chạy Unity.

## Môi trường & cách kiểm tra

- Unity **6000.3.13f1**, URP 2D (SRP Batcher bật), Input System (chỉ Input System), uGUI + TextMeshPro. Build target hiện tại: Windows
  (chuyển Android ở Phase 13 hoặc sớm hơn nếu cần thử trên máy).
- Unity CLI: `C:\Users\Admin\AppData\Local\Unity\bin\unity.exe` (không có trong PATH của shell agent).
  Gọi kiểu `unity command <tên> --caller plugin --skill unity-cli`. Chạy C# bằng `eval_file --file x.cs`
  (không dùng `eval` inline vì PowerShell 5.1 làm hỏng dấu nháy; trong file `.cs` tránh ký tự `'\\'` khi viết qua heredoc của shell).
- **Play mode không chạy frame khi Editor không được focus** (`runInBackground: 0` trong Player Settings; bản build cũng vậy).
  Khi test: `editor_play`, rồi eval `UnityEngine.Application.runInBackground = true;`. Chỉ đổi lúc chạy, file cài đặt không đổi.
- Chụp màn hình: `capture_game_view --save_path "Temp/x.png"` (file lưu vào `Assets/Temp/`, nhớ `delete_asset --confirm true` sau đó).
- Unit test: `run_tests --mode EditMode` (hiện 65/65 đạt), hoặc Window → General → Test Runner.
- Hiện Editor mở scene nào thì Play chạy scene đó. Muốn test cả vòng thì mở `MainMenu.unity` trước.
- Build Development: `build --target StandaloneWindows64 --outputPath "Builds/Dev/ArenaSurvival.exe" --options '["Development"]' --confirm true`,
  rồi theo dõi `build_status`. `Builds/` đã được git bỏ qua.
- ffmpeg đã cài qua winget (`Gyan.FFmpeg`), dùng để đo/chuyển đổi âm thanh.

## Cấu trúc project

```
Docs/                   project-plan.md, asset-licenses.md, specs/, plans/, archive/
Assets/_Project/
├── Scripts/            assembly ArenaSurvival (ArenaSurvival.asmdef); namespace = ArenaSurvival.<tên thư mục>
│   ├── Core/           GameManager, GameState, GameEvents, ObjectPool, SceneLoader, PauseInput, CameraFollow
│   ├── Player/         PlayerController, PlayerStats, PlayerDeathHandler, PlayerControls.inputactions
│   ├── Combat/         Health, Projectile
│   ├── Weapons/        PlayerWeapon, OrbitWeapon, SawBladeRig, WeaponDefinition/IWeapon, WeaponData, OrbitWeaponData
│   ├── Enemies/        Enemy, EnemyData, EnemyModifiers (hệ số máu/sát thương/kích thước/màu lúc sinh)
│   ├── Stages/         StageSession (điều phối màn), WaveSpawner, StageData + EliteGroup, WaveData, StageCatalog,
│   │                   StageSelection (màn đang chọn, static), StageResult, StageRules (hàm thuần: độ khó, sao, thưởng), RunStats
│   ├── RunUpgrades/    Lên cấp + chọn kỹ năng TRONG MÀN: ExperienceSystem, XpCurve, XpOrb, XpOrbSpawner, UpgradeData,
│   │                   UpgradeManager, RandomPicker
│   ├── Save/           SaveData, SaveSystem (chưa dùng; nền cho PlayerProfile/SaveService ở Phase 7)
│   ├── UI/             HudController, LevelUpPanel(+Animator), UpgradeOptionButton, PauseMenu, StageResultPanel, RevivePanel, MainMenu,
│   │                   TimeFormatter, UiHoverScale, VolumeSlider (tách thư mục con khi có Hub ở Phase 9)
│   ├── Audio/          AudioManager, SoundBank, SoundEffect, ButtonSound, VolumeSettings
│   ├── Vfx/            VfxSpawner, PooledVfx, FloatingText, HitFlash, DamageNumberSource, SpriteBodyAnimator
│   └── DevTools/       StressTest, PerfRecorder, BenchmarkRunner (chỉ Editor/Development build; menu cheat Phase 10 đặt ở đây)
├── Tests/EditMode/     assembly ArenaSurvival.Tests.EditMode: XpCurve, RandomPicker, SaveSystem, TimeFormatter, WeaponSpread,
│                       VolumeSettings, ObjectPool, StageRules, StageCatalog
├── Scenes/             MainMenu.unity (build index 0), Game.unity (build index 1) — sẽ thành Boot / Hub / Game
├── Data/               Enemies/, Weapons/, RunUpgrades/, Stages/ (Wave_*, Stage_C1_01..03, StageCatalog_Main), Audio/ (SoundBank)
├── Prefabs/            Player, Enemy, Projectile, XpOrb, AudioManager, Arena, Vfx/*, UI/VolumeSettings
├── Art/                Kenney-TopDownShooter, Tiles (Tilemap), UI (sprite + icon tự vẽ), Fonts, Vfx, Weapons, Pickups, Projectiles
└── Audio/              MainMixer.mixer (Master/Music/SFX), UI, Impact, Digital (Kenney), Music (Juhani Junkala, OGG)
```

Thư mục cho hệ thống mới (`Meta/`, `Inventory/`, `Economy/`, `Shop/`, `Services/`) **tạo khi tới phase của chúng**, không tạo sẵn.

## Code hiện có & quyết định chính

- **GameManager**: Singleton + state machine `Menu / Playing / Paused / LevelUp / RevivePrompt / Victory / GameOver`, pause bằng
  `Time.timeScale`. `LevelUp` tách khỏi `Paused` để menu Pause không xen vào bảng chọn nâng cấp. GameManager **không** tự vào
  GameOver khi Player chết: `StageSession` quyết định (hồi sinh hay kết thúc) rồi gọi `BeginRevivePrompt / Revive / EndStage`.
- **Màn chơi (Phase 6A)**: `StageSession` chạy các đợt thường theo thời gian (`WaveData.duration`), rồi đợt cuối (`EliteGroup`:
  EnemyData + hệ số) phải diệt sạch sân mới thắng (chờ 1 s, chỉ đếm khi đang `Playing`). Kết quả ra ngoài **chỉ qua `StageResult`**
  (instance event `OnStageEnded`); HUD, `StageResultPanel`, `RevivePanel` nghe instance event của `StageSession`.
  Màn được chọn truyền qua `StageSelection` (static); trống thì dùng màn đầu của `StageCatalog`. Hồi sinh: 1 lần/màn, 50% máu,
  bất tử 2 s, đẩy quái trong bán kính 3. Dev: `StageSession.DebugSkipToFinalWave()`, `PauseProgression(bool)`.
- **Object pool tự viết** (`ObjectPool<T>`): đối tượng gọi callback `OnReturnToPool`, không biết gì về pool; `Release` bỏ qua object đã tắt
  (chống trả hai lần); `TryGet` + `maxSize` cho thứ bỏ qua được (hiệu ứng, số sát thương), tạo sẵn đủ số để lúc chơi không `Instantiate`.
- **Data-driven bằng ScriptableObject**. Vũ khí dùng lớp cha `WeaponDefinition` và interface `IWeapon`, nên nâng cấp "mở khóa vũ khí"
  trỏ được tới súng lẫn lưỡi cưa.
- **Đạn, lưỡi cưa không làm con của Player** (container riêng ở gốc scene). Con của Rigidbody2D bị kéo/xoay theo cha.
- **PlayerStats** giữ hệ số cộng dồn từ nâng cấp trong màn. Không sửa trực tiếp `WeaponData` lúc chơi (asset sẽ bị giữ thay đổi).
- **Player/Enemy tách gốc và `Visual`**: gốc giữ Rigidbody, collider, hướng mặt, scale theo loại quái và phồng của `HitFlash`;
  con `Visual` giữ SpriteRenderer + `SpriteBodyAnimator`. Đừng gắn hoạt ảnh lên gốc.
- **Layer & Collision Matrix**:

  | Layer | Va chạm với |
  |---|---|
  | `Player` (8) | `Enemy`, `Pickup`, `Boundary`, `Default` |
  | `Enemy` (6) | `Player`, `PlayerProjectile` |
  | `PlayerProjectile` (7) | `Enemy` |
  | `Pickup` (9) | `Player` |
  | `Boundary` (10, tường) | chỉ `Player` |

- **Quái sinh trong sân** (`WaveSpawner._arenaArea`), ngoài tầm nhìn camera; tường chỉ giữ Player.
- **Chưa lưu tiến độ** (Phase 7): kỷ lục/chế độ vô tận đã gỡ ở 6A; `SaveSystem` (JSON, chịu file hỏng) giữ làm nền.
- **Âm thanh**: `AudioManager` sống xuyên scene (DontDestroyOnLoad), 10 AudioSource xoay vòng, lệch cao độ ngẫu nhiên, giới hạn tần suất.
  Âm lượng qua `MainMixer` (exposed `MasterVolume/MusicVolume/SFXVolume`), lưu bằng **PlayerPrefs** (cài đặt thuộc thiết bị,
  không thuộc hồ sơ người chơi). Tiếng thắng màn / hồi sinh phát theo `OnGameStateChanged`.

## Hiệu năng: công cụ đo và bài học

**Công cụ** (`DevTools/`, tự gỡ ở bản phát hành nhờ `Debug.isDebugBuild`):
- `StressTest` (F9): giữ N quái, Player bất tử, tắt XP, mở hết vũ khí, bỏ giới hạn fps. Có `Configure(count, weapons)` / `Begin()` / `Stop()`.
- `PerfRecorder` (F10): đo bằng `ProfilerRecorder` (Main Thread, Scripts, Physics2D, GC Alloc, Batches, SetPass, Draw Calls),
  ghi `%USERPROFILE%\AppData\LocalLow\DefaultCompany\ArenaSurvival\perf-log.txt`.
- Benchmark bằng dòng lệnh trên Development build:
  `ArenaSurvival.exe -benchmark -enemies 300 -weapons 1 -label tên [-stage N] -settle 12 -frames 3000 [-profile-log] -screen-fullscreen 0`
  (tự vào Game, đo, ghi log, thoát). `-profile-log` ghi file `.raw` cho cửa sổ Profiler (~350 MB/2000 frame, nhớ xóa).

**Bài học đã đo được**:
- Đo GC **phải dùng Development build**: Editor tự tạo ~200 KB rác/frame kể cả cảnh 0 quái.
- Build chạy ~800 fps: cửa sổ đo phải dài theo **thời gian** (chờ 12 s cho pool ổn định, đo 3000 frame), không chỉ theo số frame.
- **Không tạo delegate mới** khi lấy object từ pool (`obj.OnReturnToPool = Method` → cache delegate trong `Awake`), và không `+=`/`-=`
  event trong `OnEnable`/`OnDisable` của object nằm trong pool (đăng ký 1 lần ở `Awake`). Đây là nguồn rác chính đã sửa
  (300 quái: 429.7 KB → 1.3 KB rác trong 2000 frame).
- Có **SRP Batcher** nên Batches tăng theo số object là bình thường; nhìn **SetPass Calls** (hiện ~22, không đổi theo số quái).
  Sprite Atlas đã thử, không cải thiện gì; Tilemap trong atlas còn lộ đường kẻ ô → đã bỏ.
- Số liệu nền (Development build, 300 quái, mở hết vũ khí): Main Thread ~1.5–2.5 ms (dao động theo máy), GC 0 B/frame.

## Nội dung game hiện có

| Loại | Chi tiết |
|---|---|
| Quái (3) | Basic (máu 20, tốc 2), Fast (máu 10, tốc 4), Tank (máu 50, tốc 1.2, to x1.5, từ wave 3) |
| Màn chơi (3) | 1-1, 1-2, 1-3: 3 đợt × 60 s (`Wave_01..03`) rồi đợt tinh anh (Tank/Fast máu ×3–5, to ×1.3, màu cam). Độ khó ×(1 + 0.12×(màn−1)). Sao: thắng / >50% máu / dưới 3:30–3:45. Thưởng (chỉ hiển thị): 100 vàng, 20 XP × (1 + 0.15×(màn−1)) |
| Vũ khí (4) | Súng thường (có sẵn), Súng chùm (5 viên tỏa 50°), Súng máy (4 phát/giây, từ cấp 5), Lưỡi cưa xoay (3 lưỡi) |
| Nâng cấp trong màn (8) | Sát thương +20%, Tốc độ bắn +15%, Tốc độ chạy +10%, Máu tối đa +20, Hồi máu +30, 3 nâng cấp mở khóa vũ khí |
| XP trong màn | Cấp 1 cần 5 XP, mỗi cấp sau cần thêm 5; orb bị hút trong bán kính 2.5 |
| Màn chơi | Đấu trường Tilemap: sân cát 30×30 ô, viền đá, đường đất, bãi cỏ, bụi cây |
| UI | HUD (đợt i/n + thanh tiến độ màn, banner "ĐỢT TINH ANH!"), bảng lên cấp, menu ESC (slider âm lượng), bảng hồi sinh, màn kết quả (sao, thưởng, Màn tiếp/Chơi lại/Về menu), menu chính + bảng Cài đặt |
| Hiệu ứng | Trúng đạn, quái chết, lên cấp, nháy đỏ khi trúng đòn, rung camera, số sát thương (gộp 0.2s) |
| Hoạt ảnh | Bằng code: thở khi đứng, lắc + co giãn theo bước khi chạy, quái phóng to lúc xuất hiện |
| Âm thanh | Bắn, trúng, quái chết, bị đánh, nhặt XP, lên cấp, chọn nâng cấp, rê/bấm nút, Game Over, nhạc nền |

## Tiến độ (theo `Docs/project-plan.md`)

| Phase | Trạng thái |
|---|---|
| 0–5 – Core gameplay | Xong (gồm tối ưu GC và đợt cấu trúc lại 2026-10-01) |
| 6A – Màn chơi | Xong: StageData, StageSession, đợt tinh anh, độ khó, sao, hồi sinh, StageResult, màn kết quả |
| 6B – Boss | **Tiếp theo**: tách hành vi quái, boss nhiều pha (state machine), mini-boss, đạn của quái, thanh máu boss |
| 7 – Hồ sơ người chơi, save v2, tiền tệ | Chưa |
| 8 – Trang bị, tài nguyên, Talent | Chưa |
| 9 – Hub và level tài khoản | Chưa |
| 10 – Nội dung và cân bằng | Chưa |
| 11 – Cửa hàng, IAP, quảng cáo | Chưa |
| 12 – Giữ chân, analytics, cloud save | Chưa |
| 13 – Mobile, tối ưu, playtest | Chưa |
| 14 – Soft launch | Chưa |

Tính năng cũ **giữ chạy cho tới khi có thứ thay thế**: MainMenu (→ Hub, Phase 9; nút "Chơi" vào màn 1, chơi nối tiếp qua [Màn tiếp]).

## Vấn đề đã biết / lưu ý

- Sau khi hồi sinh, Player có thể chết lại ~1 s sau khi hết 2 s bất tử nếu quái đông: xem lại khi cân bằng (Phase 10).
- Test UI có hiệu ứng theo thời gian bằng unity-cli: mỗi lệnh mất ~1 s, chụp ngay sau lệnh kích hoạt (banner chỉ hiện 2 s).
- Giấy phép âm thanh Kenney và nhạc Juhani Junkala **chưa xác minh** (xem `Docs/asset-licenses.md`) — phải xong trước khi phát hành.
- `PlayerWeapon.FindNearestEnemy` dùng buffer 64 phần tử và `OverlapCircleNonAlloc` (obsolete trong Unity 6): hơn 64 quái trong tầm
  thì có thể bỏ sót con gần nhất; còn ~25 B rác lẻ tẻ ở đây. Sửa khi làm lại vũ khí ở Phase 8.
- Collider của quái là collider cứng (người dùng tự chỉnh), nên quái đẩy được Player. Muốn tắt thì bật lại Is Trigger trên prefab `Enemy`.
- Ba font asset TMP mỗi file khoảng 4 MB (atlas 1024 động) và tự thay đổi khi gặp ký tự mới lúc chạy; commit cùng lúc là được.
- Mỗi lần build, Unity tự ghi lại vài file cài đặt (`UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`,
  `ProjectSettings.asset`): commit riêng dạng `chore`.
- Icon nâng cấp Súng máy dùng sprite súng Kenney khá nhỏ (33×10 px), phóng to hơi mờ.
- Âm thanh được chọn bằng cách đo độ dài và độ to bằng ffmpeg; cần người nghe thật để tinh chỉnh (`Data/Audio/SoundBank.asset`).
