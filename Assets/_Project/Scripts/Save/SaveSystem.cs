using System;
using System.IO;
using UnityEngine;

namespace ArenaSurvival.Save
{
    /// <summary>
    /// Đọc/ghi SaveData dưới dạng file JSON trong Application.persistentDataPath.
    ///
    /// Vì sao không dùng PlayerPrefs: PlayerPrefs hợp với vài cài đặt lẻ (âm lượng...), còn dữ liệu
    /// có cấu trúc thì JSON dễ đọc, dễ mở rộng, và trên Windows PlayerPrefs lại nằm trong Registry.
    ///
    /// Mỗi hàm có bản nhận đường dẫn để unit test ghi vào thư mục tạm thay vì đè lên save thật.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load() => Load(DefaultPath);

        public static SaveData Load(string path)
        {
            // Lần chơi đầu tiên chưa có file: đó là trường hợp bình thường, không phải lỗi.
            if (!File.Exists(path)) return new SaveData();

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                return data ?? new SaveData();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                // File hỏng hoặc không đọc được: chơi tiếp với dữ liệu trống thay vì làm game lỗi.
                Debug.LogWarning($"Không đọc được file save '{path}', dùng dữ liệu mặc định. Lỗi: {e.Message}");
                return new SaveData();
            }
        }

        public static bool Save(SaveData data) => Save(data, DefaultPath);

        public static bool Save(SaveData data, string path)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: true));
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Không ghi được file save '{path}'. Lỗi: {e.Message}");
                return false;
            }
        }
    }
}
