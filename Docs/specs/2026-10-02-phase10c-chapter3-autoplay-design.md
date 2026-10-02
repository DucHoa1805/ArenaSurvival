# Phase 10C — Chương 3 "Căn cứ quân sự" + bot tự chơi + cân bằng 30 màn

Ngày: 2026-10-02
Trạng thái: đã duyệt thiết kế (phương án A cho bot)
Liên quan: spec 10A (bảng cân bằng CSV, mô phỏng), spec 10B (ArenaLoader, Sniper, SpiralAttack), spec 6B (hành vi quái, boss),
`Docs/project-plan.md` Phase 10

## 1. Mục tiêu
1. Chương 3 chơi được trọn vẹn: sân căn cứ riêng, 10 màn (số màn toàn cục 21–30), 5 biến thể + 1 quái mới (Lính Khiên),
   1 đòn boss mới (Mưa Pháo), mini-boss 3-5 và boss cuối 3-10.
2. Bot tự chơi cả 30 màn từ tài khoản mới trên build Development (thay cho "chơi thử 30 màn không cheat" của plan),
   ghi nhật ký từng lượt.
3. Dùng nhật ký bot + mô phỏng để cân bằng 30 màn; ghi lại các vòng chỉnh vào `Docs/Balance/playtest-report.md`.

## 2. Quyết định đã chốt
| Câu hỏi | Chọn |
|---|---|
| Bối cảnh Chương 3 | Căn cứ quân sự (Tilemap từ tile Kenney, chỉ là nền, không va chạm) |
| Mức nội dung | 5 biến thể + 1 quái mới (Lính Khiên) + 1 đòn boss mới (Mưa Pháo); boss ghép đòn |
| Chơi thử 30 màn | Bot tự chơi |
| Cách làm bot | **Phương án A**: chế độ `-autoplay` trong build Development, save riêng, tăng tốc thời gian |

## 3. Ngoài phạm vi
Vật cản có va chạm; nhạc riêng từng chương; vũ khí phụ mới cho bảng lên cấp; Chương 4; bot "học" pattern; thay thế playtest người thật
(vẫn cần trước soft launch).

## 4. Sân `Arena_Base`
- Dựng bằng script (tô lại từ `Arena.prefab` như `Arena_City`): cùng Grid 0.64, cùng phạm vi ô → không sửa `WaveSpawner`, camera, tường.
- Ground trong sân: sàn bê tông xám, vạch trắng (62/63) kẻ ô bãi đỗ/bãi đáp, 1 dải đường nhựa cắt ngang. Viền 358 giữ.
  Ngoài viền: đất nện/cỏ úa (khác vỉa hè C2). Details: thùng hàng, thùng phuy sát mép. Tile cụ thể chốt khi làm, sau khi render soi
  từng tile (bài học 10B: số tile đoán theo bảng dễ nhầm).
- Biến thể chọn theo băm vị trí ô + `System.Random` cố định → dựng lại ra y hệt.
- `Chapter_C3.arenaPrefab = Arena_Base` (nền Hub tự đổi qua `ArenaLoader`).

## 5. Lính Khiên (`ShieldBehaviour`)
- Đuổi chậm, luôn quay về phía Player với tốc xoay giới hạn (~90°/s, tham số) → chạy vòng ra sau được.
- Khiên chặn **85%** sát thương (tham số `_blockFraction`) khi đòn đến từ phía trước trong cung **120°** (`_arc`).
- Đường sát thương mới:
  - `EnemyBehaviour.ModifyIncomingDamage(EnemyContext ctx, float damage, Vector2 hitDirection)` — mặc định trả `damage`.
  - `Enemy.TakeHit(float damage, Vector2 hitDirection)`: gọi behaviour rồi `Health.TakeDamage`. Đạn Player (`Projectile`) truyền hướng bay.
  - Nổ (Phóng lựu, nổ lan), Lưỡi cưa: hướng = `Vector2.zero` → không bị chặn (khắc chế có chủ đích).
  - Hàm thuần `EnemyRules.ShieldBlocks(Vector2 facing, Vector2 hitDirection, float arcDegrees)`: true khi hướng đạn đi **ngược** hướng
    mặt trong nửa cung; `hitDirection == zero` → false.
- Phản hồi: sprite khiên (tự vẽ bằng script, `Art/Enemies/shield.png`) là con của prefab `Enemy`, tắt sẵn, `ShieldBehaviour.OnSpawn` bật,
  `ResetVisual` tắt. Đòn bị chặn: số sát thương màu xám (`DamageNumberSource` nhận cờ blocked).
- Ô nhớ EnemyContext: không cần (hướng mặt lấy từ transform).

