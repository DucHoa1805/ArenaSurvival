# Kế hoạch dự án Unity: Arena Survival (định hướng thương mại)

> **Mục tiêu:** phát hành một game 2D hành động thương mại trên mobile (Android trước), kiếm tiền từ **bán vật phẩm trong game (IAP)** và **quảng cáo có thưởng**.
> **Hiện trạng:** phần gameplay cốt lõi (Phase 0–5 của kế hoạch cũ) đã xong phần lớn và được giữ lại làm nền.
> **Thời lượng dự kiến tới bản thử nghiệm (soft launch):** khoảng **8–12 tháng** nếu làm 10–15 giờ/tuần; nhanh hơn đáng kể nếu làm toàn thời gian. Phạm vi lớn hơn bản cũ nhiều lần, nên kỷ luật cắt phạm vi (Phần A6) là sống còn.

---

## PHẦN A. PHÂN TÍCH TỔNG QUAN

### A1. Bài toán và mục tiêu

| Câu hỏi | Trả lời |
|---|---|
| Làm để làm gì? | Một sản phẩm thương mại có doanh thu, đồng thời là sản phẩm portfolio chất lượng cao |
| Doanh thu đến từ đâu? | Bán kim cương và các gói vật phẩm (IAP), quảng cáo có thưởng, gói xóa quảng cáo |
| Tham khảo thể loại | Survivor.io, Archero, Vampire Survivors: hành động tự bắn + tiến trình trang bị ngoài màn |
| Người chơi giữ lại vì sao? | Luôn có mục tiêu gần: màn tiếp theo, món trang bị sắp nâng được, mảnh vũ khí sắp đủ |
| Rủi ro lớn nhất? | Phạm vi quá lớn cho một người; kinh tế game mất cân bằng; làm hệ thống kiếm tiền trước khi gameplay đủ vui |
| Nguyên tắc chủ đạo | **Gameplay vui trước, kiếm tiền sau.** Ra mắt nhỏ, đo số liệu, rồi mới mở rộng |

### A2. Ý tưởng game và hai vòng lặp

**2D Top-down Action Survival, chia chương và màn.** Người chơi vào từng màn, sống sót qua các đợt quái, hạ boss, nhận thưởng, rồi về giao diện chính để nâng cấp nhân vật và mở màn khó hơn.

Game có **hai vòng lặp lồng nhau**. Đây là điểm khác biệt quan trọng nhất so với bản kế hoạch cũ.

**Vòng lặp trong màn (giữ nguyên core đã làm):** mọi thứ trong vòng này **reset khi kết thúc màn**.

```
Di chuyển/né  →  Vũ khí tự bắn  →  Hạ quái  →  Nhặt XP trong màn
      ↑                                              ↓
  Mạnh dần trong màn  ←  Chọn kỹ năng  ←  Lên cấp trong màn
                                ↓
                  Hết các đợt quái  →  Đánh boss  →  Thắng màn
```

**Vòng lặp ngoài màn (meta, mới):** mọi thứ trong vòng này **được lưu vĩnh viễn**.

```
Chọn màn  →  Chơi màn  →  Nhận thưởng (vàng, nguyên liệu, mảnh vũ khí, XP tài khoản)
   ↑                                                    ↓
Mở màn khó hơn  ←  Lực chiến tăng  ←  Nâng cấp vũ khí / giáp / chỉ số gốc
```

> Quyết định thiết kế: giữ cơ chế lên cấp và chọn kỹ năng **trong màn** để mỗi lượt chơi có biến hóa, còn sức mạnh **lâu dài** đến từ trang bị và chỉ số ngoài màn. Nếu muốn đơn giản hơn, có thể bỏ lên cấp trong màn, nhưng game sẽ kém đa dạng giữa các lượt chơi.

### A3. Cấu trúc màn chơi

| Thành phần | Thiết kế |
|---|---|
| Chương | Mỗi chương một bối cảnh/bản đồ, 10 màn, quái và boss riêng |
| Màn thường | 3–5 đợt quái, kết thúc bằng mini-boss hoặc đợt quái tinh anh |
| Màn 5 và màn 10 | Màn 5 có mini-boss; màn 10 là **boss chương**, rơi lõi boss và mảnh vũ khí hiếm |
| Điều kiện thắng | Sống sót hết các đợt và hạ boss |
| Điều kiện thua | HP về 0 (có thể hồi sinh 1 lần bằng kim cương hoặc xem quảng cáo) |
| Độ khó tăng dần | Máu, sát thương, mật độ quái tăng theo số màn; quái mới xuất hiện theo chương |
| Lực chiến đề xuất | Mỗi màn hiển thị lực chiến đề xuất để người chơi biết khi nào cần nâng cấp |
| Sao đánh giá | 1 sao: thắng; 2 sao: còn trên 50% HP; 3 sao: thắng dưới thời gian quy định |
| Chơi lại (farm) | Lần thắng đầu thưởng nhiều; chơi lại thưởng ít hơn nhưng vẫn rơi nguyên liệu |
| Quét nhanh | Màn đạt 3 sao có thể "quét" nhận thưởng ngay, không cần chơi lại |

### A4. Hệ thống tiến trình ngoài màn

**Level tài khoản (hiển thị ở giao diện chính)**
- Nhận XP tài khoản mỗi khi hoàn thành màn (lần đầu nhiều hơn).
- Lên cấp mở khóa tính năng theo mốc, ví dụ: cấp 3 mở cửa hàng, cấp 5 mở ô trang bị thứ ba, cấp 10 mở chế độ khó.
- Mỗi lần lên cấp nhận thưởng (vàng, kim cương nhỏ, nguyên liệu).

**Trang bị**
- Bản ra mắt có **3 ô**: Vũ khí, Giáp, Phụ kiện. Sau có thể mở rộng thêm Mũ, Giày.
- Độ hiếm: Thường → Hiếm → Sử thi → Huyền thoại (mỗi bậc có chỉ số gốc và giới hạn cấp cao hơn).
- **Nâng cấp:** tốn vàng + nguyên liệu, tăng chỉ số theo từng cấp.
- **Đột phá:** đến mốc cấp (ví dụ cấp 10, 20) cần lõi boss mới nâng tiếp.

