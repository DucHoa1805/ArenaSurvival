# Phase 10B — Chương 2 "Thành phố bỏ hoang"

Ngày: 2026-10-02
Trạng thái: đã duyệt thiết kế (phương án A)
Liên quan: spec 10A (bảng cân bằng CSV, ChapterData, mô phỏng), spec 6B (hành vi quái, boss), `Docs/project-plan.md` Phase 10

## 1. Mục tiêu
Chương 2 chơi được trọn vẹn: sân thành phố riêng, 10 màn (số màn toàn cục 11–20), biến thể quái + 1 quái mới (Sniper),
mini-boss 2-5 và boss chương 2-10 mới (1 đòn mới: đạn xoắn ốc), bảng rơi nhiều quặng hơn, lực chiến đề xuất chỉnh bằng mô phỏng.

## 2. Quyết định đã chốt
| Câu hỏi | Chọn |
|---|---|
| Bối cảnh | Thành phố bỏ hoang (Tilemap từ tile Kenney) |
| Vật cản | Chỉ trang trí nền, không va chạm (AI quái giữ nguyên) |
| Mức nội dung | 5 biến thể + Sniper + mini-boss/boss mới ghép đòn có sẵn + `SpiralAttack` |
| Đổi sân theo chương | **Phương án A**: `ChapterData.arenaPrefab` + `ArenaLoader` (Game và Hub) |

## 3. Ngoài phạm vi
Vật cản có va chạm; nhạc nền riêng từng chương; Chương 3 (10C); chơi thử 30 màn bằng tài khoản mới (10C).

## 4. Sân & đổi sân
- `ChapterData.arenaPrefab` (GameObject). Chương 1 = `Arena.prefab` hiện có.
- `ArenaLoader` (`Scripts/Stages/`, có trong `Game.unity` và `MainMenu.unity`): `_sceneArena` (instance có sẵn trong scene) +
  `_sceneArenaSource` (prefab gốc của instance đó). `Awake`: lấy chương của màn (Game: `StageSelection.Current` hoặc màn đầu; Hub:
  màn nên chơi tiếp); nếu `arenaPrefab` khác `_sceneArenaSource` → tắt instance trong scene, tạo prefab chương tại cùng vị trí.
  Hàm thuần `ArenaLoader.NeedsSwap(sceneSource, chapterPrefab)` để test.
- `Arena_City.prefab` (dựng bằng script, cùng Grid 0.64, cùng phạm vi ô với sa mạc → không phải sửa `WaveSpawner._arenaArea` hay camera):
  - Ground: trong sân (−15..14) đường nhựa (86 chủ đạo, 93 xen kẽ; 31/58 bị bỏ vì có vệt vạch vàng ở góc); vạch vàng đứt đoạn giữa sân theo
    trục ngang (56) và dọc (55), trống ở ngã tư; viền 358 giữ làm bó vỉa; ngoài viền là bê tông vỉa hè (331, 07, 08 xen kẽ).
  - Details: đống ván–đá (292), mảnh gạch (289, 290), thùng phuy sát mép trong (198, 207, 316). Khi soi tile thì 132 là đĩa sáng,
    134 là chậu cây — không dùng. Biến thể chọn theo băm vị trí ô và `System.Random(2026)` nên dựng lại ra y hệt.
  - Foliage: vài bụi cây xanh (183) trên vỉa hè ngoài sân.
  - Tile asset mới tạo trong `Art/Tiles/` (`Tile_<số>.asset`) từ sprite Kenney.

## 5. Quái & boss
- **Sniper** (`SniperBehaviour`): giữ khoảng 8 ± 1.5; mỗi 3.5 s, nếu Player trong 11 → ngắm 1.0 s: bật tia đỏ (`LineRenderer` con `Telegraph`
  của prefab `Enemy`, tắt sẵn) theo hướng Player, 0.25 s cuối **chốt hướng** (né được) → bắn 1 viên tốc 16, sát thương `projectileDamage`.
  Ô nhớ: State (0 di chuyển, 1 ngắm), Timer, Vector (hướng chốt). `EnemyContext.Telegraph` (lấy trong `Enemy.Awake`),
  `ResetVisual` tắt tia (quái chết/trả về pool giữa lúc ngắm không để lại tia). `EnemyRules.AimLocked(remaining, lockWindow)`.
