# Phase 8C — Vũ khí meta — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 5 vũ khí chính có cấp + sao + mảnh, mang vào màn thay súng khởi đầu, mỗi khẩu một kiểu bắn (xuyên, nổ, đẩy lùi, tầm).
**Architecture:** `WeaponData` mở rộng + `WeaponRules` (hàm thuần) + `WeaponService` (Meta, `GameServices.Weapons`); trong màn 1 `PlayerWeapon`
nhận `WeaponStats` từ `PlayerLoadout`; `Projectile` thêm xuyên/nổ/đẩy lùi/tầm (phương án A). Mảnh = nguyên liệu `shard_<id>`.
**Spec:** `Docs/specs/2026-10-01-phase8c-weapons-design.md`

## Global Constraints
- Không tự commit (trừ khi người dùng yêu cầu trong tin nhắn); sửa scene/prefab qua Editor, kiểm tra `isDirty`; đổi tên asset bằng `MoveAsset`.
- Scene phải sạch trước `run_tests` (Test Runner hỏi lưu → CLI treo).
- Mọi chi phí qua `EconomyService.TrySpendMany`. Save chỉ lưu id. Không cấp phát trong vòng lặp bắn/trúng đạn (buffer dùng chung, pool).
- Comment tiếng Việt giải thích vì sao.

### Task 1 (TDD): WeaponStats, WeaponRules, WeaponData mở rộng, ItemDatabase.weapons
**Files:** sửa `Weapons/WeaponData.cs`, `Items/ItemDatabase.cs`; tạo `Weapons/WeaponRules.cs` (+ `WeaponStats`, `WeaponStarBonus`); test `WeaponRulesTests.cs`.
**Produces:** `WeaponRules.ShardId(string)`, `ShardsFor(int stars)`, `StarMultiplier(int)`, `LevelCap(int)`, `MaxStars`, `UpgradeGold(int)`,
`UpgradeOre(int)`, `WeaponStats Effective(WeaponData, int level, int stars)`, `int Power(WeaponStats)`; `ItemDatabase.GetWeapon(id)`.
- [ ] Test → code → đạt.

### Task 2 (TDD): LootTable.alsoRoll
- [ ] Test (bảng con được quay; tự tham chiếu không lặp vô hạn) → `LootTable.alsoRoll`, `LootRules.Roll` đệ quy có giới hạn độ sâu → đạt.

### Task 3 (TDD): Save v5 + WeaponService + lực chiến
**Files:** tạo `Meta/OwnedWeapon.cs`, `Meta/WeaponService.cs`; sửa `PlayerProfile` (v5, `weapons`), `SaveMigrations.V4ToV5`, `SaveService.ToProfile`,
`ProfileService.ResetToNew`, `EquipSlotIds.Weapon`, `GameServices.Weapons`, `LoadoutStats.Power`; test `WeaponServiceTests.cs` (+ migration v5).
- [ ] Test → code → đạt (test cũ vẫn đạt).

### Task 4: Trong màn
**Files:** `Combat/Projectile.cs` (Launch mở rộng, xuyên, nổ, đẩy lùi, tầm), `Weapons/PlayerWeapon.cs` (`Equip`, `WeaponStats`, jitter, OverlapCircle mới),
`Player/PlayerLoadout.cs` (gán vũ khí chính, `EquipMainWeapon(id)` cho benchmark), `Core/GameEvents.cs` (`OnExplosion`), `Vfx/VfxSpawner.cs`,
`Audio/AudioManager.cs` + `SoundBank.explosion`, `Stages/StageData.firstClearReward` + `StageSession.FillLoot`, `DevTools/BenchmarkRunner.cs` (`-mainWeapon`).
- [ ] Code → biên dịch 0 lỗi → test đạt.

### Task 5: Nội dung
- [ ] Đổi tên `Weapon_Basic` → `Weapon_Pistol`, `Weapon_Spread` → `Weapon_Shotgun`; số liệu 5 khẩu (spec 4.3); tạo `Weapon_Grenade`, `Weapon_Laser`;
  icon súng + mảnh + sprite đạn lựu/laser (vẽ bằng script); prefab đạn lựu/laser; 5 `Mat_Shard_*`; `ItemDatabase.weapons/materials`;
  `Loot_Elite` + `Loot_WeaponShards` + `alsoRoll` cho boss; `firstClearReward` 3 màn; prefab Player bỏ Súng chùm/Súng máy;
  `UpgradeManager` bỏ 2 nâng cấp + xóa asset; hiệu ứng nổ + âm thanh; `asset-licenses.md`.

### Task 6: UI
- [ ] `WeaponRowView`, `WeaponPanel` (+ prefab, trong EquipmentPanel); ô Vũ khí trong hàng đang mặc; lực chiến dùng `LoadoutStats.Power`;
  menu dev "+20 mảnh mỗi vũ khí".

### Task 7: Kiểm tra, bàn giao
- [ ] Sao lưu save → chơi thử (spec 10.3) → khôi phục save; Missing Script = 0; toàn bộ test; benchmark Phóng lựu + Laser (GC 0 B/frame);
  dọn `Assets/Temp`; CLAUDE.md / plan / asset-licenses; gợi ý commit.
