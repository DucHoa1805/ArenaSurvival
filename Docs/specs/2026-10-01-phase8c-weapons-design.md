# Phase 8C — Vũ khí meta: ô Vũ khí, cấp, sao, mảnh, 5 kiểu bắn

Ngày: 2026-10-01
Trạng thái: đã duyệt thiết kế (phương án A), chờ duyệt spec
Liên quan: `Docs/project-plan.md` (Phase 8: "Mảnh vũ khí", "Vũ khí khác nhau về cách bắn"; A4 "Mảnh vũ khí"; D1),
spec 8A, 8B (`2026-10-01-phase8b-items-loot-design.md`)

## 1. Mục tiêu
Ngoài màn người chơi chọn **1 vũ khí chính** mang vào trận. Vũ khí mạnh lên bằng **cấp** (vàng + quặng — quặng có chỗ tiêu)
và **sao** (mảnh riêng từng khẩu, mở hiệu ứng ở 3★/5★). 5 khẩu khác nhau về **cách bắn**, không chỉ khác chỉ số.
Mảnh rơi từ tinh anh, boss và quà thắng lần đầu.

## 2. Quyết định đã chốt
| Câu hỏi | Chọn |
|---|---|
| Vũ khí meta là gì | Vũ khí chính mang vào màn, thay "Súng thường" làm súng khởi đầu; vũ khí phụ trong màn (Lưỡi cưa) giữ nguyên |
| Bộ vũ khí | 5 khẩu: Súng lục (có sẵn), Shotgun (thay Súng chùm), Súng máy, Phóng lựu, Laser |
| Súng chùm / Súng máy cũ | Cả hai thành vũ khí chính; bỏ 2 nâng cấp "mở khóa" khỏi bảng lên cấp |
| Tiến trình | Cấp (vàng + quặng) + Sao 1★→5★ (mảnh) |
| Nguồn mảnh | Tinh anh (10%), mini-boss/boss (3–5 mảnh), quà thắng lần đầu (đủ mở 1 khẩu mỗi màn) |
| Kiểu bắn | **Phương án A**: thuộc tính đạn trong dữ liệu (xuyên, nổ vùng, đẩy lùi, tầm), 1 `PlayerWeapon` + 1 `Projectile` |

## 3. Ngoài phạm vi
Vũ khí phụ mới trong màn, cân bằng số liệu (Phase 10); rương/cửa hàng bán mảnh (Phase 11); tia laser liên tục kiểu beam
(nếu cần → chuyển kiểu bắn sang SO chiến lược, phương án B); vũ khí mang hiệu ứng trạng thái (cháy, choáng).

## 4. Dữ liệu
### 4.1 `WeaponData` (mở rộng, `Scripts/Weapons/`)
Thêm vào asset hiện có (giữ các trường cũ `damage, fireRate, range, projectileSpeed, projectileCount, spreadAngle`):
- `id` (cố định: `weapon_pistol`, `weapon_shotgun`, `weapon_machinegun`, `weapon_grenade`, `weapon_laser`), `description` (1 dòng cách bắn).
- `damagePerLevel`; `aimJitter` (độ, lệch ngẫu nhiên mỗi viên).
- Thuộc tính đạn: `pierce` (số con xuyên thêm), `explosionRadius`, `explosionDamageFraction`, `knockback` (đơn vị thế giới).
- `projectilePrefab` (không bắt buộc; trống = đạn mặc định của `PlayerWeapon`).
- `shardMaterial` (`MaterialData`, id = `WeaponRules.ShardId(id)` = `"shard_" + id`).
- `starBonus3`, `starBonus5`: `WeaponStarBonus { int extraProjectiles; int extraPierce; float fireRatePercent; float explosionRadiusPercent;
  float knockbackPercent; string text; }` (`text` để UI ghi "3★: +2 viên").

### 4.2 Database
`ItemDatabase` thêm `WeaponData[] weapons` + `GetWeapon(id)`; mảnh nằm trong `materials` như nguyên liệu.

