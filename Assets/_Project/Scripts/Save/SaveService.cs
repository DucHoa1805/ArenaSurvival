using System;
using System.IO;
using ArenaSurvival.Meta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArenaSurvival.Save
{
    public enum SaveLoadStatus
    {
        New,                // chưa có save: hồ sơ mới
        Loaded,             // đọc bình thường
        Migrated,           // save cũ đã được nâng lên cấu trúc hiện tại
        RestoredFromBackup, // file chính hỏng/bị sửa, dùng bản .bak
        Tampered,           // bị sửa tay, không có .bak hợp lệ: vẫn dùng nhưng đánh dấu
        Corrupt,            // hỏng hẳn, không có .bak: hồ sơ mới (file hỏng giữ lại ở .corrupt)
    }

    public readonly struct SaveLoadResult
    {
        public readonly PlayerProfile Profile;
        public readonly SaveLoadStatus Status;

        public SaveLoadResult(PlayerProfile profile, SaveLoadStatus status)
        {
            Profile = profile;
            Status = status;
        }
    }

    /// <summary>
    /// Đọc/ghi PlayerProfile. Ba lớp bảo vệ dữ liệu người chơi:
    /// 1) Ghi ra .tmp rồi mới thay file chính — tắt máy giữa lúc ghi chỉ hỏng file tạm, không hỏng save.
    /// 2) Bản trước luôn còn ở .bak — file chính hỏng/bị sửa thì tự khôi phục.
    /// 3) Chữ ký HMAC — phát hiện sửa tay.
    /// Nhận đường dẫn qua constructor để unit test ghi vào thư mục tạm, không đụng save thật.
    /// </summary>
    public class SaveService
    {
        private const int FileFormat = 2;

        private readonly string _path;
        private string TempPath => _path + ".tmp";
        private string BackupPath => _path + ".bak";
        private string CorruptPath => _path + ".corrupt";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "save.json");

        /// <summary>Đường dẫn file save chính (menu dev hiển thị / mở thư mục).</summary>
        public string FilePath => _path;

        public SaveService(string path)
        {
            _path = path;
        }

        public bool Save(PlayerProfile profile)
        {
            try
            {
                string payload = JsonConvert.SerializeObject(profile);
                var envelope = new JObject
                {
                    ["format"] = FileFormat,
                    ["payload"] = payload,
                    ["hmac"] = SaveSecurity.Sign(payload),
                };
                File.WriteAllText(TempPath, envelope.ToString(Formatting.Indented));
                ReplaceMainWithTemp();
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[SaveService] Không ghi được save '{_path}': {e.Message}");
                return false;
            }
        }

        private void ReplaceMainWithTemp()
        {
            if (!File.Exists(_path))
            {
                File.Move(TempPath, _path);
                return;
            }

            try
            {
                // Thay file chính bằng file tạm và giữ bản cũ làm .bak trong 1 thao tác của hệ điều hành.
                File.Replace(TempPath, _path, BackupPath);
            }
            catch (PlatformNotSupportedException)
            {
                // Vài nền tảng không hỗ trợ File.Replace: làm tay theo đúng thứ tự (bản cũ thành .bak trước, rồi mới đưa bản mới vào).
                File.Copy(_path, BackupPath, true);
                File.Delete(_path);
                File.Move(TempPath, _path);
            }
        }

        public SaveLoadResult Load()
        {
            if (!File.Exists(_path))
            {
                // Mất file chính nhưng còn .bak (ví dụ tắt máy đúng lúc thay file trên nền tảng không có File.Replace).
                ReadOutcome backupOnly = Read(BackupPath);
                if (backupOnly.IsUsable) return new SaveLoadResult(backupOnly.Profile, SaveLoadStatus.RestoredFromBackup);
                return new SaveLoadResult(new PlayerProfile(), SaveLoadStatus.New);
            }

            ReadOutcome main = Read(_path);
            if (main.IsUsable)
            {
                return new SaveLoadResult(main.Profile, main.Migrated ? SaveLoadStatus.Migrated : SaveLoadStatus.Loaded);
            }

            ReadOutcome backup = Read(BackupPath);
            if (backup.IsUsable)
            {
                Debug.LogWarning($"[SaveService] Save chính {(main.Tampered ? "bị sửa" : "bị hỏng")}, đã khôi phục từ bản sao lưu.");
                return new SaveLoadResult(backup.Profile, SaveLoadStatus.RestoredFromBackup);
            }

            if (main.Tampered)
            {
                // Không xóa tiến độ: có thể chính game ghi lỗi. Chỉ đánh dấu để thống kê.
                Debug.LogWarning("[SaveService] Save bị sửa tay và không có bản sao lưu hợp lệ: vẫn dùng, đã đánh dấu.");
                main.Profile.tampered = true;
                return new SaveLoadResult(main.Profile, SaveLoadStatus.Tampered);
            }

            Debug.LogWarning($"[SaveService] Save hỏng và không có bản sao lưu: tạo hồ sơ mới, giữ file hỏng ở {CorruptPath}.");
            TryKeepCorruptCopy();
            return new SaveLoadResult(new PlayerProfile(), SaveLoadStatus.Corrupt);
        }

        private void TryKeepCorruptCopy()
        {
            try
            {
                File.Copy(_path, CorruptPath, true);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[SaveService] Không giữ được bản hỏng: {e.Message}");
            }
        }

        private readonly struct ReadOutcome
        {
            public readonly PlayerProfile Profile;
            public readonly bool Tampered;
            public readonly bool Migrated;
            public bool IsUsable => Profile != null && !Tampered;

            public ReadOutcome(PlayerProfile profile, bool tampered, bool migrated)
            {
                Profile = profile;
                Tampered = tampered;
                Migrated = migrated;
            }

            public static ReadOutcome Unreadable => new ReadOutcome(null, false, false);
        }

        private static ReadOutcome Read(string path)
        {
            if (!File.Exists(path)) return ReadOutcome.Unreadable;

            try
            {
                JObject root = JObject.Parse(File.ReadAllText(path));

                // Không có "payload" = save v1 của bản portfolio (JsonUtility, không chữ ký).
                if (root["payload"] == null)
                {
                    int legacyVersion = root["version"]?.Value<int>() ?? 1;
                    return new ReadOutcome(ToProfile(SaveMigrations.Migrate(root, legacyVersion)), false, true);
                }

                string payload = root["payload"].Value<string>();
                bool signatureOk = SaveSecurity.Verify(payload, root["hmac"]?.Value<string>());
                JObject data = JObject.Parse(payload);
                int version = data["saveVersion"]?.Value<int>() ?? 1;
                PlayerProfile profile = ToProfile(SaveMigrations.Migrate(data, version));
                return new ReadOutcome(profile, !signatureOk, version < PlayerProfile.CurrentVersion);
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException ||
                                      e is FormatException || e is InvalidCastException || e is ArgumentException)
            {
                return ReadOutcome.Unreadable;
            }
        }

        private static PlayerProfile ToProfile(JObject data)
        {
            PlayerProfile profile = data.ToObject<PlayerProfile>() ?? new PlayerProfile();
            // JSON có thể ghi "currencies": null; đảm bảo phần còn lại của game không phải kiểm tra null.
            if (profile.currencies == null) profile.currencies = new System.Collections.Generic.Dictionary<string, long>();
            if (profile.stageStars == null) profile.stageStars = new System.Collections.Generic.Dictionary<string, int>();
            return profile;
        }
    }
}
