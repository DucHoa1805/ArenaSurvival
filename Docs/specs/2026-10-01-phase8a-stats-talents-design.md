# Phase 8A — Chỉ số, Talent, lực chiến

Ngày: 2026-10-01
Trạng thái: đã duyệt (phương án A)
Liên quan: `Docs/project-plan.md` (Phase 8, A4 "Công thức chỉ số cuối cùng", D1)

## 1. Mục tiêu
Vàng có chỗ tiêu: 5 Talent nâng vĩnh viễn; chỉ số tính bằng hàm thuần (`StatCalculator`) và đưa vào trận qua một cửa duy nhất (`PlayerLoadout`);
giáp giảm sát thương; hồi máu theo thời gian; lực chiến. Phase 8 chia 3 đợt: **8A (này)** → 8B vật phẩm/kho đồ/nâng cấp/rơi đồ → 8C vũ khí meta.

## 2. Ngoài phạm vi
Vật phẩm, trang bị, nguyên liệu, bảng rơi đồ (8B); mảnh/sao vũ khí, cách bắn (8C); lực chiến đề xuất từng màn (Phase 9/10); Hub (Phase 9).

## 3. Dữ liệu
- `StatBlock` (struct, `Scripts/Stats/`): `maxHealth`, `armor`, `damagePercent`, `moveSpeedPercent`, `healthRegen` (máu/giây). Toán tử `+`.
- `HeroBaseStats` (SO, `Stats/`): `StatBlock baseStats` (mặc định 100 máu). Asset `Data/Stats/HeroBaseStats.asset`.
- `StatKind` (enum): `MaxHealth, Armor, DamagePercent, MoveSpeedPercent, HealthRegen` — enum ở đây chỉ chọn **trường nào của StatBlock**, thêm Talent mới không cần sửa code.
- `TalentData` (SO, `Meta/Talents/` → namespace `ArenaSurvival.Meta`): `id` (cố định), `displayName`, `icon`, `StatKind stat`, `valuePerLevel`, `maxLevel`, `baseCost`, `costGrowth` (mặc định 1.12).
- `TalentCatalog` (SO): `TalentData[] talents`. Asset `Data/Talents/TalentCatalog_Main.asset` + 5 asset Talent (bảng mục 7).
- `PlayerProfile` v3: thêm `Dictionary<string,int> talents`. `SaveMigrations` bước v2→v3 (thêm `talents` rỗng).

## 4. Logic thuần
- `StatRules.Add(StatBlock, StatKind, float)` → StatBlock (cộng giá trị vào đúng trường).
- `StatCalculator.Compute(StatBlock heroBase, TalentCatalog catalog, Func<string,int> levelOf)` → `heroBase + Σ(talent.valuePerLevel × level)`.
- `StatCalculator.Power(StatBlock s)` = `round(maxHealth × 1 + armor × 6 + damagePercent × 12 + moveSpeedPercent × 4 + healthRegen × 40)` (trọng số khởi điểm, cân bằng Phase 10).
- `TalentRules.Cost(baseCost, growth, currentLevel)` = `floor(baseCost × growth^currentLevel)` (giá để lên cấp kế tiếp); `IsMaxed(level, max)`.
- `CombatRules.ApplyArmor(damage, armor)` = `damage × 100 / (100 + max(0, armor))`.

## 5. Dịch vụ
- `TalentService(PlayerProfile, EconomyService, Action save)`: `GetLevel(id)`, `GetCost(TalentData)` (−1 nếu đã tối đa), `CanUpgrade`, `TryUpgrade(TalentData)` (trừ vàng qua `EconomyService.TrySpend`; không đủ/tối đa → false, không đổi gì; thành công → lưu ngay, phát `OnTalentChanged(id, level)`).
- `GameServices.Talents`. `GameServices` cần `TalentCatalog`? — không: `TalentService` không cần catalog; UI và `PlayerLoadout` giữ tham chiếu catalog.

## 6. Đưa vào trận
- `Health`: thêm `float Armor`; `TakeDamage` áp `CombatRules.ApplyArmor` (quái armor 0 → không đổi).
- `PlayerStats`: thêm `SetBaseMultipliers(damage, moveSpeed)` — giá trị gốc trước khi nâng cấp trong màn cộng thêm.
- `HealthRegen` (mới, `Combat/`): hồi `RegenPerSecond` máu/giây khi còn sống (dùng `deltaTime`, dừng khi pause).
- `PlayerLoadout` (mới, `Player/`, trên prefab Player): ở `Awake` tính `StatCalculator.Compute(heroBase, catalog, GameServices.Talents.GetLevel)` → `Health.SetMaxHealth(hp, healToFull)`, `Health.Armor`, `PlayerStats.SetBaseMultipliers(1 + dmg%/100, 1 + speed%/100)`, `HealthRegen.RegenPerSecond`. `Health._maxHealth` của prefab không còn là nguồn chỉ số.

## 7. Talent khởi điểm
| id | Tên | StatKind | Mỗi cấp | Max | Giá gốc |
|---|---|---|---|---|---|
| `talent_hp` | Máu tối đa | MaxHealth | 10 | 50 | 100 |
| `talent_armor` | Giáp | Armor | 2 | 50 | 150 |
| `talent_damage` | Sát thương | DamagePercent | 3 | 50 | 150 |
| `talent_speed` | Tốc chạy | MoveSpeedPercent | 1 | 20 | 200 |
| `talent_regen` | Hồi máu | HealthRegen | 0.2 | 25 | 200 |

## 8. Giao diện
- Prefab `Prefabs/UI/TalentPanel.prefab` + script `UI/TalentPanel.cs`, `UI/TalentRow.cs`: tiêu đề "NÂNG CẤP", số vàng, lực chiến, 5 dòng (icon, tên, "Cấp n/max", "hiện tại → sau", nút giá; mờ khi không đủ vàng/tối đa), nút Đóng. Tự cập nhật theo `OnCurrencyChanged`/`OnTalentChanged`.
- Menu chính: nút "Nâng cấp" (giữa Chơi và Cài đặt) mở bảng. Icon giáp mới `icon_shield.png` (vẽ bằng script).

## 9. Kiểm tra
1. Unit test: `StatRulesTests` (Add từng loại, Compute cộng talent, Power đơn điệu), `TalentServiceTests` (giá tăng 1.12^n, nâng đủ tiền, thiếu tiền không đổi, tối đa, lưu, sự kiện), `CombatRulesTests` (giáp 0/100/âm), migration v2→v3 (thêm vào `SaveServiceTests`).
2. Toàn bộ test đạt; biên dịch 0 lỗi; Missing Script = 0.
3. Chơi thử: cộng vàng (menu dev) → nâng Talent → lực chiến tăng → vào màn: máu tối đa, giáp (sát thương nhận giảm đúng công thức), hồi máu, tốc chạy, sát thương đúng.
4. Benchmark 300 quái: GC 0 B/frame.