### 4.3 Bộ vũ khí khởi điểm (cân bằng Phase 10)
| id | Tên | Sát thương | Phát/giây | Viên | Tỏa | Tầm | Tốc đạn | Khác | 3★ | 5★ |
|---|---|---|---|---|---|---|---|---|---|---|
| `weapon_pistol` | Súng lục | 5 | 1.5 | 1 | 0° | 6 | 10 | — | +1 viên | xuyên +1 |
| `weapon_shotgun` | Shotgun | 2.5 | 0.9 | 6 | 60° | 3.5 | 11 | đẩy lùi 0.35 | +2 viên | đẩy lùi +100% |
| `weapon_machinegun` | Súng máy | 1.6 | 6 | 1 | 0° | 6.5 | 16 | lệch ±6° | tốc bắn +25% | xuyên +1 |
| `weapon_grenade` | Phóng lựu | 9 | 0.8 | 1 | 0° | 6.5 | 7 | nổ r=1.6, 70% | bán kính +30% | +1 quả |
| `weapon_laser` | Laser | 6 | 1.0 | 1 | 0° | 8 | 24 | xuyên 4 | xuyên +3 | tốc bắn +30% |

`damagePerLevel` = 8% sát thương gốc. Asset: `Weapon_Basic` → `Weapon_Pistol`, `Weapon_Spread` → `Weapon_Shotgun` (đổi tên bằng
`AssetDatabase.MoveAsset`, giữ GUID), `Weapon_MachineGun` giữ tên; tạo `Weapon_Grenade`, `Weapon_Laser`.
`Upgrade_SpreadGun`, `Upgrade_MachineGun` gỡ khỏi bảng lên cấp và xóa asset.

### 4.4 Nguồn mảnh
- `LootTable` thêm `LootTable[] alsoRoll` — quay kèm từng bảng con (đệ quy 1 tầng là đủ; chặn tự tham chiếu).
- `Loot_Elite`: thêm vào phần `weighted` 5 mục mảnh (mỗi khẩu trọng số 2 → 10% tổng), giảm "không gì" tương ứng.
- `Loot_WeaponShards` (mới): weighted 5 mục mảnh, mỗi mục 3–5 mảnh; gắn `alsoRoll` của `Loot_MiniBoss` và `Loot_Boss`.
- `StageData.firstClearReward` (`LootEntry[]`): 1-1 → 10 mảnh Shotgun, 1-2 → 10 mảnh Súng máy, 1-3 → 10 mảnh Phóng lựu.
  `StageSession.FillLoot` thêm vào `bonusLoot` khi thắng lần đầu. Laser chỉ gom từ tinh anh/boss.

## 5. Save v5
`PlayerProfile.CurrentVersion = 5`; `SaveMigrations.V4ToV5`:
- thêm `weapons = { "weapon_pistol": { "level": 1, "stars": 1 } }`;
- `equipped["weapon"] = "weapon_pistol"` nếu chưa có.
```
weapons:  { "weapon_pistol": { "level": 4, "stars": 1 }, "weapon_shotgun": { "level": 1, "stars": 2 } }
equipped: { "armor": "i17", "accessory": "i20", "weapon": "weapon_shotgun" }   // ô vũ khí lưu id vũ khí, không phải uid
currencies: { ..., "shard_weapon_laser": 7 }
```
- `OwnedWeapon` (class thuần, `Meta/`): `level` (≥1), `stars` (1..5). Có trong `weapons` = đã sở hữu.
- `EquipSlotIds.Weapon = "weapon"`. `ProfileService.ResetToNew` đặt lại về đúng trạng thái sau migration (có Súng lục 1★).
- `SaveService.ToProfile`: `weapons` null → rỗng; nếu rỗng thì tặng lại Súng lục (người chơi không bao giờ vào trận tay không).

