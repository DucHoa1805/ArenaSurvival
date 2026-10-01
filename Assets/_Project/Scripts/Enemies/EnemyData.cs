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

        [Tooltip("Hệ số phóng to cả hình lẫn collider (1 = kích thước gốc của prefab).")]
        [Min(0.1f)] public float scale = 1f;
        [Tooltip("Màu riêng của loại quái (nhân với sprite). Giúp boss dùng lại sprite Kenney mà vẫn khác biệt.")]
        public Color tint = Color.white;

        [Header("Stats")]
        public float maxHealth = 20f;
        public float moveSpeed = 2f;
        public float contactDamage = 5f;
        [Tooltip("Sát thương mỗi viên đạn (chỉ quái/boss có bắn mới dùng).")]
        public float projectileDamage = 6f;

        [Header("Behaviour")]
        [Tooltip("Cách di chuyển và tấn công (asset trong Data/Enemies/Behaviours hoặc Data/Bosses). " +
                 "Để trống thì đuổi thẳng như quái thường.")]
        public EnemyBehaviour behaviour;
        [Tooltip("Hiện thanh máu boss khi xuất hiện.")]
        public bool isBoss;

        [Header("Reward")]
        public int xpReward = 1;
    }
}