## 6. Mưa Pháo (`ArtilleryAttack`, `BossAttack`)
- Gồng `_windUp`, rồi trong `_duration` gọi `_shells` quả, cách nhau `_interval`; mỗi quả đặt ở vị trí Player hiện tại + lệch ngẫu nhiên
  trong `_scatter`. Ô nhớ: Timer2 (đếm tới quả kế), Counter (số quả đã gọi), Vector.x (thời gian gồng/đã chạy).
- Quả pháo = object pool `ArtilleryShell` (prefab vòng đỏ báo trước, đầy dần theo thời gian) nằm trong `EnemyProjectiles`
  (`PlaceShell(Vector2 pos, float delay, float radius, float damage)`). Quả tự đếm và nổ: Player trong bán kính thì nhận sát thương qua
  `Health.TakeDamage` (giáp vẫn áp), phát `GameEvents.Explosion` (VFX + âm thanh có sẵn). Hàm thuần `EnemyRules.InBlast(center, radius, point)`.
- Sát thương quả = `ctx.ProjectileDamage × _damageMultiplier` (theo hệ số độ khó như đạn).

## 7. Biến thể, boss, màn, bảng rơi
- Biến thể (CSV): `Enemy_C3_Basic`, `Enemy_C3_Fast`, `Enemy_C3_Tank`, `Enemy_C3_Charger`, `Enemy_C3_Sniper` (sprite cùng bộ khác tư thế/màu, ~×1.2 máu
  so với bản C2). `Enemy_Shield` (Lính Khiên). Kamikaze/Splitter/Healer/Xạ thủ C2 dùng lại.
- **Mini-boss 3-5 "Pháo Đài Di Động"** (`Boss_Fortress`): pha 1 Mưa Pháo + vòng đạn → pha 2 (50%) xoắn ốc + Mưa Pháo dày + lao.
- **Boss cuối 3-10 "Tướng Chỉ Huy"** (`Boss_General`): 3 pha 100/66/33% — vòng đạn + Mưa Pháo → xoắn ốc + gọi 3 Lính Khiên (tối đa 6) + lao
  → Mưa Pháo dày + xoắn ốc 5 cánh + gọi Kamikaze.
- Màn `c3_s01..s10`, `number` 21–30, chapterName "Căn cứ". Mỗi màn 3 đợt × 60 s, nhịp sinh 0.8 → 0.6 s.

| Màn | Mới | Đợt cuối |
|---|---|---|
| 3-1 | biến thể Basic/Fast | Basic C3 tinh anh |
| 3-2 | + Lính Khiên | Lính Khiên tinh anh |
| 3-3 | + Sniper C3 | Tank C3 tinh anh · quà lần đầu mảnh Shotgun |
| 3-4 | Lính Khiên + Healer | Charger C3 tinh anh |
| 3-5 | trộn | **Mini-boss** Pháo Đài Di Động · quà lần đầu mảnh Súng máy |
| 3-6..3-9 | trộn, dày dần | tinh anh xoay vòng (Splitter, Lính Khiên, Kamikaze, Sniper C3) |
| 3-10 | tổng hợp | **Boss cuối** Tướng Chỉ Huy · quà lần đầu mảnh Laser |

- Bảng rơi: `Loot_Common_C3`, `Loot_StageClear_C3`, `Loot_MiniBoss_C3`, `Loot_Boss_C3` — nhiều lõi boss hơn C2, độ hiếm cao hơn
  (có Huyền thoại ở boss). Số cụ thể chốt khi cân bằng.

## 8. Bot tự chơi (`DevTools/Autoplay/`)
### 8.1 Khởi chạy
- `ArenaSurvival.exe -autoplay -speed 4 -label <tên> [-maxRuns 400] -screen-fullscreen 0`. Chỉ khi `Debug.isDebugBuild`.
- Save riêng: `GameServices` dùng `persistentDataPath/autoplay-save.json` khi có `-autoplay`; xóa ở đầu lượt chạy (tài khoản mới).
  Save thật không bị chạm tới. Hàm thuần chọn đường dẫn để test.
- `runInBackground = true` lúc chạy (như benchmark).
- Tăng tốc: `GameManager.PlayingTimeScale` (static, mặc định 1) — mọi chỗ "tiếp tục chơi" đặt `Time.timeScale = PlayingTimeScale`
  thay vì 1. Bot đặt = `-speed`.

### 8.2 Hub (`AutoplayHub` + hàm thuần `AutoplayRules`)
- Lặp tới khi không còn lựa chọn mua được: liệt kê ứng viên {Talent +1, nâng vũ khí, mở khóa/lên sao vũ khí, nâng đồ đang mặc, đột phá},
  mỗi ứng viên có chi phí vàng (quy đổi: chi phí chỉ nguyên liệu/mảnh coi như 0 vàng nhưng vẫn cần đủ) và mức tăng lực chiến
  (tính bằng `LoadoutStats`/`StatCalculator`/`WeaponRules`/`EquipmentRules`). `AutoplayRules.PickBest` chọn tăng/chi phí lớn nhất
  (ứng viên không tốn vàng mà tăng > 0 được ưu tiên).
