using System.Collections.Generic;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Toàn bộ dữ liệu được lưu vĩnh viễn của người chơi. Chỉ chứa dữ liệu thuần và ID ổn định (id màn, id tiền tệ) —
    /// không tham chiếu asset, không tên hiển thị — để đổi tên/di chuyển asset hay dịch game không làm vỡ save.
    /// Đổi cấu trúc thì tăng CurrentVersion và thêm 1 bước trong SaveMigrations.
    /// </summary>
    public class PlayerProfile
    {
        public const int CurrentVersion = 3;

        public int saveVersion = CurrentVersion;
        public long accountXp;
        public Dictionary<string, long> currencies = new Dictionary<string, long>();
        /// <summary>id màn → số sao cao nhất đã đạt. Có mặt trong từ điển = đã thắng màn đó ít nhất 1 lần.</summary>
        public Dictionary<string, int> stageStars = new Dictionary<string, int>();
        /// <summary>id Talent → cấp đã nâng (v3, Phase 8A).</summary>
        public Dictionary<string, int> talents = new Dictionary<string, int>();
        /// <summary>Phát hiện file save bị sửa tay. Không phạt, chỉ đánh dấu để analytics (Phase 12) thống kê.</summary>
        public bool tampered;
    }
}
