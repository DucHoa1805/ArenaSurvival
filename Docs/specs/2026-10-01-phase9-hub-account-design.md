# Phase 9 — Hub (giao diện chính) và level tài khoản

Ngày: 2026-10-01
Trạng thái: **làm theo yêu cầu "làm tiếp rồi báo cáo"** — quyết định thiết kế do Claude chốt theo `Docs/project-plan.md` (Phase 9, A4, D1),
liệt kê ở mục 2 để người dùng duyệt lại sau khi xong.
Liên quan: spec 8A/8B/8C (`TalentPanel`, `EquipmentPanel`, `WeaponPanel` vốn đã làm thành prefab để đặt vào Hub).

## 1. Mục tiêu
Người chơi có "ngôi nhà" để quay về sau mỗi màn và luôn thấy việc tiếp theo: level tài khoản (XP sau màn, popup lên cấp, thưởng,
mở khóa tính năng), Hub có thanh trên/thanh dưới và các tab, tab Chiến đấu có chọn màn + lực chiến đề xuất, chấm đỏ, tiền bay vào
thanh trên, Safe Area. Tiêu chí (plan): Hub → vào màn → thắng → về Hub thấy thưởng cộng, level tăng, nâng cấp được đồ, mở màn tiếp.

## 2. Quyết định đã chốt (Claude tự chọn — cần người dùng duyệt)
| Câu hỏi | Chọn | Vì sao |
|---|---|---|
| Lưu level tài khoản | **Suy ra từ `accountXp` (tổng XP)**, không thêm trường save | Không cần save v6; level không bao giờ lệch với XP |
| XP cần mỗi cấp | `round(100 × level^1.5)` để lên từ `level` → `level+1` (plan D1), tối đa cấp 50 | Đúng plan |
| Thưởng lên cấp | Công thức: vàng `100 × cấp mới`, kim cương 5 (cấp chia hết cho 5: 20) | Không phải nhập tay 50 dòng; cân bằng Phase 10 |
| Save cũ đã có XP | Level hiện tại tính ngay từ XP; **không trả thưởng hồi tố** cho các cấp đã vượt | Đơn giản, không phát vàng bất ngờ |
| Mở khóa tính năng | `AccountLevelTable`: Trang bị cấp 1, Talent cấp 2, Cửa hàng cấp 5 (vẫn "Sắp ra mắt" tới Phase 11), Sự kiện "Sắp ra mắt" | Plan A4 gợi ý cửa hàng cấp 3–5; Talent mở sớm để có vòi xả vàng |
| Hub thay MainMenu | Dựng Hub mới **trong scene `MainMenu.unity`** (đổi tên scene để sau, giữ build index); khung menu cũ (Box) bị gỡ khi Hub chạy được | Plan: MainMenu chỉ giữ tới khi có thứ thay thế |
| Tab | 5 tab: Cửa hàng 🔒, Trang bị, **Chiến đấu** (giữa, mặc định), Talent, Sự kiện 🔒 | Đúng plan |
| Bảng Trang bị/Talent trong tab | Dùng lại prefab có sẵn ở **chế độ nhúng** (`EmbeddedPanel`: ẩn nền tối + nút Đóng, co hộp theo vùng tab) | Không viết lại UI đã kiểm tra ở 8A–8C |
| Chọn màn | Danh sách nút màn của chương trong tab Chiến đấu (sao, khóa, lực chiến đề xuất); bản đồ chương vẽ đẹp để Phase 10 | Đủ chức năng, ít asset |
| Lực chiến đề xuất | Trường mới `StageData.recommendedPower` (1-1: 130, 1-2: 200, 1-3: 280) | Người mới (Súng lục) ≈ 130 |
| Cài đặt / Thoát | Nút bánh răng ở thanh trên mở bảng Cài đặt có sẵn; nút Thoát chuyển vào bảng Cài đặt | Hub mobile không có nút thoát giữa màn hình |
| Chấm đỏ | Talent: có Talent đủ vàng nâng. Trang bị: có vũ khí mở khóa/lên sao được, hoặc ô đồ trống mà kho có món đúng ô | Gợi ý "việc tiếp theo", không spam |
| Tiền bay | Về Hub sau màn: vài đồng xu/kim cương bay từ giữa màn hình vào thanh trên, số đếm tăng dần | Plan Phase 9 |

## 3. Ngoài phạm vi
Cửa hàng, IAP, quảng cáo (Phase 11); sự kiện, nhiệm vụ (Phase 12); bản đồ chương có hình vẽ, chương 2–3, chế độ khó (Phase 10);
avatar/nhân vật hiển thị trong tab Trang bị (dùng sprite Player tĩnh); đổi tên scene MainMenu → Hub.