- Mặc đồ và vũ khí có lực chiến cao nhất; kho > 80 món → phân rã đồ yếu nhất không mặc.
- Nhận popup lên cấp tài khoản (thưởng đã cộng ở service, chỉ đóng UI nếu cần).
- Chọn màn: màn chưa thắng đầu tiên; thua 3 lượt liền ở 1 màn → chơi lại màn cao nhất đã thắng 1 lượt (farm) rồi thử tiếp;
  >15 lượt ở 1 màn → ghi "kẹt" và dừng. Dừng khi thắng 3-10 hoặc hết `-maxRuns`.

### 8.3 Trong màn (`BotPilot` + hàm thuần `BotSteering`)
- `PlayerController` có `Vector2? MoveOverride` (bot đặt; null = bàn phím). Không có `BotPilot` ở bản phát hành.
- Hướng đi = tổng các lực (hàm thuần, đầu vào là danh sách mối nguy đã thu thập):
  - đẩy khỏi quái trong 5 đơn vị ~ 1/d² (boss ×3, Charger/Kamikaze đang gồng ×2);
  - đạn quái trong 3 đơn vị đang bay tới: né vuông góc với vận tốc đạn;
  - vòng Mưa Pháo: đang trong (bán kính + 0.5) → chạy ra khỏi tâm;
  - tia Sniper đang bật: cách tia < 0.8 → bước ngang tia;
  - mép sân < 2.5 đơn vị và góc sân: kéo về giữa (tránh bị dồn góc);
  - hút nhẹ về orb XP / đồ rơi gần nhất khi không có lực đẩy đáng kể.
- Thu thập mối nguy bằng `Physics2D.OverlapCircle` (buffer dùng lại) + danh sách quả pháo/tia ngắm đang bật; cập nhật ~10 lần/giây
  (thời gian game) để rẻ.
- Bảng lên cấp: Lưỡi cưa → Sát thương → Tốc độ bắn → Máu tối đa → Tốc chạy; Hồi máu lên đầu khi máu < 50%.
- Hồi sinh: luôn nhận. Màn kết quả: ghi nhật ký → về Hub.

### 8.4 Nhật ký
- `autoplay-log.csv` (cùng thư mục save, ghi nối từng dòng, flush ngay): `run,stage,result,gameTime,hpPercent,revived,power,recommended,
  stars,goldGained,totalMinutes,accountLevel,farm`.
- Hết lượt chạy: `autoplay-summary.md` — mỗi màn: số lượt tới khi thắng, lực chiến lúc thắng / đề xuất, phút cộng dồn, số lần hồi sinh,
  số lần chết < 3 s sau hồi sinh; cuối bảng: tổng phút, màn kẹt (nếu có).

## 9. Quy trình cân bằng
1. Làm xong Chương 3 (đề xuất lực chiến tạm từ mô phỏng nối tiếp C2).
2. Bot chạy 30 màn → nhật ký nền.
3. Phân loại lệch: **theo màn** (màn cần lực chiến/đề xuất cao bất thường so với màn bên cạnh → sửa đội hình/máu/nhịp sinh trong CSV) và
   **toàn cục** (cả chương lệch → `recommendedPower`/thưởng; chạy lại mô phỏng cho khớp).
4. Mục tiêu (theo plan): Chương 1 ~50 phút; boss chương là "tường" cần nâng cấp nhưng farm vẫn qua; không màn nào kẹt.
5. Lặp 2–3 vòng; mỗi vòng ghi thay đổi + lý do vào `Docs/Balance/playtest-report.md`.
6. Xem lại "chết lại ngay sau hồi sinh" bằng số liệu nhật ký; sửa nếu tần suất đáng kể (vd. tăng thời gian bất tử/bán kính đẩy).

## 10. Kiểm tra
1. Unit test: `EnemyRules.ShieldBlocks`, `EnemyRules.InBlast`, `AutoplayRules.PickBest`, `BotSteering` (từng lực + tổng), đường dẫn save
   autoplay, catalog 3 chương (`GetNext` 2-10 → 3-1).
2. Nhập CSV 2 lần: lần 2 "không đổi".
3. Chơi thử trong Editor: Lính Khiên chặn đạn phía trước (số xám), đạn từ sau/nổ không bị chặn; vòng Mưa Pháo né được; 3-5, 3-10; nền Hub căn cứ.
4. Bot chạy trọn 30 màn không lỗi; save thật không đổi (so sánh file trước/sau).
5. Benchmark 300 quái màn 30 (`-stage 30`): GC 0 B/frame (bot không chạy khi benchmark).
