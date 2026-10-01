# Phase 8B — Trang bị, nguyên liệu, kho đồ, bảng rơi đồ

Ngày: 2026-10-01
Trạng thái: đã duyệt thiết kế (phương án A), chờ duyệt spec
Liên quan: `Docs/project-plan.md` (Phase 8, A4 "Trang bị", B3, D1, D2), spec 8A `2026-10-01-phase8a-stats-talents-design.md`

## 1. Mục tiêu
Sức mạnh nhân vật đến từ cả Talent (8A) **và trang bị**. Người chơi nhặt nguyên liệu/trang bị trên sân và nhận thưởng cuối màn,
mặc đồ vào 2 ô (Giáp, Phụ kiện), nâng cấp bằng vàng + da, đột phá bằng lõi boss, phân rã đồ thừa lấy lại nguyên liệu.
Chỉ số trang bị cộng vào `StatCalculator` và vào trận qua `PlayerLoadout` (cửa duy nhất, có từ 8A).

## 2. Quyết định đã chốt
| Câu hỏi | Chọn |
|---|---|
| Kiểu sở hữu | Mỗi lần rơi là 1 món riêng (`uid`), có cấp riêng |
| Độ hiếm | Quay theo từng món khi rơi; 1 `ItemData` có thể ra ở 4 độ hiếm |
| Nguyên liệu | Là "tiền tệ phụ" trong `EconomyService` (cùng sổ cái với vàng/kim cương) |
| Nguồn rơi | Cuối màn + rơi vật lý trong màn (quái thường tỉ lệ nhỏ, tinh anh và boss chắc chắn) |
| Khi thua | Giữ nguyên liệu đã nhặt, **mất trang bị đã nhặt**, không có thưởng thắng màn |
| Đồ thừa | Phân rã → nguyên liệu (+ hoàn một phần chi phí đã nâng); kho tối đa 100 món, đầy thì đồ mới tự phân rã |
| Đưa loot vào save | **Phương án A**: trong màn chỉ gom vào `RunLoot`; `StageResult` mang ra; `StageProgressService.Apply` cộng + lưu 1 lần |

## 3. Ngoài phạm vi
Ô Vũ khí, mảnh/sao vũ khí, cách bắn, dùng quặng để nâng vũ khí (8C — 8B vẫn rơi quặng để tích trữ);
hợp đồ, rương, bán đồ lấy vàng (Phase 10–11); lực chiến đề xuất từng màn (Phase 9–10); Hub (Phase 9);
khóa đồ chống phân rã nhầm (làm khi có Hub nếu cần).

## 4. Dữ liệu (ScriptableObject)
Namespace `ArenaSurvival.Items` (thư mục `Scripts/Items/`), trừ chỗ ghi khác.

- `Rarity` (enum, giá trị lưu vào save nên **không đổi thứ tự**): `Common = 0, Rare = 1, Epic = 2, Legendary = 3`.
- `EquipSlot` (enum): `Armor, Accessory` (8C thêm `Weapon`). Khóa lưu save là chuỗi cố định trong `EquipSlotIds`
  (`"armor"`, `"accessory"`), không dùng `ToString()` của enum, để đổi tên enum không vỡ save.
- `MaterialData` (SO): `id` (trùng id tiền tệ), `displayName`, `icon`. Asset `Data/Items/Materials/Mat_Ore|Leather|BossCore.asset`.
  `CurrencyIds` thêm `Ore = "ore"`, `Leather = "leather"`, `BossCore = "boss_core"`.
- `ItemData` (SO): `id` (cố định), `displayName`, `icon`, `EquipSlot slot`, `StatBlock baseStats`, `StatBlock statsPerLevel`,
  `MaterialData upgradeMaterial` (8B: da cho cả giáp và phụ kiện).
