using System;
using ArenaSurvival.Enemies;
using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Một màn chơi: các đợt quái chạy theo thời gian, rồi đợt cuối gồm quái tinh anh phải diệt hết.
    /// Ở Phase 6B, đợt cuối sẽ có thêm boss mà không phải đổi cấu trúc này.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Stage Data", fileName = "Stage_")]
    public class StageData : ScriptableObject
    {
        [Header("Định danh")]
        [Tooltip("ID cố định, ví dụ \"c1_s01\". KHÔNG đổi sau khi phát hành: save (Phase 7) lưu tiến độ theo id này, " +
                 "đổi tên asset hay tên hiển thị thì save vẫn khớp.")]
        public string id;
        public string displayName = "1-1";
        [Tooltip("Số thứ tự toàn cục, dùng cho công thức độ khó và phần thưởng (StageRules).")]
        [Min(1)] public int stageNumber = 1;

        [Header("Đợt quái")]
        [Tooltip("Chạy lần lượt, mỗi đợt kéo dài WaveData.duration giây.")]
        public WaveData[] waves;
        [Tooltip("Đợt cuối: sân ngừng sinh quái thường, các nhóm này phải bị hạ hết mới thắng.")]
        public EliteGroup[] finalWave;
        [Min(0.05f)] public float eliteSpawnInterval = 0.6f;

        [Header("Đánh giá & phần thưởng")]
        [Tooltip("Thắng trong thời gian này (giây) thì đạt sao thứ 3.")]
        [Min(1f)] public float targetTime = 210f;
        [Min(0)] public int baseGold = 100;
        [Min(0)] public int baseAccountXp = 20;

        /// <summary>Tổng thời gian các đợt thường, dùng tính tiến độ màn.</summary>
        public float PlannedDuration
        {
            get
            {
                float total = 0f;
                if (waves == null) return total;
                foreach (WaveData wave in waves)
                {
                    if (wave != null) total += wave.duration;
                }
                return total;
            }
        }
    }

    /// <summary>Một nhóm quái tinh anh: dùng lại EnemyData thường, chỉ nhân thêm hệ số — không cần asset quái riêng.</summary>
    [Serializable]
    public class EliteGroup
    {
        public EnemyData enemy;
        [Min(1)] public int count = 1;
        [Tooltip("Nhân thêm trên hệ số độ khó của màn.")]
        [Min(0.1f)] public float healthMultiplier = 5f;
        [Min(0.1f)] public float scaleMultiplier = 1.3f;
        public Color tint = new Color(1f, 0.75f, 0.45f, 1f);
    }
}
