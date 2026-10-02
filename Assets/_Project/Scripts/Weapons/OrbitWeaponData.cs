using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>Dữ liệu cho vũ khí dạng vật thể quay vòng quanh Player (lưỡi cưa xoay).</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Orbit Weapon Data", fileName = "NewOrbitWeaponData")]
    public class OrbitWeaponData : WeaponDefinition
    {
        [Header("Blades")]
        public Sprite bladeSprite;
        [Min(1)] public int bladeCount = 3;
        [Tooltip("Đường kính 1 lưỡi cưa (đơn vị thế giới). Dùng cho cả hình lẫn vùng va chạm.")]
        [Min(0.05f)] public float bladeSize = 0.4f;
        [Tooltip("Tốc độ tự xoay của từng lưỡi, chỉ để nhìn cho đã mắt (độ/giây).")]
        public float bladeSpinSpeed = 720f;

        [Header("Orbit")]
        [Tooltip("Khoảng cách từ Player tới tâm lưỡi cưa.")]
        [Min(0.1f)] public float orbitRadius = 1.3f;
        [Tooltip("Tốc độ quay vòng quanh Player (độ/giây). Âm = quay ngược chiều kim đồng hồ.")]
        public float orbitSpeed = 180f;

        [Header("Effects (10D)")]
        [Tooltip("Quầng sáng dưới mỗi lưỡi (sprite tròn mờ). Không bắt buộc.")]
        public Sprite glowSprite;
        [Tooltip("Material cho vệt và quầng (unlit, nhận màu đỉnh). Không bắt buộc.")]
        public Material effectMaterial;
        public Color glowColor = new Color(0.55f, 0.85f, 1f, 0.55f);
        [Tooltip("Màu khi lưỡi cưa thức tỉnh (cấp tối đa).")]
        public Color awakenedColor = new Color(1f, 0.55f, 0.15f, 0.8f);

        [Header("Damage")]
        [Tooltip("Sát thương mỗi lần 1 lưỡi cưa quét trúng quái.")]
        public float damage = 8f;
    }
}