- `ItemDatabase` (SO): `ItemData[] items`, `MaterialData[] materials`; `GetItem(id)`, `GetMaterial(id)` (từ điển dựng lúc đầu, trả null nếu không có).
  Asset **`Resources/ItemDatabase.asset`** — `GameServices` tự khởi tạo không qua scene nên nạp bằng `Resources.Load`.
- `LootTable` (SO, `Scripts/Loot/`, namespace `ArenaSurvival.Loot`):
  - `LootEntry[] guaranteed` — luôn rơi.
  - `int rolls` + `LootEntry[] weighted` + `float nothingWeight` — quay `rolls` lần theo trọng số (có thể ra "không gì").
  - `float[] rarityWeights` (4 phần tử, theo `Rarity`) — dùng khi kết quả là trang bị.
  - `LootEntry`: `MaterialData material` **hoặc** `ItemData item` (đúng 1 cái, kiểm tra trong `OnValidate`), `int min, max`, `float weight`.
- `EnemyData`: thêm `LootTable loot`, `float lootChance` (0–1).
- `EliteGroup`: thêm `LootTable loot` (tinh anh luôn rơi khi có bảng).
- `StageData`: thêm `LootTable bossLoot`, `LootTable victoryLoot`.
- `StatBlock`: thêm toán tử `* float` (nhân mọi trường).

## 5. Save v4
`PlayerProfile.CurrentVersion = 4`; `SaveMigrations.V3ToV4` thêm `items = []`, `equipped = {}`, `nextItemUid = 1`.

```
items:       [{ "uid": "i17", "id": "armor_leather", "rarity": 1, "level": 12, "ascension": 1 }]
equipped:    { "armor": "i17", "accessory": "i20" }
nextItemUid: 21
currencies:  { "gold": 2725, "ore": 34, "leather": 12, "boss_core": 1 }   // nguyên liệu nằm ở đây, không thêm trường
```

- `OwnedItem` (class thuần, `Meta/`): `uid`, `id`, `rarity` (int), `level` (≥1), `ascension` (≥0).
- `uid` = `"i" + nextItemUid++` — không trùng, đọc dễ, không cần GUID.
- `ProfileService.ResetToNew` xóa `items`, `equipped`, đặt `nextItemUid = 1`.
- `SaveService.ToProfile` xử lý `items`/`equipped` null như `talents`.
- Món có `id` không còn trong `ItemDatabase`: bỏ qua khi tính chỉ số/hiển thị, ghi cảnh báo 1 lần, **giữ nguyên trong save**.

## 6. Logic thuần
### 6.1 `EquipmentRules` (`Items/`)
| Hằng số | Thường | Hiếm | Sử thi | Huyền thoại |
|---|---|---|---|---|
| `StatMultiplier` | 1 | 1.5 | 2.2 | 3.2 |
| `RarityLevelCap` | 10 | 20 | 30 | 40 |
| `CostMultiplier` | 1 | 1.2 | 1.5 | 2 |
| `DismantleBase` (da) | 2 | 5 | 12 | 30 |

- `Stats(ItemData, Rarity, level)` = `(baseStats + statsPerLevel × (level − 1)) × StatMultiplier[rarity]`.
- `LevelCap(rarity, ascension)` = `min(RarityLevelCap[rarity], 10 × (ascension + 1))`.
- `CanAscend(rarity, level, ascension)` = `level == LevelCap(...)` và `LevelCap < RarityLevelCap[rarity]`.
- `UpgradeCost(rarity, level)` (giá để từ `level` lên `level + 1`):
  vàng = `floor(100 × 1.15^(level − 1) × CostMultiplier[rarity])`, nguyên liệu = `1 + floor(level / 2)`.
- `AscendCost(ascension)`: lõi boss = `ascension + 1`, vàng = `500 × (ascension + 1)`.
- `DismantleRefund(rarity, level, ascension)` → (vàng, nguyên liệu, lõi boss) =
  `DismantleBase[rarity]` da + `floor(70% × tổng đã chi)` cho từng loại (tổng các `UpgradeCost` từ cấp 1 tới `level − 1`
  và các `AscendCost` từ 0 tới `ascension − 1`).
