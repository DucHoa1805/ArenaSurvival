using System;
using System.Collections.Generic;
using System.IO;
using ArenaSurvival.Core;
using Newtonsoft.Json;

namespace ArenaSurvival.Services
{
    /// <summary>Nguồn Remote Config (UGS Remote Config ở 12B). onDone(null) = không lấy được (offline, lỗi).</summary>
    public interface IRemoteConfigProvider
    {
        void Fetch(Action<Dictionary<string, string>> onDone);
    }

    /// <summary>Không có nguồn từ xa: luôn báo không lấy được (dùng bản lưu tạm hoặc mặc định).</summary>
    public class NullRemoteConfigProvider : IRemoteConfigProvider
    {
        public void Fetch(Action<Dictionary<string, string>> onDone) => onDone?.Invoke(null);
    }

    /// <summary>
    /// Bản dev (12A): đọc remote-config.json cạnh file save ({"khóa": giá trị}) — sửa file rồi mở lại game (hoặc bấm "Nạp lại" ở DevMenu)
    /// để thử "đổi số không cần build". Không có file = không lấy được.
    /// </summary>
    public class FileRemoteConfigProvider : IRemoteConfigProvider
    {
        private readonly string _path;

        public FileRemoteConfigProvider(string path) => _path = path;

        public string Path => _path;

        public void Fetch(Action<Dictionary<string, string>> onDone) => onDone?.Invoke(RemoteConfigService.ReadFile(_path));
    }

    /// <summary>
    /// Lấy giá trị từ xa và áp vào Tuning. Giá trị lấy được lưu bản tạm (remote-cache.json) để lần mở sau không có mạng vẫn chạy
    /// với số mới nhất đã biết, thay vì quay về mặc định trong bản build.
    /// </summary>
    public class RemoteConfigService
    {
        private readonly IRemoteConfigProvider _provider;
        private readonly string _cachePath;

        public RemoteConfigService(IRemoteConfigProvider provider, string cachePath)
        {
            _provider = provider;
            _cachePath = cachePath;
        }

        public IRemoteConfigProvider Provider => _provider;
        /// <summary>"remote", "cache" hoặc "default" — DevMenu hiện để biết số đang chạy lấy từ đâu.</summary>
        public string Source { get; private set; } = "default";
        public int AppliedKeys { get; private set; }
        public event Action OnApplied;

        /// <summary>Áp bản tạm ngay (đồng bộ) rồi mới hỏi nguồn từ xa — vào màn sớm vẫn có số đã biết.</summary>
        public void Initialize()
        {
            Tuning.Reset();
            Dictionary<string, string> cached = ReadFile(_cachePath);
            if (cached != null) Apply(cached, "cache");
            Refresh();
        }

        public void Refresh()
        {
            _provider.Fetch(values =>
            {
                if (values == null) return;
                Apply(values, "remote");
                WriteCache(values);
            });
        }

        private void Apply(Dictionary<string, string> values, string source)
        {
            AppliedKeys = Tuning.Apply(values);
            Source = source;
            OnApplied?.Invoke();
        }

        private void WriteCache(Dictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(_cachePath)) return;
            try { File.WriteAllText(_cachePath, JsonConvert.SerializeObject(values)); }
            catch (Exception e) { UnityEngine.Debug.LogWarning($"RemoteConfigService: không ghi được bản tạm ({e.Message})"); }
        }

        /// <summary>Đọc JSON phẳng {khóa: giá trị}; file thiếu hoặc hỏng → null (giữ số đang có).</summary>
        public static Dictionary<string, string> ReadFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try { return Parse(File.ReadAllText(path)); }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"RemoteConfigService: bỏ qua {path} ({e.Message})");
                return null;
            }
        }

        /// <summary>Số/bool được đổi thành chuỗi InvariantCulture để Tuning đọc theo một kiểu duy nhất.</summary>
        public static Dictionary<string, string> Parse(string json)
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (raw == null) return null;
            var result = new Dictionary<string, string>(raw.Count);
            foreach (KeyValuePair<string, object> kv in raw)
            {
                if (kv.Value == null) continue;
                result[kv.Key] = kv.Value is IFormattable f
                    ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
                    : kv.Value.ToString();
            }
            return result;
        }
    }
}
