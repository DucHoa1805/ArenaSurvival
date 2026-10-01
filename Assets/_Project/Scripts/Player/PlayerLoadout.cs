using ArenaSurvival.Combat;
using ArenaSurvival.Meta;
using ArenaSurvival.Stats;
using ArenaSurvival.Weapons;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// CỬA DUY NHẤT đưa sức mạnh ngoài trận (Talent, trang bị, vũ khí chính) vào nhân vật trong trận. Chỉ đọc hồ sơ, không ghi gì.
    /// Gom về một chỗ để khi thêm nguồn chỉ số mới chỉ phải sửa StatCalculator, không phải đi sửa Health/PlayerStats/vũ khí.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(HealthRegen))]
    public class PlayerLoadout : MonoBehaviour
    {
        [SerializeField] private HeroBaseStats _heroBase;
        [SerializeField] private TalentCatalog _talents;

        /// <summary>Chỉ số đã áp lúc vào màn (menu dev / HUD có thể hiển thị).</summary>
        public StatBlock Stats { get; private set; }
        public int Power => StatCalculator.Power(Stats);

        private void Awake()
        {
            Stats = LoadoutStats.Compute(_heroBase, _talents);

            var health = GetComponent<Health>();
            health.SetMaxHealth(Mathf.Max(1f, Stats.maxHealth), healToFull: true);
            health.Armor = Stats.armor;
            GetComponent<PlayerStats>().SetBaseMultipliers(1f + Stats.damagePercent / 100f, 1f + Stats.moveSpeedPercent / 100f);
            GetComponent<HealthRegen>().RegenPerSecond = Stats.healthRegen;

            WeaponService weapons = GameServices.Weapons;
            if (weapons != null) EquipMainWeapon(weapons.EquippedId);
        }

        /// <summary>
        /// Gắn vũ khí chính theo cấp/sao người chơi đang có (chưa sở hữu → cấp 1, 1★). Public để benchmark đo từng khẩu
        /// (-mainWeapon). Khẩu không có trong database → giữ khẩu dự phòng trên prefab.
        /// </summary>
        public bool EquipMainWeapon(string weaponId)
        {
            WeaponService weapons = GameServices.Weapons;
            WeaponData data = weapons?.GetData(weaponId);
            PlayerWeapon main = GetComponent<PlayerWeapon>();
            if (data == null || main == null) return false;

            main.Equip(data, weapons.GetEffective(weaponId));
            return true;
        }
    }
}
