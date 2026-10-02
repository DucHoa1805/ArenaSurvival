using System;
using UnityEngine;

namespace ArenaSurvival.Missions
{
    /// <summary>Thứ nhiệm vụ đếm. Lưu theo số trong asset: chỉ thêm ở cuối, không đổi thứ tự.</summary>
    public enum MissionMetric
    {
        StagePlayed,
        StageWon,
        EnemyKilled,
        Upgrade,
        AdWatched,
        GoldSpent,
        ChestOpened,
    }

    /// <summary>
    /// 1 nhiệm vụ ngày. Save lưu tiến độ theo <see cref="id"/> — đổi id của nhiệm vụ đã phát hành là người chơi mất tiến độ hôm đó.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Mission", fileName = "Mission_")]
    public class MissionData : ScriptableObject
    {
        public string id;
        [Tooltip("{0} = mục tiêu. Ví dụ: \"Chơi {0} màn\".")]
        public string description;
        public MissionMetric metric;
        [Min(1)] public int target = 1;
        [Min(1)] public int points = 20;

        public string Describe() => string.Format(description ?? "", target);
    }

    [Serializable]
    public class RewardAmount
    {
        public string currencyId;
        [Min(1)] public int amount = 1;
    }

    /// <summary>Phần thưởng của mốc điểm / ngày đăng nhập: tiền tệ + (tùy chọn) 1 rương trong cửa hàng.</summary>
    [Serializable]
    public class MissionReward
    {
        public RewardAmount[] currencies = new RewardAmount[0];
        [Tooltip("Rương mở ngay khi nhận (sản phẩm cửa hàng có bảng rơi). Trống = không có rương.")]
        public Shop.ShopProductData chest;
    }

    [Serializable]
    public class MissionMilestone
    {
        [Min(1)] public int points = 40;
        public MissionReward reward = new MissionReward();
    }
}
