using ArenaSurvival.Combat;
using ArenaSurvival.Items;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Dữ liệu 1 khẩu súng chính (8C): số liệu gốc ở cấp 1/1★ + cách đạn hành xử (xuyên, nổ, đẩy lùi) + phần meta (id, mảnh, hiệu ứng sao).
    /// Kiểu bắn khác nhau chỉ là bộ số khác nhau (phương án A) — thêm súng mới thường chỉ cần tạo asset, không sửa PlayerWeapon.
    /// KHÔNG đọc thẳng các số này lúc bắn: dùng WeaponRules.Effective (đã cộng cấp, sao).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : WeaponDefinition
    {
        [Header("Meta")]
        [Tooltip("ID cố định, save lưu theo chuỗi này. KHÔNG đổi sau khi phát hành.")]
        public string id;
        [Tooltip("1 dòng mô tả cách bắn cho bảng Vũ khí.")]
        [TextArea] public string description;
        [Tooltip("Mảnh của chính khẩu này (MaterialData, id = WeaponRules.ShardId(id)).")]
        public MaterialData shardMaterial;

        [Header("Stats (cấp 1, 1★)")]
        public float damage = 5f;
        [Tooltip("Sát thương cộng thêm mỗi cấp (trước khi nhân hệ số sao).")]
        public float damagePerLevel = 0.4f;
        public float fireRate = 1.5f; // số phát/giây
        public float range = 6f;
        public float projectileSpeed = 10f;

        [Header("Pattern")]
        [Tooltip("Số viên bắn ra mỗi lần. 1 = súng thường; nhiều hơn = súng chùm.")]
        [Min(1)] public int projectileCount = 1;
        [Tooltip("Góc tỏa (độ) giữa viên ngoài cùng bên trái và bên phải. Không có tác dụng khi chỉ bắn 1 viên.")]
        [Range(0f, 180f)] public float spreadAngle = 0f;
        [Tooltip("Lệch ngẫu nhiên tối đa (độ) mỗi viên — súng máy bắn hơi tản.")]
        [Range(0f, 30f)] public float aimJitter = 0f;

        [Header("Đạn")]
        [Tooltip("Số quái xuyên THÊM sau con đầu tiên (laser).")]
        [Min(0)] public int pierce;
        [Tooltip("Bán kính nổ khi trúng (0 = không nổ).")]
        [Min(0f)] public float explosionRadius;
        [Tooltip("Tỉ lệ sát thương lên quái xung quanh trong vùng nổ.")]
        [Range(0f, 1f)] public float explosionDamageFraction = 0.7f;
        [Tooltip("Đẩy lùi quái trúng đạn bấy nhiêu đơn vị (shotgun).")]
        [Min(0f)] public float knockback;
        [Tooltip("Prefab đạn riêng (lựu đạn, tia laser). Trống = đạn mặc định của PlayerWeapon.")]
        public Projectile projectilePrefab;

        [Header("Hiệu ứng sao")]
        public WeaponStarBonus starBonus3;
        public WeaponStarBonus starBonus5;
    }
}
