# Phase 10C (bổ sung) — Kỹ năng trong màn có cấp, vũ khí mạnh theo cấp, hạ độ khó chung

Ngày: 2026-10-02
Trạng thái: đã duyệt (người dùng chọn "làm cả gói")
Liên quan: spec 10C (Chương 3 + bot), `RunUpgrades/`, `Weapons/`

## 1. Vì sao
Bot chơi thử cho thấy quái tăng theo màn (+12%/màn) còn sức mạnh người chơi trong màn gần như đứng yên: bảng lên cấp chỉ có
6 lựa chọn cộng chỉ số, vũ khí chính chỉ +8%/cấp. Chỉnh từng đợt quái là vá triệu chứng. Hướng của thể loại (Survivor.io):
người chơi mạnh vọt lên trong màn nhờ kỹ năng có cấp, kết hợp vũ khí.

## 2. Kỹ năng có cấp
- `UpgradeData` thêm `maxLevel` (0 = không giới hạn, không tính là 1 kỹ năng — dùng cho Hồi máu) và `levelDescriptions[]`
  (mô tả cho từng cấp kế, trống thì dùng `description`). `UpgradeManager` giữ cấp từng kỹ năng trong màn.
- Chỉ đưa ra kỹ năng chưa tối đa; đã có `maxSkills` (8) kỹ năng khác nhau thì chỉ đưa ra kỹ năng đã có. Hàm thuần `SkillRules`.
- Thẻ lên cấp ghi "Tên · Cấp 2 → 3" (kỹ năng mới: "MỚI").

| Kỹ năng | Tối đa | Mỗi cấp |
|---|---|---|
| Sát thương | 5 | +25% |
| Tốc độ bắn | 5 | +20% |
| Máu tối đa | 5 | +20 (hồi luôn phần tăng) |
| Tốc chạy | 5 | +10% |
| Hồi máu | ∞ | +30 máu (không tính vào giới hạn kỹ năng) |
| Đa nòng (mới) | 4 | +1 viên mỗi phát, tỏa quạt 10°/viên |
| Đạn xuyên (mới) | 3 | +1 lần xuyên |
| Lưỡi cưa | 6 | cấp 1 mở 3 lưỡi; mỗi cấp +1 lưỡi (tối đa 8) và +10% sát thương lưỡi |
| Lá chắn (mới) | 5 | chặn trọn 1 đòn, hồi sau 8/7/6/5/4 s; cấp 5 giữ 2 lớp |
| Vòng điện (mới) | 5 | mỗi 0.5 s gây sát thương quái trong bán kính 1.6 + 0.25/cấp, sát thương 4 + 2/cấp (× hệ số sát thương) |
| Tên lửa (mới) | 5 | mỗi 2.5 s bắn 1 + (cấp−1)/2 tên lửa nổ (r 1.2) vào quái gần nhất, sát thương 10 + 4/cấp |
| Nam châm (mới) | 3 | bán kính hút XP ×1.5 / ×2 / ×2.5 |

- Lá chắn: `Health.ShieldLayers` — đòn tới khi còn lớp thì mất 1 lớp, không mất máu (phát `OnShieldBlocked`). Chỉ Player dùng.
- Vòng điện/Tên lửa/Lá chắn là component trên prefab Player, tắt sẵn; kỹ năng bật và đặt cấp. Không cấp phát lúc chơi
  (buffer dùng lại, tên lửa dùng pool đạn riêng).

## 3. Vũ khí chính
- `damagePerLevel` của 5 khẩu = 12% sát thương gốc (trước ~8%).
- Sao 3 thêm 1 viên đạn cho Súng lục, Súng máy, Laser (Shotgun giữ +2, Phóng lựu giữ ở sao 5).

## 4. Độ khó chung
- `StageRules.DifficultyGrowthPerStage` 0.12 → 0.09 (màn 10 ×1.81 thay vì ×2.08; màn 30 ×3.61 thay vì ×4.48).
- Sau đó chỉnh `recommendedPower` bằng mô phỏng + bot chạy 1 lần cả 30 màn.

## 5. Bot
`AutoplayRules.UpgradeRank` thêm kỹ năng mới (Lưỡi cưa, Đa nòng, Vòng điện, Tên lửa, Lá chắn ưu tiên trước chỉ số).

## 6. Kiểm tra
Unit test `SkillRules` (đưa ra/không đưa ra, giới hạn kỹ năng, số lưỡi, thời gian hồi lá chắn, số tên lửa), `Health` lá chắn,
độ khó mới. Chơi thử trong Editor từng kỹ năng. Benchmark 300 quái + mọi kỹ năng: GC 0 B/frame.
