# Cấu trúc lại dự án theo định hướng thương mại

Ngày: 2026-10-01
Trạng thái: chờ duyệt
Liên quan: `Docs/project-plan.md` (mục B4 và mục "Phase 0–5: Cần rà lại để tái sử dụng")

## 1. Mục tiêu

Đưa code và asset hiện có về đúng cấu trúc của kế hoạch thương mại, để Phase 6 (màn chơi và boss) bắt đầu trên một nền sạch.

**Game phải chạy y hệt trước khi cấu trúc lại.** Đợt này chỉ di chuyển, đổi namespace, sắp xếp asset, viết tài liệu. Không thêm tính năng, không đổi logic.

## 2. Ngoài phạm vi

- Đổi tên class (ví dụ `EnemySpawner` → `WaveSpawner`): làm ở Phase 6, khi class đổi luôn vai trò.
- Tạo trước thư mục hoặc class rỗng cho các hệ thống chưa làm (`Meta/`, `Economy/`, `Shop/`, `Services/`, `Inventory/`): tạo khi tới phase của chúng.
- Tách thư mục con trong `UI/`: làm ở Phase 9, khi có Hub.
- Tách nhiều assembly definition: giữ một assembly `ArenaSurvival`; xem lại ở Phase 11 (IAP/Ads).
- Gỡ tính năng cũ: MainMenu, lưu kỷ lục, wave vô tận vẫn chạy cho tới khi có thứ thay thế (Hub ở Phase 9, hồ sơ người chơi ở Phase 7, StageData ở Phase 6).

## 3. Điều kiện trước khi bắt đầu

Phần tối ưu hiệu năng (DevTools, `ObjectPool.TryGet`, cache delegate...) đã được commit riêng. Đợt cấu trúc lại là một commit riêng để lịch sử git tách được thay đổi logic với thay đổi vị trí file.

## 4. Sắp xếp code

Thư mục gốc của script: `Assets/_Project/Scripts/`. Namespace theo tên thư mục (quy ước hiện có).

| Hiện tại | Sau | Namespace mới |
|---|---|---|
| `Progression/ExperienceSystem.cs`, `RandomPicker.cs`, `UpgradeData.cs`, `UpgradeManager.cs`, `XpCurve.cs`, `XpOrb.cs`, `XpOrbSpawner.cs` | `RunUpgrades/` | `ArenaSurvival.RunUpgrades` |
| `Enemies/EnemySpawner.cs`, `Enemies/WaveData.cs`, `Core/RunStats.cs` | `Stages/` | `ArenaSurvival.Stages` |
| `Combat/Playerweapon.cs` | `Weapons/PlayerWeapon.cs` (sửa tên file cho khớp tên class) | `ArenaSurvival.Weapons` |
| `Combat/OrbitWeapon.cs`, `OrbitWeaponData.cs`, `SawBladeRig.cs`, `WeaponData.cs`, `WeaponDefinition.cs` | `Weapons/` | `ArenaSurvival.Weapons` |
| `Combat/Health.cs`, `Combat/Projectile.cs` | Giữ nguyên | `ArenaSurvival.Combat` |
| `Core/`, `Player/`, `Enemies/Enemy.cs`, `Enemies/EnemyData.cs`, `Save/`, `UI/`, `Audio/`, `Vfx/`, `DevTools/` | Giữ nguyên | Giữ nguyên |
| `Tests/EditMode/*` | Giữ nguyên vị trí; chỉ cập nhật `using` | Giữ nguyên |

Hai chỗ lệch so với mục B4 của plan:

1. **Thư mục `Weapons/`.** Plan đặt `PlayerWeapon` vào `Player/` nhưng không nói chỗ cho lưỡi cưa và `WeaponData`. Gom mọi vũ khí vào một chỗ để Phase 8 (vũ khí khác nhau về cách bắn, mảnh và sao vũ khí) có sẵn nơi đặt code.
2. **Đổi tên file `Playerweapon.cs`.** Tên file khác tên class; trên hệ thống phân biệt hoa/thường, Unity sẽ không tìm thấy script.

## 5. Sắp xếp asset

| Hiện tại | Sau |
|---|---|
| `Assets/Scenes/MainMenu.unity`, `Game.unity` | `Assets/_Project/Scenes/` (Build Settings tự cập nhật khi di chuyển qua Editor; build index giữ nguyên: MainMenu 0, Game 1) |
| `Data/Enemy_*.asset` | `Data/Enemies/` |
| `Data/Weapon_*.asset`, `Data/Orbit_SawBlade.asset` | `Data/Weapons/` |
| `Data/Upgrade_*.asset` | `Data/RunUpgrades/` |
| `Data/Wave_*.asset` | `Data/Stages/` |
| `Data/SoundBank.asset` | `Data/Audio/` |
| `Audio/Impact/desktop.ini`, `Audio/Digital/desktop.ini` (file rác của Windows) | Xóa (cùng file `.meta`) |

## 6. Cách thực hiện an toàn

- **Mọi thao tác di chuyển đi qua Unity Editor** (`AssetDatabase.MoveAsset`, unity-cli `eval_file`) để giữ GUID. Scene, prefab, ScriptableObject tham chiếu script và asset bằng GUID nên không mất liên kết.
- **Không dùng `git mv` hay di chuyển file bằng shell khi Editor đang mở.**
- Đổi namespace sửa trực tiếp file `.cs` (đổi khai báo `namespace` và các dòng `using`/tên đầy đủ ở nơi dùng).
- Đã kiểm tra trước: không có UnityEvent gán trong Inspector theo tên kiểu `ArenaSurvival.*`, không có `[SerializeReference]`, không có đường dẫn asset viết cứng trong code. Vì vậy đổi namespace không làm hỏng dữ liệu đã serialize.
- Trước khi mở/lưu scene luôn kiểm tra `scene.isDirty` (quy ước hiện có).