## 6. Logic thuần — `WeaponRules` (`Weapons/`)
| Sao | 1★ (mở khóa) | 2★ | 3★ | 4★ | 5★ |
|---|---|---|---|---|---|
| Mảnh cần để lên sao này | 10 | 20 | 40 | 80 | 150 |
| Hệ số sát thương | ×1 | ×1.25 | ×1.5 | ×1.8 | ×2.2 |
| Trần cấp | 10 | 20 | 30 | 40 | 50 |

- `ShardsFor(stars)` (mảnh để đạt sao `stars`), `StarMultiplier(stars)`, `LevelCap(stars)`, `MaxStars = 5`.
- `UpgradeGold(level)` = `floor(100 × 1.15^(level − 1))`; `UpgradeOre(level)` = `1 + floor(level / 2)`.
- `WeaponStats Effective(WeaponData, int level, int stars)` (struct): `damage = (damage + damagePerLevel × (level − 1)) × StarMultiplier`,
  `fireRate`, `projectileCount`, `spreadAngle`, `aimJitter`, `range`, `projectileSpeed`, `pierce`, `explosionRadius`,
  `explosionDamageFraction`, `knockback`; cộng `starBonus3` khi `stars ≥ 3`, `starBonus5` khi `stars ≥ 5`.
- `Power(WeaponStats)` = `round(4 × damage × fireRate × projectileCount × (1 + 0.25 × pierce) × (1 + 2 × explosionDamageFraction nếu có nổ))`.
  Lực chiến hiển thị = `StatCalculator.Power(chỉ số)` + `WeaponRules.Power(vũ khí đang mặc)`.

## 7. Dịch vụ — `WeaponService(PlayerProfile, EconomyService, ItemDatabase, Action save)` → `GameServices.Weapons`
- Đọc: `IsOwned(id)`, `GetLevel(id)`, `GetStars(id)`, `GetShards(id)`, `EquippedId`, `EquippedData`, `GetEffective(id)`,
  `GetUpgradeCost(id, List<CurrencyCost>)`, `GetStarCost(id, List<CurrencyCost>)` (mở khóa = lên 1★).
- Thao tác (thành công → lưu ngay + `OnWeaponChanged(id)`; thất bại → false, không đổi gì):
  `TryUnlock(id)`, `TryStarUp(id)`, `TryUpgrade(id)` (chặn ở `LevelCap(stars)`), `Equip(id)` (chỉ khẩu đã sở hữu).
- Mọi chi phí trừ qua `EconomyService.TrySpendMany`.

## 8. Trong màn
- **Player prefab**: còn 1 `PlayerWeapon` (súng chính) + `OrbitWeapon` (Lưỡi cưa). Bỏ component Súng chùm, Súng máy.
- `PlayerWeapon.Equip(WeaponData data, WeaponStats stats)`: gán dữ liệu, tạo pool theo `projectilePrefab` (lần đầu). Bắn theo `WeaponStats`
  × hệ số trong màn (`PlayerStats`). `aimJitter` lệch ngẫu nhiên từng viên.
- `PlayerLoadout.Awake`: lấy vũ khí đang mặc từ `GameServices.Weapons` (không có/không hợp lệ → Súng lục cấp 1, 1★) → `Equip`.
- `FindNearestEnemy`: thay `OverlapCircleNonAlloc` (obsolete) bằng `Physics2D.OverlapCircle(pos, r, ContactFilter2D, Collider2D[])`,
  buffer 128.
- **`Projectile`** (dùng chung, vẫn qua pool): `Launch(dir, damage, speed, range, pierce, explosionRadius, explosionFraction, knockback)`.
  - `lifetime = range / speed` (Shotgun tầm ngắn thật).
  - Xuyên: đếm số con đã trúng; trúng tới `pierce + 1` con mới trả về pool.
  - Đẩy lùi: dời `Rigidbody2D.position` của quái theo hướng đạn một đoạn `knockback` (quái bị `ClampToArena` giữ trong sân).
  - Nổ: khi trúng (hoặc hết tầm với lựu đạn) quét `OverlapCircle` buffer dùng chung, gây `damage × fraction` cho mọi quái khác trong bán kính;
    phát `GameEvents.Explosion(vị trí, bán kính)` (hiệu ứng + âm thanh tự nghe).
  - Đạn quái (EnemyProjectile) không đổi hành vi: mặc định pierce 0, không nổ, không đẩy.