- **`SpiralAttack`** (`BossAttack`): bắn liên tục trong `duration` (3 s), mỗi `interval` (0.09 s) bắn `arms` (3) viên theo
  `EnemyRules.RadialDirection(i, arms, góc)` với góc tăng `rotateSpeed` (110°/s). Ô nhớ: Timer2 (đếm tới lượt bắn), Vector.x (thời gian đã bắn — góc suy ra từ đó).
- **Biến thể** (qua CSV, sprite Kenney cùng bộ khác tư thế + màu): `Enemy_C2_Basic`, `Enemy_C2_Fast`, `Enemy_C2_Tank`, `Enemy_C2_Shooter`,
  `Enemy_C2_Charger` (~×1.2 máu so với bản gốc; độ khó chính vẫn từ công thức màn: 2-1 = màn 11, ×2.2). Kamikaze/Splitter/Healer dùng lại.
- **Mini-boss 2-5 "Xe Bọc Thép"** (`Boss_Armored`, sprite Soldier tư thế súng máy, to ×2.1): 2 pha — lao + vòng đạn dày → xoắn ốc + lao nhanh.
- **Boss chương 2-10 "Chỉ Huy Robot"** (`Boss_Commander`, sprite Robot, to ×2.7): 3 pha 100/66/33% — vòng đạn → xoắn ốc + gọi 3 Kamikaze →
  xoắn ốc dày + lao + gọi Kamikaze (tối đa 6).

## 6. Màn (stages.csv, chương c2 — cột `number` là số màn toàn cục 11–20)
| Màn | Mới | Đợt cuối |
|---|---|---|
| 2-1 | biến thể Basic/Fast | Basic C2 tinh anh |
| 2-2 | + Sniper | Sniper tinh anh |
| 2-3 | + Charger C2 | Tank C2 tinh anh · quà lần đầu 10 mảnh Laser |
| 2-4 | Xạ thủ C2 + Kamikaze | Charger C2 tinh anh |
| 2-5 | trộn | **Mini-boss** Xe Bọc Thép |
| 2-6 | Splitter + Sniper | Splitter tinh anh |
| 2-7 | Healer + Tank C2 | Healer tinh anh |
| 2-8 | Kamikaze + Sniper dày | Kamikaze tinh anh |
| 2-9 | tổng hợp | Sniper tinh anh (đợt cuối chỉ có 1 loại quái tinh anh) |
| 2-10 | tổng hợp | **Boss chương** Chỉ Huy Robot · quà lần đầu 20 mảnh Phóng lựu |

Mỗi màn 3 đợt × 60 s, nhịp sinh nhanh dần 0.95 → 0.65 s. Thêm `Loot_MiniBoss_C2` (lõi ×2, độ hiếm 0/60/35/5 + mảnh) cho 2-5. Bảng rơi mới: `Loot_Common_C2` (quặng nhiều hơn da), `Loot_StageClear_C2`
(da 4–8 + quặng 3–6, 30% trang bị, độ hiếm 50/40/10/0), `Loot_Boss_C2` (lõi boss ×3 + trang bị 0/30/50/20 + mảnh). Lực chiến đề xuất
từ mô phỏng nối tiếp Chương 1: màn thường ~1.5–2 lượt, 2-5 ~2.5 lượt, 2-10 ~4 lượt.

## 7. Kiểm tra
1. Unit test: `EnemyRules.AimLocked`, góc `SpiralAttack` (qua `RadialDirection` + góc xoay), `ArenaLoader.NeedsSwap`, catalog 2 chương
   (`GetNext` 1-10 → 2-1, `ChapterOf`).
2. Nhập CSV 2 lần: lần 2 "không đổi"; save cũ giữ nguyên.
3. Chơi thử: Hub ◀ ▶ đổi chương, sân thành phố ở 2-x (cả nền Hub khi đang ở chương 2), Sniper ngắm/né được, 2-5 và 2-10 đánh được.
4. Benchmark 300 quái ở màn 20 (`-stage 20`): GC 0 B/frame.
