# Arena Survival — ghi chú dự án

Game 2D top-down arena survival (kiểu Vampire Survivors thu nhỏ), dự án thực tập / portfolio phỏng vấn Unity Developer.
Kế hoạch gốc: `unity-game-project-plan.md` (Phase 0–6). Design doc: `DESIGN.md`.

Cập nhật lần cuối: 2026-10-01, sau commit `f096152`.

## Quy ước làm việc (quan trọng)

- **Trả lời bằng tiếng Việt.** Comment trong code viết tiếng Việt, giải thích **vì sao** chứ không giải thích **cái gì**.
- **Không tự `git commit` / `git push`.** Chuẩn bị thay đổi, kiểm tra xong thì đưa người dùng danh sách file và message để họ tự commit.
  Message theo kiểu `feat: …`, `fix: …`, `test: …`, `polish: …`, `chore: …` (tiếng Việt có dấu).
- Tính năng mới: trình bày thiết kế ngắn và chờ đồng ý trước khi code, trừ khi yêu cầu đã rõ ràng.
- Sửa scene/prefab **qua Unity Editor** (unity-cli `eval_file`), không sửa tay YAML khi Editor đang mở.
  Luôn kiểm tra `scene.isDirty` trước khi lưu, vì người dùng hay có chỉnh sửa chưa lưu.
- Sửa UI **tại chỗ** (đổi sprite, font, vị trí, thêm con), không xóa rồi dựng lại, để giữ liên kết script.
- `Enemy_Tank.asset` thường có thay đổi riêng của người dùng: không đưa vào commit của mình.

## Môi trường & cách kiểm tra

- Unity **6000.3.13f1**, URP 2D, Input System (chỉ Input System, không dùng Input cũ), uGUI + TextMeshPro.
- Unity CLI: `C:\Users\Admin\AppData\Local\Unity\bin\unity.exe` (không có trong PATH của shell agent).
  Gọi kiểu `unity command <tên> --caller plugin --skill unity-cli`. Chạy C# bằng `eval_file --file x.cs`
  (không dùng `eval` inline vì PowerShell 5.1 làm hỏng dấu nháy).
- **Play mode không chạy frame khi Editor không được focus** (`runInBackground: 0` trong Player Settings).
  Khi test: `editor_play`, rồi eval `UnityEngine.Application.runInBackground = true;`. Chỉ đổi lúc chạy, file cài đặt không đổi.
- Chụp màn hình: `capture_game_view --save_path "Temp/x.png"` (file lưu vào `Assets/Temp/`, nhớ `delete_asset` sau đó).
- Unit test: `run_tests --mode EditMode` (hiện 28/28 đạt), hoặc Window → General → Test Runner.
- Hiện Editor mở scene nào thì Play chạy scene đó. Muốn test cả vòng thì mở `MainMenu.unity` trước.
- ffmpeg đã cài qua winget (`Gyan.FFmpeg`), dùng để đo/chuyển đổi âm thanh.

## Cấu trúc project

