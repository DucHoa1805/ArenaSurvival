using System;

namespace ArenaSurvival.Services
{
    /// <summary>Nguồn giờ UTC cho mọi tính năng theo ngày (quảng cáo, nhiệm vụ, quà đăng nhập) — test thay bằng giờ giả.</summary>
    public interface ITimeSource
    {
        DateTime UtcNow { get; }
    }

    /// <summary>Luật giờ tin được (hàm thuần).</summary>
    public static class ClockRules
    {
        /// <summary>
        /// Giờ máy + độ lệch máy chủ, nhưng không bao giờ lùi về trước mốc lớn nhất đã thấy (lastSeen): chỉnh lùi giờ điện thoại
        /// thì đồng hồ đứng yên ở mốc cũ, không lấy lại được lượt quảng cáo/quà. Chỉnh tiến giờ chỉ chặn được khi có giờ máy chủ (12B).
        /// </summary>
        public static DateTime Trusted(DateTime deviceUtc, long lastSeenTicks, TimeSpan serverOffset)
        {
            DateTime now = deviceUtc + serverOffset;
            return lastSeenTicks > now.Ticks ? new DateTime(lastSeenTicks, DateTimeKind.Utc) : now;
        }
    }

    /// <summary>
    /// Đồng hồ của game. Nhớ mốc lớn nhất đã thấy qua 2 hàm truyền vào (đọc/ghi trường trong hồ sơ) để mốc sống qua các lần mở game.
    /// 12B gọi SetServerOffset sau khi lấy được giờ máy chủ.
    /// </summary>
    public class GameClock : ITimeSource
    {
        private readonly Func<DateTime> _device;
        private readonly Func<long> _readLastSeen;
        private readonly Action<long> _writeLastSeen;
        private TimeSpan _serverOffset;

        public GameClock(Func<long> readLastSeen, Action<long> writeLastSeen, Func<DateTime> device = null)
        {
            _readLastSeen = readLastSeen;
            _writeLastSeen = writeLastSeen;
            _device = device ?? (() => DateTime.UtcNow);
        }

        public bool HasServerTime { get; private set; }

        public void SetServerOffset(TimeSpan offset)
        {
            _serverOffset = offset;
            HasServerTime = true;
        }

        public DateTime UtcNow
        {
            get
            {
                long lastSeen = _readLastSeen();
                DateTime now = ClockRules.Trusted(_device(), lastSeen, _serverOffset);
                if (now.Ticks > lastSeen) _writeLastSeen(now.Ticks);
                return now;
            }
        }
    }
}
