# Arena Survival — ghi chú dự án

Game 2D top-down action survival **thương mại trên mobile** (Android trước, iOS sau), kiếm tiền từ **bán vật phẩm (IAP)**
và **quảng cáo có thưởng**. Tham khảo thể loại: Survivor.io, Archero. Có hai vòng lặp: trong màn (bắn, lên cấp, chọn kỹ năng,
reset sau màn) và ngoài màn (trang bị, Talent, level tài khoản, lưu vĩnh viễn).

- Kế hoạch: `Docs/project-plan.md` (Phase 0–14, kinh tế, pháp lý). **Đọc trước khi làm phase mới.**
- Spec/plan từng đợt việc: `Docs/specs/`, `Docs/plans/`. Thiết kế bản core cũ: `Docs/archive/core-design-v1.md`.
- Giấy phép asset: `Docs/asset-licenses.md` (asset mới phải cho phép dùng thương mại và được ghi vào đây).
- Cài đặt Unity Gaming Services (môi trường, khóa Remote Config, sự kiện Analytics): `Docs/ugs-setup.md`.

Cập nhật lần cuối: 2026-10-02, sau Phase 12B (code nối Unity Gaming Services: đăng nhập ẩn danh, Cloud Save, Remote Config, Analytics, giờ máy chủ — chờ bật dashboard để thử kết nối thật).

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
- Unit test: `run_tests --mode EditMode` (hiện 348/348 đạt), hoặc Window → General → Test Runner.
- Hiện Editor mở scene nào thì Play chạy scene đó (dịch vụ vẫn tự khởi tạo). Muốn test cả vòng như bản build thì mở `Boot.unity` trước.
- **Editor và bản build dùng CHUNG file save** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\ArenaSurvival\save.json` (+ `.bak`):
  test trong Play mode cộng vàng/sao thật vào save của người dùng. Sao lưu file trước khi test thứ có thể phá dữ liệu.
- Build Development: `build --target StandaloneWindows64 --outputPath "Builds/Dev/ArenaSurvival.exe" --options '["Development"]' --confirm true`,
  rồi theo dõi `build_status`. `Builds/` đã được git bỏ qua.
- ffmpeg đã cài qua winget (`Gyan.FFmpeg`), dùng để đo/chuyển đổi âm thanh.

## Cấu trúc project

```
Docs/                   project-plan.md, asset-licenses.md, specs/, plans/, archive/, Balance/ (enemies/waves/stages.csv, sim-report.md)
Assets/_Project/
├── Scripts/            assembly ArenaSurvival (ArenaSurvival.asmdef); namespace = ArenaSurvival.<tên thư mục>
│   ├── Core/           GameManager, GameState, GameEvents, ObjectPool, SceneLoader (+ FirstSceneOverride), PauseInput, CameraFollow,
│   │                   Tuning (số liệu chỉnh từ xa: độ khó, thưởng, giá hồi sinh, lượt QC)
│   ├── Player/         PlayerController (+MoveOverride cho bot), PlayerStats (+đa nòng/xuyên/nam châm), PlayerDeathHandler, PlayerLoadout,
│   │                   PlayerShield, PlayerAura, PlayerMissiles, PlayerLightning, PlayerDrones (kỹ năng trong màn, tắt sẵn trên prefab),
│                   HeroFx (vòng sáng dưới chân, vệt chạy), PlayerControls.inputactions
│   ├── Combat/         Health (+giáp, ShieldLayers), IHitReceiver (đòn có hướng), HealthRegen, CombatRules (hàm thuần: công thức giáp), Projectile (tầm, xuyên, nổ vùng, đẩy lùi)
│   ├── Stats/          StatKind, StatBlock (+StatRules), HeroBaseStats (SO)
│   ├── Items/          Rarity/EquipSlot (+EquipSlotIds), ItemData, MaterialData, ItemDatabase (Resources), EquipmentRules (hàm thuần),
│   │                   RarityColors
│   ├── Loot/           LootDrop, LootTable, LootRules (hàm thuần), LootOdds (+LootOddsRules: tỷ lệ công bố), LootDropper (rơi đồ trong màn, gom đồ đã nhặt), LootPickup
│   ├── Weapons/        PlayerWeapon (súng chính, Equip theo WeaponStats), OrbitWeapon, SawBladeRig, WeaponDefinition/IWeapon,
│   │                   WeaponData (+ meta: id, mảnh, hiệu ứng sao, thuộc tính đạn), WeaponRules (+WeaponStats, WeaponStarBonus), OrbitWeaponData
│   ├── Enemies/        Enemy (lõi), EnemyData, EnemyModifiers, EnemyContext (trạng thái mỗi con), IEnemyWorld, EnemyRules (hàm thuần),
│   │                   EnemyBehaviour (+OnDeath) → ChaseBehaviour, KeepDistanceShooterBehaviour, ChargerBehaviour, KamikazeBehaviour,
│   │                   SplitterBehaviour, HealerBehaviour, SniperBehaviour, ShieldBehaviour, BossBehaviour (+BossPhase);
│   │                   BossAttack → RadialBurstAttack, DashAttack, SummonAttack, SpiralAttack, ArtilleryAttack;
│   │                   EnemyProjectiles (pool đạn chung + quả pháo ArtilleryShell)
│   ├── Stages/         StageSession (điều phối màn), WaveSpawner, StageData + EliteGroup, WaveData, StageCatalog (+chapters), ChapterData (+arenaPrefab),
│   │                   ArenaLoader (đổi nền theo chương, trong Game + MainMenu), InfiniteGround + GroundRules (nền vô tận theo chunk),
│   │                   StageSelection (màn đang chọn, static), StageResult, StageRules (hàm thuần: độ khó, sao, thưởng), RunStats
│   ├── RunUpgrades/    Lên cấp + chọn kỹ năng TRONG MÀN: ExperienceSystem, XpCurve, XpOrb, XpOrbSpawner, UpgradeData (+maxLevel, mô tả theo cấp),
│   │                   UpgradeManager (cấp từng kỹ năng), SkillRules (hàm thuần: số theo cấp), RandomPicker
│   ├── Save/           SaveService (tmp → File.Replace + .bak, HMAC, khôi phục), SaveSecurity (HMAC-SHA256), SaveMigrations (JObject, v1→…→v7)
│   ├── Meta/           PlayerProfile, ProfileService, StageProgressService (+Rules, StageGrant), GameServices (static), ServicesHost, BootLoader,
│   │                   TalentData, TalentCatalog, TalentRules (giá), TalentService, StatCalculator (chỉ số cuối + lực chiến),
│   │                   OwnedItem, InventoryService (kho đồ), OwnedWeapon, WeaponService (vũ khí chính), LoadoutStats (chỉ số + lực chiến),
│   │                   AccountRules, AccountLevelTable (+FeatureIds), AccountService (level tài khoản), RedDotRules, ProgressionSim
│   ├── Data/           CsvReader, BalanceRows (EnemyRow/WaveRow/StageRow) — đọc bảng cân bằng (hàm thuần)
│   ├── Shop/           ShopProductData (+ShopContent), ShopCatalog (Resources), ShopRules, ShopService (trao nội dung, mua bằng kim cương, rương),
│   │                   IStore (+StorePurchase), FakeStore / UnavailableStore, IReceiptValidator (FakeReceiptValidator), IAPService
│   ├── Services/       AdsService (+IAdProvider), AdRules + AdPlacements, FakeAdProvider / UnavailableAdProvider,
│   │                   GameClock (+ITimeSource, ClockRules), AnalyticsService (+IAnalyticsSink, Local/Null sink), AnalyticsTracker,
│   │                   RemoteConfigService (+IRemoteConfigProvider, File/Null provider), CloudSaveRules (+CloudSummary), CloudSyncService
│   │                   (+ICloudStorage, CloudSnapshot); adapter UGS: UgsBootstrap, UgsRemoteConfigProvider, UgsAnalyticsSink, UgsCloudStorage
│   ├── Missions/       MissionData (+MissionMetric, MissionReward), MissionCatalog (Resources), MissionRules, MissionService (nhiệm vụ ngày, mốc, quà 7 ngày)
│   ├── Economy/        EconomyService (đường duy nhất đổi tiền tệ VÀ nguyên liệu; TrySpendMany, CanAfford; OnTransaction/OnSpent có lý do),
│   │                   CurrencyIds, CurrencyCost, EconomyReasons (+CurrencyTransaction)
│   ├── UI/             HudController, LevelUpPanel(+Animator), UpgradeOptionButton, PauseMenu, StageResultPanel, RevivePanel, BossHealthBar, SafeAreaFitter,
│   │                   TalentPanel, TalentRow, EquipmentPanel, ItemDetailPopup, ItemSlotView, ItemTextFormatter, LootRowView, WeaponPanel, WeaponRowView,
│   │                   TimeFormatter, UiHoverScale, VolumeSlider
│   │   ├── Hub/        HubController, HubTopBar, HubTab, BattleTab, StageNodeView, LevelUpPopup, RewardFly, HubToast, ConsentPopup, CloudConflictPopup
│   │   ├── Shop/       ShopPanel, ShopCardView, ShopConfirmPopup, ShopResultPopup, ShopOddsPopup
│   │   └── Missions/   MissionPanel, MissionRowView, LoginDayView, MilestoneView, RewardIcons
│   ├── Audio/          AudioManager, SoundBank, SoundEffect, ButtonSound, VolumeSettings
│   ├── Vfx/            VfxSpawner, PooledVfx, FloatingText, HitFlash, DamageNumberSource, SpriteBodyAnimator
│   └── DevTools/       Autoplay/ (bot tự chơi: AutoplayRunner, AutoplayHub, BotPilot, BotSteering, AutoplayRules), StressTest (F9), PerfRecorder (F10), BenchmarkRunner, DevMenu (F1: xem hồ sơ, +vàng/kim cương, mở khóa màn,
│                       xóa hồ sơ, +nguyên liệu, nhận trang bị ngẫu nhiên, +20 mảnh mỗi vũ khí, tới đợt cuối, thắng ngay, bất tử). Chỉ Editor/Development build; tự tạo, sống xuyên scene
├── Editor/             assembly ArenaSurvival.Editor: BalanceImporter, BalanceExporter, ProgressionSimMenu, BalanceAssets (menu ArenaSurvival/...)
├── Tests/EditMode/     assembly ArenaSurvival.Tests.EditMode: XpCurve, RandomPicker, SaveSystem, TimeFormatter, WeaponSpread,
│                       VolumeSettings, ObjectPool, StageRules, StageCatalog, EnemyRules, EconomyService, SaveService, StageProgress,
│                       StatRules, TalentService (+ migration v3), EquipmentRules, LootRules, InventoryService (+ migration v4),
│                       StageLootApply, WeaponRules, WeaponService (+ migration v5), AccountService, RedDotRules, CsvReader,
│                       BalanceRows, Chapter, ProgressionSim, ArenaLoader, Autoplay, SkillRules, GroundRules, Shop (+ MemoryStore, migration v6), Mission (+ đồng hồ, Tuning, analytics, migration v7), CloudSave
├── Resources/        ItemDatabase.asset, AccountLevelTable.asset, ShopCatalog.asset, MissionCatalog.asset (GameServices nạp bằng Resources.Load vì dịch vụ tự khởi tạo trước mọi scene)
├── Scenes/             Boot.unity (0), MainMenu.unity (1, chứa Hub — chưa đổi tên scene), Game.unity (2)
├── link.xml            giữ namespace ArenaSurvival.Meta + CloudSummary khi build IL2CPP (Newtonsoft đọc bằng reflection)
├── Data/               Enemies/ (+ Behaviours/), Bosses/ (Boss_*, BossBehaviour_*, Attack_*), Weapons/, RunUpgrades/,
│                       Stages/ (Waves/Wave_C1..C3_*, Stage_C1..C3_01..10, Chapter_C1/C2/C3, StageCatalog_Main), Audio/ (SoundBank), Stats/ (HeroBaseStats),
│                       Talents/ (Talent_*, TalentCatalog_Main), Items/ (Item_*, Materials/Mat_*), Loot/ (Loot_*, Loot_Chest_*), Shop/ (Shop_*), Missions/ (Mission_*)
├── Prefabs/            Player, Enemy (dùng chung cho mọi quái và boss; con `Telegraph` = tia ngắm), Projectile, EnemyProjectile, XpOrb, AudioManager,
│                       Ground_Desert/City/Base (nền vô tận 3 chương; Arena/Arena_City/Arena_Base cũ là nguồn lớp tilemap, không còn dùng trong scene),
│                       Vfx/* (+ArtilleryShell),
│                       LootPickup, UI/VolumeSettings, UI/TalentPanel, UI/TalentRow, UI/EquipmentPanel, UI/ItemSlot, UI/ItemSlotSmall, UI/WeaponRow, UI/StageNode, UI/ShopCard, UI/LoginDay, UI/MissionRow,
│                       Projectile_Grenade, Projectile_Laser, Vfx/Vfx_Explosion, Vfx/Vfx_HealPulse
├── Art/                Kenney-TopDownShooter, Tiles (Tilemap), UI (sprite + icon tự vẽ), Player (nhân vật, lóe nòng, drone — tự vẽ), Enemies, Fonts, Vfx, Weapons, Pickups, Projectiles
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
- **Chỉ số & Talent (Phase 8A)**: chỉ số meta là `StatBlock` (máu, giáp, % sát thương, % tốc chạy, hồi máu/giây).
  `StatCalculator.Compute(heroBase, catalog, levelOf)` cộng gốc + Talent; overload có tham số trang bị (8B) cộng thêm tổng chỉ số đồ đang mặc.
  `PlayerLoadout` (trên prefab Player) tính 1 lần trong `Awake` rồi đặt vào `Health` (máu tối đa, `Armor`), `PlayerStats.SetBaseMultipliers`
  (hệ số gốc, nâng cấp trong màn vẫn nhân chồng lên) và `HealthRegen`. Giáp: `sát thương × 100 / (100 + giáp)`.
  Lực chiến = máu×1 + giáp×6 + %st×12 + %tốc×4 + hồi×40 (chỉ để hiển thị). Talent: giá `floor(gốc × 1.12^cấp)`, nâng qua
  `TalentService.TryUpgrade` (trừ vàng bằng `EconomyService`, lưu ngay, phát `OnTalentChanged`). Cấp lưu trong `PlayerProfile.talents`
  theo `id` (đừng đổi id của Talent đã phát hành). Talent mới = 1 asset + thêm vào `TalentCatalog_Main`; chỉ số mới = thêm `StatKind`.
  Bảng Talent là prefab độc lập `UI/TalentPanel`, nằm trong tab Talent của Hub.
- **Trang bị & rơi đồ (Phase 8B)**: mỗi lần rơi là 1 món riêng (`OwnedItem {uid, id, rarity, level, ascension}`, uid = "i" + bộ đếm);
  độ hiếm quay theo từng món (1 `ItemData` ra được 4 độ hiếm). 2 ô: Giáp, Phụ kiện (Vũ khí ở 8C); khóa ô trong save là chuỗi
  `EquipSlotIds`, giá trị số của `Rarity` **không đổi**. Nguyên liệu (`ore`, `leather`, `boss_core`) là tiền tệ phụ trong `EconomyService`.
  Công thức (`EquipmentRules`): chỉ số ×1/1.5/2.2/3.2 theo độ hiếm, trần cấp 10/20/30/40, đột phá mỗi 10 cấp (lõi boss),
  nâng cấp `floor(100 × 1.15^(cấp−1) × 1/1.2/1.5/2)` vàng + `1 + cấp/2` da, phân rã = nguyên liệu gốc + 70% đã chi, kho 100 món
  (đầy thì đồ mới tự phân rã). `InventoryService.Add` không tự lưu (áp thưởng hàng loạt), thao tác của người chơi lưu ngay.
  Món có id không còn trong `ItemDatabase` bị bỏ qua khi tính chỉ số nhưng **giữ trong save**.
  Rơi đồ (phương án A): quái chết → `Enemy.HandleDied` → `IEnemyWorld.NotifyKilled` → `WaveSpawner.OnEnemyKilled` → `LootDropper`
  quay bảng (`EnemyModifiers.loot` của tinh anh/boss thắng `EnemyData.loot`) → `LootPickup` (pool) → nhặt thì ghi vào `Collected`.
  `DespawnAll` (nổ theo boss) không rơi đồ. Hết màn: thắng thì gom hết pickup + quay `StageData.victoryLoot` (thắng lần đầu 2 lần) vào
  `StageResult.bonusLoot`; `StageProgressService.Apply` cộng nguyên liệu luôn, trang bị chỉ khi thắng (thua → `StageGrant.lostItems`).
  Màn kết quả đọc `StageGrant` (hàng "Vật phẩm nhận được", đồ mất hiện mờ). Bảng Trang bị là prefab `UI/EquipmentPanel` (nút "Trang bị").
- **Vũ khí chính (Phase 8C)**: ngoài màn chọn 1 khẩu (ô `equipped["weapon"]` lưu **id vũ khí**, mỗi khẩu 1 bản: `weapons[id] = {level, stars}`).
  `WeaponService` (mở khóa/lên sao bằng mảnh `shard_<id>` — nguyên liệu trong `EconomyService`, nâng cấp bằng vàng + quặng, mặc).
  Súng lục luôn có (`WeaponService.EnsureDefaults` ở migration v5, `ToProfile`, `ResetToNew`). Công thức `WeaponRules`: mảnh 10/20/40/80/150,
  hệ số ×1/1.25/1.5/1.8/2.2, trần cấp 10×sao, mỗi cấp +8% sát thương gốc, hiệu ứng riêng ở 3★/5★ (`WeaponStarBonus`).
  Kiểu bắn = dữ liệu (phương án A): `WeaponData` có xuyên, nổ (bán kính + % lan), đẩy lùi, lệch ngẫu nhiên, tầm, prefab đạn riêng;
  `Projectile.Launch` nhận hết (đạn quái gọi bản ngắn, không có hiệu ứng). Tầm: đạn tự hủy theo `range / speed`. Nổ phát
  `GameEvents.Explosion` (VFX + âm thanh). `PlayerLoadout` gọi `PlayerWeapon.Equip(data, WeaponRules.Effective(...))` lúc vào màn.
  Player chỉ còn 1 `PlayerWeapon` + `OrbitWeapon`; bảng lên cấp chỉ còn mở khóa Lưỡi cưa. Lực chiến = `StatCalculator.Power` + `WeaponRules.Power`
  (`LoadoutStats.Power`). Mảnh: tinh anh 10%, boss/mini-boss `alsoRoll` → `Loot_WeaponShards` (3–5), quà thắng lần đầu
  `StageData.firstClearReward` (1-1 Shotgun, 1-2 Súng máy, 1-3 Phóng lựu). Bảng Vũ khí: prefab con `WeaponPanel` trong `EquipmentPanel`.
- **Hub & level tài khoản (Phase 9)**: level **suy ra từ `accountXp`** (không lưu riêng, không đổi save): `AccountRules.XpToNext = round(100 × cấp^1.5)`,
  tối đa 50; đạt cấp N thưởng vàng 100×N + kim cương 5 (cấp chia hết 5: 20). `AccountService.AddXp` (gọi từ `StageProgressService.Apply`)
  cộng thưởng qua `EconomyService` và xếp hàng `LevelUpInfo`; Hub hiện popup lần lượt khi người chơi quay về. XP có sẵn trong save cũ
  không được thưởng hồi tố. Mở khóa tính năng theo `Resources/AccountLevelTable` (Trang bị 1, Talent 2, Cửa hàng 5); không có trong bảng = mở từ đầu.
  Hub (scene `MainMenu.unity`, menu cũ + `MainMenu.cs` đã gỡ): thanh trên (`HubTopBar`: level, XP, vàng/kim cương chạy số), 5 tab
  (`HubTab`: Cửa hàng 🔒 "Sắp ra mắt", Trang bị, **Chiến đấu** mặc định, Talent, Sự kiện 🔒), `EquipmentPanel`/`TalentPanel` nhúng trong vùng tab
  (tắt nền tối + nút Đóng, co hộp theo vùng — chỉnh trên instance), `BattleTab` (chọn màn, sao, khóa, lực chiến so với `StageData.recommendedPower`),
  chấm đỏ theo `RedDotRules`, tiền bay vào thanh trên khi vừa chơi xong màn (`RewardFly`, `HubTopBar.HoldCounters`), bảng Cài đặt chứa nút Thoát.
  `SafeAreaFitter` trên gốc Hub và HUD/thanh máu boss trong màn.
- **Bảng cân bằng (Phase 10A)**: `Docs/Balance/enemies.csv`, `waves.csv`, `stages.csv` (UTF-8 có BOM, `;` ngăn phần tử trong 1 ô) là nguồn số liệu
  của quái/đợt/màn; menu `ArenaSurvival > Nhập bảng cân bằng` tạo/sửa asset tại chỗ (giữ GUID). Id: quái và đợt = **tên asset**
  (`Enemy_Basic`, `Wave_C1_03a`), màn = `StageData.id`. "Tất cả hoặc không gì": lỗi ở bất kỳ dòng/tham chiếu nào thì không ghi gì.
  Ô trống = giữ giá trị đang có (asset chỉnh tay chỉ đổi đúng ô bảng ghi); màu so theo hex (không làm tròn màu lẻ). Nhập lại phải báo
  "không đổi". `Xuất bảng cân bằng` ghi CSV từ asset. Boss: pha/đòn dựng tay, chỉ số qua CSV. `StageData.stageNumber` là số thứ tự
  **toàn cục** (chương 2 từ 11) để độ khó/thưởng tăng qua các chương. `StageCatalog` giữ cả `chapters` lẫn `stages` phẳng (công cụ ghi cả hai).
  `Mô phỏng tiến trình` (`ProgressionSim`): người không nạp, thắng theo lực chiến/đề xuất (0% dưới 75% → 100% từ 125%), ghi
  `Docs/Balance/sim-report.md` — dùng để chỉnh `recommendedPower` (Chương 1: ~50 phút, 1-5 ≈ 2 lượt, 1-10 ≈ 3 lượt).
- **Quái mới (10A)**: Charger (gồng 0.6 s rồi lao), Kamikaze (chớp rồi nổ r=1.8, tự nổ thì **không** XP/đồ — `Despawn(false)`),
  Splitter (`OnDeath` → 2 `Enemy_SplitterSmall`; không gọi khi nổ theo boss), Healer (giữ khoảng cách, 4 s hồi 20% máu quái quanh r=3.5,
  `GameEvents.EnemyHealPulse` → vòng xanh). `EnemyBehaviour.OnDeath` chỉ gọi khi chết thật.
- **Chương 2 (10B)**: sân theo chương = `ChapterData.arenaPrefab` (C1 `Arena`, C2 `Arena_City`). `ArenaLoader` (Awake, chạy sớm) trong `Game.unity`
  (chương của `StageSelection.Current`) và `MainMenu.unity` (chương của màn nên chơi tiếp → nền Hub đổi theo tiến độ): khác sân có sẵn thì tắt
  instance trong scene và tạo prefab chương **cùng vị trí**. Mọi sân dùng chung Grid 0.64 và phạm vi ô, chỉ là nền (không collider) — tường
  biên vẫn nằm trong scene, nên sân mới phải giữ đúng khung ô. `Arena_City` dựng bằng script (tô lại từ `Arena`), không vẽ tay.
  Sniper (`SniperBehaviour`): giữ khoảng 8, ngắm 1 s bằng tia đỏ (`EnemyContext.Telegraph` = LineRenderer con của prefab `Enemy`, tắt sẵn,
  `ResetVisual` tắt khi chết/trả pool), 0.25 s cuối chốt hướng rồi bắn 1 viên tốc 16. `SpiralAttack`: bắn liên tục vài cánh, góc xoay đều
  (`EnemyRules.SpiralAngle`). Biến thể quái C2 = asset `Enemy_C2_*` qua CSV (sprite cùng bộ, tư thế/màu khác). Bảng rơi riêng `Loot_*_C2`.
- **Chương 3 + cân bằng (10C)**: Lính Khiên (`ShieldBehaviour`, xoay chậm 90°/s, chặn 85% đòn có hướng trong cung 120° phía trước —
  đạn gọi `IHitReceiver.TakeHit(dmg, hướng)` → `EnemyBehaviour.ModifyIncomingDamage`; nổ/lưỡi cưa/vòng điện không hướng → không bị chặn; số xám).
  `ArtilleryAttack` (Mưa Pháo): vòng đỏ báo trước, quả `ArtilleryShell` (pool trong `EnemyProjectiles.PlaceShell`) tự đếm rồi nổ.
  **Luật đợt cuối**: hạ hết tinh anh là quái thường còn sót nổ (`StageRules.EliteWaveCleared`); màn boss: boss xuất hiện thì quái thường nổ,
  giữ tinh anh. `Enemy.SpawnId` để biết tham chiếu tới quái còn đúng con đó (object trong pool tái dùng). Độ khó **+9%/màn** (trước 12%).
  Hồi sinh: bất tử 3 s, đẩy quái r=5. Player Rigidbody mass 10 (quái không ép vào góc được). Charger chạm thấp hơn.
- **Kỹ năng trong màn có cấp (10C)**: `UpgradeData.maxLevel` (0 = vô hạn, không tính giới hạn — Hồi máu), tối đa 8 kỹ năng/màn,
  thẻ ghi "Cấp a → b"/"MỚI". Kỹ năng: Sát thương, Tốc bắn, Máu, Tốc chạy, Hồi máu, Đa nòng (+1 viên/cấp, quạt 10°), Đạn xuyên,
  Lưỡi cưa (3 → 8 lưỡi, +10%/cấp; `OrbitWeapon.SetLevel` dựng lại lưỡi), Lá chắn (`Health.ShieldLayers`, hồi 8→4 s, cấp 5 hai lớp),
  Vòng điện, Tên lửa (đạn lựu, pool riêng), Nam châm. Số theo cấp ở `SkillRules`. Enum `UpgradeType` chỉ thêm ở cuối.
  Vũ khí chính +12% sát thương gốc/cấp; sao 3 thêm 1 viên (Súng lục, Súng máy, Laser).
- **Bản đồ vô tận + hình ảnh (10D)**: không còn tường/sân giữa. `InfiniteGround` tô chunk 16×16 ô quanh camera (bán kính 2), xóa chunk xa;
  chọn tile bằng `GroundRules` (hash + nhiễu, tất định — quay lại thấy y hệt); mảng phụ dạng **khối chữ nhật** (`blockSize`) vì tile Kenney
  không có mảnh chuyển tiếp cho mép cong. `WaveSpawner._endless`: quái sinh trên vòng ngay ngoài màn hình quanh camera, `ClampToArena`
  trả nguyên vị trí, quái xa hơn 1.8× nửa đường chéo màn hình được đưa lại vòng sinh (không "bỏ" quái được). Bot dùng `PlayArea` (vô hạn).
  Nhân vật `Art/Player/hero.png` (Visual ×1.3), bóng, `GroundRing`, `RunTrail`, lóe nòng (`PlayerWeapon._muzzleFlash`). Vệt đạn: TrailRenderer
  trên prefab đạn, `Projectile.Launch` gọi `Clear()` (đạn từ pool). Lưỡi cưa: quầng + vệt, cấp 6 "thức tỉnh" lửa cam (+30%). Vòng điện xoay/nảy,
  cấp 5 vàng; lá chắn lấp lánh + chớp khi chặn. Kỹ năng mới: Sét lan (LineRenderer dựng sẵn), Drone hộ vệ (pool đạn riêng).
  Hiệu ứng dùng chung `Art/Vfx/Vfx_Particle.mat` (URP unlit) — không dùng `Shader.Find` (dễ bị loại khỏi bản build). Độ khó **+7.5%/màn**.
- **Cửa hàng, IAP, quảng cáo (11A — giả lập)**: luồng IAP `IAPService`: store giao giao dịch → `IReceiptValidator` → `ShopService.Grant`
  + ghi `transactionId` vào `profile.processedTransactions` → **lưu** → `IStore.Confirm` (giao lại = chỉ xác nhận, không trao trùng; tắt
  game trước khi lưu = store giao lại lần khởi tạo sau). Gói mua-1-lần (`purchaseLimit == 1`) là non-consumable, khôi phục được.
  Editor/bản dev dùng `FakeStore` (giao dịch dở dang + gói đã sở hữu trong **PlayerPrefs**, chế độ Success/Fail/Cancel/SuccessNoConfirm ở
  DevMenu) và `FakeAdProvider` (lớp phủ đếm ngược 3 s, `Duration = 0` cho bot); bản phát hành chưa có SDK dùng `UnavailableStore`/
  `UnavailableAdProvider`. 11B chỉ thêm `UnityIapStore`, validator thật, `LevelPlayAdProvider`. Quảng cáo: `double_reward` 5, `revive` 3,
  `free_chest` 2 lượt/ngày UTC; đã mua Xóa quảng cáo → thưởng ngay (vẫn tính lượt). Hồi sinh: quảng cáo hoặc 20 kim cương (1 lần/màn).
  Màn kết quả: nhân đôi vàng của lượt đó (1 lần). Tỷ lệ rương tính từ `LootTable` (`LootOddsRules`). Save **v6**: `processedTransactions`,
  `purchaseCounts`, `removeAds`, `adViews`/`adDay`, `consent` (-1 chưa hỏi). Popup quyền riêng tư lần đầu vào Hub + nút trong Cài đặt.
  `GameServices` có thuộc tính `Shop` trùng tên namespace → trong `Meta/GameServices.cs` viết đầy đủ `ArenaSurvival.Shop.*`.
  Mỗi MonoBehaviour 1 file cùng tên (nếu không prefab mất script).
- **Giữ chân + dịch vụ (12A — cục bộ)**: `GameClock` = giờ máy + độ lệch máy chủ (12B), không lùi về trước `profile.lastSeenUtcTicks`
  (chỉnh lùi giờ → đồng hồ đứng; chỉnh tiến chỉ chặn được khi có giờ máy chủ). Quảng cáo, nhiệm vụ, quà dùng chung đồng hồ, ngày = UTC.
  `MissionService`: 5 nhiệm vụ ngày cố định (chơi 3 màn, hạ 300 quái, nâng cấp 2 lần, xem 1 QC, tiêu 1000 vàng; mỗi cái 20 điểm), mốc
  40/70/100 điểm (500 vàng / 15 KC + 1000 vàng / 25 KC + rương thường), quà 7 ngày (bỏ ngày không mất chuỗi, hết 7 thì lặp). Đếm **không**
  bằng hook rải rác: `EconomyService.OnTransaction` (tiêu vàng), `OnSpent(lý do)` (1 lần nâng cấp dù trừ nhiều loại; mở khóa vũ khí không tính),
  `AdsService.OnRewarded`, `ShopService.OnProductGranted` (rương), còn kết quả màn do `GameServices` gọi `TrackStage` sau `Apply`.
  Mọi `Add/TrySpend` nên truyền lý do `EconomyReasons` (thiếu = `unknown` trên analytics). `AnalyticsService` chỉ gửi khi `consent == 1`;
  `AnalyticsTracker` gom mọi sự kiện (session_start, stage_start/end + đợt thua, currency + lý do, purchase, ad_reward, level_up,
  mission/login_claim); bản dev ghi `analytics-log.jsonl` cạnh save. Remote Config: `Tuning` (mặc định = số trong game, khóa sai/ngoài khoảng
  bị bỏ qua), bản dev đọc `remote-config.json` cạnh save, lưu bản tạm `remote-cache.json`; áp lúc khởi tạo dịch vụ (đổi file + mở lại game
  hoặc DevMenu "Nạp lại"). `StageRules.DifficultyGrowthPerStage`/`RewardGrowthPerStage` giờ là property đọc `Tuning`; test đổi `Tuning`
  phải `Tuning.Reset()` ở TearDown. Save **v7**: `daily`, `login`, `lastSeenUtcTicks`. Tab Sự kiện → **Nhiệm vụ** (`featureId` `missions`, mở từ
  đầu, chấm đỏ = `HasClaimable`). DevMenu: "Giả lập sang ngày mới" (xóa mốc ngày đã lưu — không dịch đồng hồ), "Xong hết nhiệm vụ".
- **Unity Gaming Services (12B)**: gói Authentication 3.8, Cloud Save 3.4.1, Remote Config 4.2.5, Analytics 6.3 (asmdef tham chiếu). Chỉ là adapter cho
  interface 11A/12A. `GameServices.UsingUgs` = bản phát hành luôn bật; Editor/dev chỉ khi `PlayerPrefs dev_use_ugs = 1` (DevMenu, mở lại game);
  bot (`SavePathOverride`) không bao giờ. Môi trường: dev → `development`, phát hành → `production`. `UgsBootstrap`: init → đăng nhập ẩn danh →
  giờ máy chủ (header `Date` của HEAD tới `player-auth.services.api.unity.com`) → `GameClock.SetServerOffset` → gọi các `WhenReady`; lỗi thì
  `ServicesHost.Update` thử lại sau 60 s. Analytics: `CustomEvent` (sự kiện + tham số phải khai báo trên dashboard), đồng ý qua
  `UnityEngine.UnityConsent.EndUserConsent` (struct `ConsentState`), `AdsService.OnConsentChanged` báo khi đổi. Remote Config đọc `GetJson(key)`; sự kiện analytics xếp hàng tới khi SDK nhận đồng ý Granted (gửi sớm hơn bị SDK bỏ im lặng)
  (GetString đoán ngày tháng). Cloud Save: khóa `profile` (nguyên chuỗi save có HMAC, `SaveService.ToEnvelope/TryParseEnvelope`) + `profile_meta`
  (`CloudSummary`); syncId đã đồng bộ lưu ở **PlayerPrefs** `cloud_sync_<playerId>` (trạng thái lần cài → không đổi save, không vòng lặp lưu).
  `CloudSaveRules.Decide`: không có bản mây / bản mây do máy này tải / mây rỗng → tải lên; máy rỗng → lấy bản mây; còn lại → hỏi
  (`CloudConflictPopup` trong Hub). Chờ quyết định thì không tải lên. Lấy bản mây: `ProfileService.ReplaceWith` (PopulateObject vào CÙNG object,
  collection thay mới) + `MergePurchases` (xóa QC, mã giao dịch, số lần mua) → lưu → nạp lại Hub. Tải lên: tối đa 1 lần/60 s sau `OnSaved`,
  và ngay khi chuyển nền/thoát.
- **Mật độ + thẻ theo màn (10E A+B)**: mỗi nhịp sinh `StageRules.SpawnDensity(n)` con (cộng dồn phần lẻ, `SpawnCount`), chương đầu ×1.8 giảm về ×1
  ở màn 21; đàn mở màn 5 con; trần 220 quái (chỉ chặn sinh theo nhịp). Máu Basic 12, Fast 7, Súng lục 2 phát/giây. Thẻ lên cấp nhân
  `SkillRules.StageScale(n) = 1 + 0.03(n−1)` (`PlayerStats.StageScale`, StageSession đặt): thẻ chỉ số hiện số đã nhân (`StatDescription`,
  bỏ qua chữ trong asset), kỹ năng gây sát thương nhân thêm và ghi "Sức mạnh theo màn ×…". Mọi số chỉnh bằng Remote Config (`Docs/ugs-setup.md`).
- **Chiêu vũ khí + mana (10E C)**: `PlayerUltimate` (trên prefab Player) giữ mana (`UltimateRules`: tối đa 100, +4/giây, +1/quái hạ, dừng khi
  pause/lên cấp), dùng chiêu `WeaponData.ultimate` bằng nút HUD (`UltimateButton`, vòng mana + đếm ngược), phím Space hoặc tự dùng
  (PlayerPrefs `auto_ultimate`, công tắc trong prefab `UI/VolumeSettings` → có cả ở Hub và menu tạm dừng; bot `ForceAuto`). Chiêu = asset
  `WeaponUltimate` (không giữ trạng thái, chạy coroutine `Run(UltimateContext)`), kiểu: `BulletRingUltimate` (Súng lục — Bắn loạn),
  `WeaponBuffUltimate` (Shotgun — Đạn chùm: đạn nổ 5 s; Súng máy — Cuồng nộ: bắn ×3 4 s; qua `PlayerWeapon.SetBuff/ClearBuff`),
  `GrenadeRainUltimate` (Phóng lựu — Mưa lựu), `BeamSweepUltimate` (Laser — tia `LineRenderer` dựng sẵn, quét 360°). Sát thương = bội số
  `PlayerWeapon.CurrentShotDamage`. `GameEvents.UltimateUsed` → `UltimateFlash` (lóe màu + tên chiêu) và rung camera. DevMenu: "Đầy mana".
- **3 vũ khí mới (10E D)**: Phun lửa (đạn `Projectile_Flame` cầu lửa + vệt, xuyên 99, đốt cháy `burnDps` qua `IBurnable` → `Enemy.ApplyBurn`:
  trừ máu mỗi 0.25 s, ngọn lửa con tạo 1 lần/object, `StopBurn` khi spawn/chết), Tesla (`WeaponData.fireMode = Chain`: tia nảy tức thì qua
  `ChainBolts` trên Player — pool 40 LineRenderer, viên thêm từ sao/Đa nòng = nảy thêm), Railgun (`Projectile_Rail`, xuyên 99, chậm, mạnh).
  Chiêu: `FireStormUltimate` (Bão lửa), `ChainStormUltimate` (Lưới điện), `RailBlastUltimate` (tia dày `CircleCast`). Tia điện/tia chiêu dùng
  vật liệu `Sprite-Unlit-Default` của URP (vật liệu cộng màu `Vfx_Particle` làm xanh lơ thành tím nhạt trên nền cát). Mảnh `shard_weapon_*`
  trong `Loot_WeaponShards`; quà thắng lần đầu 2-6 Phun lửa, 3-1 Tesla, 3-7 Railgun (10 mảnh). Icon chiêu riêng `icon_ult_*`.
- **Bot tự chơi (10C)**: `ArenaSurvival.exe -autoplay -speed 8 -label tên [-maxRuns N]` (build dev): save riêng `autoplay-save.json` (xóa đầu lượt,
  save thật không bị chạm — `GameServices.SavePathOverride`), `GameManager.PlayingTimeScale` để chạy nhanh, ghi `autoplay-log.csv` +
  `autoplay-summary.md` cạnh save. Kết quả các vòng: `Docs/Balance/playtest-report.md`.
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
  `[-mainWeapon weapon_grenade]` (tự vào Game, đo, ghi log, thoát). `-profile-log` ghi file `.raw` cho cửa sổ Profiler (~350 MB/2000 frame, nhớ xóa).

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
| Quái (10 + 1 con nhỏ + 10 biến thể) | Basic, Fast, Tank, Xạ thủ, Charger, Kamikaze, Splitter (+ Phân Thân Nhỏ), Healer, Sniper (C2), Lính Khiên (C3); biến thể C2 và C3 (~×1.2 máu mỗi chương) — số liệu trong `Docs/Balance/enemies.csv` |
| Boss (4) | Mini-boss "Cỗ Máy Gác Cổng" (1-5, máu 400, 2 pha: lao + vòng đạn), boss "Thủ Lĩnh Sa Mạc" (1-10, máu 1500,
  3 pha 100/66/33%: vòng đạn → + lao → vòng đạn dày + lao + gọi 4 Basic, tối đa 8); mini-boss "Xe Bọc Thép" (2-5, máu 450, 2 pha 100/50%:
  lao + vòng đạn dày → xoắn ốc + lao nhanh), boss "Chỉ Huy Robot" (2-10, máu 1700, 3 pha: vòng đạn → xoắn ốc + gọi 3 Kamikaze →
  xoắn ốc 5 cánh + lao + gọi Kamikaze, tối đa 6). Máu gốc, nhân hệ số độ khó theo số màn. Sprite Kenney phóng to + tô màu (tạm) |
| Màn chơi (10) | Chương 1 "Sa mạc" 1-1..1-10 (`Docs/Balance/stages.csv`): mỗi màn 3 đợt (45 s ở 1-1/1-2, 60 s từ 1-3) + đợt tinh anh; quái mới giới thiệu dần (1-3 Charger, 1-4 Xạ thủ, 1-6 Kamikaze, 1-7 Splitter, 1-9 Healer); 1-5 mini-boss, 1-10 boss chương. Quà lần đầu: 1-3 mảnh Shotgun, 1-5 Súng máy, 1-10 Phóng lựu. Lực chiến đề xuất 110 → 575 |
| Màn chơi (10, C3) | Chương 3 "Căn cứ" 3-1..3-10 (số màn 21–30): 3-2 Lính Khiên, 3-3 Sniper C3, 3-5 mini-boss Pháo Đài Di Động (Mưa Pháo), 3-10 boss Tướng Chỉ Huy (Mưa Pháo, xoắn ốc, gọi Lính Khiên/Kamikaze). Lực chiến đề xuất 1040 → 1530 |
| Màn chơi (10, C2) | Chương 2 "Thành phố" 2-1..2-10 (số màn 11–20): 3 đợt × 60 s (nhịp sinh 0.95 → 0.67 s) + đợt tinh anh; 2-2 Sniper, 2-3 Charger C2, 2-4 Xạ thủ C2 + Kamikaze, 2-6 Splitter, 2-7 Healer; 2-5 mini-boss, 2-10 boss. Quà lần đầu: 2-3 10 mảnh Laser, 2-10 20 mảnh Phóng lựu. Lực chiến đề xuất 565 → 1115 (mô phỏng: 20 màn ~108 phút) |
| Vũ khí chính (8) | Súng lục (có sẵn), Shotgun (6 viên tầm ngắn, đẩy lùi), Súng máy (6 phát/giây, tản), Phóng lựu (nổ r=1.6), Laser (xuyên 4), Phun lửa (nón ngắn, đốt cháy), Tesla (tia nảy 4 quái), Railgun (chậm, xuyên mọi quái); cấp + 5 sao; mỗi khẩu 1 chiêu dùng mana |
| Kỹ năng trong màn (14, có cấp) | Sát thương +25%, Tốc bắn +20%, Tốc chạy +10%, Máu +20, Hồi máu +30, Lưỡi cưa (3→8 lưỡi, cấp 6 thức tỉnh), Đa nòng, Đạn xuyên, Lá chắn, Vòng điện, Tên lửa, Nam châm, Sét lan, Drone hộ vệ |
| XP trong màn | Cấp 1 cần 5 XP, mỗi cấp sau cần thêm 5; orb bị hút trong bán kính 2.5 |
| Bản đồ | Vô tận, không tường. Sa mạc: cát + bãi đất, đá, bụi cây. Thành phố: đường nhựa + ô bê tông, mảnh vụn, thùng phuy. Căn cứ: sân bê tông + bãi nhựa/đất, thùng hàng |
| UI | HUD (đợt i/n + thanh tiến độ màn, banner "ĐỢT TINH ANH!" / "BOSS XUẤT HIỆN!" / "BOSS NỔI GIẬN!"), thanh máu boss (tên + vạch ngưỡng pha), bảng lên cấp, menu ESC (slider âm lượng), bảng hồi sinh, màn kết quả (sao, thưởng, Màn tiếp/Chơi lại/Về menu), Hub (thanh trên, 5 tab, chọn màn, popup lên cấp, tiền bay, chấm đỏ) + bảng Cài đặt + bảng Nâng cấp (Talent, lực chiến) + bảng Trang bị (kho, chi tiết, nâng cấp/đột phá/phân rã, ô Vũ khí) + bảng Vũ khí (mở khóa, lên sao, nâng cấp), hàng vật phẩm ở màn kết quả |
| Trang bị (4) | Áo Da (máu 20, giáp 3), Giáp Sắt (máu 10, giáp 6), Nhẫn Sức Mạnh (+4% sát thương), Bùa Gió (+3% tốc chạy, +0.2 hồi máu); 4 độ hiếm |
| Nguyên liệu | Quặng (nâng vũ khí), Da (nâng giáp/phụ kiện), Lõi boss (đột phá), 5 loại mảnh vũ khí |
| Rơi đồ | Quái thường 2–4% (1 da/quặng; C2 quặng nhiều hơn), tinh anh (2–4 da, 15% trang bị), mini-boss/boss (lõi + 1 trang bị Hiếm+; C2: lõi 2/3, độ hiếm cao hơn), thắng màn (3–6 da, 25% trang bị; C2: 4–8 da + 3–6 quặng, 30%) |
| Talent (5) | Máu +10/cấp (50 cấp, giá 100), Giáp +2 (50, 150), Sát thương +3% (50, 150), Tốc chạy +1% (20, 200), Hồi máu +0.2/s (25, 200); giá ×1.12 mỗi cấp |
| Hiệu ứng | Trúng đạn, quái chết, lên cấp, nháy đỏ khi trúng đòn, rung camera, số sát thương (gộp 0.2s) |
| Hoạt ảnh | Bằng code: thở khi đứng, lắc + co giãn theo bước khi chạy, quái phóng to lúc xuất hiện |
| Âm thanh | Bắn, trúng, quái chết, bị đánh, nhặt XP, nhặt nguyên liệu/trang bị, lên cấp, chọn nâng cấp, rê/bấm nút, Game Over, nhạc nền |

## Tiến độ (theo `Docs/project-plan.md`)

| Phase | Trạng thái |
|---|---|
| 0–5 – Core gameplay | Xong (gồm tối ưu GC và đợt cấu trúc lại 2026-10-01) |
| 6A – Màn chơi | Xong: StageData, StageSession, đợt tinh anh, độ khó, sao, hồi sinh, StageResult, màn kết quả |
| 6B – Boss | Xong: hệ hành vi quái (SO), Xạ thủ, đạn của quái, mini-boss, boss 3 pha, thanh máu boss |
| 7 – Hồ sơ người chơi, save v2, tiền tệ | Xong: PlayerProfile, SaveService (tmp/bak/HMAC/migration), EconomyService, tiến độ màn, scene Boot |
| 8A – Chỉ số, Talent, lực chiến | Xong: StatBlock, StatCalculator, 5 Talent, giáp, hồi máu, PlayerLoadout, bảng Talent, save v3 |
| 8B – Vật phẩm, kho đồ | Xong: 4 trang bị, 3 nguyên liệu, kho đồ, nâng cấp/đột phá/phân rã, LootTable, rơi đồ trong màn, bảng Trang bị, save v4 |
| 8C – Vũ khí meta | Xong: 5 vũ khí chính, cấp/sao/mảnh, xuyên/nổ/đẩy lùi/tầm, bảng Vũ khí, quà thắng lần đầu, save v5 |
| 9 – Hub và level tài khoản | Xong: AccountService, Hub 5 tab, chọn màn + lực chiến đề xuất, popup lên cấp, chấm đỏ, tiền bay, Safe Area |
| 10A – Công cụ dữ liệu + Chương 1 | Xong: CSV + nhập/xuất, mô phỏng tiến trình, ChapterData, 10 màn, 4 quái mới, tab Chiến đấu 10 màn |
| 10B – Chương 2 | Xong: Arena_City + ArenaLoader (sân theo chương, cả nền Hub), 10 màn, Sniper, 5 biến thể, đòn xoắn ốc, mini-boss + boss mới, bảng rơi C2 |
| 10C – Chương 3 + cân bằng | Xong: Arena_Base, 10 màn, Lính Khiên, Mưa Pháo, 2 boss, bot tự chơi, kỹ năng có cấp, cân bằng (bot qua 30 màn ~199 phút) |
| 10E – Quái đông, thẻ theo màn, chiêu, 3 vũ khí | Xong: mật độ chương đầu, thẻ +3%/màn, mana + 8 chiêu, Phun lửa/Tesla/Railgun, icon chiêu |
| 10D – Bản đồ vô tận + hình ảnh | Xong: nền vô tận theo chunk, bỏ tường/sân giữa, nhân vật tự vẽ + hiệu ứng, Sét lan + Drone, lưỡi cưa thức tỉnh, độ khó +7.5%/màn |
| 11A – Cửa hàng, IAP, quảng cáo (giả lập) | Xong: tab Cửa hàng (gói kim cương, khởi đầu, xóa QC, vàng, rương + tỷ lệ), IAPService + FakeStore, AdsService, hồi sinh/nhân đôi bằng QC, consent, save v6 |
| 11B – SDK thật | Chờ tài khoản Google Play Console + điện thoại Android: Unity IAP, xác minh biên lai, LevelPlay, build Android |
| 12A – Giữ chân + dịch vụ (cục bộ) | Xong: nhiệm vụ ngày + mốc, quà 7 ngày, tab Nhiệm vụ, analytics (log cục bộ), Remote Config (file), GameClock, tiền tệ có lý do, save v7 |
| 12B – Unity Gaming Services | Xong: adapter, Cloud Save + chọn bản, giờ máy chủ, DevMenu, 339 test; đã kết nối thật ở `development` (đăng nhập, Cloud Save, analytics đủ sự kiện, Remote Config đổi giá hồi sinh 25). Khóa Remote Config phải **publish** mới có hiệu lực |
| 13 – Mobile, tối ưu, playtest | Chưa |
| 14 – Soft launch | Chưa |

Scene `MainMenu.unity` giờ là Hub (giữ tên + build index để không phải sửa `SceneLoader`/build settings; đổi tên khi tiện).

## Vấn đề đã biết / lưu ý

- Mỗi lần build, benchmark có thể có 1 frame tệ bất thường (đã gặp 10 ms, chạy lại 2 lần còn 3.7–4.8 ms): chạy lại trước khi kết luận.
- Test UI có hiệu ứng theo thời gian bằng unity-cli: mỗi lệnh mất ~1 s, chụp ngay sau lệnh kích hoạt (banner chỉ hiện 2 s).
- Giấy phép âm thanh Kenney và nhạc Juhani Junkala **chưa xác minh** (xem `Docs/asset-licenses.md`) — phải xong trước khi phát hành.
- Benchmark Phóng lựu/Laser có vài frame cấp phát (9/2000) do TMP thêm ký tự mới vào atlas font động + chữ nổi khi nhặt đồ;
  không có rác lặp mỗi frame (đo bằng `-profile-log` + `ProfilerDriver`). Đồng hồ HUD tạo 1 chuỗi/giây (`TimeFormatter.ToClock`).
- Save cũ có Talent đã nâng nhưng tài khoản cấp 1: tab Talent bị khóa tới cấp 2 (chỉ khóa UI, chỉ số Talent vẫn áp vào trận).
- Test Runner thỉnh thoảng lỗi ở bước khôi phục scene (`RestoreSceneSetupTask` NullReference) và để lại hộp thoại chặn mọi lệnh CLI:
  nhờ người dùng đóng hộp thoại rồi chạy lại.
- Collider của quái là collider cứng (người dùng tự chỉnh); Player mass 10 nên quái chặn đường nhưng không ép được vào góc.
- Benchmark 300 quái + mọi kỹ năng cấp tối đa (màn 30): Main Thread ~2.0 ms, GC trung bình 5 B/frame (đỉnh ~3 KB, vài frame — chưa tìm nguồn).
- Sau 10D chưa chạy lại bot (người dùng yêu cầu dừng test): độ khó giảm và kỹ năng mạnh hơn nên game dễ hơn bản bot v13 đã qua.
- Nhiệm vụ "Xem 1 quảng cáo" không làm được ở bản phát hành tới khi có SDK quảng cáo (11B). Thông báo đẩy để Phase 13 (cần Android).
- **Luôn sao lưu save ngay trước MỖI lần vào Play** (người dùng có thể đã chơi giữa hai lượt, bản sao lưu cũ không còn đúng).
- Khôi phục save sau khi thử trong Play mode: `editor_stop` trả về TRƯỚC khi Editor lưu lần cuối lúc thoát → chờ ~5 s rồi mới chép bản sao lưu,
  và kiểm tra lại md5 sau vài giây (đã bị ghi đè 1 lần ở 12A).
- Đăng nhập UGS ẩn danh gắn với lần cài: gỡ game là mất tài khoản; Cloud Save chuyển máy thật sự cần liên kết Google Play Games (Play Console).
- Thử nhiệm vụ/Remote Config trong Editor ghi `analytics-log.jsonl`, `remote-config.json`, `remote-cache.json` cạnh save thật: xóa sau khi thử.
- Store giả lưu "đã mua" trong PlayerPrefs của máy: thử mua trong Editor xong nên bấm "Xóa store giả" ở DevMenu, nếu không nút Khôi phục
  sẽ trao lại gói vào save thật. Popup quảng cáo giả dùng font mặc định của TMP (chỉ bản dev).
- Gói khởi đầu được khôi phục như non-consumable (cài lại game nhận lại cả vàng/kim cương trong gói) — xem lại khi khai báo trên Google Play.
- Mảng bê tông/đất là khối chữ nhật lặp theo lưới; muốn mép tự nhiên hơn cần tile chuyển tiếp (asset mới) hoặc RuleTile.
- Bot chỉ dùng súng tầm ≥ 5 và né theo luật cứng: số liệu bot là mốc so sánh, vẫn cần người chơi thật.
- Ba font asset TMP mỗi file khoảng 4 MB (atlas 1024 động) và tự thay đổi khi gặp ký tự mới lúc chạy; commit cùng lúc là được.
- Mỗi lần build, Unity tự ghi lại vài file cài đặt (`UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`,
  `ProjectSettings.asset`): commit riêng dạng `chore`.
- Test chạy bằng unity-cli khi scene đang dirty: Test Runner hiện hộp thoại hỏi lưu và mọi lệnh CLI treo (timeout 30 s) tới khi
  người dùng bấm trong Editor. Luôn để scene sạch trước `run_tests`; sau khi thoát Play hãy kiểm tra `isDirty`.
- Icon nâng cấp Súng máy dùng sprite súng Kenney khá nhỏ (33×10 px), phóng to hơi mờ.
- Âm thanh được chọn bằng cách đo độ dài và độ to bằng ffmpeg; cần người nghe thật để tinh chỉnh (`Data/Audio/SoundBank.asset`).
