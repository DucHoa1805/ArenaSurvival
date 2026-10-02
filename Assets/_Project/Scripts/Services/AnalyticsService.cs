using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ArenaSurvival.Meta;

namespace ArenaSurvival.Services
{
    /// <summary>Nơi nhận sự kiện analytics (UGS Analytics ở 12B; file cục bộ ở 12A).</summary>
    public interface IAnalyticsSink
    {
        /// <summary>Sink phải đọc xong parameters trong lời gọi (từ điển được dùng lại cho sự kiện sau).</summary>
        void Send(string eventName, IReadOnlyDictionary<string, object> parameters);
    }

    /// <summary>Bản phát hành khi chưa có SDK: bỏ qua mọi sự kiện.</summary>
    public class NullAnalyticsSink : IAnalyticsSink
    {
        public void Send(string eventName, IReadOnlyDictionary<string, object> parameters) { }
    }

    /// <summary>
    /// Bản dev: mỗi sự kiện 1 dòng JSON trong analytics-log.jsonl cạnh file save — đọc lại để kiểm tra đủ sự kiện trước khi nối UGS.
    /// File quá 2 MB thì đổi thành .old để không phình mãi.
    /// </summary>
    public class LocalAnalyticsSink : IAnalyticsSink
    {
        private const long MaxBytes = 2 * 1024 * 1024;
        private readonly StringBuilder _sb = new StringBuilder(256);

        public LocalAnalyticsSink(string path) => Path = path;

        public string Path { get; }

        public void Send(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            _sb.Clear();
            _sb.Append("{\"t\":\"").Append(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))
               .Append("\",\"e\":\"").Append(eventName).Append('"');
            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> kv in parameters)
                {
                    _sb.Append(",\"").Append(kv.Key).Append("\":");
                    AppendValue(kv.Value);
                }
            }
            _sb.Append("}\n");
            try
            {
                var info = new FileInfo(Path);
                if (info.Exists && info.Length > MaxBytes)
                {
                    File.Copy(Path, Path + ".old", true);
                    File.Delete(Path);
                }
                File.AppendAllText(Path, _sb.ToString());
            }
            catch (Exception e)
            {
                // Analytics không bao giờ được làm hỏng game: lỗi ghi file chỉ báo log.
                UnityEngine.Debug.LogWarning($"LocalAnalyticsSink: {e.Message}");
            }
        }

        private void AppendValue(object value)
        {
            switch (value)
            {
                case null: _sb.Append("null"); break;
                case bool b: _sb.Append(b ? "true" : "false"); break;
                case string s: _sb.Append(Newtonsoft.Json.JsonConvert.ToString(s)); break;
                case float f: _sb.Append(f.ToString("0.###", CultureInfo.InvariantCulture)); break;
                case double d: _sb.Append(d.ToString("0.###", CultureInfo.InvariantCulture)); break;
                case IFormattable n: _sb.Append(n.ToString(null, CultureInfo.InvariantCulture)); break;
                default: _sb.Append(Newtonsoft.Json.JsonConvert.ToString(value.ToString())); break;
            }
        }
    }

    /// <summary>Tên sự kiện — hằng số để dashboard không bị tách 2 cột vì gõ sai.</summary>
    public static class AnalyticsEvents
    {
        public const string SessionStart = "session_start";
        public const string StageStart = "stage_start";
        public const string StageEnd = "stage_end";
        public const string Currency = "currency";
        public const string Purchase = "purchase";
        public const string AdReward = "ad_reward";
        public const string LevelUp = "level_up";
        public const string MissionClaim = "mission_claim";
        public const string LoginClaim = "login_claim";
    }

    /// <summary>
    /// Ghi sự kiện analytics. CHỈ gửi khi người chơi đã đồng ý trong popup quyền riêng tư (consent == 1) — yêu cầu của GDPR và của UGS.
    /// Không có sự kiện nào gửi mỗi frame: chỉ ở mốc meta (bắt đầu/kết thúc màn, đổi tiền tệ, mua, quảng cáo).
    /// Cách dùng: Begin().Param(...).Param(...).Send(tên).
    /// </summary>
    public class AnalyticsService
    {
        private readonly IAnalyticsSink _sink;
        private readonly PlayerProfile _profile;
        // Dùng lại 1 từ điển cho mọi sự kiện.
        private readonly Dictionary<string, object> _params = new Dictionary<string, object>(8);

        public AnalyticsService(IAnalyticsSink sink, PlayerProfile profile)
        {
            _sink = sink;
            _profile = profile;
        }

        public IAnalyticsSink Sink => _sink;
        public bool Enabled => _profile.consent == 1;
        /// <summary>Số sự kiện đã gửi trong phiên (DevMenu, test).</summary>
        public int SentCount { get; private set; }

        public AnalyticsService Begin()
        {
            _params.Clear();
            return this;
        }

        public AnalyticsService Param(string key, object value)
        {
            _params[key] = value;
            return this;
        }

        /// <summary>Trả false (không gửi) nếu người chơi chưa đồng ý.</summary>
        public bool Send(string eventName)
        {
            if (!Enabled || _sink == null)
            {
                _params.Clear();
                return false;
            }
            _sink.Send(eventName, _params);
            _params.Clear();
            SentCount++;
            return true;
        }
    }
}
