# Phase 10A — Công cụ dữ liệu (CSV, mô phỏng) + Chương 1 đủ 10 màn + 4 quái mới

Ngày: 2026-10-01
Trạng thái: đã duyệt thiết kế (phương án A), chờ duyệt spec
Liên quan: `Docs/project-plan.md` (Phase 10, A2/A3 cấu trúc chương, D1–D2 kinh tế), spec 6A/6B (màn, hành vi quái), spec 9 (Hub)

## 1. Mục tiêu
Có **đường ống dữ liệu** để làm 30 màn mà không tạo asset bằng tay: bảng CSV (Google Sheets/Excel) → menu Editor nhập thành
ScriptableObject; **mô phỏng tiến trình** để đo "tường" ở boss chương; **Chương 1 hoàn chỉnh 10 màn** với 4 loại quái mới
có hành vi riêng. Phase 10 chia: **10A (này)** → 10B Chương 2 (bối cảnh mới) → 10C Chương 3 + cân bằng + chơi thử 30 màn.

## 2. Quyết định đã chốt
| Câu hỏi | Chọn |
|---|---|
| Chia đợt | 10A công cụ + Chương 1 → 10B Chương 2 → 10C Chương 3 + cân bằng |
| CSV quản lý | Màn + đợt + quái (`stages.csv`, `waves.csv`, `enemies.csv`); chi phí nâng cấp vẫn là công thức trong code |
| Cách nhập | **Phương án A**: menu Editor tạo/cập nhật asset tại chỗ (giữ GUID); game vẫn chạy bằng SO |
| Quái mới | Charger, Kamikaze, Splitter, Healer — làm cả 4 trong 10A, xuất hiện dần trong Chương 1 |
| Boss | Pha/đòn vẫn dựng tay; chỉ số (máu, tốc, sát thương, XP) lấy từ `enemies.csv` |

## 3. Ngoài phạm vi
Bản đồ chương có hình vẽ, bối cảnh/Tilemap mới (10B); Chương 2–3 (10B/10C); chơi thử 30 màn bằng tài khoản mới, chỉnh số cuối (10C);
bảng hệ số kinh tế dạng dữ liệu (giữ công thức trong code); cửa hàng/rương (Phase 11).

## 4. Dữ liệu
### 4.1 Chương
- `ChapterData` (SO, `Scripts/Stages/`): `id` (`c1`), `number`, `displayName` ("Sa mạc"), `StageData[] stages`.
- `StageCatalog` thêm `ChapterData[] chapters`; giữ `StageData[] stages` (danh sách phẳng theo thứ tự chơi, công cụ nhập ghi cả hai)
  để code hiện có (mở khóa tuần tự, `NextStage`, benchmark) không phải đổi. `ChapterOf(StageData)`.
- `StageData.stageNumber` là **số thứ tự toàn cục** (Chương 2 bắt đầu từ 11) → công thức độ khó/thưởng tự tăng qua các chương.
- Giữ id `c1_s01..c1_s03` (sao đã đạt không mất). Mini-boss chuyển sang 1-5, boss chương sang 1-10 (1-2, 1-3 thành màn thường).

### 4.2 CSV (`Docs/Balance/`, UTF-8, dòng đầu là tên cột, dấu `;` ngăn phần tử trong 1 ô)
- `enemies.csv`: `id, displayName, sprite, behaviour, maxHealth, moveSpeed, contactDamage, projectileDamage, xpReward, scale, tint,
  isBoss, loot, lootChance` — `sprite` = đường dẫn trong `Art/` (vd `Kenney-TopDownShooter/PNG/Hitman 1/hitman1_gun.png`),
  `behaviour` = tên asset hành vi, `tint` = mã màu `#RRGGBB`, `loot` = tên `LootTable`. Asset: `Data/Enemies/Enemy_<Id>.asset`
  (quái thường) hoặc cập nhật asset boss có sẵn theo `id`.
