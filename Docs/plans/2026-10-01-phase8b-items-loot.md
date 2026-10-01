# Phase 8B — Trang bị, nguyên liệu, kho đồ, bảng rơi đồ — Kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 2 ô trang bị (Giáp, Phụ kiện), nguyên liệu, kho đồ có nâng cấp/đột phá/phân rã, rơi đồ trong màn + thưởng thắng màn, chỉ số trang bị vào trận.
**Architecture:** dữ liệu SO (`Items/`, `Loot/`) + hàm thuần (`EquipmentRules`, `LootRules`) + `InventoryService` (Meta, trong `GameServices`);
trong màn chỉ gom loot vào `LootDropper` → `StageResult.collected/bonusLoot` → `StageProgressService.Apply` cộng + lưu 1 lần (phương án A).
**Spec:** `Docs/specs/2026-10-01-phase8b-items-loot-design.md`

## Global Constraints
- Không tự commit; sửa scene/prefab qua Editor (`isDirty`, `ForceUpdateCanvases` + lưu lại sau khi sửa UI). Tạo asset qua `eval_file`.
- Mọi thay đổi tiền tệ/nguyên liệu qua `EconomyService`. Save chỉ lưu id; giá trị `Rarity` và khóa ô (`"armor"`, `"accessory"`) không đổi sau khi phát hành.
- Không cấp phát trong vòng lặp màn chơi (pool pickup, cache delegate ở `Awake`, `LootRules.Roll` ghi vào list có sẵn).
- Comment tiếng Việt giải thích vì sao. Test EditMode chạy bằng `run_tests --mode EditMode`.

## Bản đồ file
| File | Trách nhiệm |
|---|---|
| `Scripts/Items/Rarity.cs` | `enum Rarity`, `enum EquipSlot`, `static EquipSlotIds` (Key/TryParse) |
| `Scripts/Items/MaterialData.cs`, `ItemData.cs`, `ItemDatabase.cs` | SO dữ liệu + tra cứu theo id |
| `Scripts/Items/EquipmentRules.cs` | Hàm thuần: chỉ số, trần cấp, giá nâng cấp/đột phá, hoàn trả phân rã |
| `Scripts/Loot/LootDrop.cs`, `LootTable.cs`, `LootRules.cs` | Dữ liệu rơi đồ + quay trọng số |
| `Scripts/Loot/LootDropper.cs`, `LootPickup.cs` | Trong màn: quay bảng khi quái chết, pickup pool, gom đồ đã nhặt |
| `Scripts/Meta/OwnedItem.cs`, `InventoryService.cs`, `LoadoutStats.cs` | Kho đồ (save v4), tổng chỉ số gốc+Talent+trang bị |
| `Scripts/Economy/EconomyService.cs` (+`CurrencyCost`) | `TrySpendMany` |
| `Scripts/UI/EquipmentPanel.cs`, `ItemSlotView.cs`, `ItemDetailPopup.cs`, `LootRowView.cs`, `RarityColors.cs` | Giao diện |
| Sửa: `StatBlock`, `StatCalculator`, `PlayerLoadout`, `TalentPanel`, `PlayerProfile`, `SaveMigrations`, `SaveService`, `ProfileService`, `GameServices`, `CurrencyIds`, `EnemyData`, `EnemyModifiers`, `Enemy`, `IEnemyWorld`, `WaveSpawner`, `StageData`, `StageSession`, `StageResult`, `StageGrant`, `StageProgressService`, `StageResultPanel`, `MainMenu`, `DevMenu` | |

### Task 1 (TDD): Kiểu dữ liệu trang bị + EquipmentRules + StatBlock × float
**Files:** tạo `Items/Rarity.cs`, `MaterialData.cs`, `ItemData.cs`, `ItemDatabase.cs`, `EquipmentRules.cs`; sửa `Stats/StatBlock.cs`; test `Tests/EditMode/EquipmentRulesTests.cs`.
**Produces:**
- `enum Rarity { Common = 0, Rare = 1, Epic = 2, Legendary = 3 }`, `enum EquipSlot { Armor, Accessory }`,
  `EquipSlotIds.Key(EquipSlot) → "armor"|"accessory"`.
