using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Dữ liệu định nghĩa 1 loại súng bắn đạn — tách khỏi logic PlayerWeapon.cs.
    /// Muốn thêm súng mới (ví dụ súng bắn nhanh, súng damage cao): chỉ tạo thêm asset,
    /// không cần sửa code PlayerWeapon.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : WeaponDefinition
    {
        [Header("Stats")]
        public float damage = 5f;
        public float fireRate = 1.5f; // số phát/giây
        public float range = 6f;
        public float projectileSpeed = 10f;

        [Header("Pattern")]
        [Tooltip("Số viên bắn ra mỗi lần. 1 = súng thường; nhiều hơn = súng chùm.")]
        [Min(1)] public int projectileCount = 1;
        [Tooltip("Góc tỏa (độ) giữa viên ngoài cùng bên trái và bên phải. Không có tác dụng khi chỉ bắn 1 viên.")]
        [Range(0f, 180f)] public float spreadAngle = 0f;
    }
}