- `waves.csv`: `id, duration, spawnInterval, enemies` → `Data/Stages/Waves/Wave_<id>.asset`.
- `stages.csv`: `chapter, chapterName, number, id, displayName, waves, eliteEnemy, eliteCount, eliteHealth, boss, bossDelay,
  targetTime, baseGold, baseXp, recommendedPower, bossLoot, victoryLoot, firstClearShard, firstClearAmount`
  → `Data/Stages/Stage_<id>.asset`, `Data/Stages/Chapter_<chapter>.asset`, cập nhật `StageCatalog_Main`.
- `Enemy_Tank.asset` (thường có thay đổi riêng của người dùng): công cụ chỉ ghi các cột có trong CSV; báo rõ trong kết quả nhập.

### 4.3 Công cụ nhập (assembly mới `ArenaSurvival.Editor`, thư mục `Assets/_Project/Editor/`)
- `CsvReader` (hàm thuần, trong assembly game để unit test được): tách dòng/ô, hỗ trợ ngoặc kép, dấu phẩy trong ngoặc, dòng trống, BOM.
- `BalanceImporter` (Editor): menu `ArenaSurvival/Nhập bảng cân bằng`. Thứ tự: quái → đợt → màn → chương → catalog.
  Kiểm tra trước khi ghi: id trùng, tham chiếu không tồn tại (sprite, hành vi, quái, đợt, bảng rơi, boss) → **không ghi gì**,
  in danh sách lỗi theo dòng. Thành công: in số asset tạo/sửa/không đổi. Chạy 2 lần liên tiếp thì lần 2 "không đổi".
- `BalanceExporter`: menu `ArenaSurvival/Xuất bảng cân bằng` — ghi CSV từ asset hiện có (lần đầu tạo bảng, và để người dùng có bản gốc).

### 4.4 Mô phỏng tiến trình
- `ProgressionSim` (hàm thuần, `Meta/`): đầu vào = danh sách màn (lực chiến đề xuất, thưởng, thời lượng), mô hình lực chiến theo vàng đã tiêu,
  seed. Mỗi lượt: chơi màn mở cao nhất; xác suất thắng = `clamp01(0.15 + 0.85 × (lực chiến / đề xuất − 0.75) / 0.5)` (thua chắc dưới 75%,
  gần chắc thắng từ 125%); nhận thưởng theo `StageRules`/`StageProgressRules`; tiêu vàng theo "lực chiến tăng nhiều nhất mỗi đồng"
  (mô hình gộp: mỗi 100 vàng ≈ +X lực chiến, X giảm dần theo tổng đã tiêu).
- Kết quả: mỗi màn → số lượt trung bình để qua (100 lần chạy), phút chơi cộng dồn. Menu `ArenaSurvival/Mô phỏng tiến trình` in bảng
  và ghi `Docs/Balance/sim-report.md`.

## 5. Quái mới (`Scripts/Enemies/`)
| Hành vi | Mô tả | Tham số (asset) |
|---|---|---|
| `ChargerBehaviour` | Đuổi; < `triggerRange` (5) → gồng `windup` 0.6 s (rung Visual, khóa hướng) → lao `dashSpeed` 12 trong 0.45 s → nghỉ 0.8 s | triggerRange, windup, dashSpeed, dashTime, recover |
| `KamikazeBehaviour` | Chạy nhanh; < `fuseRange` 1.3 → dừng, chớp 0.6 s → nổ r=1.8 gây `contactDamage × 2` cho Player trong bán kính, phát `GameEvents.Explosion`, `Despawn(false)` (không XP/đồ) | fuseRange, fuseTime, radius, damageMultiplier |
| `SplitterBehaviour` | Như đuổi thẳng; `OnDeath` → `World.Summon(child, mods máu ×0.4)` × 2 cách nhau 0.4 | child (EnemyData), count |
| `HealerBehaviour` | Giữ khoảng 6±1 (như Xạ thủ, không bắn); mỗi 4 s hồi 20% máu tối đa cho quái khác trong r=3.5; phát `GameEvents.EnemyHealPulse` | keepDistance, interval, radius, healFraction |