**Mảnh vũ khí**
- Gom đủ số mảnh để **mở khóa** vũ khí mới; thêm mảnh để **tăng sao** (tăng chỉ số và mở hiệu ứng đặc biệt).
- Nguồn chính: boss, rương, cửa hàng.

**Chỉ số gốc (Talent)**
- Nâng vĩnh viễn: Máu tối đa, Giáp, Sát thương, Tốc độ chạy, Hồi máu.
- Tốn vàng, giá tăng dần theo cấp. Đây là "vòi xả vàng" chính của nền kinh tế.

**Công thức chỉ số cuối cùng**

```
Chỉ số trong màn = (Chỉ số gốc nhân vật + Talent + Trang bị) × Hệ số sao vũ khí + Buff kỹ năng trong màn
Lực chiến       = tổng có trọng số của Máu, Giáp, Sát thương, Tốc độ bắn
```

**Bảng tài nguyên**

| Tài nguyên | Nguồn | Dùng để |
|---|---|---|
| Vàng | Mọi màn, quét nhanh, nhiệm vụ, quảng cáo | Nâng trang bị, nâng Talent |
| Kim cương (tiền cao cấp) | Nạp tiền, mốc level tài khoản, nhiệm vụ, sự kiện | Mua rương, hồi sinh, mua gói, tăng lượt quét |
| Quặng | Màn thường (rơi nhiều ở chương sau) | Nâng cấp vũ khí |
| Vải/da | Màn thường | Nâng cấp giáp, phụ kiện |
| Lõi boss | Màn boss | Đột phá trang bị |
| Mảnh vũ khí | Boss, rương, cửa hàng | Mở khóa và tăng sao vũ khí |
| XP tài khoản | Hoàn thành màn | Lên level tài khoản |

### A5. Kiếm tiền (bán vật phẩm)

**Hai loại tiền tệ:** Vàng (kiếm được khi chơi) và Kim cương (chủ yếu từ nạp tiền, có rải nhỏ giọt cho người không nạp).

| Sản phẩm | Mô tả | Giai đoạn |
|---|---|---|
| Gói kim cương | Nhiều mệnh giá, lần nạp đầu nhân đôi | Ra mắt |
| Gói khởi đầu | Giá thấp, giá trị cao, mua một lần | Ra mắt |
| Gói xóa quảng cáo | Bỏ quảng cáo bắt buộc (nếu có), giữ quảng cáo có thưởng dạng tự chọn | Ra mắt |
| Gói theo mốc | Mở khi đạt level hoặc qua boss chương, có thời hạn | Ra mắt |
| Thẻ tháng | Nhận kim cương mỗi ngày trong 30 ngày | Sau ra mắt |
| Battle pass | Nhiệm vụ theo mùa, nhánh miễn phí và nhánh trả phí | Sau ra mắt |

**Quảng cáo có thưởng (người chơi tự chọn xem):** nhân đôi thưởng sau màn, hồi sinh, rương miễn phí mỗi ngày, thêm lượt quét nhanh.

**Thể lực/năng lượng (quyết định sau):** giới hạn số lượt chơi mỗi ngày giúp kiếm tiền nhưng dễ làm người chơi bỏ game. Ra mắt **không có thể lực**, chỉ thêm nếu số liệu cho thấy cần.

**Nguyên tắc kiếm tiền**
- Người không nạp vẫn qua được toàn bộ màn bằng cách chơi lại và nâng cấp; tiền chỉ giúp **nhanh hơn**.
- Không bán thẳng "thắng ngay"; không ép xem quảng cáo giữa trận.
- Vật phẩm ngẫu nhiên (rương) phải **công bố tỷ lệ** rõ ràng. Google Play và App Store đều yêu cầu điều này với vật phẩm ngẫu nhiên mua bằng tiền thật.
- Mọi giao dịch IAP phải được **xác minh biên lai** và **khôi phục được** khi người chơi đổi máy.

### A6. Phạm vi bản ra mắt (MoSCoW)

| Mức | Nội dung |
|---|---|
| **Must have** | 3 chương × 10 màn, 3 boss chương + mini-boss, level tài khoản, 3 ô trang bị, 4 vũ khí có mảnh và sao, nâng cấp + đột phá, Talent (máu/giáp/sát thương), giao diện chính, màn chọn màn, màn kết quả, lưu dữ liệu local có version, cửa hàng cơ bản + IAP, quảng cáo có thưởng, analytics, joystick ảo, hồi sinh |
| **Should have** | Nhiệm vụ hằng ngày, quà đăng nhập 7 ngày, rương có tỷ lệ công khai, cloud save, remote config, quét nhanh, thông báo đẩy |
| **Could have** | Thẻ tháng, battle pass, sự kiện giới hạn thời gian, chế độ vô tận, bảng xếp hạng, bản iOS |
| **Won't have** (lần này) | Multiplayer, PvP, bang hội, chat, trao đổi vật phẩm giữa người chơi |

> Quy tắc: không làm mục "Should" khi "Must" chưa chạy ổn. Không làm hệ thống kiếm tiền khi vòng lặp ngoài màn chưa vui.

### A7. Công nghệ sử dụng

