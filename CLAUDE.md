# Arena Survival — ghi chú dự án

Game 2D top-down action survival **thương mại trên mobile** (Android trước, iOS sau), kiếm tiền từ **bán vật phẩm (IAP)**
và **quảng cáo có thưởng**. Tham khảo thể loại: Survivor.io, Archero. Có hai vòng lặp: trong màn (bắn, lên cấp, chọn kỹ năng,
reset sau màn) và ngoài màn (trang bị, Talent, level tài khoản, lưu vĩnh viễn).

- Kế hoạch: `Docs/project-plan.md` (Phase 0–14, kinh tế, pháp lý). **Đọc trước khi làm phase mới.**
- Spec/plan từng đợt việc: `Docs/specs/`, `Docs/plans/`. Thiết kế bản core cũ: `Docs/archive/core-design-v1.md`.
- Giấy phép asset: `Docs/asset-licenses.md` (asset mới phải cho phép dùng thương mại và được ghi vào đây).

Cập nhật lần cuối: 2026-10-01, sau Phase 7 (hồ sơ người chơi, save v2 có HMAC/sao lưu/migration, EconomyService, tiến độ màn, scene Boot).

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
- `simulate_key` của unity-cli **không** làm đổi `Keyboard.current` (kể cả phím D): không dùng để test phím tắt; gọi thẳng
  hàm/bật component qua `eval_file` (ví dụ bật `DevMenuView`) và nhờ người dùng bấm phím thật.
