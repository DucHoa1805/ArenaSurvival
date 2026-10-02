# Phase 10E — Quái đông hơn, nâng cấp mạnh dần, chiêu vũ khí (mana), 3 vũ khí mới — Thiết kế

Ngày: 2026-10-02. Yêu cầu người dùng: đầu màn ít quái nên chơi lâu; màn càng cao lựa chọn tăng chỉ số càng mạnh; thêm vũ khí, hiệu ứng
mạnh/đẹp hơn; chiêu đặc biệt cho từng vũ khí khi đủ mana. Đã chọn: làm **A+B → C → D**; thẻ **chỉ tăng theo màn** (không độ hiếm);
chiêu kích hoạt bằng **nút + tùy chọn tự dùng**; vũ khí mới **Phun lửa, Tesla, Railgun**.

## A. Mật độ quái

- `StageRules.SpawnDensity(số màn)` = `Tuning.SpawnDensity × (1 + EarlyBoost × max(0, 1 − (n−1)/20))`, EarlyBoost mặc định 0.8:
  màn 1 ×1.8, màn 10 ×1.44, màn 20 ×1.04, chương 3 ×1 (vốn đã dày). Mỗi nhịp sinh `density` con (cộng dồn phần lẻ) thay vì 1 con.
- Vào màn sinh sẵn `Tuning.OpeningPack` con (mặc định 5) trên vòng ngoài màn hình.
- Trần quái sống `Tuning.MaxAlive` (mặc định 220; benchmark 300 quái vẫn ~2 ms) — chỉ chặn sinh theo nhịp, không chặn tinh anh/boss/triệu hồi.
- Khóa Remote Config mới: `spawn_density` (0.2–5), `spawn_early_boost` (0–5), `opening_pack` (0–60), `max_alive` (30–400).

- Máu quái chương 1 giảm (Basic 20 → 12, Fast 10 → 7), Súng lục 1.5 → 2 phát/giây: đông quái nhưng chết nhanh, đã tay.
  Đo đứng yên ở 1-1: hạ 40 quái/46 s (bản cũ 19), mất 15 máu.

## B. Nâng cấp trong màn mạnh dần theo màn

- `SkillRules.StageScale(n)` = `1 + Tuning.UpgradeGrowth × (n − 1)`, mặc định 0.03 → màn 10 ×1.27, màn 20 ×1.57, màn 30 ×1.87.
- Áp cho: Sát thương, Tốc bắn, Tốc chạy, Máu tối đa, Hồi máu (giá trị thẻ × hệ số) và sát thương kỹ năng (Lưỡi cưa, Vòng điện, Tên lửa,
  Sét lan, Drone — `PlayerStats.SkillPower`). Đa nòng/Xuyên/Lá chắn/Nam châm giữ nguyên (đã là số đếm).
- Thẻ hiện đúng số đã nhân: mô tả có `{0}` được điền giá trị; kỹ năng gây sát thương có thêm dòng "Sức mạnh theo màn ×1.42".
- `StageSession` đặt `PlayerStats.StageScale` lúc vào màn. Khóa Remote Config: `upgrade_growth` (0–0.2).

## C. Mana + chiêu vũ khí (đợt sau)

- Thanh mana dưới thanh máu HUD: hồi 4/giây + 1 mỗi quái hạ, đầy 100. Nút chiêu trên HUD (PC: Space), Cài đặt có "Tự dùng chiêu".
- `WeaponData.ultimate` = asset `WeaponUltimate` (lớp con theo kiểu chiêu, như `BossAttack`): Súng lục — Bắn loạn (3 vòng đạn 360°);
  Shotgun — Đạn chùm (đạn nổ khi trúng, 5 s); Súng máy — Cuồng nộ (tốc bắn ×3, 4 s); Phóng lựu — Mưa lựu (12 quả quanh người);
  Laser — Tia hủy diệt (tia lớn quét 360°). Hiệu ứng: lóe màn hình, rung, hạt riêng.

## D. 3 vũ khí mới (đợt sau)

Phun lửa (nón ngắn, đốt cháy theo thời gian; chiêu Bão lửa), Tesla (tia nảy 4 quái; chiêu Lưới điện), Railgun (chậm, xuyên vô hạn,
sát thương cao; chiêu Phát bắn xuyên màn hình). Có cấp/sao/mảnh như 5 khẩu cũ, nguồn mảnh từ bảng rơi chương 2–3 + quà thắng lần đầu.

## Kiểm tra

Unit test hàm thuần (mật độ, cộng dồn phần lẻ, trần quái, hệ số thẻ, mana). Benchmark 300 quái sau khi thêm hiệu ứng mới (GC 0 B/frame).
