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
            V3ToV4,
            V4ToV5,
            V5ToV6,
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

        /// <summary>v4 (Phase 8B) thêm kho trang bị. Nguyên liệu nằm trong currencies nên không cần thêm gì cho chúng.</summary>
        private static JObject V3ToV4(JObject v3)
        {
            if (v3["items"] == null) v3["items"] = new JArray();
            if (v3["equipped"] == null) v3["equipped"] = new JObject();
            if (v3["nextItemUid"] == null) v3["nextItemUid"] = 1;
            v3["saveVersion"] = 4;
            return v3;
        }

        /// <summary>v5 (Phase 8C) thêm vũ khí chính: tặng Súng lục 1★ và mặc sẵn, để người chơi cũ vào màn vẫn có súng như trước.</summary>
        private static JObject V4ToV5(JObject v4)
        {
            if (!(v4["weapons"] is JObject weapons)) v4["weapons"] = weapons = new JObject();
            if (weapons[WeaponService.DefaultWeaponId] == null)
                weapons[WeaponService.DefaultWeaponId] = new JObject { ["level"] = 1, ["stars"] = 1 };

            if (!(v4["equipped"] is JObject equipped)) v4["equipped"] = equipped = new JObject();
            if (equipped["weapon"] == null) equipped["weapon"] = WeaponService.DefaultWeaponId;

            v4["saveVersion"] = 5;
            return v4;
        }

        /// <summary>v6 (Phase 11A) thêm dữ liệu cửa hàng/quảng cáo. Người chơi cũ chưa mua gì, chưa được hỏi đồng ý quyền riêng tư.</summary>
        private static JObject V5ToV6(JObject v5)
        {
            if (v5["processedTransactions"] == null) v5["processedTransactions"] = new JArray();
            if (v5["purchaseCounts"] == null) v5["purchaseCounts"] = new JObject();
            if (v5["removeAds"] == null) v5["removeAds"] = false;
            if (v5["adViews"] == null) v5["adViews"] = new JObject();
            if (v5["adDay"] == null) v5["adDay"] = 0;
            if (v5["consent"] == null) v5["consent"] = -1;
            v5["saveVersion"] = 6;
            return v5;
        }
    }
}