```
Assets/_Project/
├── Scripts/            assembly ArenaSurvival (ArenaSurvival.asmdef)
│   ├── Core/           GameManager, GameState, GameEvents, ObjectPool, RunStats, SceneLoader, PauseInput, CameraFollow
│   ├── Player/         PlayerController, PlayerStats, PlayerDeathHandler, PlayerControls.inputactions
│   ├── Combat/         Health, Projectile, PlayerWeapon, WeaponDefinition/WeaponData, OrbitWeapon, OrbitWeaponData, SawBladeRig
│   ├── Enemies/        Enemy, EnemyData, EnemySpawner, WaveData
│   ├── Progression/    ExperienceSystem, XpCurve, XpOrb, XpOrbSpawner, UpgradeData, UpgradeManager, RandomPicker
│   ├── UI/             HudController, LevelUpPanel, LevelUpPanelAnimator, UpgradeOptionButton, PauseMenu, GameOverPanel,
│   │                   MainMenu, TimeFormatter, UiHoverScale
│   ├── Save/           SaveData, SaveSystem, HighScoreTracker
│   ├── Audio/          AudioManager, SoundBank, SoundEffect, ButtonSound
│   └── Vfx/            VfxSpawner, PooledVfx, FloatingText
├── Tests/EditMode/     assembly ArenaSurvival.Tests.EditMode: XpCurve, RandomPicker, SaveSystem, TimeFormatter, WeaponSpread
├── Data/               ScriptableObject: Enemy_*, Weapon_*, Orbit_SawBlade, Upgrade_*, Wave_01..03, SoundBank
├── Prefabs/            Player, Enemy, Projectile, XpOrb, AudioManager, Arena, Vfx/*
├── Art/                Kenney-TopDownShooter, Tiles (Tilemap), UI (sprite + icon tự vẽ), Fonts, Vfx, Weapons, Pickups, Projectiles
└── Audio/              UI, Impact, Digital (Kenney), Music (Juhani Junkala, OGG)
Assets/Scenes/          MainMenu.unity (build index 0), Game.unity (build index 1)
```

## Kiến trúc & quyết định chính

- **GameEvents** (static events) là trung tâm giao tiếp: gameplay phát sự kiện, còn HUD, âm thanh, hiệu ứng, XP, kỷ lục tự nghe.
  Event không mang kiểu dữ liệu của tầng khác (Core không phụ thuộc Progression). Riêng UpgradeManager ↔ LevelUpPanel
  dùng instance event và tham chiếu trực tiếp.
- **GameManager**: Singleton + state machine `Menu / Playing / Paused / LevelUp / GameOver`, pause bằng `Time.timeScale`.
  `LevelUp` tách khỏi `Paused` để menu Pause không xen vào bảng chọn nâng cấp.
- **Object pool tự viết** (`ObjectPool<T>`) cho đạn, quái, XP orb, hiệu ứng. Đối tượng gọi callback `OnReturnToPool`,
  không biết gì về pool. `Release` bỏ qua object đã tắt để chống trả về hai lần.
- **Data-driven bằng ScriptableObject**: loại quái, súng, wave, nâng cấp, âm thanh đều là asset.
  Vũ khí dùng lớp cha `WeaponDefinition` và interface `IWeapon`, nên nâng cấp "mở khóa vũ khí" trỏ được tới súng lẫn lưỡi cưa.
- **Đạn, lưỡi cưa không làm con của Player** (container riêng ở gốc scene). Con của Rigidbody2D bị kéo/xoay theo cha
  (đây từng là lỗi đạn bay lệch).
- **PlayerStats** giữ hệ số cộng dồn từ nâng cấp. Không sửa trực tiếp `WeaponData` lúc chơi, vì asset sẽ bị giữ thay đổi.
- **Layer & Collision Matrix**:

  | Layer | Va chạm với |
  |---|---|
  | `Player` (8) | `Enemy`, `Pickup`, `Boundary`, `Default` |
  | `Enemy` (6) | `Player`, `PlayerProjectile` |
  | `PlayerProjectile` (7) | `Enemy` |
  | `Pickup` (9) | `Player` |
  | `Boundary` (10, tường) | chỉ `Player` |

- **Quái sinh trong sân** (`EnemySpawner._arenaArea`), ngoài tầm nhìn camera; tường chỉ giữ Player.
- **Lưu kỷ lục**: JSON (`JsonUtility`) ở `persistentDataPath/save.json`; file hỏng hoặc thiếu thì dùng dữ liệu trống.
- **Âm thanh**: `AudioManager` sống xuyên scene (DontDestroyOnLoad), 10 AudioSource xoay vòng, có lệch cao độ ngẫu nhiên và
  giới hạn tần suất phát cho từng tiếng.

## Nội dung game hiện có

