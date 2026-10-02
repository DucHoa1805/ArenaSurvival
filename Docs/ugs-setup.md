# Bật Unity Gaming Services cho Arena Survival (Phase 12B)

Project đã liên kết sẵn với Unity Cloud (org `duchoa0741`, project `ArenaSurvival`). Code dùng 2 môi trường:
**`production`** cho bản phát hành, **`development`** cho Editor/bản dev (bật bằng DevMenu), để số liệu thử không lẫn vào số liệu thật.

## 1. Môi trường

1. Vào <https://cloud.unity.com> → chọn project **ArenaSurvival**.
2. **Project settings → Environments** → **Add environment** → tên `development` (đã có sẵn `production`).

## 2. Authentication (đăng nhập ẩn danh)

**Products → Player Authentication → Identity providers**: kiểm tra **Anonymous** đang bật (thường bật sẵn).

## 3. Cloud Save

**Products → Cloud Save** → **Get started / Enable**. Không cần tạo gì thêm: game tự ghi 2 khóa `profile` và `profile_meta` cho từng người chơi.

## 4. Remote Config

**Products → Remote Config** → chọn môi trường (`development` để thử, sau đó làm lại cho `production`) → **Add key**.
Thêm/sửa khóa xong phải bấm **Publish** thì game mới nhận. Không thêm khóa nào thì game dùng số mặc định. Khóa sai kiểu hoặc ngoài khoảng sẽ bị bỏ qua.

| Khóa | Kiểu | Mặc định | Khoảng hợp lệ | Ý nghĩa |
|---|---|---|---|---|
| `difficulty_growth` | float | 0.075 | 0 – 1 | Độ khó tăng mỗi màn (+7.5%) |
| `reward_growth` | float | 0.15 | 0 – 2 | Thưởng tăng mỗi màn |
| `revive_gem_cost` | int | 20 | 1 – 1000 | Giá hồi sinh bằng kim cương |
| `ad_limit_double_reward` | int | 5 | 0 – 50 | Lượt QC nhân đôi vàng/ngày |
| `ad_limit_revive` | int | 3 | 0 – 50 | Lượt QC hồi sinh/ngày |
| `ad_limit_free_chest` | int | 2 | 0 – 50 | Lượt QC rương miễn phí/ngày |
| `stage_gold_multiplier` | float | 1 | 0.1 – 10 | Hệ số vàng thưởng màn |
| `mission_reward_multiplier` | float | 1 | 0.1 – 10 | Hệ số thưởng mốc/quà đăng nhập |
| `spawn_density` | float | 1 | 0.2 – 5 | Hệ số mật độ quái mọi màn |
| `spawn_early_boost` | float | 0.8 | 0 – 5 | Thêm mật độ cho chương đầu (giảm dần về 0 ở màn 21) |
| `opening_pack` | int | 5 | 0 – 60 | Số quái sinh sẵn khi vào màn |
| `max_alive` | int | 220 | 30 – 400 | Trần quái sống (chỉ chặn sinh theo nhịp) |
| `upgrade_growth` | float | 0.03 | 0 – 0.2 | Thẻ lên cấp mạnh thêm mỗi màn (+3%) |

## 5. Analytics

1. **Products → Analytics** → bật, đọc và đồng ý phần quyền riêng tư dữ liệu.
2. **Analytics → Event Manager → Create event**: tạo **9 sự kiện tùy chỉnh** bên dưới, mỗi sự kiện thêm đúng các tham số (Add parameter).
   Thiếu sự kiện hoặc tham số thì UGS bỏ qua sự kiện đó. Nếu dashboard báo tên tham số bị trùng với tham số có sẵn, báo lại để đổi tên trong code.

| Sự kiện | Tham số (kiểu) |
|---|---|
| `session_start` | `version` (string), `level` (integer), `stages_cleared` (integer), `tampered` (boolean) |
| `stage_start` | `stage` (string), `stage_number` (integer), `first_try` (boolean), `level` (integer), `weapon` (string) |
| `stage_end` | `stage` (string), `victory` (boolean), `stars` (integer), `wave` (integer), `time` (integer), `kills` (integer), `hp` (float), `revived` (boolean) |
| `currency` | `currency` (string), `delta` (integer), `balance` (integer), `reason` (string) |
| `purchase` | `product` (string), `kind` (string), `source` (string), `gem_cost` (integer) |
| `ad_reward` | `placement` (string), `remove_ads` (boolean) |
| `level_up` | `level` (integer) |
| `mission_claim` | `mission` (string), `points` (integer) |
| `login_claim` | `day` (integer) |

Biểu đồ "người chơi bỏ cuộc ở màn nào" (tiêu chí Phase 12): **Data Explorer** → sự kiện `stage_end`, lọc `victory = false`, chia theo `stage` (và `wave`).

## 6. Kiểm tra kết nối trong Editor

1. Play → **F1** → tích **"Dùng UGS thật"** → dừng Play rồi Play lại.
2. Dòng **UGS** trong F1 phải hiện `Ready · development · <mã người chơi> · lệch giờ …s`. Lỗi sẽ hiện màu đỏ.
3. Đồng ý ở popup quyền riêng tư (analytics chỉ gửi khi đã đồng ý). Chơi 1 màn rồi về Hub.
4. Dashboard (môi trường `development`):
   - **Analytics → Event Browser**: thấy `session_start`, `stage_start`, `stage_end`… (có thể trễ vài phút).
   - **Cloud Save → Player data**: tìm mã người chơi, thấy khóa `profile` và `profile_meta` (game tải lên tối đa 1 lần/phút, hoặc bấm "Tải lên đám mây ngay").
   - **Remote Config**: đổi `revive_gem_cost` thành 25 → F1 "Nạp lại Remote Config" → dòng Remote Config ghi `remote` và nút hồi sinh hiện 25 KC.
5. Thử xong: bỏ tích "Dùng UGS thật" để quay về chế độ cục bộ.

## Lưu ý

- Đăng nhập ẩn danh gắn với lần cài đặt: gỡ game là mất tài khoản. Cloud Save chỉ thật sự chuyển máy được sau khi liên kết
  Google Play Games (cần tài khoản Play Console).
- Editor dùng chung save với bản build: bật UGS trong Editor sẽ đẩy save của bạn lên môi trường `development`.
