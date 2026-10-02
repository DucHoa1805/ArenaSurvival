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
        public const int CurrentVersion = 7;

        public int saveVersion = CurrentVersion;
        public long accountXp;
        /// <summary>Tiền tệ VÀ nguyên liệu (8B): id → số lượng.</summary>
        public Dictionary<string, long> currencies = new Dictionary<string, long>();
        /// <summary>id màn → số sao cao nhất đã đạt. Có mặt trong từ điển = đã thắng màn đó ít nhất 1 lần.</summary>
        public Dictionary<string, int> stageStars = new Dictionary<string, int>();
        /// <summary>id Talent → cấp đã nâng (v3, Phase 8A).</summary>
        public Dictionary<string, int> talents = new Dictionary<string, int>();
        /// <summary>Kho trang bị (v4, Phase 8B).</summary>
        public List<OwnedItem> items = new List<OwnedItem>();
        /// <summary>id vũ khí chính → cấp/sao (v5, Phase 8C). Súng lục luôn có (WeaponService.EnsureDefaults).</summary>
        public Dictionary<string, OwnedWeapon> weapons = new Dictionary<string, OwnedWeapon>();
        /// <summary>Khóa ô (EquipSlotIds) → uid món đang mặc (v4); riêng ô "weapon" lưu id vũ khí (v5).</summary>
        public Dictionary<string, string> equipped = new Dictionary<string, string>();
        /// <summary>Bộ đếm sinh uid cho món mới: tăng dần nên không bao giờ trùng, kể cả sau khi phân rã.</summary>
        public long nextItemUid = 1;
        /// <summary>Mã giao dịch IAP đã trao vật phẩm (v6, Phase 11): store giao lại giao dịch cũ thì chỉ xác nhận, không trao lần 2.</summary>
        public List<string> processedTransactions = new List<string>();
        /// <summary>id sản phẩm cửa hàng → số lần đã mua (giới hạn mua, thưởng lần đầu).</summary>
        public Dictionary<string, int> purchaseCounts = new Dictionary<string, int>();
        /// <summary>Đã mua gói Xóa quảng cáo: phần thưởng quảng cáo nhận ngay không cần xem.</summary>
        public bool removeAds;
        /// <summary>Vị trí quảng cáo → số lượt đã xem trong ngày adDay (UTC, yyyyMMdd).</summary>
        public Dictionary<string, int> adViews = new Dictionary<string, int>();
        public int adDay;
        /// <summary>Đồng ý quảng cáo cá nhân hóa: -1 chưa hỏi, 0 không, 1 có. 11B truyền vào SDK quảng cáo.</summary>
        public int consent = -1;
        /// <summary>Nhiệm vụ ngày hôm nay (v7, Phase 12).</summary>
        public DailyMissionState daily = new DailyMissionState();
        /// <summary>Quà đăng nhập 7 ngày (v7).</summary>
        public LoginRewardState login = new LoginRewardState();
        /// <summary>Mốc giờ UTC lớn nhất đã thấy (ticks): GameClock không cho lùi về trước mốc này khi người chơi chỉnh lùi giờ máy.</summary>
        public long lastSeenUtcTicks;
        /// <summary>Phát hiện file save bị sửa tay. Không phạt, chỉ đánh dấu để analytics (Phase 12) thống kê.</summary>
        public bool tampered;
    }

    /// <summary>Nhiệm vụ ngày: tiến độ theo id nhiệm vụ, nhiệm vụ/mốc đã nhận. Sang ngày UTC mới thì xóa hết (MissionService).</summary>
    public class DailyMissionState
    {
        /// <summary>Ngày của dữ liệu này (yyyyMMdd UTC).</summary>
        public int day;
        public Dictionary<string, long> progress = new Dictionary<string, long>();
        public List<string> claimed = new List<string>();
        /// <summary>Chỉ số mốc điểm đã nhận.</summary>
        public List<int> milestones = new List<int>();
    }

    public class LoginRewardState
    {
        /// <summary>Ngày nhận gần nhất (yyyyMMdd UTC) — mỗi ngày 1 lần.</summary>
        public int lastDay;
        /// <summary>Tổng số lần đã nhận; ô hôm nay = count % 7 (bỏ ngày không mất chuỗi).</summary>
        public int count;
    }
}
