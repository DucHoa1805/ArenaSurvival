using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.Progression
{
    public enum UpgradeType
    {
        Damage,     // value = phần cộng thêm vào hệ số sát thương (0.2 = +20%)
        FireRate,   // value = phần cộng thêm vào hệ số tốc độ bắn
        MoveSpeed,  // value = phần cộng thêm vào hệ số tốc độ chạy
        MaxHealth,  // value = số máu tối đa cộng thêm
        Heal,       // value = số máu hồi ngay
        AddWeapon   // mở khóa vũ khí trong trường weapon; chỉ được đưa ra 1 lần
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

        [Header("Availability")]
        [Tooltip("Chỉ được đưa ra khi người chơi đạt tới cấp này — để vũ khí mạnh không xuất hiện quá sớm.")]
        [Min(1)] public int requiredLevel = 1;
    }
}