- Chụp màn hình: `capture_game_view --save_path "Temp/x.png"` (file lưu vào `Assets/Temp/`, nhớ `delete_asset --confirm true` sau đó).
- Unit test: `run_tests --mode EditMode` (hiện 112/112 đạt), hoặc Window → General → Test Runner.
- Hiện Editor mở scene nào thì Play chạy scene đó (dịch vụ vẫn tự khởi tạo). Muốn test cả vòng như bản build thì mở `Boot.unity` trước.
- **Editor và bản build dùng CHUNG file save** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\ArenaSurvival\save.json` (+ `.bak`):
  test trong Play mode cộng vàng/sao thật vào save của người dùng. Sao lưu file trước khi test thứ có thể phá dữ liệu.
- Build Development: `build --target StandaloneWindows64 --outputPath "Builds/Dev/ArenaSurvival.exe" --options '["Development"]' --confirm true`,
  rồi theo dõi `build_status`. `Builds/` đã được git bỏ qua.
- ffmpeg đã cài qua winget (`Gyan.FFmpeg`), dùng để đo/chuyển đổi âm thanh.

## Cấu trúc project

```
Docs/                   project-plan.md, asset-licenses.md, specs/, plans/, archive/
Assets/_Project/
├── Scripts/            assembly ArenaSurvival (ArenaSurvival.asmdef); namespace = ArenaSurvival.<tên thư mục>
│   ├── Core/           GameManager, GameState, GameEvents, ObjectPool, SceneLoader (+ FirstSceneOverride), PauseInput, CameraFollow
│   ├── Player/         PlayerController, PlayerStats, PlayerDeathHandler, PlayerControls.inputactions
│   ├── Combat/         Health, Projectile
│   ├── Weapons/        PlayerWeapon, OrbitWeapon, SawBladeRig, WeaponDefinition/IWeapon, WeaponData, OrbitWeaponData
│   ├── Enemies/        Enemy (lõi), EnemyData, EnemyModifiers, EnemyContext (trạng thái mỗi con), IEnemyWorld, EnemyRules (hàm thuần),
│   │                   EnemyBehaviour → ChaseBehaviour, KeepDistanceShooterBehaviour, BossBehaviour (+BossPhase);
│   │                   BossAttack → RadialBurstAttack, DashAttack, SummonAttack; EnemyProjectiles (pool đạn chung)
│   ├── Stages/         StageSession (điều phối màn), WaveSpawner, StageData + EliteGroup, WaveData, StageCatalog,
│   │                   StageSelection (màn đang chọn, static), StageResult, StageRules (hàm thuần: độ khó, sao, thưởng), RunStats
│   ├── RunUpgrades/    Lên cấp + chọn kỹ năng TRONG MÀN: ExperienceSystem, XpCurve, XpOrb, XpOrbSpawner, UpgradeData,
│   │                   UpgradeManager, RandomPicker
│   ├── Save/           SaveService (tmp → File.Replace + .bak, HMAC, khôi phục), SaveSecurity (HMAC-SHA256), SaveMigrations (JObject, v1→v2)
│   ├── Meta/           PlayerProfile, ProfileService, StageProgressService (+Rules, StageGrant), GameServices (static), ServicesHost, BootLoader
│   ├── Economy/        EconomyService (đường duy nhất đổi tiền tệ), CurrencyIds
│   ├── UI/             HudController, LevelUpPanel(+Animator), UpgradeOptionButton, PauseMenu, StageResultPanel, RevivePanel, BossHealthBar, MainMenu,
│   │                   TimeFormatter, UiHoverScale, VolumeSlider (tách thư mục con khi có Hub ở Phase 9)
│   ├── Audio/          AudioManager, SoundBank, SoundEffect, ButtonSound, VolumeSettings
│   ├── Vfx/            VfxSpawner, PooledVfx, FloatingText, HitFlash, DamageNumberSource, SpriteBodyAnimator
│   └── DevTools/       StressTest (F9), PerfRecorder (F10), BenchmarkRunner, DevMenu (F1: xem hồ sơ, +vàng/kim cương, mở khóa màn,
│                       xóa hồ sơ, tới đợt cuối, thắng ngay, bất tử). Chỉ Editor/Development build; tự tạo, sống xuyên scene
├── Tests/EditMode/     assembly ArenaSurvival.Tests.EditMode: XpCurve, RandomPicker, SaveSystem, TimeFormatter, WeaponSpread,
│                       VolumeSettings, ObjectPool, StageRules, StageCatalog, EnemyRules, EconomyService, SaveService, StageProgress
├── Scenes/             Boot.unity (0), MainMenu.unity (1, sẽ thành Hub ở Phase 9), Game.unity (2)
├── link.xml            giữ namespace ArenaSurvival.Meta khi build IL2CPP (Newtonsoft đọc PlayerProfile bằng reflection)
├── Data/               Enemies/ (+ Behaviours/), Bosses/ (Boss_*, BossBehaviour_*, Attack_*), Weapons/, RunUpgrades/,
│                       Stages/ (Wave_*, Stage_C1_01..03, StageCatalog_Main), Audio/ (SoundBank)
├── Prefabs/            Player, Enemy (dùng chung cho mọi quái và boss), Projectile, EnemyProjectile, XpOrb, AudioManager, Arena, Vfx/*,
│                       UI/VolumeSettings
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
  Màn có boss (`StageData.boss`): rải tinh anh → sau `bossDelay` sinh boss (`OnBossSpawned`) → boss chết thì `WaveSpawner.DespawnAll()`
  (quái còn lại nổ, XP 0 nên không rơi orb) → thắng. Màn không boss: dọn sạch sân như cũ.
- **Hành vi quái (Phase 6B)**: `Enemy` chỉ là lõi (máu, chạm, pool, hệ số). Di chuyển/tấn công là asset `EnemyBehaviour` gán trong
  `EnemyData.behaviour` (trống = đuổi thẳng). Asset dùng chung nên **không giữ trạng thái**: mọi bộ đếm nằm trong `EnemyContext`
  (tạo 1 lần/Enemy, ô nhớ Timer/Timer2/State/Index/Counter/Phase/Vector — mỗi asset ghi rõ quy ước dùng ô nào).
  Boss = `BossBehaviour` (các `BossPhase`: ngưỡng máu + danh sách `BossAttack` xoay vòng; đổi pha chỉ tiến, bỏ ngang đòn đang dở,
  phát `Enemy.OnBossPhaseChanged`). Boss mới = ghép đòn có sẵn thành asset; kiểu đòn mới = 1 class `BossAttack`.
  Quái không va chạm tường nên `EnemyContext.Move` kẹp vị trí trong sân (`IEnemyWorld.ClampToArena`). Đòn lao luôn có gồng báo trước.
  Đạn của quái: 1 pool chung `EnemyProjectiles` trong scene (quái nằm trong pool nên không tự giữ pool đạn), layer `EnemyProjectile`.
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
  | `Player` (8) | `Enemy`, `EnemyProjectile`, `Pickup`, `Boundary`, `Default` |
  | `Enemy` (6) | `Player`, `PlayerProjectile` |
  | `PlayerProjectile` (7) | `Enemy` |
  | `Pickup` (9) | `Player` |
  | `EnemyProjectile` (11) | chỉ `Player` (bay xuyên tường, tự hủy sau 4 s) |
  | `Boundary` (10, tường) | chỉ `Player` |

- **Quái sinh trong sân** (`WaveSpawner._arenaArea`), ngoài tầm nhìn camera; tường chỉ giữ Player.
- **Dịch vụ & save (Phase 7)**: dịch vụ là class C# thường, truy cập qua `GameServices` (static), khởi tạo ở scene Boot hoặc tự khởi tạo
  (`RuntimeInitializeOnLoadMethod`, có reset static cho trường hợp tắt domain reload). `ServicesHost` (ẩn, DontDestroyOnLoad) lưu khi
  app chuyển nền/thoát. Save = Newtonsoft JSON `{format, payload(chuỗi), hmac}`; ghi `.tmp` rồi `File.Replace` (bản cũ thành `.bak`).
  Đọc: hỏng/sai chữ ký → dùng `.bak` hợp lệ; sai chữ ký không có `.bak` → vẫn dùng, `tampered = true` (**không bao giờ xóa tiến độ**);
  hỏng hẳn → hồ sơ mới + giữ `.corrupt`. Đổi cấu trúc `PlayerProfile` = tăng `CurrentVersion` + thêm 1 bước `SaveMigrations`.
  Kết quả màn: `StageSession` → `StageEvents.OnStageCompleted` → `StageProgressService.Apply` (thưởng qua `EconomyService`, lần thắng
  đầu ×2, chơi lại ×0.5, sao chỉ tăng, lưu ngay) → rồi mới `GameManager.EndStage` (màn kết quả đọc `LastGrant`). Mở khóa màn tuần tự;
  nút Chơi ở menu vào màn chưa thắng đầu tiên. Volume vẫn ở PlayerPrefs (cài đặt thiết bị).
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
| Quái (4) | Basic (máu 20, tốc 2), Fast (máu 10, tốc 4), Tank (máu 50, tốc 1.2, to x1.5), Xạ thủ (máu 15, giữ khoảng 5.5±1, bắn 2 s/viên, đạn 6) |
| Boss (2) | Mini-boss "Cỗ Máy Gác Cổng" (màn 1-2, máu 400, 2 pha: lao + vòng đạn), boss "Thủ Lĩnh Sa Mạc" (màn 1-3, máu 1500,
  3 pha 100/66/33%: vòng đạn → + lao → vòng đạn dày + lao + gọi 4 Basic, tối đa 8). Sprite Kenney phóng to + tô màu (tạm) |
| Màn chơi (3) | 1-1, 1-2, 1-3: 3 đợt × 60 s (`Wave_01..03`, Xạ thủ từ đợt 2) rồi đợt cuối: 1-1 tinh anh; 1-2 mini-boss + 2 Tank tinh anh; 1-3 boss + 3 Fast tinh anh. Độ khó ×(1 + 0.12×(màn−1)). Sao: thắng / >50% máu / dưới 3:30–3:45. Thưởng (chỉ hiển thị): 100 vàng, 20 XP × (1 + 0.15×(màn−1)) |
| Vũ khí (4) | Súng thường (có sẵn), Súng chùm (5 viên tỏa 50°), Súng máy (4 phát/giây, từ cấp 5), Lưỡi cưa xoay (3 lưỡi) |
| Nâng cấp trong màn (8) | Sát thương +20%, Tốc độ bắn +15%, Tốc độ chạy +10%, Máu tối đa +20, Hồi máu +30, 3 nâng cấp mở khóa vũ khí |
| XP trong màn | Cấp 1 cần 5 XP, mỗi cấp sau cần thêm 5; orb bị hút trong bán kính 2.5 |
| Màn chơi | Đấu trường Tilemap: sân cát 30×30 ô, viền đá, đường đất, bãi cỏ, bụi cây |
| UI | HUD (đợt i/n + thanh tiến độ màn, banner "ĐỢT TINH ANH!" / "BOSS XUẤT HIỆN!" / "BOSS NỔI GIẬN!"), thanh máu boss (tên + vạch ngưỡng pha), bảng lên cấp, menu ESC (slider âm lượng), bảng hồi sinh, màn kết quả (sao, thưởng, Màn tiếp/Chơi lại/Về menu), menu chính + bảng Cài đặt |
| Hiệu ứng | Trúng đạn, quái chết, lên cấp, nháy đỏ khi trúng đòn, rung camera, số sát thương (gộp 0.2s) |
| Hoạt ảnh | Bằng code: thở khi đứng, lắc + co giãn theo bước khi chạy, quái phóng to lúc xuất hiện |
| Âm thanh | Bắn, trúng, quái chết, bị đánh, nhặt XP, lên cấp, chọn nâng cấp, rê/bấm nút, Game Over, nhạc nền |

## Tiến độ (theo `Docs/project-plan.md`)

| Phase | Trạng thái |
|---|---|
| 0–5 – Core gameplay | Xong (gồm tối ưu GC và đợt cấu trúc lại 2026-10-01) |
| 6A – Màn chơi | Xong: StageData, StageSession, đợt tinh anh, độ khó, sao, hồi sinh, StageResult, màn kết quả |
| 6B – Boss | Xong: hệ hành vi quái (SO), Xạ thủ, đạn của quái, mini-boss, boss 3 pha, thanh máu boss |
| 7 – Hồ sơ người chơi, save v2, tiền tệ | Xong: PlayerProfile, SaveService (tmp/bak/HMAC/migration), EconomyService, tiến độ màn, scene Boot |
| 8 – Trang bị, tài nguyên, Talent | **Tiếp theo**: ItemData/ItemDatabase, InventoryService, nâng cấp/đột phá, mảnh vũ khí, Talent, StatCalculator + lực chiến, giáp, LootTable |
| 9 – Hub và level tài khoản | Chưa |
| 10 – Nội dung và cân bằng | Chưa |
| 11 – Cửa hàng, IAP, quảng cáo | Chưa |
| 12 – Giữ chân, analytics, cloud save | Chưa |
| 13 – Mobile, tối ưu, playtest | Chưa |
| 14 – Soft launch | Chưa |

Tính năng cũ **giữ chạy cho tới khi có thứ thay thế**: MainMenu (→ Hub, Phase 9; nút "Chơi" vào màn 1, chơi nối tiếp qua [Màn tiếp]).

## Vấn đề đã biết / lưu ý

- Mỗi lần build, benchmark có thể có 1 frame tệ bất thường (đã gặp 10 ms, chạy lại 2 lần còn 3.7–4.8 ms): chạy lại trước khi kết luận.
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