- `StatBlock operator *(StatBlock, float)`.
- `ItemData { string id, displayName; Sprite icon; EquipSlot slot; StatBlock baseStats, statsPerLevel; MaterialData upgradeMaterial; }`.
- `MaterialData { string id, displayName; Sprite icon; }`.
- `ItemDatabase { ItemData[] items; MaterialData[] materials; ItemData GetItem(string); MaterialData GetMaterial(string); }`.
- `EquipmentRules`: `StatMultiplier(Rarity)`, `RarityLevelCap(Rarity)`, `CostMultiplier(Rarity)`, `DismantleBase(Rarity)`,
  `Stats(StatBlock base, StatBlock perLevel, Rarity, int level)`, `LevelCap(Rarity, int ascension)`, `CanAscend(Rarity, int level, int ascension)`,
  `UpgradeGold(Rarity, int level)`, `UpgradeMaterial(int level)`, `AscendCores(int ascension)`, `AscendGold(int ascension)`,
  `Refund DismantleRefund(Rarity, int level, int ascension)` với `struct Refund { long gold; long material; long cores; }`, `InventoryCapacity = 100`.
- [ ] Test (chỉ số Hiếm cấp 3; trần 10/20/30/40 + theo đột phá; CanAscend; giá cấp 1/cấp 5 Sử thi; nguyên liệu `1+floor(l/2)`; hoàn trả 70% làm tròn xuống) → lỗi biên dịch → code → đạt.

### Task 2 (TDD): LootTable + LootRules
**Files:** tạo `Loot/LootDrop.cs`, `LootTable.cs`, `LootRules.cs`; test `LootRulesTests.cs`.
**Produces:** `enum LootKind { Material, Item }`; `struct LootDrop { LootKind kind; string id; int amount; Rarity rarity; }`;
`LootEntry { MaterialData material; ItemData item; int min = 1, max = 1; float weight = 1; }`;
`LootTable { LootEntry[] guaranteed; int rolls = 1; LootEntry[] weighted; float nothingWeight; float[] rarityWeights = {1,0,0,0}; }`;
`LootRules.PickWeighted(float[] weights, int count, float r01) → int`; `LootRules.Roll(LootTable, Func<float> random01, List<LootDrop> output)`.
- [ ] Test (guaranteed luôn có; min–max; "không gì"; 10.000 lần: tỉ lệ mục và độ hiếm ±2%; không ghi gì khi bảng null) → code → đạt.

### Task 3 (TDD): Save v4 + TrySpendMany + InventoryService + LoadoutStats
**Files:** tạo `Meta/OwnedItem.cs`, `InventoryService.cs`, `LoadoutStats.cs`, `Economy/CurrencyCost.cs`; sửa `PlayerProfile` (v4), `SaveMigrations` (V3ToV4),
`SaveService.ToProfile`, `ProfileService.ResetToNew`, `EconomyService`, `CurrencyIds`, `StatCalculator`, `GameServices`, `PlayerLoadout`, `TalentPanel`;
test `InventoryServiceTests.cs`, thêm vào `EconomyServiceTests`, `SaveServiceTests` (v3→v4).
**Produces:**
- `PlayerProfile.items: List<OwnedItem>`, `equipped: Dictionary<string,string>`, `nextItemUid: long`; `OwnedItem { uid, id, rarity, level = 1, ascension }`.
- `EconomyService.TrySpendMany(IReadOnlyList<CurrencyCost>) → bool`; `struct CurrencyCost { string id; long amount; }`.
- `InventoryService(PlayerProfile, EconomyService, ItemDatabase, Action save)`: `Items`, `Get(uid)`, `GetEquipped(EquipSlot)`, `IsEquipped(uid)`,
  `IsFull`, `EquippedStats()`, `ItemStats(OwnedItem)`, `Add(string itemId, Rarity) → OwnedItem` (null + tự phân rã khi đầy; không lưu),
  `Equip(uid)`, `Unequip(EquipSlot)`, `TryUpgrade(uid)`, `TryAscend(uid)`, `TryDismantle(uid)`, `DismantleQuick() → int`,
  `GetUpgradeCost(OwnedItem, List<CurrencyCost>)`, `GetDismantleRefund(OwnedItem, List<CurrencyCost>)`,
  events `OnInventoryChanged`, `OnEquipmentChanged`.
