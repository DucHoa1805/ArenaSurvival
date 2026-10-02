using System;
using System.Collections.Generic;
using ArenaSurvival.Meta;

namespace ArenaSurvival.Services
{
    /// <summary>Tóm tắt 1 bản save để so sánh và hiện cho người chơi chọn (lưu kèm bản mây ở khóa profile_meta).</summary>
    [Serializable]
    public class CloudSummary
    {
        public long accountXp;
        public int stagesCleared;
        public long savedAtUtcTicks;
        /// <summary>Guid của lần tải lên — biết bản mây có phải bản máy này đồng bộ lần cuối không.</summary>
        public string syncId;

        public bool IsEmpty => accountXp <= 0 && stagesCleared <= 0;

        public static CloudSummary Of(PlayerProfile profile, DateTime utcNow, string syncId = null) => new CloudSummary
        {
            accountXp = profile.accountXp,
            stagesCleared = profile.stageStars.Count,
            savedAtUtcTicks = utcNow.Ticks,
            syncId = syncId,
        };
    }

    public enum CloudDecision
    {
        /// <summary>Bản máy là bản đúng: tải lên (đè bản mây).</summary>
        Upload,
        /// <summary>Máy chưa có gì, mây có tiến độ: lấy bản mây không cần hỏi.</summary>
        AdoptCloud,
        /// <summary>Cả hai đều có tiến độ và bản mây đã đổi từ nơi khác: hỏi người chơi.</summary>
        AskPlayer,
    }

    /// <summary>Luật đồng bộ Cloud Save (hàm thuần, có unit test).</summary>
    public static class CloudSaveRules
    {
        public const float UploadInterval = 60f;
        public const int MaxTransactions = 200;

        /// <param name="cloud">null = trên mây chưa có bản nào.</param>
        /// <param name="lastSyncedId">syncId của bản mây mà lần cài này đồng bộ lần cuối (null = chưa từng).</param>
        public static CloudDecision Decide(CloudSummary local, CloudSummary cloud, string lastSyncedId)
        {
            if (cloud == null) return CloudDecision.Upload;
            // Bản mây vẫn là bản máy này tải lên lần trước: không ai khác ghi vào, bản máy mới hơn hoặc bằng.
            if (!string.IsNullOrEmpty(cloud.syncId) && cloud.syncId == lastSyncedId) return CloudDecision.Upload;
            if (cloud.IsEmpty) return CloudDecision.Upload;
            if (local.IsEmpty) return CloudDecision.AdoptCloud;
            return CloudDecision.AskPlayer;
        }

        /// <summary>
        /// Chọn 1 bản thì bản kia mất — trừ quyền lợi người chơi đã trả tiền: giữ Xóa quảng cáo, hợp danh sách mã giao dịch (để store giao lại
        /// không trao trùng) và lấy số lần mua lớn hơn (gói giới hạn không mua lại được).
        /// </summary>
        public static void MergePurchases(PlayerProfile chosen, PlayerProfile other)
        {
            if (chosen == null || other == null) return;
            chosen.removeAds |= other.removeAds;
            foreach (string tx in other.processedTransactions)
                if (!chosen.processedTransactions.Contains(tx)) chosen.processedTransactions.Add(tx);
            if (chosen.processedTransactions.Count > MaxTransactions)
                chosen.processedTransactions.RemoveRange(0, chosen.processedTransactions.Count - MaxTransactions);
            foreach (KeyValuePair<string, int> kv in other.purchaseCounts)
            {
                chosen.purchaseCounts.TryGetValue(kv.Key, out int mine);
                if (kv.Value > mine) chosen.purchaseCounts[kv.Key] = kv.Value;
            }
        }

        public static bool ShouldUpload(bool dirty, float now, float lastUpload) => dirty && now - lastUpload >= UploadInterval;
    }
}
