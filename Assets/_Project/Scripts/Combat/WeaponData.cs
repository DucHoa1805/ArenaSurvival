using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Dữ liệu định nghĩa 1 loại vũ khí — tách khỏi logic PlayerWeapon.cs.
    /// Muốn thêm vũ khí mới (ví dụ súng bắn nhanh, súng damage cao): chỉ tạo thêm asset,
    /// không cần sửa code PlayerWeapon.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Info")]
        public string displayName = "Weapon";
        public Sprite icon;

        [Header("Stats")]
        public float damage = 5f;
        public float fireRate = 1.5f; // số phát/giây
        public float range = 6f;
        public float projectileSpeed = 10f;
    }
}