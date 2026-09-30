using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Dữ liệu định nghĩa 1 loại quái — tách hoàn toàn khỏi logic (script Enemy.cs).
    /// Muốn thêm loại quái mới: chỉ cần tạo thêm 1 asset từ menu này, KHÔNG cần sửa code.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Data", fileName = "NewEnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Info")]
        public string displayName = "Enemy";
        public Sprite sprite;

        [Header("Stats")]
        public float maxHealth = 20f;
        public float moveSpeed = 2f;
        public float contactDamage = 5f;

        [Header("Reward")]
        public int xpReward = 1;
    }
}