# Phase 10D — Bản đồ vô tận, nhân vật mới, hiệu ứng kỹ năng, giảm độ khó

Ngày: 2026-10-02
Trạng thái: đã duyệt (người dùng: "dừng test và làm đi", chọn bản đồ vô tận + tự vẽ nhân vật + hiệu ứng)

## 1. Bản đồ vô tận
- Bỏ tường bao (`Walls` trong `Game.unity`) và sân giữa (bệ đá, viền đá). Nền `InfiniteGround` (trên 1 Grid + 3 Tilemap như cũ):
  chia ô thành chunk 16×16, tô các chunk quanh camera (bán kính 2 chunk), xóa chunk ra xa — tilemap luôn nhỏ.
- Chọn tile theo hàm thuần `GroundRules` (băm ô + nhiễu thấp tần) nên quay lại chỗ cũ vẫn thấy y hệt: nền chính, mảng phụ
  (đường đất/bê tông/cỏ theo nhiễu), vật trang trí rải thưa, bụi cây. Mỗi chương 1 prefab `Ground_Desert/City/Base` (theme inline)
  — `ChapterData.arenaPrefab` + `ArenaLoader` giữ nguyên cách đổi.
- `WaveSpawner`: quái sinh trên vòng quanh Player ngay ngoài màn hình; không kẹp vị trí nữa (`ClampToArena` trả nguyên).
  Quái bị bỏ quá xa (> 1.8 lần nửa đường chéo màn hình) được đưa lại vòng sinh phía trước hướng chạy — người chơi không "bỏ" được quái.
- Bot: không còn mép sân (vùng né tường vô hạn).

## 2. Nhân vật chính
- Sprite tự vẽ bằng code (top-down, hướng phải): mũ giáp có kính sáng, vai giáp viền vàng, áo choàng, súng cầm tay.
- Vòng sáng xoay dưới chân, bóng đổ, bụi khi chạy (pool hạt), lóe nòng khi bắn.

## 3. Hiệu ứng kỹ năng & súng
- Đạn: vệt sáng (TrailRenderer, xóa khi lấy từ pool). Lóe nòng mỗi phát.
- Lưỡi cưa: quầng sáng + vệt; cấp tối đa đổi sang lửa vàng cam ("thức tỉnh", +30% sát thương).
- Vòng điện: xoay + nảy theo nhịp gây sát thương. Lá chắn: lấp lánh, chớp khi chặn đòn. Tên lửa: vệt khói.

## 4. Kỹ năng mạnh mới
| Kỹ năng | Tối đa | Mỗi cấp |
|---|---|---|
| Sét lan | 5 | mỗi 1.6 s đánh quái gần nhất rồi lan sang 2 + cấp con, sát thương 12 + 5/cấp; tia sét LineRenderer |
| Drone hộ vệ | 4 | 1 + (cấp−1)/2… drone bay quanh người tự bắn quái gần nhất (đạn súng lục), cấp cao bắn nhanh hơn |

## 5. Độ khó
`DifficultyGrowthPerStage` 0.09 → 0.075. Không chạy lại bot (người dùng yêu cầu dừng test).

## 6. Kiểm tra
Unit test `GroundRules` (tất định, phân bố hợp lệ), vòng sinh quái ngoài màn hình, kéo quái xa lại gần, công thức Sét lan/Drone.
Chơi thử Editor từng chương (ảnh chụp). Benchmark 300 quái + mọi kỹ năng: GC trung bình ≈ 0.