- `const int InventoryCapacity = 100`.

### 6.2 `LootRules` (`Loot/`)
- `Roll(LootTable, Func<float> random01, List<LootDrop> output)` — ghi vào list có sẵn (không cấp phát trong màn).
  Thứ tự: mọi `guaranteed` → `rolls` lần quay `weighted` (+ `nothingWeight`). Số lượng = số nguyên đều trong `[min, max]`.
  Trang bị: quay độ hiếm theo `rarityWeights`.
- `PickWeighted(float[] weights, float r01)` → chỉ số (dùng chung cho mục và độ hiếm).
- `LootDrop` (struct, dữ liệu thuần): `LootKind kind` (Material/Item), `string id`, `int amount`, `Rarity rarity`.

### 6.3 `StatCalculator`
- `Compute(heroBase, catalog, levelOf, StatBlock equipment)` = kết quả 8A + `equipment`. Giữ overload cũ cho test 8A.
- `LoadoutStats.Compute(HeroBaseStats, TalentCatalog)` (`Meta/`): gốc + Talent + `GameServices.Inventory.EquippedStats()`.
  `PlayerLoadout`, `TalentPanel`, `EquipmentPanel` đều gọi đây để lực chiến luôn khớp.

## 7. Dịch vụ
### 7.1 `EconomyService`
- `TrySpendMany(IReadOnlyList<CurrencyCost>)`: kiểm tra **đủ tất cả** rồi mới trừ; thiếu 1 thứ → false, không đổi gì.
  `CurrencyCost` = struct `{ string id; long amount; }`. Phát `OnCurrencyChanged` cho từng loại đã trừ.

### 7.2 `InventoryService(PlayerProfile, EconomyService, ItemDatabase, Action save)` — `GameServices.Inventory`
- Đọc: `Items` (IReadOnlyList<OwnedItem>), `Get(uid)`, `GetEquipped(EquipSlot)`, `IsEquipped(uid)`, `EquippedStats()`, `IsFull`.
- `Add(itemId, rarity)` → `OwnedItem` hoặc null nếu kho đầy (khi đầy: phân rã ngay, cộng nguyên liệu, trả null). **Không tự lưu** —
  người gọi (StageProgressService) lưu 1 lần.
- Thao tác của người chơi (thành công thì **lưu ngay** + phát sự kiện; thất bại → false, không đổi gì):
  `Equip(uid)` (thay món cũ cùng ô), `Unequip(EquipSlot)`, `TryUpgrade(uid)`, `TryAscend(uid)`,
  `TryDismantle(uid)` (từ chối món đang mặc), `DismantleQuick()` → số món đã phân rã (mọi món Thường, cấp 1, chưa mặc).
- Sự kiện: `OnInventoryChanged`, `OnEquipmentChanged`.

### 7.3 `StageProgressService.Apply(StageResult)`
- Nguyên liệu trong `result.collected` + `result.bonusLoot`: **luôn** cộng qua `EconomyService`.
- Trang bị trong `result.collected`: chỉ cộng khi `victory`; khi thua đưa vào `grant.lostItems`.
- Trang bị trong `result.bonusLoot` (chỉ có khi thắng): cộng.
- Ghi vào `StageGrant`: `Dictionary<string,int> materials`, `List<string> newItemUids`, `List<LootDrop> lostItems`, `int autoDismantled`.
- Vẫn **lưu 1 lần** ở cuối như hiện tại.

## 8. Trong màn
- `EnemyModifiers` thêm `LootTable loot`, `float lootChance`. Mặc định lấy từ `EnemyData`; `EliteGroup` ghi đè (`lootChance = 1`);
  boss lấy `StageData.bossLoot` (`lootChance = 1`). Quái do boss gọi ra dùng bảng của `EnemyData` như quái thường.
