# Phase 7 — Hồ sơ người chơi, save v2, tiền tệ

Ngày: 2026-10-01
Trạng thái: đã duyệt (phương án A + chính sách chống sửa save)
Liên quan: `Docs/project-plan.md` (Phase 7, Phần B2/B3, D1)

## 1. Mục tiêu

Nền dữ liệu vững cho mọi hệ thống meta: `PlayerProfile` lưu bằng JSON có version, ghi an toàn (không hỏng khi tắt máy đột ngột),
có bản sao lưu, phát hiện sửa tay; `EconomyService` là đường duy nhất thay đổi tiền tệ; kết quả màn được áp vào hồ sơ ngay khi màn kết thúc;
tiến độ màn (sao, mở khóa, thưởng lần đầu/chơi lại); scene `Boot`.

## 2. Quyết định đã chốt

| Câu hỏi | Quyết định |
|---|---|
| JSON | Newtonsoft Json (`com.unity.nuget.newtonsoft-json`, khai báo trực tiếp trong manifest) |
| Áp thưởng | Ngay khi màn kết thúc: Game phát `StageResult`, `StageProgressService` áp + lưu (Game không đụng save) |
| Tiến độ màn | Làm phần dữ liệu: sao cao nhất theo id màn, mở khóa tuần tự, lần thắng đầu ×2, chơi lại ×0.5 |
| Kiến trúc | Dịch vụ là class C# thuần + `GameServices` (static), khởi tạo ở Boot hoặc tự khởi tạo khi Play thẳng scene khác |
| Save bị sửa tay | Khôi phục từ `.bak` nếu hợp lệ; không có thì vẫn tải nhưng đánh dấu `tampered`; **không bao giờ xóa tiến độ** |

## 3. Ngoài phạm vi

Level tài khoản + phần thưởng lên cấp, Hub, màn chọn màn (Phase 9); trang bị/nguyên liệu (Phase 8); cloud save, analytics (Phase 12); IAP (Phase 11).

## 4. Dữ liệu — `PlayerProfile` (`Scripts/Meta/`)

```
saveVersion: int (= 2)
accountXp: long
currencies: Dictionary<string,long>   // CurrencyIds.Gold = "gold", CurrencyIds.Gem = "gem"
stageStars: Dictionary<string,int>    // id màn → số sao cao nhất; có mặt = đã thắng ít nhất 1 lần
tampered: bool                        // phát hiện sửa tay (để analytics Phase 12)
```
Chỉ lưu ID ổn định (id màn, id tiền tệ), không lưu tham chiếu asset hay tên hiển thị.

## 5. Lưu trữ — `SaveService` (`Scripts/Save/`, class thuần, nhận đường dẫn qua constructor để test)

- **Định dạng file** `save.json`: `{ "format": 2, "payload": "<JSON của PlayerProfile>", "hmac": "<base64>" }`.
  `payload` là chuỗi để HMAC tính trên đúng các byte đã ghi.
- **Ghi**: `save.json.tmp` → nếu đã có `save.json` thì `File.Replace(tmp, main, bak)` (bản cũ thành `save.json.bak`), chưa có thì `File.Move`.
- **Đọc** (trả `SaveLoadResult { Profile, Status }`, `Status ∈ New, Loaded, Migrated, RestoredFromBackup, Tampered, Corrupt`):
  1. Không có file → hồ sơ mới (`New`).
  2. File không phải JSON hợp lệ → thử `.bak` (`RestoredFromBackup`); không được → chép file hỏng thành `save.json.corrupt` (để hỗ trợ người chơi), hồ sơ mới (`Corrupt`).
  3. JSON không có `payload` → **save v1** (`{version, bestSurvivalTime, bestKillCount}`) → migration → `Migrated`.
  4. HMAC sai → thử `.bak` hợp lệ (`RestoredFromBackup`); không có → dùng payload nhưng `tampered = true` (`Tampered`).
  5. Hợp lệ → chạy migration theo `saveVersion` trong payload → `Loaded`.
- **Migration** (`SaveMigrations`): chuỗi bước trên `JObject`, mỗi bước `vN → vN+1`. Bước 1→2: bỏ `bestSurvivalTime`/`bestKillCount`
  (chế độ vô tận đã gỡ ở 6A), tạo các trường mặc định.
- **HMAC** (`SaveSecurity`): HMAC-SHA256, khóa nằm trong code. Chỉ chống sửa tay thông thường; khóa trong app nên người quyết tâm vẫn gỡ được
  (ghi rõ trong comment). Chống gian lận thật cho IAP ở Phase 11 (xác minh biên lai).
- Gỡ `SaveData`, `SaveSystem` và `SaveSystemTests` cũ (thay bằng `SaveService` + test mới).

## 6. Dịch vụ

