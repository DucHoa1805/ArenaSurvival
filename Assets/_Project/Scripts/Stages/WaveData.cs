using UnityEngine;
using ArenaSurvival.Enemies;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Dữ liệu định nghĩa 1 wave — tách khỏi logic WaveSpawner/StageSession.
    /// Muốn thêm wave mới hoặc cân bằng lại độ khó: chỉ tạo/sửa asset, KHÔNG cần sửa code.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Wave Data", fileName = "NewWaveData")]
    public class WaveData : ScriptableObject
    {
        [Header("Timing")]
        [Min(1f)] public float duration = 30f; // wave kéo dài bao nhiêu giây trước khi sang wave kế
        [Min(0.05f)] public float spawnInterval = 1.5f; // cứ bao nhiêu giây sinh 1 quái

        [Header("Enemies")]
        [Tooltip("Các loại quái được phép xuất hiện trong wave này, mỗi phần tử có cơ hội như nhau. " +
                 "Muốn 1 loại xuất hiện thường hơn thì thêm nó vào danh sách nhiều lần.")]
        public EnemyData[] enemyTypes;
    }
}