- `VfxSpawner` nghe `OnExplosion` → hiệu ứng nổ (pool, có `maxSize`, tỉ lệ theo bán kính); `AudioManager` → `SoundBank.explosion`.
- `BenchmarkRunner`: thêm `-mainWeapon <id>` (mặc định vũ khí đang mặc) để đo Phóng lựu/Laser.

## 9. Giao diện
- **Bảng Trang bị**: hàng ô đang mặc thành 3 ô **Vũ khí** | Giáp | Phụ kiện. Ô vũ khí: icon, chữ "★n Lv m". Bấm → mở `WeaponPanel`.
  Lực chiến tính thêm phần vũ khí (mục 6) — `LoadoutStats` thêm `Power(heroBase, talents)` dùng chung cho mọi bảng.
- **`WeaponPanel`** (prefab, phủ lên bảng Trang bị, dùng lại ở Hub Phase 9):
  - Danh sách 5 dòng (`WeaponRowView`): icon, tên, sao ★★☆☆☆, thanh mảnh "12/20" (tới 5★: "TỐI ĐA"), nhãn "Đang dùng";
    chưa sở hữu: icon xám + "Mở khóa 7/10".
  - Chi tiết khẩu đang chọn: tên, mô tả cách bắn, chỉ số hiện tại → cấp sau (sát thương, tốc bắn, số viên, xuyên/nổ nếu có),
    2 dòng hiệu ứng 3★/5★ (sáng khi đã mở), nút: **Mở khóa** / **Dùng** / "Đang dùng", **Nâng cấp** (vàng + quặng),
    **Lên sao** (mảnh). Mờ khi không đủ tài nguyên / tới trần.
- **Màn kết quả**: mảnh hiện như nguyên liệu (icon mảnh riêng) — không cần sửa.
- **Menu dev (F1)**: "+20 mảnh mỗi vũ khí".
- **Icon / sprite** (vẽ bằng script, ghi `asset-licenses.md`): 5 icon súng 128 px, 5 icon mảnh (tinh thể màu riêng + hình súng nhỏ),
  đạn lựu, tia laser.

## 10. Kiểm tra
1. Unit test:
   - `WeaponRulesTests`: Effective theo cấp/sao, cộng starBonus 3★/5★, `ShardsFor`, `LevelCap`, giá nâng cấp, `ShardId`, `Power` tăng theo cấp/sao.
   - `WeaponServiceTests`: mở khóa đủ/thiếu mảnh, lên sao tới 5★ rồi chặn, nâng cấp thiếu quặng không đổi gì, chặn ở trần cấp,
     không mặc được khẩu chưa sở hữu, sự kiện + lưu.
   - `LootRulesTests`: `alsoRoll` quay kèm bảng con; bảng tự tham chiếu không lặp vô hạn.
   - Migration v4→v5 (+ save thiếu `weapons` được tặng Súng lục).
2. Toàn bộ test đạt; biên dịch 0 lỗi; Missing Script = 0.
3. Chơi thử: menu dev cộng mảnh → mở khóa Shotgun → Dùng → vào màn: bắn chùm tầm ngắn, đẩy lùi; Phóng lựu nổ vùng; Laser xuyên;
   nâng cấp/lên sao thấy sát thương + lực chiến tăng; bảng lên cấp không còn Súng chùm/Súng máy; thắng 1-1 lần đầu nhận 10 mảnh Shotgun.
4. Benchmark 300 quái với `-mainWeapon weapon_grenade` và `weapon_laser`: GC 0 B/frame.