- `IEnemyWorld.NotifyKilled(Enemy)` — `Enemy.HandleDied` gọi (không gọi khi `DespawnAll`). `WaveSpawner` phát instance event
  `OnEnemyKilled(Enemy)`. `GameEvents.OnEnemyKilled` giữ nguyên (Core không phụ thuộc Loot).
- `LootDropper` (scene Game, `Loot/`): nghe `WaveSpawner.OnEnemyKilled`, quay `lootChance` → `LootRules.Roll` vào buffer dùng lại →
  sinh `LootPickup` (pool, toả nhẹ quanh chỗ chết). `CollectAll()` cho mọi pickup còn lại bay về Player.
- `LootPickup` (prefab `Prefabs/LootPickup`, layer `Pickup`): icon nguyên liệu/trang bị + vòng màu độ hiếm; hút vào Player
  trong bán kính như XP orb; chạm Player → `RunLoot.Add(drop)`, chữ nổi ("+2 Da", "Nhẫn Sức Mạnh (Hiếm)"), tiếng nhặt
  (trang bị có tiếng riêng). Delegate cache ở `Awake` (bài học GC).
- `RunLoot` (scene Game): `List<LootDrop>` đã nhặt.
- `StageSession` khi thắng: `LootDropper.CollectAll()` → quay `StageData.victoryLoot` (2 lần nếu chưa từng thắng màn, theo
  `GameServices.StageProgress.IsCleared`; 1 lần nếu chơi lại) vào `result.bonusLoot`. Khi thua: pickup còn trên sân bị bỏ.
- `StageResult` thêm `List<LootDrop> collected`, `List<LootDrop> bonusLoot`.
- Quay ngẫu nhiên: trong màn `UnityEngine.Random.value`; test truyền chuỗi số cố định.

## 9. Nội dung khởi điểm (chỉnh ở Phase 10)
**Nguyên liệu**: Quặng (`ore`), Da (`leather`), Lõi boss (`boss_core`).

**Trang bị** (`Data/Items/`):
| id | Tên | Ô | Chỉ số gốc | Mỗi cấp |
|---|---|---|---|---|
| `armor_leather` | Áo Da | Giáp | máu 20, giáp 3 | máu 4, giáp 0.6 |
| `armor_iron` | Giáp Sắt | Giáp | máu 10, giáp 6 | máu 2, giáp 1.2 |
| `acc_ring_power` | Nhẫn Sức Mạnh | Phụ kiện | sát thương 4% | sát thương 0.6% |
| `acc_charm_wind` | Bùa Gió | Phụ kiện | tốc chạy 3%, hồi máu 0.2 | tốc chạy 0.3%, hồi máu 0.05 |

**Bảng rơi đồ** (`Data/Loot/`):
| Bảng | Dùng cho | Luôn rơi | Quay | Độ hiếm (T/H/S/HT) |
|---|---|---|---|---|
| `Loot_Common` | Basic/Fast 2%, Xạ thủ 3%, Tank 4% | — | 1: da ×1 (3), quặng ×1 (1) | — |
| `Loot_Elite` | Mọi `EliteGroup` | da 2–4 | 1: 4 trang bị (mỗi món 3.75), quặng 1–2 (25), không gì (60) | 80/20/0/0 |
| `Loot_MiniBoss` | Màn 1-2 | lõi boss ×1 | 1: 4 trang bị (bằng nhau) | 0/80/20/0 |
| `Loot_Boss` | Màn 1-3 | lõi boss ×2 | 1: 4 trang bị (bằng nhau) | 0/50/40/10 |
| `Loot_StageClear` | Thắng màn 1-1..1-3 | da 3–6 | 1: 4 trang bị (tổng 25), không gì (75) | 70/25/5/0 |

**Icon** (vẽ bằng script như `icon_shield`, ghi vào `Docs/asset-licenses.md`): áo da, giáp sắt, nhẫn, bùa, quặng, da, lõi boss,
khung ô đồ. Màu độ hiếm: Thường xám, Hiếm xanh dương, Sử thi tím, Huyền thoại cam.

