using System;
using ArenaSurvival.Meta;
using Newtonsoft.Json.Linq;

namespace ArenaSurvival.Save
{
    /// <summary>
    /// Nâng dữ liệu save cũ lên cấu trúc hiện tại, từng bước một (v1→v2→v3...). Làm trên JSON thô (JObject) chứ không trên class,
    /// vì class chỉ mô tả được version MỚI NHẤT — dữ liệu cũ có thể mang trường đã bị xóa hoặc đổi nghĩa.
    /// Đổi cấu trúc PlayerProfile: tăng PlayerProfile.CurrentVersion và thêm đúng 1 bước vào Steps.
    /// </summary>
    public static class SaveMigrations
    {
        // Steps[i] nâng dữ liệu từ version (i + 1) lên (i + 2).
        private static readonly Func<JObject, JObject>[] Steps =
        {
            V1ToV2,
            V2ToV3,
        };

        public static JObject Migrate(JObject data, int fromVersion)
        {
            // Save của bản game MỚI hơn (người chơi cài lại bản cũ): không đoán cách hạ cấp, giữ nguyên và bỏ qua trường lạ.
            if (fromVersion >= PlayerProfile.CurrentVersion) return data;

            for (int version = Math.Max(1, fromVersion); version < PlayerProfile.CurrentVersion; version++)
            {
                data = Steps[version - 1](data);
            }

            data["saveVersion"] = PlayerProfile.CurrentVersion;
            return data;
        }

        /// <summary>
        /// v1 = SaveData của bản portfolio: chỉ có kỷ lục chế độ vô tận (bestSurvivalTime, bestKillCount).
        /// Chế độ vô tận đã gỡ ở Phase 6A nên kỷ lục không còn ý nghĩa; tạo hồ sơ mới với các trường mặc định.
        /// </summary>
        private static JObject V1ToV2(JObject v1) => new JObject
        {
            ["saveVersion"] = 2,
            ["accountXp"] = 0,
            ["currencies"] = new JObject(),
            ["stageStars"] = new JObject(),
            ["tampered"] = false,
        };

        /// <summary>v3 (Phase 8A) thêm cấp Talent. Mọi dữ liệu v2 giữ nguyên, chỉ thêm từ điển rỗng.</summary>
        private static JObject V2ToV3(JObject v2)
        {
            if (v2["talents"] == null) v2["talents"] = new JObject();
            v2["saveVersion"] = 3;
            return v2;
        }
    }
}