| Class | Thư mục | Vai trò |
|---|---|---|
| `ProfileService` | `Meta/` | Giữ `PlayerProfile`; `Load()`, `Save()`, `MarkDirty()`, `SaveIfDirty()`, `LastLoadStatus` |
| `EconomyService` | `Economy/` | `long GetBalance(id)`, `bool Add(id, amount)` (amount ≤ 0 → false), `bool TrySpend(id, amount)` (không đủ → false, không đổi gì), event `OnCurrencyChanged(id, balance)`; đánh dấu hồ sơ cần lưu |
| `CurrencyIds` | `Economy/` | Hằng số `Gold`, `Gem` |
| `StageProgressService` | `Meta/` | Nghe `StageEvents.OnStageCompleted`; tính thưởng cuối, cộng qua `EconomyService`, cộng XP, ghi sao cao nhất, lưu ngay; `LastGrant`; `IsCleared`, `GetStars`, `IsUnlocked(catalog, stage)`, `NextStage(catalog)` |
| `StageProgressRules` | `Meta/` | Hàm thuần: `RewardMultiplier(victory, alreadyCleared)` = thắng lần đầu 2, thắng lại 0.5, thua 1 (thua đã bị giảm theo tiến độ ở 6A); `ApplyMultiplier`; `IsUnlocked(index, clearedFlags)` |
| `GameServices` | `Core/`? → **`Meta/`** (cần biết các service) | Static: `Profile`, `Economy`, `StageProgress`, `IsInitialized`, `Initialize(savePath = null)` (gọi lại không sao); `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` tự khởi tạo |
| `ServicesHost` | `Meta/` | MonoBehaviour ẩn, `DontDestroyOnLoad`, tạo bởi `GameServices`: lưu khi `OnApplicationPause(true)` và `OnApplicationQuit` |
| `StageEvents` | `Stages/` | Static event `OnStageCompleted(StageResult)` — Stages không phụ thuộc Meta |
| `StageGrant` | `Meta/` | `stageId, gold, accountXp, firstClear, multiplier, starsBefore, starsAfter` |

`StageSession.EndStage`: tạo `Result` → `StageEvents.RaiseStageCompleted(Result)` (dịch vụ áp + lưu) → `GameManager.EndStage` → `OnStageEnded`.
Nhờ thứ tự này, màn kết quả (hiện khi trạng thái đổi) đọc được `StageProgress.LastGrant`.

## 7. Scene Boot

- `Assets/_Project/Scenes/Boot.unity` (build index 0; MainMenu 1, Game 2): camera nền tối + `BootLoader`.
- `BootLoader.Start`: `GameServices.Initialize()` → nạp `SceneLoader.FirstSceneOverride ?? MainMenu`.
- `BenchmarkRunner`: hook đổi sang `BeforeSceneLoad`, đặt `SceneLoader.FirstSceneOverride = Game` khi có `-benchmark` (thay vì tự nạp scene).

## 8. Giao diện

- **Menu chính**: ô số dư vàng (icon đồng vàng + số, đặt lại chỗ ô "Kỷ lục" cũ). Nút "Chơi" ghi `"Chơi {màn}"`, vào **màn chưa thắng đầu tiên** (thắng hết → màn cuối).
- **Màn kết quả**: phần thưởng hiển thị **số thực nhận** từ `LastGrant` (khớp id màn), thêm dòng phụ "Lần thắng đầu: thưởng ×2" / "Chơi lại: thưởng ×0.5"; khi thua không có dòng phụ.

## 9. Kiểm tra

1. Unit test (`SaveServiceTests`, `EconomyServiceTests`, `StageProgressTests`):
   - Save: chưa có file; lưu rồi đọc đủ dữ liệu; lần lưu thứ 2 tạo `.bak` chứa bản trước; file hỏng + `.bak` tốt → khôi phục; file hỏng không `.bak` → hồ sơ mới + có `.corrupt`; sửa tay payload + `.bak` tốt → dùng `.bak`; sửa tay không `.bak` → giữ dữ liệu, `tampered = true`; file v1 → `Migrated`, `saveVersion = 2`; file `.tmp` sót lại không ảnh hưởng.
   - Economy: cộng; trừ đủ; trừ thiếu → false, số dư không đổi; số ≤ 0 → false; sự kiện phát đúng; không bao giờ âm.
   - Progress: thắng lần đầu ×2 + ghi sao; thắng lại ×0.5; sao chỉ tăng không giảm; thua không ghi sao, ×1; mở khóa tuần tự; `NextStage` chọn màn chưa thắng đầu tiên, thắng hết → màn cuối.
2. Toàn bộ test cũ (trừ `SaveSystemTests` đã thay) đạt; biên dịch 0 lỗi; Missing Script = 0.
3. Chơi thử: Boot → menu hiện vàng và "Chơi 1-1" → thắng 1-1 → kết quả "+200 (lần đầu ×2)", file `save.json` có HMAC → về menu: vàng tăng, nút "Chơi 1-2" → chơi lại 1-1 nhận ×0.5. Sửa tay `save.json` → mở lại phát hiện (`Tampered` hoặc khôi phục `.bak`).
4. Benchmark 300 quái: GC 0 B/frame.