- `StatCalculator.Compute(heroBase, catalog, levelOf, StatBlock equipment)`; `LoadoutStats.Compute(HeroBaseStats, TalentCatalog) → StatBlock`.
- `GameServices.Inventory` (nạp `Resources.Load<ItemDatabase>("ItemDatabase")`, thiếu thì database rỗng + cảnh báo).
- [ ] Test → lỗi → code → đạt (toàn bộ test cũ vẫn đạt).

### Task 4 (TDD): Áp loot cuối màn
**Files:** sửa `Stages/StageResult.cs` (`collected`, `bonusLoot`), `Meta/StageGrant.cs` (`materials`, `newItemUids`, `lostItems`, `autoDismantled`),
`Meta/StageProgressService.cs` (nhận `InventoryService`), `GameServices`; test thêm vào `StageProgressTests.cs`.
- [ ] Test (thắng: nguyên liệu + trang bị + bonus, lưu 1 lần; thua: nguyên liệu cộng, trang bị vào `lostItems`; kho đầy → `autoDismantled`) → code → đạt.

### Task 5: Rơi đồ trong màn
**Files:** tạo `Loot/LootDropper.cs`, `Loot/LootPickup.cs`, prefab `Prefabs/LootPickup.prefab`; sửa `EnemyData` (`loot`, `lootChance`), `EnemyModifiers` (`loot`, `lootChance`, `hasLootOverride`),
`EliteGroup.loot`, `StageData` (`bossLoot`, `victoryLoot`), `IEnemyWorld.NotifyKilled(Enemy)`, `Enemy.HandleDied`, `WaveSpawner` (event `OnEnemyKilled(Enemy)`),
`StageSession` (mods tinh anh/boss, `LootDropper.CollectAll()` + quay `victoryLoot` khi thắng, gắn `collected`/`bonusLoot`); scene `Game.unity` thêm `LootDropper`.
- [ ] Code → biên dịch 0 lỗi → test cũ đạt.

### Task 6: Nội dung (asset + icon)
- [ ] Icon vẽ bằng script (áo da, giáp sắt, nhẫn, bùa, quặng, da, lõi boss, vòng độ hiếm); 3 `MaterialData`, 4 `ItemData`, `Resources/ItemDatabase`,
  5 `LootTable` (spec mục 9); gán `loot/lootChance` cho 4 `EnemyData`, `loot` cho `EliteGroup` 3 màn, `bossLoot` màn 1-2/1-3, `victoryLoot` 3 màn.
  Không đụng phần khác của `Enemy_Tank.asset` (chỉ ghi 2 trường loot; báo người dùng khi đưa vào commit). Ghi `asset-licenses.md`.

### Task 7: UI
- [ ] `RarityColors`, `ItemSlotView`, `ItemDetailPopup`, `EquipmentPanel` + prefab; nút "Trang bị" ở menu chính (hàng phụ 3 nút);
  `LootRowView` + hàng "Vật phẩm nhận được" trong `StageResultPanel`; menu dev: "+50 mỗi nguyên liệu", "Nhận 1 trang bị ngẫu nhiên".

### Task 8: Kiểm tra, bàn giao
- [ ] Chơi thử theo spec 11.3 (sao lưu save trước), Missing Script = 0, toàn bộ test, benchmark 300 quái (GC 0 B/frame),
  dọn `Assets/Temp`, cập nhật CLAUDE.md / plan / asset-licenses, gợi ý commit.