| Hạng mục | Lựa chọn | Ghi chú |
|---|---|---|
| Engine | Unity 6.3 LTS | Đang dùng |
| Nền tảng | Android trước, iOS sau | Android rẻ hơn để ra mắt thử (tài khoản Google Play trả phí một lần) |
| Input | Input System + joystick ảo | Bắt buộc cho mobile |
| IAP | Unity In-App Purchasing | Hỗ trợ Google Play và App Store |
| Quảng cáo | Unity LevelPlay hoặc Google AdMob | Chọn một, chỉ dùng quảng cáo có thưởng lúc đầu |
| Backend nhẹ | Unity Gaming Services (Authentication, Cloud Save, Remote Config, Analytics) hoặc Firebase | Chọn một hệ sinh thái để đỡ phức tạp |
| Cân bằng kinh tế | Google Sheets | Bảng công thức, xuất ra ScriptableObject |
| Quản lý asset | Addressables (giai đoạn sau) | Khi nội dung nhiều, cần giảm dung lượng tải |
| Version control | Git + GitHub | Đang dùng |
| Art / Audio | Asset miễn phí hoặc mua bản quyền thương mại | **Kiểm tra giấy phép cho phép dùng thương mại** |

---

## PHẦN B. KIẾN TRÚC TỔNG THỂ

### B1. Scene và luồng chơi

```
Boot (khởi tạo dịch vụ, tải save, đăng nhập)
  ↓
Hub (giao diện chính: level tài khoản, trang bị, Talent, cửa hàng, chọn màn)
  ↓  chọn màn → truyền StageData + chỉ số đã tính
Game (chơi màn: đợt quái → boss)
  ↓  kết thúc → tạo StageResult (thắng/thua, sao, phần thưởng)
Hub (áp phần thưởng vào hồ sơ → lưu → hiển thị kết quả)
```

### B2. Các hệ thống chính

```
┌──────────────────────── Dịch vụ tồn tại suốt game (tạo ở Boot) ───────────────────────┐
│ SaveService   ProfileService   EconomyService   InventoryService   ProgressionService │
│ IAPService    AdsService       AnalyticsService RemoteConfigService                    │
└─────────────┬─────────────────────────────────────────────────────────┬───────────────┘
              │                                                         │
      ┌───────▼────────┐                                       ┌────────▼────────┐
      │   Hub (meta)   │                                       │  Game (trong màn)│
      │ HubUI, Shop UI │   StageData + PlayerLoadout  ───────▶ │ StageSession     │
      │ Inventory UI   │                                       │ WaveSpawner, Boss│
      │ Stage Select   │   ◀───────  StageResult               │ Player, Combat   │
      └────────────────┘                                       │ In-run Upgrades  │
                                                               └─────────────────┘
```

**Nguyên tắc thiết kế**

1. **Tách trạng thái trong màn và trạng thái lưu vĩnh viễn.** Scene Game không ghi trực tiếp vào save; nó chỉ trả về `StageResult`, và Hub chịu trách nhiệm áp thưởng.
2. **Mọi thay đổi tiền tệ đi qua `EconomyService`** (cộng/trừ, kiểm tra đủ tiền, phát sự kiện, ghi analytics). Không hệ thống nào tự sửa số vàng.
3. **Dữ liệu tách khỏi logic** bằng ScriptableObject; số liệu cân bằng có thể ghi đè bằng Remote Config.
4. **Hệ thống giao tiếp qua event**, giữ nguyên nguyên tắc cũ.
5. **Object sinh hàng loạt phải pool**, giữ nguyên nguyên tắc cũ.
6. **Save có số version** để nâng cấp cấu trúc dữ liệu mà không làm mất dữ liệu người chơi.
7. **Save chỉ lưu ID ổn định** (chuỗi `id`), không lưu tham chiếu asset hay tên hiển thị.

### B3. Dữ liệu (ScriptableObject và dữ liệu lưu)

| Loại | Asset / Class | Nội dung chính |
|---|---|---|
| Màn chơi | `ChapterData`, `StageData` | Danh sách màn, đợt quái, boss, lực chiến đề xuất, bảng thưởng, điều kiện 3 sao |
| Quái | `EnemyData`, `BossData` | Chỉ số gốc, hệ số tăng theo màn, các pha tấn công của boss |
| Trang bị | `ItemData` | ID, ô, độ hiếm, chỉ số gốc, tăng theo cấp, số mảnh cần cho mỗi sao |
| Nguyên liệu | `MaterialData` | ID, tên, icon, nguồn rơi |
| Rơi đồ | `LootTable` | Danh sách vật phẩm, trọng số, số lượng min–max |
| Chi phí | `UpgradeCostCurve` | Công thức giá nâng cấp theo cấp |
| Tài khoản | `AccountLevelTable` | XP cần mỗi cấp, phần thưởng, tính năng mở khóa |
| Cửa hàng | `ShopProductData` | ID sản phẩm IAP, nội dung gói, giới hạn mua |
| Dữ liệu lưu | `PlayerProfile` (class thường, serialize JSON) | Level, XP, tiền tệ, kho đồ, đồ đang mặc, tiến độ màn, Talent |

**Đoạn code minh họa**

```csharp
// Dữ liệu trang bị: ID ổn định để save không vỡ khi đổi tên asset
[CreateAssetMenu(menuName = "ArenaSurvival/Item Data")]
public class ItemData : ScriptableObject
{
    public string id;                 // ví dụ "weapon_pistol_01", không bao giờ đổi
    public string displayName;
    public EquipSlot slot;            // Weapon, Armor, Accessory
    public Rarity rarity;
    public Sprite icon;
    public StatBlock baseStats;
    public StatBlock statsPerLevel;
    public int[] shardsPerStar;       // số mảnh cần cho từng sao
}

// Hồ sơ người chơi: chỉ dữ liệu thuần, lưu JSON
[System.Serializable]
public class PlayerProfile
{
    public int saveVersion = 1;
    public int accountLevel = 1;
    public int accountXp;
    public SerializableDictionary<string, long> currencies;   // "gold", "gem"
    public SerializableDictionary<string, int> materials;     // "ore", "leather", "boss_core"
    public List<OwnedItem> items;                             // id, level, star, shards
    public SerializableDictionary<string, string> equipped;   // slot -> instance id
    public SerializableDictionary<string, int> talents;       // "hp", "armor", "damage"
    public SerializableDictionary<string, int> stageStars;    // "c1_s05" -> 3
}

// Tính chỉ số cuối: logic thuần, dễ viết unit test
public static class StatCalculator
{
    public static StatBlock Compute(StatBlock heroBase, PlayerProfile p, ItemDatabase db)
    {
        var total = heroBase + TalentStats.From(p.talents);
        foreach (var item in p.GetEquippedItems(db))
            total += item.Data.baseStats + item.Data.statsPerLevel * (item.level - 1);
        return total;
    }
}
```