## 10. Giao diện (prefab để Phase 9 đặt vào Hub)
- **Menu chính**: hàng phụ thành 3 nút — "Trang bị", "Nâng cấp", "Cài đặt".
- **`EquipmentPanel`** (`Prefabs/UI/EquipmentPanel`): tiêu đề "TRANG BỊ"; 2 ô Giáp/Phụ kiện (bấm → chi tiết món đang mặc);
  lực chiến; thanh tài nguyên (vàng, da, quặng, lõi boss); lưới đồ cuộn (`ScrollRect` + `GridLayoutGroup`), sắp xếp đang mặc →
  độ hiếm giảm → cấp giảm; ô đồ (`ItemSlotView`): icon, viền màu độ hiếm, "Lv n", dấu "đang mặc". Ô đồ **dùng lại**
  (tạo thêm khi thiếu, ẩn khi thừa), không hủy/tạo lại mỗi lần đổi. Nút "Phân rã nhanh". Tự cập nhật theo
  `OnInventoryChanged` / `OnEquipmentChanged` / `OnCurrencyChanged`.
- **`ItemDetailPopup`**: tên + độ hiếm (màu), "Cấp n/trần", chỉ số hiện tại → cấp sau, giá (vàng + da, hoặc lõi boss khi đột phá);
  nút Mặc/Tháo, Nâng cấp (tới mốc thì thành "Đột phá"; tới trần độ hiếm thì "TỐI ĐA"), Phân rã (hỏi xác nhận, ghi rõ nhận lại gì;
  ẩn với món đang mặc). Mờ nút khi không đủ tài nguyên.
- **Màn kết quả** (`StageResultPanel`): hàng "Vật phẩm nhận được" — icon + số lượng nguyên liệu, trang bị có viền độ hiếm;
  khi thua, trang bị đã nhặt hiện mờ + "Mất khi thua"; dòng "n món tự phân rã (kho đầy)" nếu có.
- **Menu dev (F1)**: "+50 mỗi nguyên liệu", "Nhận 1 trang bị ngẫu nhiên".

## 11. Kiểm tra
1. Unit test:
   - `EquipmentRulesTests`: chỉ số theo độ hiếm/cấp, trần cấp + đột phá, giá nâng cấp, `DismantleRefund` (70%, làm tròn xuống).
   - `LootRulesTests`: guaranteed luôn có; phân phối mục và độ hiếm sau 10.000 lần nằm trong ±2% kỳ vọng; min–max đúng; "không gì".
   - `EconomyServiceTests`: `TrySpendMany` đủ/thiếu một thứ (không trừ gì).
   - `InventoryServiceTests`: Add tăng uid, kho đầy tự phân rã, mặc/tháo/thay cùng ô, nâng cấp đủ/thiếu tiền, chặn ở trần,
     đột phá, phân rã món đang mặc bị từ chối, phân rã nhanh, id lạ bị bỏ qua khi tính chỉ số nhưng giữ trong save, sự kiện + lưu.
   - `StageProgressServiceTests`: thắng cộng nguyên liệu + trang bị + bonus; thua cộng nguyên liệu, trang bị vào `lostItems`.
   - Migration v3→v4.
2. Toàn bộ test đạt; biên dịch 0 lỗi; Missing Script = 0.
3. Chơi thử: nhặt đồ trên sân → thắng → màn kết quả hiện đúng → kho có đồ → mặc → lực chiến tăng → vào màn: máu/giáp/sát thương
   tăng đúng; nâng cấp, đột phá (lõi boss từ menu dev), phân rã; thua → mất trang bị, giữ nguyên liệu.
4. Benchmark 300 quái: GC 0 B/frame (pickup dùng pool, `LootRules.Roll` ghi vào list có sẵn).