## 7. Tài liệu

### 7.1. Thư mục `Docs/` ở gốc repo
- `unity-game-project-plan.md` → `Docs/project-plan.md`.
- `DESIGN.md` → `Docs/archive/core-design-v1.md`, thêm dòng đầu: đây là thiết kế bản core (Phase 0–5), đã được thay bằng `project-plan.md`.
- File spec này nằm ở `Docs/specs/`.

### 7.2. `Docs/asset-licenses.md`
Bảng liệt kê từng bộ asset, giấy phép, nguồn và trạng thái. Chỉ ghi giấy phép khi có bằng chứng (file giấy phép trong repo); còn lại ghi "cần kiểm tra":

| Asset | Giấy phép | Trạng thái |
|---|---|---|
| Kenney Topdown Shooter (`Art/Kenney-TopDownShooter`) | CC0 | Có `License.txt` |
| Âm thanh Kenney (`Audio/UI`, `Audio/Impact`, `Audio/Digital`) | Cần kiểm tra (gói Kenney thường là CC0) | Thiếu file giấy phép: người dùng tải `License.txt` từ trang gói trên kenney.nl |
| Nhạc `Audio/Music/JuhaniJunkala_Level3.ogg` | Cần kiểm tra | Người dùng xác minh giấy phép và ghi link nguồn |
| Font Bungee, Chakra Petch (`Art/Fonts`) | SIL OFL | Có file OFL |
| LiberationSans (TextMesh Pro mặc định) | Cần kiểm tra (đi kèm package TMP) | Ghi chú lại |
| Sprite UI, icon, XP orb, đạn, lưỡi cưa, VFX (`Art/UI`, `Pickups`, `Projectiles`, `Weapons`, `Vfx`) | Tự làm trong dự án | Người dùng xác nhận |

### 7.3. Viết lại `CLAUDE.md`
- Định hướng: game thương mại mobile (Android trước), IAP và quảng cáo có thưởng; link `Docs/project-plan.md`.
- Giữ nguyên toàn bộ quy ước làm việc hiện có (tiếng Việt, không tự commit, trình bày thiết kế trước, sửa scene qua Editor, kiểm tra `isDirty`, `Enemy_Tank.asset`).
- Thêm nguyên tắc kiến trúc từ mục B2 của plan: scene Game chỉ trả về `StageResult`; mọi thay đổi tiền tệ đi qua `EconomyService`; ID ổn định; save có version.
- Cấu trúc thư mục mới (theo mục 4 và 5).
- Tiến độ: Phase 0–5 xong; tiếp theo Phase 6 (màn chơi và boss).
- Mục mới **Công cụ đo hiệu năng**: StressTest (F9), PerfRecorder (F10), benchmark bằng dòng lệnh (`-benchmark -enemies N -weapons 0|1 -label tên -settle 12 -frames 3000 [-profile-log]`), vị trí `perf-log.txt`.
- Mục mới **Bài học hiệu năng**: đo GC phải dùng Development Build (Editor tự tạo ~200 KB rác/frame); không tạo delegate mới khi lấy object từ pool hay trong `OnEnable`/`OnDisable` của object trong pool; dự án dùng SRP Batcher nên chỉ số Batches tăng theo số object là bình thường, nhìn SetPass Calls; Sprite Atlas đã thử, không cải thiện, Tilemap trong atlas lộ đường kẻ.
- Bỏ các mục chỉ dành cho bản portfolio (itch.io, video demo, tag theo phase cũ).

### 7.4. Plan của người dùng
Chỉ đánh dấu hoàn thành các ô đã xong trong mục "Phase 0–5: Cần rà lại để tái sử dụng" (slider âm lượng, animation, unit test; chuyển code sang `RunUpgrades/`; cập nhật `CLAUDE.md`). Không sửa nội dung khác.

### 7.5. Bộ nhớ của Claude
Cập nhật ghi chú dự án từ "dự án thực tập" sang "dự án thương mại".

## 8. Kiểm tra

1. Biên dịch không lỗi (chỉ còn warning `OverlapCircleNonAlloc` có từ trước).
2. Unit test EditMode: 40/40 đạt.
3. Mở `MainMenu.unity` và `Game.unity`: không có component "Missing Script"; kiểm tra bằng code (đếm `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` trên mọi object, kể cả prefab trong `Prefabs/`).
4. Mọi ScriptableObject trong `Data/` load được đúng kiểu (không `null`, không mất script).
5. Build Settings vẫn có 2 scene với đường dẫn mới, đúng thứ tự.
6. Chơi thử trong Play mode bắt đầu từ MainMenu: vào Game → lên cấp, chọn nâng cấp → quái chết rơi orb → Game Over. Chụp màn hình từng bước, console không lỗi.
7. `git status` chỉ chứa thay đổi của đợt này (các file Unity tự sinh khi build được liệt kê riêng để người dùng quyết định).

## 9. Bàn giao

Gợi ý một commit `chore: cấu trúc lại dự án theo định hướng thương mại` với danh sách file. Báo người dùng các file lớn ngoài repo có thể xóa (`.raw` Profiler ~350 MB trong `LocalLow/DefaultCompany/ArenaSurvival/`, `Builds/Dev/`).
