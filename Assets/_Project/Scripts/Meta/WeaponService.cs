using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Weapons;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Vũ khí chính (8C): mở khóa / lên sao bằng mảnh, nâng cấp bằng vàng + quặng, chọn khẩu mang vào màn.
    /// Class thuần (unit test được), nằm trong GameServices. Mọi chi phí trừ qua EconomyService.TrySpendMany; công thức ở WeaponRules.
    /// </summary>
    public class WeaponService
    {
        /// <summary>Khẩu luôn có sẵn: người chơi mới, save cũ (migration) và save hỏng đều được tặng để không bao giờ vào trận tay không.</summary>
        public const string DefaultWeaponId = "weapon_pistol";

        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly ItemDatabase _database;
        private readonly Action _save;
        private readonly List<CurrencyCost> _costBuffer = new List<CurrencyCost>(2);

        /// <summary>(id vũ khí vừa đổi cấp/sao/được mặc)</summary>
        public event Action<string> OnWeaponChanged;

        public WeaponService(PlayerProfile profile, EconomyService economy, ItemDatabase database, Action save)
        {
            _profile = profile;
            _economy = economy;
            _database = database;
            _save = save;
            EnsureDefaults(profile);
        }

        /// <summary>Đảm bảo hồ sơ có Súng lục và có khẩu đang mặc. Gọi ở mọi đường tạo/nạp hồ sơ.</summary>
        public static void EnsureDefaults(PlayerProfile profile)
        {
            if (!profile.weapons.ContainsKey(DefaultWeaponId)) profile.weapons[DefaultWeaponId] = new OwnedWeapon();
            if (!profile.equipped.TryGetValue(EquipSlotIds.Weapon, out string id) || !profile.weapons.ContainsKey(id))
                profile.equipped[EquipSlotIds.Weapon] = DefaultWeaponId;
        }

        public ItemDatabase Database => _database;

        public bool IsOwned(string id) => id != null && _profile.weapons.ContainsKey(id);
        public int GetLevel(string id) => _profile.weapons.TryGetValue(id ?? "", out OwnedWeapon w) ? w.level : 0;
        public int GetStars(string id) => _profile.weapons.TryGetValue(id ?? "", out OwnedWeapon w) ? w.stars : 0;
        public long GetShards(string id) => _economy.GetBalance(WeaponRules.ShardId(id));

        /// <summary>Khẩu đang mặc; save trỏ tới khẩu không còn (asset bị xóa) thì coi như Súng lục.</summary>
        public string EquippedId
        {
            get
            {
                if (_profile.equipped.TryGetValue(EquipSlotIds.Weapon, out string id) && IsOwned(id) && GetData(id) != null) return id;
                return DefaultWeaponId;
            }
        }

        public WeaponData EquippedData => GetData(EquippedId);

        public WeaponData GetData(string id) => _database != null ? _database.GetWeapon(id) : null;

        /// <summary>Chỉ số thực ở cấp/sao hiện tại (chưa sở hữu → như cấp 1, 1★, để bảng Vũ khí xem trước).</summary>
        public WeaponStats GetEffective(string id)
        {
            WeaponData data = GetData(id);
            if (data == null) return default;
            return WeaponRules.Effective(data, Math.Max(1, GetLevel(id)), Math.Max(1, GetStars(id)));
        }

        /// <summary>Chi phí lên cấp kế. false nếu chưa sở hữu hoặc đã ở trần cấp của số sao hiện tại.</summary>
        public bool GetUpgradeCost(string id, List<CurrencyCost> output)
        {
            output.Clear();
            if (!IsOwned(id) || GetData(id) == null) return false;
            int level = GetLevel(id);
            if (level >= WeaponRules.LevelCap(GetStars(id))) return false;

            output.Add(new CurrencyCost(CurrencyIds.Gold, WeaponRules.UpgradeGold(level)));
            output.Add(new CurrencyCost(CurrencyIds.Ore, WeaponRules.UpgradeOre(level)));
            return true;
        }

        /// <summary>Chi phí lên sao kế (chưa sở hữu = mở khóa lên 1★). false nếu đã 5★.</summary>
        public bool GetStarCost(string id, List<CurrencyCost> output)
        {
            output.Clear();
            if (GetData(id) == null) return false;
            int next = GetStars(id) + 1;
            if (next > WeaponRules.MaxStars) return false;

            output.Add(new CurrencyCost(WeaponRules.ShardId(id), WeaponRules.ShardsFor(next)));
            return true;
        }

        public bool TryUnlock(string id)
        {
            if (IsOwned(id) || !GetStarCost(id, _costBuffer) || !_economy.TrySpendMany(_costBuffer, EconomyReasons.WeaponUnlock)) return false;

            _profile.weapons[id] = new OwnedWeapon { level = 1, stars = 1 };
            Commit(id);
            return true;
        }

        public bool TryStarUp(string id)
        {
            // Chưa sở hữu phải đi qua TryUnlock: tách rõ để UI ghi đúng chữ "Mở khóa" / "Lên sao".
            if (!IsOwned(id) || !GetStarCost(id, _costBuffer) || !_economy.TrySpendMany(_costBuffer, EconomyReasons.WeaponStar)) return false;

            _profile.weapons[id].stars++;
            Commit(id);
            return true;
        }

        public bool TryUpgrade(string id)
        {
            if (!GetUpgradeCost(id, _costBuffer) || !_economy.TrySpendMany(_costBuffer, EconomyReasons.WeaponUpgrade)) return false;

            _profile.weapons[id].level++;
            Commit(id);
            return true;
        }

        public bool Equip(string id)
        {
            if (!IsOwned(id) || GetData(id) == null) return false;
            if (_profile.equipped.TryGetValue(EquipSlotIds.Weapon, out string current) && current == id) return false;

            _profile.equipped[EquipSlotIds.Weapon] = id;
            Commit(id);
            return true;
        }

        private void Commit(string id)
        {
            _save?.Invoke();
            OnWeaponChanged?.Invoke(id);
        }
    }
}
