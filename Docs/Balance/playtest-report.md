# Báo cáo chơi thử bằng bot (Phase 10C)

Bot `-autoplay` (build Development, save riêng `autoplay-save.json`, tài khoản mới mỗi lượt chạy): ra Hub tiêu hết tiền vào thứ tăng
lực chiến nhiều nhất trên mỗi đồng vàng, vào màn chưa thắng đầu tiên, thua 3 lượt liền thì farm màn đã thắng, kẹt sau 15 lượt.
Trong màn: chấm điểm 16 hướng né quái/đạn/vòng pháo/tia ngắm/mép sân, chọn kỹ năng theo thứ tự cố định, luôn nhận hồi sinh.

**Giới hạn:** bot né theo luật cứng, chỉ cầm súng tầm ≥ 5 (không dùng Shotgun), không "đọc" pattern boss. Số liệu là mốc để so
giữa các màn, không thay được cảm giác chơi của người thật — vẫn cần playtest người trước soft launch.

## Các vòng chạy

| Lượt | Thay đổi trước khi chạy | Kết quả |
|---|---|---|
| v3 | (ban đầu) | Kẹt 1-9 sau ~371 phút; 1-4 mất 9 lượt, 1-7 mất 15 lượt; 11/15 lượt ở 1-9 hết 900 s vì không dọn sạch được sân |
| v4 | Đợt cuối chỉ cần hạ tinh anh, quái thường còn lại nổ; súng chính ×1.3 sát thương; nâng cấp trong màn mạnh hơn (+25%/+20%); hồi sinh 3 s + đẩy r=5 | Qua 1-8, kẹt 1-9 |
| v5 | Healer không hồi cho Healer khác, đứng gần hơn (6 → 5) | Kẹt 1-10 (~156 phút) |
| v6 | Player nặng hơn (mass 1 → 10): quái hết ép được vào góc (đo: kẹt góc mất ~170 máu/s) | 1-6..1-9 thắng ngay lượt đầu, kẹt boss 1-10 |
| v7–v8 | Máu boss −20%; đợt a/b sinh chậm hơn (×1.3 / ×1.15) | Vẫn kẹt 1-10; 9/15 lượt chết ở **đợt đầu** |
| v9 | Đo riêng: Xạ thủ 0 máu/s, 5 Charger ~7 máu/s → Charger chạm 8→6 (C2 9→7, C3 11→8), bỏ Charger khỏi đợt đầu 1-10 | Chương 1 ~46 phút, kẹt boss 1-10 (quái cũ vẫn còn trên sân khi đánh boss) |
| v10 | Boss xuất hiện thì quái thường còn sót nổ hết (giữ tinh anh) | **Chương 1 qua trong ~51 phút**; kẹt 2-3 (đợt đầu ½ là Charger C2) |
| v11–v12 | Pha loãng Charger ở 2-3 | 2-3 sống lâu hơn nhưng vẫn khó |
| **v13** | **Gói kỹ năng có cấp** (Đa nòng, Xuyên, Lưỡi cưa 3→8, Lá chắn, Vòng điện, Tên lửa, Nam châm), vũ khí +12%/cấp, độ khó +12% → +9%/màn | **Thắng cả 30 màn**, 55 lượt, ~199 phút chơi |

## Kết quả v13 (theo màn)
- Chương 1: mọi màn thắng ngay lượt đầu (~40 phút). Lực chiến lúc thắng ≈ 1.4–1.75× đề xuất → chương 1 hơi dễ với bot.
- Chương 2: 2-1..2-8 thắng ngay; 2-9 mất 4 lượt; **2-10 mất 12 lượt** (boss Chỉ Huy Robot) → đã giảm máu boss 1360 → 1150 (chưa chạy lại).
- Chương 3: hầu hết 1 lượt; 3-3 và 3-8 2–3 lượt; boss 3-10 mất 4 lượt (đúng vai "tường" cuối).
- Không còn lần nào "chết trong 3 s sau hồi sinh".

## Việc còn để ý
- Chương 1 dễ hơn mô phỏng (bot dư ~50% lực chiến): có thể nâng `recommendedPower` C1 hoặc nhích độ khó đầu game khi có số liệu người thật.
- 2-10 cần xác nhận lại sau khi giảm máu boss.
