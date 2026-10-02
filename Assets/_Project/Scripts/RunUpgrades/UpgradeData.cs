using ArenaSurvival.Combat;
using UnityEngine;
using ArenaSurvival.Weapons;

namespace ArenaSurvival.RunUpgrades
{
    public enum UpgradeType
    {
        Damage,     // value = phần cộng thêm vào hệ số sát thương (0.2 = +20%)
        FireRate,   // value = phần cộng thêm vào hệ số tốc độ bắn
        MoveSpeed,  // value = phần cộng thêm vào hệ số tốc độ chạy
        MaxHealth,  // value = số máu tối đa cộng thêm
        Heal,       // value = số máu hồi ngay
        AddWeapon,  // cấp 1 mở khóa vũ khí trong trường weapon; cấp sau nâng vũ khí đó (Lưỡi cưa: thêm lưỡi)
        // Thêm ở CUỐI enum: asset lưu giá trị số, chèn giữa sẽ làm đổi loại của asset cũ.
        Multishot,  // mỗi cấp +1 viên mỗi phát
        Pierce,     // mỗi cấp +1 lần xuyên
        Shield,     // bật/nâng PlayerShield
        Aura,       // bật/nâng PlayerAura (vòng điện)
        Missile,    // bật/nâng PlayerMissiles
        Magnet      // bán kính hút XP theo SkillRules.MagnetMultiplier
    }

    /// <summary>
    /// Dữ liệu định nghĩa 1 nâng cấp — tách khỏi logic UpgradeManager.cs.
    /// Muốn thêm nâng cấp cùng loại với giá trị khác (ví dụ +50% sát thương): chỉ tạo thêm asset.
    /// Muốn thêm LOẠI mới thì mới cần sửa code (thêm vào UpgradeType và UpgradeManager.Apply).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Upgrade Data", fileName = "NewUpgradeData")]
    public class UpgradeData : ScriptableObject
    {
        [Header("Info")]
        public string displayName = "Upgrade";
        [TextArea] public string description;
        public Sprite icon;

        [Header("Effect")]
        public UpgradeType type;
        public float value;

        [Tooltip("Chỉ dùng khi type = AddWeapon: vũ khí được mở khóa (súng hoặc lưỡi cưa...).")]
        public WeaponDefinition weapon;

        [Header("Levels")]
        [Tooltip("Cấp tối đa trong 1 màn. 0 = lặp vô hạn và không chiếm chỗ trong giới hạn kỹ năng (Hồi máu).")]
        [Min(0)] public int maxLevel = 5;
        [Tooltip("Mô tả cho từng cấp kế (phần tử 0 = mô tả khi nhận cấp 1). Trống/thiếu thì dùng description.")]
        public string[] levelDescriptions = new string[0];

        public string DescriptionForLevel(int nextLevel)
        {
            int i = nextLevel - 1;
            return levelDescriptions != null && i >= 0 && i < levelDescriptions.Length && !string.IsNullOrEmpty(levelDescriptions[i])
                ? levelDescriptions[i] : description;
        }

        [Header("Availability")]
        [Tooltip("Chỉ được đưa ra khi người chơi đạt tới cấp này — để vũ khí mạnh không xuất hiện quá sớm.")]
        [Min(1)] public int requiredLevel = 1;
    }
}