> `SerializableDictionary` là kiểu tự viết, vì `JsonUtility` không serialize `Dictionary`. Có thể thay bằng Newtonsoft Json (`com.unity.nuget.newtonsoft-json`).

### B4. Cấu trúc thư mục

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/          # GameManager, GameEvents, ObjectPool, Bootstrap, SceneLoader
│   │   ├── Player/        # PlayerController, PlayerWeapon
│   │   ├── Enemies/       # Enemy, EnemyData, Boss, BossData
│   │   ├── Combat/        # Health, Projectile, DamageInfo
│   │   ├── Stages/        # StageSession, WaveSpawner, ChapterData, StageData, StageResult
│   │   ├── RunUpgrades/   # Lên cấp và chọn kỹ năng trong màn (code cũ của Progression)
│   │   ├── Meta/          # ProfileService, AccountLevel, Talents, StatCalculator
│   │   ├── Inventory/     # ItemData, InventoryService, Equip, Upgrade, Shards
│   │   ├── Economy/       # EconomyService, LootTable, UpgradeCostCurve, Rewards
│   │   ├── Shop/          # ShopService, ShopProductData
│   │   ├── Services/      # IAPService, AdsService, AnalyticsService, RemoteConfigService
│   │   ├── Save/          # SaveService, migration theo version
│   │   ├── UI/            # Hud, Hub, StageSelect, Inventory, Shop, Result, Popup
│   │   └── Audio/         # AudioManager
│   ├── Data/              # Chapters, Stages, Enemies, Bosses, Items, Materials, Loot, Shop
│   ├── Prefabs/
│   ├── Scenes/            # Boot, Hub, Game
│   ├── Art/  Audio/  Animations/
│   └── Tests/             # EditMode tests (kinh tế, chỉ số, rơi đồ, save)
└── Settings/
```

### B5. Quy ước code

- Giữ nguyên quy ước cũ: namespace theo thư mục, `[SerializeField] private`, `PascalCase`/`_camelCase`, không trộn logic gameplay vào UI.
- **ID vật phẩm, tiền tệ, màn chơi là hằng số**, khai báo tập trung, không gõ chuỗi rải rác.
- **Logic kinh tế viết dạng hàm thuần** (nhận đầu vào, trả kết quả) để test được mà không cần chạy Unity.
- Mọi hàm trừ tiền trả về `bool` (thành công/thất bại), không bao giờ để số dư âm.

---

## PHẦN C. LỘ TRÌNH CHI TIẾT

### Tổng quan tiến độ

| Giai đoạn | Thời gian | Nội dung chính | Sản phẩm cuối giai đoạn |
|---|---|---|---|
| Phase 0–5 | Đã xong phần lớn | Core gameplay (kế hoạch cũ) | Chơi được một màn sinh tồn hoàn chỉnh |
| Phase 6 | 3–4 tuần | Màn chơi và boss | Màn có điểm kết thúc, boss, màn kết quả |
| Phase 7 | 2 tuần | Hồ sơ người chơi, lưu trữ v2, tiền tệ | Save có version, EconomyService |
| Phase 8 | 4 tuần | Trang bị, tài nguyên, nâng cấp, Talent | Mặc đồ, nâng cấp, mảnh vũ khí chạy được |
| Phase 9 | 3 tuần | Giao diện chính và level tài khoản | Hub đầy đủ, chọn màn, mở khóa theo level |
| Phase 10 | 4–6 tuần | Nội dung và cân bằng kinh tế | 3 chương × 10 màn, bảng cân bằng |
| Phase 11 | 3–4 tuần | Kiếm tiền: cửa hàng, IAP, quảng cáo | Mua thật được trên bản test nội bộ |
| Phase 12 | 3 tuần | Giữ chân và dịch vụ | Nhiệm vụ ngày, analytics, cloud save, remote config |
| Phase 13 | 3–4 tuần | Mobile, tối ưu, playtest | Bản Android ổn định, đã có người ngoài chơi thử |
| Phase 14 | Liên tục | Soft launch và vận hành | Game lên Google Play ở thị trường thử nghiệm |

---

### PHASE 0–5: Core gameplay (đã xong phần lớn)

**Đã có:** di chuyển, bắn tự động, quái theo đợt, object pooling, ScriptableObject, event system, state machine, XP và lên cấp trong màn, HUD, menu, lưu điểm cao, âm thanh và AudioMixer, VFX, game feel (nháy đỏ, rung camera, số sát thương).

**Cần rà lại để tái sử dụng**
- [x] Hoàn thành các mục còn dở của Phase 5 (slider âm lượng, animation, unit test).
- [x] Chuyển code lên cấp trong màn sang thư mục `RunUpgrades/`, tách rõ khỏi tiến trình ngoài màn.
- [x] `WaveData` cũ trở thành một phần của `StageData`.
- [ ] Main Menu cũ sẽ được thay bằng Hub ở Phase 9; ~~lưu điểm cao được thay bằng hồ sơ người chơi ở Phase 7~~ (xong ở Phase 7).
- [x] Cập nhật `CLAUDE.md` theo định hướng mới để Claude Code nắm được kiến trúc.

---

### PHASE 6: Màn chơi và boss (3–4 tuần)

**Mục tiêu:** một màn chơi có đầu có cuối, thay vì sinh tồn vô hạn.

**Việc cần làm**
- [x] `StageData`: danh sách đợt quái, boss cuối màn, hệ số độ khó, thời gian mục tiêu, bảng thưởng.
- [x] `StageSession`: điều phối màn (đếm đợt → gọi boss → thắng/thua), phát sự kiện bắt đầu/kết thúc.
- [x] `BossData` + `Boss`: thanh máu riêng, 2–3 pha tấn công (bắn tỏa, lao tới, triệu hồi quái), đổi pha theo % máu.
- [x] Mini-boss: dùng lại hệ thống boss với ít pha hơn.
- [x] Hệ số độ khó theo màn: máu, sát thương, mật độ quái nhân theo công thức (Phần D).
- [x] Đánh giá sao (thắng, HP > 50%, dưới thời gian).
- [x] Hồi sinh 1 lần (tạm thời miễn phí, nối với kim cương/quảng cáo ở Phase 11).
- [x] `StageResult` và màn hình kết quả: thắng/thua, sao, danh sách phần thưởng.
- [x] HUD: thanh tiến độ màn, thanh máu boss, cảnh báo boss xuất hiện.

**Lưu ý thiết kế**
- Boss nên dùng **state machine riêng** (mỗi pha một state) để dễ thêm boss mới.
- Thời lượng một màn nên **3–6 phút**, hợp với thói quen chơi trên điện thoại.

**Tiêu chí hoàn thành:** chơi được 3 màn liền nhau với độ khó tăng rõ rệt, màn cuối có boss nhiều pha, màn kết quả hiển thị đúng sao và phần thưởng.

---

### PHASE 7: Hồ sơ người chơi, lưu trữ v2 và tiền tệ (2 tuần)

**Mục tiêu:** có nền dữ liệu vững để mọi hệ thống meta dựa vào.

**Việc cần làm**
- [x] `PlayerProfile` (Phần B3) và `SaveService`: lưu JSON vào `Application.persistentDataPath`, ghi file tạm rồi đổi tên để tránh hỏng file khi tắt máy đột ngột.
- [x] Số version và cơ chế migration (bản save cũ tự nâng lên cấu trúc mới).
- [x] Bản sao lưu: giữ save gần nhất trước đó, tự khôi phục nếu file chính hỏng.
- [x] `EconomyService`: `Add`, `TrySpend`, `GetBalance`, sự kiện `OnCurrencyChanged`.
- [x] Chống sửa save cơ bản: thêm mã kiểm tra (checksum/HMAC) để phát hiện file bị sửa tay.
- [x] Scene `Boot`: tải save, khởi tạo dịch vụ, rồi mới vào Hub.
- [x] Unit test: cộng/trừ tiền, không âm, migration từ version 1 lên 2, đọc file hỏng.

**Lưu ý:** dữ liệu local luôn có thể bị sửa trên máy đã root. Chống gian lận thật sự cần máy chủ, nhưng với game chơi đơn thì mức cơ bản là đủ cho bản ra mắt. Riêng **giao dịch IAP phải được xác minh**, xem Phase 11.

**Tiêu chí hoàn thành:** tắt game ở bất kỳ lúc nào, mở lại không mất dữ liệu; sửa tay file save thì game phát hiện được.

---

### PHASE 8: Trang bị, tài nguyên và nâng cấp (4 tuần)

**Mục tiêu:** sức mạnh nhân vật đến từ trang bị và chỉ số gốc, được mang vào màn chơi.

**Việc cần làm**
- [x] `ItemData`, `MaterialData`, `ItemDatabase` (tra cứu theo ID).
- [x] `InventoryService`: thêm/bớt đồ, mặc/tháo, đếm nguyên liệu.
- [x] Nâng cấp trang bị: tốn vàng + nguyên liệu theo `UpgradeCostCurve`; đột phá ở mốc cấp bằng lõi boss. _(8B: công thức trong `EquipmentRules`)_
- [x] Mảnh vũ khí: mở khóa khi đủ mảnh, tăng sao bằng mảnh thừa. _(8C: `WeaponService`)_
- [x] Talent: Máu, Giáp, Sát thương, Tốc độ chạy, Hồi máu; giá tăng dần.
- [x] `StatCalculator`: tính chỉ số cuối và **lực chiến**.
- [x] Truyền chỉ số vào màn: Player và vũ khí đọc chỉ số đã tính khi bắt đầu màn. _(8A: `PlayerLoadout`; 8B: + trang bị qua `LoadoutStats`)_
- [x] Thêm chỉ số **Giáp** vào hệ thống sát thương (công thức giảm sát thương, Phần D).
- [x] `LootTable`: quái, boss, rương rơi đồ theo trọng số; phần thưởng màn sinh từ bảng này. _(8B: quái, tinh anh, boss, thắng màn; rương ở Phase 11)_
- [x] Vũ khí khác nhau về **cách bắn**, không chỉ khác chỉ số (súng lục, shotgun, súng máy, phóng lựu...).
- [x] Unit test: công thức chỉ số, chi phí nâng cấp, rơi đồ theo tỷ lệ (chạy 10.000 lần, kiểm tra phân phối).

**Tiêu chí hoàn thành:** nâng vũ khí lên vài cấp thì vào màn bắn mạnh hơn thấy rõ; gom đủ mảnh mở được vũ khí mới; mặc giáp thì nhận ít sát thương hơn.

---

### PHASE 9: Giao diện chính và level tài khoản (3 tuần)

**Mục tiêu:** người chơi có "ngôi nhà" để quay về sau mỗi màn và luôn thấy việc tiếp theo cần làm.

**Việc cần làm**
- [x] **Thanh trên cùng:** avatar, level tài khoản + thanh XP, vàng, kim cương.
- [x] **Thanh dưới:** các tab Cửa hàng, Trang bị, Chiến đấu (giữa, nổi bật), Talent, (sau này) Sự kiện.
- [x] **Tab Chiến đấu:** chương hiện tại, nút vào màn tiếp theo, lực chiến hiện tại so với đề xuất.
- [x] **Màn chọn màn:** _(danh sách nút màn; bản đồ vẽ ở Phase 10)_  bản đồ chương, màn đã qua hiển thị sao, màn chưa mở bị khóa.
- [x] **Tab Trang bị:** _(chưa có nhân vật ở giữa)_  nhân vật ở giữa, các ô trang bị xung quanh, kho đồ bên dưới, bảng chi tiết khi chạm vào món đồ.
- [x] **Tab Talent:** danh sách chỉ số, cấp hiện tại, giá nâng, nút nâng.
- [x] Level tài khoản: nhận XP sau màn, popup lên cấp, phần thưởng, mở khóa tính năng theo `AccountLevelTable`.
- [x] **Chấm đỏ thông báo** khi có thứ nâng cấp được hoặc quà chưa nhận.
- [x] Hoạt ảnh nhận thưởng (tiền bay vào thanh trên cùng) để tạo cảm giác thỏa mãn.
- [x] UI tương thích nhiều tỉ lệ màn hình và vùng tai thỏ (Safe Area). _(Safe Area xong; chưa thử tỉ lệ 19.5:9 trên máy thật)_

**Tiêu chí hoàn thành:** từ Hub vào màn, thắng, quay về Hub thấy phần thưởng được cộng, level tài khoản tăng, nâng cấp được đồ, mở màn tiếp theo, tất cả không lỗi.

---

### PHASE 10: Nội dung và cân bằng kinh tế (4–6 tuần)

**Mục tiêu:** đủ nội dung cho vài ngày chơi đầu và một nền kinh tế không quá dễ, không quá khó.

**Việc cần làm**
- [ ] Bảng cân bằng trên Google Sheets: chỉ số quái theo màn, phần thưởng theo màn, chi phí nâng cấp, lực chiến đề xuất.
- [ ] Công cụ nhập dữ liệu từ bảng (CSV) vào ScriptableObject để không gõ tay hàng trăm asset.
- [ ] Nội dung: 3 chương × 10 màn, 3 boss chương, 2–3 mini-boss, 8–10 loại quái, 4 vũ khí.
- [ ] Mô phỏng tiến trình: người không nạp cần chơi lại bao nhiêu lần để qua mỗi boss chương.
- [ ] Thiết kế **"tường"** có chủ đích: boss chương là điểm người chơi cần nâng cấp, nhưng farm vẫn vượt qua được.
- [ ] Chơi thử toàn bộ 30 màn bằng tài khoản mới (không dùng cheat), ghi lại chỗ quá dễ/quá khó.
- [ ] Menu cheat cho bản dev: cộng tiền, nhảy màn, mở đồ, để test nhanh (tắt hẳn ở bản phát hành).

**Tiêu chí hoàn thành:** một người chơi mới không nạp tiền qua được chương 1 trong ngày đầu, chương 2 trong vài ngày, và luôn thấy rõ cần nâng cấp gì khi bị kẹt.

---

### PHASE 11: Kiếm tiền: cửa hàng, IAP, quảng cáo (3–4 tuần)

**Mục tiêu:** mua bán thật hoạt động an toàn và đúng chính sách cửa hàng.

**Việc cần làm**
- [ ] Tạo tài khoản Google Play Console, tạo ứng dụng, khai báo sản phẩm IAP.
- [ ] `IAPService` dùng Unity In-App Purchasing: khởi tạo, mua, xử lý thành công/thất bại/hủy.
- [ ] **Xác minh biên lai** trước khi trao vật phẩm (tối thiểu xác minh phía client; tốt hơn là qua máy chủ/Cloud Code).
- [ ] **Khôi phục giao dịch** (bắt buộc với gói mua một lần như xóa quảng cáo).
- [ ] Xử lý giao dịch dở dang: mua xong nhưng game tắt trước khi trao đồ thì lần mở sau phải trao bù.
- [ ] Cửa hàng: gói kim cương, gói khởi đầu, gói xóa quảng cáo, mua vàng/rương bằng kim cương.
- [ ] Rương: hiển thị tỷ lệ rơi từng loại vật phẩm.
- [ ] `AdsService` (LevelPlay hoặc AdMob): quảng cáo có thưởng cho x2 thưởng, hồi sinh, rương miễn phí; giới hạn lượt/ngày.
- [ ] Màn hình xin đồng ý quyền riêng tư (consent) theo yêu cầu của nhà cung cấp quảng cáo.
- [ ] Test mua bằng tài khoản tester nội bộ của Google Play.

**Tiêu chí hoàn thành:** mua thật trên bản test nội bộ nhận đúng vật phẩm, gỡ cài đặt rồi cài lại khôi phục được gói xóa quảng cáo, quảng cáo có thưởng trao đúng phần thưởng.

---

### PHASE 12: Giữ chân người chơi và dịch vụ (3 tuần)

**Mục tiêu:** người chơi có lý do quay lại mỗi ngày, và bạn có số liệu để ra quyết định.

**Việc cần làm**
- [ ] Nhiệm vụ hằng ngày (chơi X màn, nâng cấp Y lần, xem 1 quảng cáo) + phần thưởng mốc.
- [ ] Quà đăng nhập 7 ngày.
- [ ] **Analytics**: sự kiện bắt đầu/kết thúc màn, thua ở màn nào, nâng cấp gì, mua gì, xem quảng cáo ở đâu, nguồn và nơi tiêu tiền tệ.
- [ ] **Cloud save** gắn với tài khoản (đăng nhập ẩn danh, sau đó liên kết Google Play Games).
- [ ] **Remote Config**: chỉnh độ khó, phần thưởng, giá gói mà không cần cập nhật app.
- [ ] Thông báo đẩy: rương miễn phí đã sẵn sàng, nhiệm vụ ngày sắp làm mới.
- [ ] Dùng thời gian máy chủ cho tính năng theo ngày để tránh chỉnh giờ điện thoại gian lận.

**Tiêu chí hoàn thành:** xem được biểu đồ người chơi bỏ cuộc ở màn nào; đổi một số liệu trên Remote Config thì game nhận giá trị mới mà không cần build lại.

---

### PHASE 13: Mobile, tối ưu và playtest (3–4 tuần)

**Mục tiêu:** chạy mượt trên điện thoại tầm trung và đã được người ngoài chơi thử.

**Việc cần làm**
- [ ] Joystick ảo, nút chạm đủ lớn, hỗ trợ chơi một tay.
- [ ] Tối ưu: Profiler trên máy thật, sprite atlas, giảm draw call, khóa 60 FPS (có tùy chọn 30 FPS tiết kiệm pin).
- [ ] Kiểm tra trên 3–5 điện thoại khác nhau, đặc biệt máy Android tầm thấp.
- [ ] Giảm dung lượng bản cài; cân nhắc Addressables nếu bản cài quá nặng.
- [ ] Xử lý gián đoạn: có cuộc gọi, chuyển app, mất mạng giữa lúc mua hàng.
- [ ] Hướng dẫn chơi lần đầu (tutorial) ngắn gọn, dạy bằng cách cho chơi.
- [ ] Playtest với 10–20 người ngoài, quan sát họ chơi, ghi lại chỗ bối rối và chỗ bỏ cuộc.
- [ ] Hoàn thiện icon, ảnh chụp màn hình, video giới thiệu cho trang cửa hàng.

**Tiêu chí hoàn thành:** chạy ổn định trên máy tầm thấp không nóng máy, không crash; người chơi mới tự hiểu cách chơi mà không cần giải thích.

---

### PHASE 14: Soft launch và vận hành (liên tục)

**Mục tiêu:** ra mắt ở quy mô nhỏ, đo số liệu, cải thiện trước khi mở rộng.

**Việc cần làm**
- [ ] Chuẩn bị chính sách quyền riêng tư (privacy policy), khai báo Data Safety trên Google Play.
- [ ] Phát hành theo các bước: test nội bộ → test kín → test mở → soft launch ở một vài thị trường.
- [ ] Theo dõi chỉ số chính (Phần D3) hằng tuần.
- [ ] Mỗi bản cập nhật sửa **một vấn đề lớn nhất** theo số liệu (ví dụ tỷ lệ bỏ game ở boss chương 1 quá cao).
- [ ] Thêm nội dung theo nhịp đều (chương mới, vũ khí mới, sự kiện) chỉ sau khi chỉ số giữ chân ổn.

**Tiêu chí hoàn thành:** game có mặt trên Google Play, có người chơi thật, có số liệu giữ chân và doanh thu để quyết định đầu tư tiếp hay điều chỉnh.

---

## PHẦN D. KINH TẾ VÀ CÂN BẰNG

### D1. Công thức khởi điểm (chỉnh dần bằng playtest)

| Mục | Công thức gợi ý |
|---|---|
| Chỉ số quái theo màn | `chỉ số gốc × (1 + 0.12 × (số thứ tự màn − 1))` |
| Phần thưởng vàng theo màn | `vàng gốc × (1 + 0.15 × (số thứ tự màn − 1))`; chơi lại nhận 40–60% |
| Giá nâng trang bị | `giá gốc × 1.15 ^ (cấp − 1)` |
| Giá nâng Talent | `giá gốc × 1.12 ^ cấp` |
| XP cần lên level tài khoản | `100 × level ^ 1.5` |
| Giảm sát thương nhờ giáp | `sát thương × 100 / (100 + giáp)` (giáp không bao giờ chặn 100%) |

### D2. Nguồn vào và vòi xả

Một nền kinh tế lành mạnh cần **nguồn vào** (người chơi nhận tài nguyên) và **vòi xả** (người chơi tiêu tài nguyên) cân bằng nhau.

| Tài nguyên | Nguồn vào | Vòi xả |
|---|---|---|
| Vàng | Màn chơi, quét nhanh, nhiệm vụ, quảng cáo | Talent, nâng trang bị |
| Kim cương | IAP, level tài khoản, nhiệm vụ | Rương, hồi sinh, quét nhanh thêm lượt, gói |
| Nguyên liệu | Màn chơi, rương | Nâng trang bị |
| Mảnh vũ khí | Boss, rương, cửa hàng | Mở khóa, tăng sao |

> Dấu hiệu mất cân bằng: người chơi tích hàng chục nghìn vàng không biết tiêu vào đâu (thiếu vòi xả), hoặc kẹt nhiều ngày ở một boss mà farm không đủ (nguồn vào quá ít).

### D3. Chỉ số cần theo dõi sau ra mắt

| Chỉ số | Ý nghĩa |
|---|---|
| Giữ chân D1 / D7 / D30 | % người chơi quay lại sau 1, 7, 30 ngày; quan trọng nhất, cần ổn trước khi tối ưu doanh thu |
| Phễu màn chơi | % người chơi qua từng màn; tìm màn làm người chơi bỏ cuộc |
| Thời lượng phiên, số phiên/ngày | Game có đủ hấp dẫn để mở lại nhiều lần không |
| Tỷ lệ chuyển đổi | % người chơi từng nạp tiền |
| ARPDAU | Doanh thu trung bình mỗi người chơi hoạt động mỗi ngày |
| Lượt xem quảng cáo/người/ngày | Mức độ người chơi tự nguyện xem quảng cáo có thưởng |

---

## PHẦN E. PHÁT HÀNH VÀ PHÁP LÝ

- **Giấy phép asset:** mọi hình ảnh, âm thanh, font phải cho phép **dùng thương mại**. Lưu file giấy phép của từng bộ asset trong repo.
- **Chính sách cửa hàng:** công bố tỷ lệ vật phẩm ngẫu nhiên, khai báo Data Safety, có privacy policy, đáp ứng yêu cầu Target API level mới nhất của Google Play.
- **Quy định tại Việt Nam:** việc phát hành và thu tiền từ game tại Việt Nam có quy định riêng về cấp phép trò chơi điện tử và thanh toán. Cần **tìm hiểu kỹ văn bản hiện hành hoặc hỏi người có chuyên môn pháp lý** trước khi bật thanh toán cho người chơi Việt Nam. Nhiều nhà phát triển độc lập chọn soft launch ở thị trường nước ngoài trước.
- **Thuế và nhận tiền:** doanh thu từ Google Play/App Store và mạng quảng cáo phát sinh nghĩa vụ thuế; nên hỏi kế toán trước khi có doanh thu đáng kể.
- **Độ tuổi:** khai báo đúng phân loại độ tuổi; nếu nhắm tới trẻ em sẽ có thêm quy định nghiêm ngặt về quảng cáo và dữ liệu.

---

## PHẦN F. QUẢN LÝ RỦI RO

| Rủi ro | Dấu hiệu | Cách xử lý |
|---|---|---|
| Scope creep | Đang làm battle pass khi chưa có đủ 30 màn | Quay lại bảng MoSCoW, dừng thêm mới |
| Gameplay chưa vui | Bản thân chơi thấy chán sau vài màn | Dừng làm meta, quay lại cải thiện cảm giác bắn, đa dạng vũ khí và quái |
| Kinh tế mất cân bằng | Playtest kẹt quá lâu hoặc quá dễ | Cân bằng trên bảng tính, dùng Remote Config để chỉnh nhanh |
| Mất dữ liệu người chơi | Báo lỗi mất đồ sau cập nhật | Save có version, migration, bản sao lưu, cloud save |
| Gian lận và lỗi IAP | Người chơi nhận đồ không trả tiền, hoặc trả tiền không nhận đồ | Xác minh biên lai, xử lý giao dịch dở dang, ghi log giao dịch |
| Bị gỡ khỏi cửa hàng | Vi phạm chính sách quảng cáo/dữ liệu/tỷ lệ rương | Đọc kỹ chính sách Google Play trước Phase 11 |
| Kiếm tiền quá đà | Đánh giá 1 sao chê "pay-to-win" | Giữ nguyên tắc ở A5: tiền giúp nhanh hơn, không bắt buộc |
| Kiệt sức khi làm một mình | Không mở dự án cả tuần | Mục tiêu nhỏ mỗi ngày, mỗi buổi một commit, cho phép nghỉ có kế hoạch |
| Code "God class" | `GameManager` hoặc `HubUI` dài hàng nghìn dòng | Tách theo dịch vụ ở B2, mỗi tab UI một script |

**Mẹo giữ nhịp:** mỗi buổi kết thúc bằng một thứ chạy được và một commit. Mỗi phase kết thúc bằng một tag (`phase-6`, `phase-7`...).

---

## PHẦN G. TÀI NGUYÊN HỌC

- **Unity Documentation:** In-App Purchasing, Unity Gaming Services (Authentication, Cloud Save, Remote Config, Analytics), LevelPlay, Addressables, Mobile optimization.
- **Google Play Console Help:** chính sách nhà phát triển, quy trình test nội bộ/kín/mở, Data Safety.
- **Thiết kế kinh tế game:** Machinations (công cụ mô phỏng kinh tế), các bài phân tích game mobile của Deconstructor of Fun.
- **Tham khảo trải nghiệm:** chơi và ghi chú cách Survivor.io, Archero thiết kế Hub, cửa hàng, nhịp phần thưởng, chỗ đặt quảng cáo.
- **Game Programming Patterns** (Robert Nystrom): State cho boss, Observer cho sự kiện kinh tế, Command cho giao dịch.
- **Asset thương mại:** Unity Asset Store, itch.io (lọc giấy phép thương mại), Kenney.nl (CC0, dùng thương mại được).

---

## PHẦN H. CHECKLIST TRƯỚC SOFT LAUNCH

**Gameplay và nội dung**
- [ ] 3 chương × 10 màn, độ khó tăng dần, có boss chương và mini-boss
- [ ] 4 vũ khí khác nhau về cách bắn, 8+ loại quái
- [ ] Lên cấp và chọn kỹ năng trong màn vẫn hoạt động tốt
- [ ] Người chơi mới không nạp tiền vẫn qua được toàn bộ nội dung bằng cách farm

**Tiến trình ngoài màn**
- [x] Level tài khoản, mở khóa tính năng theo mốc
- [x] Trang bị 3 ô: nâng cấp, đột phá, độ hiếm _(8B: Giáp/Phụ kiện; 8C: ô Vũ khí — cấp + sao thay cho độ hiếm)_
- [x] Mảnh vũ khí: mở khóa và tăng sao
- [x] Talent: máu, giáp, sát thương, tốc độ, hồi máu
- [ ] Hub đầy đủ, chấm đỏ thông báo, màn chọn màn có sao

**Kinh doanh**
- [ ] Cửa hàng, IAP có xác minh và khôi phục giao dịch
- [ ] Quảng cáo có thưởng, màn hình xin đồng ý quyền riêng tư
- [ ] Rương công bố tỷ lệ
- [ ] Analytics ghi đủ sự kiện; Remote Config hoạt động

**Kỹ thuật và chất lượng**
- [ ] Save có version, migration, sao lưu, cloud save
- [ ] Unit test cho kinh tế, chỉ số, rơi đồ, save
- [ ] Chạy ổn trên máy Android tầm thấp, không crash
- [ ] Menu cheat đã tắt hoàn toàn ở bản phát hành

**Phát hành**
- [ ] Giấy phép thương mại cho mọi asset
- [ ] Privacy policy, Data Safety, phân loại độ tuổi
- [ ] Đã tìm hiểu quy định pháp lý tại thị trường phát hành
- [ ] Trang cửa hàng: icon, ảnh chụp, video, mô tả

---

*Gợi ý cách dùng file này: đặt vào repo (ví dụ `Docs/project-plan.md`) và tham chiếu trong `CLAUDE.md` để Claude Code luôn nắm định hướng. Mỗi phase, chép các checkbox vào công cụ quản lý việc, tick dần, và chỉ chuyển phase khi đạt tiêu chí hoàn thành.*