## 4. Level tài khoản
- `AccountRules` (hàm thuần, `Meta/`): `XpToNext(level)`, `LevelFromXp(totalXp)` → (level, xpVàoCấp, xpCầnCấp), `MaxLevel = 50`,
  `RewardFor(level)` → danh sách `CurrencyCost` (vàng, kim cương).
- `AccountLevelTable` (SO, `Data/Account/AccountLevelTable.asset`, nạp từ `Resources` như ItemDatabase): `FeatureUnlock[] unlocks`
  (`featureId`, `level`, `displayName`). `FeatureIds`: `equipment`, `talents`, `shop`, `events`.
- `AccountService(PlayerProfile, EconomyService, AccountLevelTable, Action save)` → `GameServices.Account`:
  `Level`, `XpInLevel`, `XpForLevel`, `AddXp(long)` (cộng XP, mỗi cấp vượt qua → cộng thưởng qua `EconomyService`, đưa vào hàng đợi
  `PendingLevelUps`; không tự lưu), `IsUnlocked(featureId)`, `UnlockLevel(featureId)`, `TakePendingLevelUp(out LevelUpInfo)`, `OnXpChanged`.
  `LevelUpInfo { int level; List<CurrencyCost> rewards; List<string> unlockedFeatures; }`.
- `StageProgressService.Apply` gọi `AccountService.AddXp` thay cho `_profile.accountXp +=`; `StageGrant.levelBefore/levelAfter`.
- Màn kết quả: dòng "Lên cấp N!" nếu có.

## 5. Hub
- `HubController` (script mới, cùng scene `MainMenu.unity`): điều phối tab, popup lên cấp, tiền bay, cài đặt/thoát. Khi Hub chạy được
  thì gỡ khung menu cũ (Box) và `MainMenu.cs` trong cùng phase — scene chỉ còn Hub.
- `HubTopBar`: avatar + "Cấp n", thanh XP, vàng, kim cương (đếm tăng dần khi đổi), nút cài đặt.
- `HubTabBar` + `HubTab`: 5 nút; tab khóa hiện ổ khóa + "Cấp n"/"Sắp ra mắt", bấm vào hiện thông báo ngắn (`HubToast`); chấm đỏ.
- `BattleTab`: tên chương, danh sách màn (`StageNodeView`: "1-1", 3 sao, khóa, đã chọn), chi tiết màn chọn (tên, lực chiến đề xuất
  so với hiện tại — xanh nếu đủ, đỏ nếu thiếu), nút **CHIẾN ĐẤU** (vào màn đang chọn).
- `EmbeddedPanel`: bật chế độ nhúng cho `TalentPanel`, `EquipmentPanel` (+ `WeaponPanel`, `ItemDetailPopup` vẫn là popup trong vùng tab).
- `LevelUpPopup`: "LÊN CẤP n!", ô thưởng (dùng `ItemSlotView` nhỏ), dòng tính năng mới mở, nút Nhận. Hub hiện lần lượt từ hàng đợi.
- `RewardFly`: pool ~12 icon, bay theo đường cong (thời gian thực), xong thì thanh trên đếm tăng.
- `RedDotRules` (hàm thuần trên dịch vụ): `TalentAvailable`, `EquipmentAvailable`.
- `SafeAreaFitter`: co RectTransform theo `Screen.safeArea` (Hub và HUD trong màn).

## 6. Kiểm tra
1. Unit test: `AccountRules` (XP từng cấp, level từ tổng XP ở biên, trần cấp 50, thưởng), `AccountService` (vượt nhiều cấp 1 lần → thưởng
   đủ, hàng đợi đúng thứ tự, mở khóa tính năng, không thưởng cho XP đã có lúc khởi tạo), `StageProgress` cộng XP qua AccountService,
   `RedDotRules`.
2. Toàn bộ test đạt; Missing Script = 0; scene sạch trước khi chạy test.
3. Chơi thử (sao lưu save trước, khôi phục sau): Hub → tab Chiến đấu chọn 1-1 → thắng → về Hub: tiền bay, popup lên cấp, XP tăng,
   màn 1-2 mở khóa; Talent khóa tới cấp 2; chấm đỏ; tab Trang bị/Talent dùng được; cài đặt + thoát.
4. Benchmark 300 quái không đổi (Hub không ảnh hưởng trong màn; Safe Area trên HUD).