| Loại | Chi tiết |
|---|---|
| Quái (3) | Basic (máu 20, tốc 2), Fast (máu 10, tốc 4), Tank (máu 50, tốc 1.2, to x1.5, từ wave 3) |
| Wave | 3 wave × 30 giây, sau đó lặp wave 3 vô tận: mỗi vòng +20% máu, spawn nhanh hơn 10% |
| Vũ khí (4) | Súng thường (có sẵn), Súng chùm (5 viên tỏa 50°), Súng máy (4 phát/giây, từ cấp 5), Lưỡi cưa xoay (3 lưỡi) |
| Nâng cấp (8) | Sát thương +20%, Tốc độ bắn +15%, Tốc độ chạy +10%, Máu tối đa +20, Hồi máu +30, 3 nâng cấp mở khóa vũ khí |
| XP | Cấp 1 cần 5 XP, mỗi cấp sau cần thêm 5; orb bị hút trong bán kính 2.5 |
| Màn chơi | Đấu trường Tilemap: sân cát 30×30 ô, viền đá, đường đất, bãi cỏ, bụi cây |
| UI | HUD, bảng lên cấp (có hoạt ảnh), menu ESC, Game Over, menu chính (nền là đấu trường); font Bungee + Chakra Petch |
| Hiệu ứng | Trúng đạn, quái chết, lên cấp (vòng sóng, tia sáng, chữ "CẤP n!") |
| Âm thanh | Bắn, trúng, quái chết, bị đánh, nhặt XP, lên cấp, chọn nâng cấp, rê/bấm nút, Game Over, nhạc nền |

## Tiến độ theo plan

| Phase | Trạng thái |
|---|---|
| 0 – Chuẩn bị | Xong |
| 1 – Nền tảng & Player | Xong |
| 2 – Combat cốt lõi | Xong (chưa làm bước so sánh với `UnityEngine.Pool`, chưa đo "100+ quái không giật") |
| 3 – Kiến trúc & dữ liệu | Xong (có asmdef) |
| 4 – Tiến trình & UI | Xong |
| 5 – Hoàn thiện & lưu trữ | Gần xong: đã có save, âm thanh, VFX, độ khó, 3 quái / 4 vũ khí / 8 nâng cấp, 28 unit test. Chưa có animation nhân vật |
| 6 – Tối ưu, build, portfolio | **Chưa bắt đầu** |

Checklist "Must have" trong DESIGN.md: **đã đủ**.

## Việc tiếp theo (gợi ý)

1. Phase 6: đo Profiler trước/sau để có ít nhất một ví dụ tối ưu thật (ví dụ quái đông, nhiều đạn, GC alloc).
2. Build Windows (và/hoặc WebGL), thử trên máy khác, đưa lên itch.io.
3. Viết README theo mẫu ở mục E1 của plan (kèm Credits: Kenney, Juhani Junkala, font Bungee/Chakra Petch – SIL OFL).
4. Quay video demo 1 phút; gắn tag git theo phase (`phase-1` … `phase-6`, `v1.0`). Repo hiện chưa có tag nào.
5. Tùy chọn: animation nhân vật, cài đặt âm lượng (cần AudioMixer tạo bằng tay trong Editor), boss, gamepad.

## Vấn đề đã biết / lưu ý

- Collider của quái là collider cứng (người dùng tự chỉnh), nên quái đẩy được Player. Muốn tắt thì bật lại Is Trigger trên prefab `Enemy`.
- Ba font asset TMP mỗi file khoảng 4 MB (atlas 1024 động). Các font asset và `LiberationSans SDF - Fallback.asset`
  tự thay đổi khi gặp ký tự mới lúc chạy; commit cùng lúc là được.
- Icon nâng cấp Súng máy dùng sprite súng Kenney khá nhỏ (33×10 px), phóng to hơi mờ.
- Âm thanh được chọn bằng cách đo độ dài và độ to bằng ffmpeg; cần người nghe thật để tinh chỉnh (`Data/SoundBank.asset`).
- `OverlapCircleNonAlloc` trong `PlayerWeapon` bị Unity 6 đánh dấu obsolete (chỉ là warning).
