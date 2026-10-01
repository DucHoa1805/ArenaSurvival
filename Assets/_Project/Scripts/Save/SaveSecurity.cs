using System;
using System.Security.Cryptography;
using System.Text;

namespace ArenaSurvival.Save
{
    /// <summary>
    /// Ký nội dung save bằng HMAC-SHA256 để PHÁT HIỆN file bị sửa tay (mở bằng Notepad sửa số vàng).
    /// Giới hạn cần nói thẳng: khóa nằm trong app, người quyết tâm dịch ngược vẫn lấy được — đây chỉ là rào cản thông thường
    /// cho game chơi đơn. Gian lận có giá trị thật (IAP) phải xác minh biên lai ở Phase 11, không dựa vào đây.
    /// </summary>
    public static class SaveSecurity
    {
        // Ghép từ nhiều mảnh để không lộ nguyên khóa khi tìm chuỗi trong file build (chỉ làm khó thêm một chút).
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("as-" + "arena" + "-survival" + "-save-" + "v2-" + "k7Qp2");

        public static string Sign(string payload)
        {
            using (var hmac = new HMACSHA256(Key))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }

        public static bool Verify(string payload, string signature)
        {
            if (payload == null || string.IsNullOrEmpty(signature)) return false;

            byte[] expected = Convert.FromBase64String(Sign(payload));
            byte[] actual;
            try
            {
                actual = Convert.FromBase64String(signature);
            }
            catch (FormatException)
            {
                return false;
            }

            // So sánh hết các byte (không dừng sớm) — thói quen tốt khi so chữ ký, dù ở đây không có máy chủ để đo thời gian.
            if (expected.Length != actual.Length) return false;
            int diff = 0;
            for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ actual[i];
            return diff == 0;
        }
    }
}
