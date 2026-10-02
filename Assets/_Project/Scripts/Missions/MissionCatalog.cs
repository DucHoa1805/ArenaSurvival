using UnityEngine;

namespace ArenaSurvival.Missions
{
    /// <summary>
    /// Nhiệm vụ ngày, mốc điểm và quà đăng nhập 7 ngày. Đặt ở Resources như ShopCatalog vì MissionService nằm trong GameServices
    /// (tự khởi tạo trước mọi scene).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Mission Catalog", fileName = "MissionCatalog")]
    public class MissionCatalog : ScriptableObject
    {
        public const string ResourcePath = "MissionCatalog";

        public MissionData[] daily = new MissionData[0];
        [Tooltip("Tăng dần theo điểm.")]
        public MissionMilestone[] milestones = new MissionMilestone[0];
        [Tooltip("Ngày 1 → 7, hết thì quay lại ngày 1.")]
        public MissionReward[] loginRewards = new MissionReward[0];

        public int MaxPoints
        {
            get
            {
                int sum = 0;
                if (daily != null) foreach (MissionData m in daily) if (m != null) sum += m.points;
                return sum;
            }
        }
    }
}
