using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Lớp cha chung cho dữ liệu mọi loại vũ khí (súng bắn đạn, lưỡi cưa xoay...). Nhờ có lớp chung này,
    /// 1 nâng cấp "mở khóa vũ khí" trỏ được tới bất kỳ loại vũ khí nào mà không cần biết nó hoạt động ra sao.
    /// </summary>
    public abstract class WeaponDefinition : ScriptableObject
    {
        [Header("Info")]
        public string displayName = "Weapon";
        public Sprite icon;
    }

    /// <summary>Component vũ khí trên Player cho biết nó dùng dữ liệu nào — để UpgradeManager tìm đúng khẩu cần bật.</summary>
    public interface IWeapon
    {
        WeaponDefinition Definition { get; }
    }
}
