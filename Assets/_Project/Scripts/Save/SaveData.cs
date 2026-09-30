using System;

namespace ArenaSurvival.Save
{
    /// <summary>
    /// Dữ liệu được ghi xuống đĩa. Chỉ chứa field public kiểu đơn giản vì JsonUtility
    /// chỉ serialize được field public (hoặc [SerializeField]), không serialize property.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Tăng số này khi đổi cấu trúc file, để sau này biết cần chuyển đổi dữ liệu cũ thế nào.
        public int version = 1;

        public float bestSurvivalTime;
        public int bestKillCount;
    }
}