- `EnemyBehaviour` thêm `virtual void OnDeath(EnemyContext ctx)` — `Enemy.HandleDied` gọi trước khi trả về pool (không gọi ở `Despawn`).
- `EnemyRules` thêm hàm thuần: `HealAmount(max, current, fraction)` (không vượt tối đa), `KamikazeHit(distance, radius)`.
- Hồi máu dùng `Physics2D.OverlapCircle` + buffer tĩnh dùng chung; `VfxSpawner` nghe `OnEnemyHealPulse` (vòng xanh, pool có giới hạn).
- Asset: `Behaviour_Charger|Kamikaze|Splitter|Healer`, `Enemy_Charger|Kamikaze|Splitter|Splitter_Small|Healer` (qua CSV).

## 6. Chương 1 "Sa mạc" (qua CSV)
| Màn | Mới | Đợt | Đợt cuối |
|---|---|---|---|
| 1-1 | Basic | 3 × 45 s | 3 Basic tinh anh |
| 1-2 | + Fast | 3 × 45 s | Fast tinh anh |
| 1-3 | + Charger (đợt 2) | 3 × 60 s | Tank tinh anh · quà lần đầu 10 mảnh Shotgun |
| 1-4 | + Xạ thủ | 3 × 60 s | Xạ thủ + Charger tinh anh |
| 1-5 | trộn | 3 × 60 s | **Mini-boss** Cỗ Máy Gác Cổng · 10 mảnh Súng máy |
| 1-6 | + Kamikaze | 3 × 60 s | Kamikaze tinh anh |
| 1-7 | + Splitter | 3 × 60 s | Splitter tinh anh |
| 1-8 | Tank + Kamikaze + Xạ thủ | 3 × 60 s | Tank tinh anh |
| 1-9 | + Healer | 3 × 60 s | Healer + Charger tinh anh |
| 1-10 | tổng hợp | 3 × 60 s | **Boss chương** Thủ Lĩnh Sa Mạc · 10 mảnh Phóng lựu |

Lực chiến đề xuất: lấy từ mô phỏng sao cho người không nạp qua Chương 1 trong ~1–2 giờ chơi (plan: "trong ngày đầu"),
1-10 là "tường" (cần 2–4 lượt farm). Số cụ thể chốt sau khi chạy mô phỏng, ghi vào `stages.csv`.

## 7. Giao diện — tab Chiến đấu
- Nút màn xếp 2 hàng × 5; màn mini-boss có đầu lâu nhỏ, màn boss chương có viền đỏ + đầu lâu lớn.
- Tên chương lấy từ `ChapterData`; nút ◀ ▶ đổi chương (ẩn khi chỉ có 1 chương); mặc định mở chương chứa màn nên chơi tiếp.

## 8. Kiểm tra
1. Unit test: `CsvReader` (ngoặc kép, dấu phẩy trong ô, dòng trống, BOM, thiếu cột), ánh xạ dòng → dữ liệu (`BalanceRows`),
   `ProgressionSim` (cùng seed cùng kết quả; lực chiến cao hơn → ít lượt hơn; màn khóa không chơi được), `EnemyRules` mới,
   `StageCatalog.ChapterOf`, tiến độ cũ (`c1_s01..03`) vẫn mở đúng.
2. Nhập 2 lần: lần 2 báo "không đổi"; GUID asset cũ giữ nguyên; `Enemy_Tank.asset` chỉ đổi các cột trong CSV.
3. Toàn bộ test đạt; Missing Script = 0; scene sạch trước khi chạy test.
4. Chơi thử: từng quái mới trong màn có nó; 1-5 mini-boss, 1-10 boss; tab Chiến đấu 10 màn; save cũ giữ sao.
5. Benchmark 300 quái trộn quái mới (`-stage 10`): GC 0 B/frame.
