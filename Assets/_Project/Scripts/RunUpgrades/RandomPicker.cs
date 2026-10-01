using System;
using System.Collections.Generic;

namespace ArenaSurvival.RunUpgrades
{
    /// <summary>
    /// Chọn ngẫu nhiên N phần tử KHÔNG trùng nhau từ 1 danh sách. Logic thuần, không phụ thuộc
    /// UnityEngine: hàm random được truyền vào, nên unit test có thể dùng random cố định để kết quả lặp lại được.
    /// </summary>
    public static class RandomPicker
    {
        /// <param name="source">Danh sách gốc — không bị thay đổi.</param>
        /// <param name="count">Số phần tử cần lấy; bị giới hạn bởi số phần tử có trong source.</param>
        /// <param name="results">Nơi nhận kết quả (bị xóa trước khi điền), truyền vào để tái sử dụng, tránh cấp phát mỗi lần.</param>
        /// <param name="buffer">Danh sách tạm để trộn, cũng được tái sử dụng.</param>
        /// <param name="randomRange">Trả về số nguyên ngẫu nhiên trong [min, max) — ví dụ UnityEngine.Random.Range.</param>
        public static void PickDistinct<T>(IReadOnlyList<T> source, int count, List<T> results, List<T> buffer, Func<int, int, int> randomRange)
        {
            results.Clear();
            buffer.Clear();
            for (int i = 0; i < source.Count; i++) buffer.Add(source[i]);

            // Fisher–Yates chỉ trộn đủ số phần tử cần lấy: phần tử đã chọn được đổi chỗ lên đầu
            // nên không thể bị chọn lại.
            int take = Math.Min(Math.Max(count, 0), buffer.Count);
            for (int i = 0; i < take; i++)
            {
                int j = randomRange(i, buffer.Count);
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
                results.Add(buffer[i]);
            }
        }
    }
}
