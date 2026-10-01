using System.Collections.Generic;
using System.IO;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;
using ArenaSurvival.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class WeaponServiceTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private WeaponService _weapons;
        private ItemDatabase _db;
        private WeaponData _pistol;
        private WeaponData _shotgun;
        private int _saves;

        private static string ShotgunShards => WeaponRules.ShardId("weapon_shotgun");

        [SetUp]
        public void SetUp()
        {
            _pistol = ScriptableObject.CreateInstance<WeaponData>(); _pistol.id = WeaponService.DefaultWeaponId; _pistol.damage = 5;
            _shotgun = ScriptableObject.CreateInstance<WeaponData>(); _shotgun.id = "weapon_shotgun"; _shotgun.damage = 2.5f; _shotgun.projectileCount = 6;
            _db = ScriptableObject.CreateInstance<ItemDatabase>();
            _db.weapons = new[] { _pistol, _shotgun };

            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _saves = 0;
            _weapons = new WeaponService(_profile, _economy, _db, () => _saves++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pistol);
            Object.DestroyImmediate(_shotgun);
            Object.DestroyImmediate(_db);
        }

        [Test]
        public void NewProfile_OwnsAndEquipsPistol()
        {
            Assert.IsTrue(_weapons.IsOwned(WeaponService.DefaultWeaponId));
            Assert.AreEqual(1, _weapons.GetStars(WeaponService.DefaultWeaponId));
            Assert.AreEqual(1, _weapons.GetLevel(WeaponService.DefaultWeaponId));
            Assert.AreEqual(WeaponService.DefaultWeaponId, _weapons.EquippedId);
            Assert.AreSame(_pistol, _weapons.EquippedData);
            Assert.IsFalse(_weapons.IsOwned("weapon_shotgun"));
        }

        [Test]
        public void TryUnlock_NeedsTenShards()
        {
            _economy.Add(ShotgunShards, 9);
            Assert.IsFalse(_weapons.TryUnlock("weapon_shotgun"));
            Assert.AreEqual(0, _saves);

            _economy.Add(ShotgunShards, 1);
            string changed = null;
            _weapons.OnWeaponChanged += id => changed = id;
            Assert.IsTrue(_weapons.TryUnlock("weapon_shotgun"));
            Assert.IsTrue(_weapons.IsOwned("weapon_shotgun"));
            Assert.AreEqual(1, _weapons.GetStars("weapon_shotgun"));
            Assert.AreEqual(0, _weapons.GetShards("weapon_shotgun"));
            Assert.AreEqual(1, _saves);
            Assert.AreEqual("weapon_shotgun", changed);
            Assert.IsFalse(_weapons.TryUnlock("weapon_shotgun"), "đã sở hữu thì không mở khóa lại");
        }

        [Test]
        public void TryStarUp_SpendsShardsUntilFiveStars()
        {
            _economy.Add(WeaponRules.ShardId(WeaponService.DefaultWeaponId), 1000);
            for (int stars = 2; stars <= WeaponRules.MaxStars; stars++)
            {
                Assert.IsTrue(_weapons.TryStarUp(WeaponService.DefaultWeaponId));
                Assert.AreEqual(stars, _weapons.GetStars(WeaponService.DefaultWeaponId));
            }
            Assert.IsFalse(_weapons.TryStarUp(WeaponService.DefaultWeaponId), "5★ là tối đa");
            Assert.AreEqual(1000 - (20 + 40 + 80 + 150), _weapons.GetShards(WeaponService.DefaultWeaponId));
        }

        [Test]
        public void TryStarUp_NotOwned_Refused()
        {
            _economy.Add(ShotgunShards, 100);
            Assert.IsFalse(_weapons.TryStarUp("weapon_shotgun"), "chưa mở khóa thì phải TryUnlock trước");
        }

        [Test]
        public void TryUpgrade_SpendsGoldAndOre_StopsAtCap()
        {
            _economy.Add(CurrencyIds.Gold, 100000);
            _economy.Add(CurrencyIds.Ore, 1000);
            for (int i = 1; i < WeaponRules.LevelCap(1); i++) Assert.IsTrue(_weapons.TryUpgrade(WeaponService.DefaultWeaponId));
            Assert.AreEqual(10, _weapons.GetLevel(WeaponService.DefaultWeaponId));
            Assert.IsFalse(_weapons.TryUpgrade(WeaponService.DefaultWeaponId), "1★ chặn ở cấp 10");
        }

        [Test]
        public void TryUpgrade_MissingOre_ChangesNothing()
        {
            _economy.Add(CurrencyIds.Gold, 1000);
            Assert.IsFalse(_weapons.TryUpgrade(WeaponService.DefaultWeaponId));
            Assert.AreEqual(1, _weapons.GetLevel(WeaponService.DefaultWeaponId));
            Assert.AreEqual(1000, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(0, _saves);
        }

        [Test]
        public void Equip_OnlyOwnedWeapons()
        {
            Assert.IsFalse(_weapons.Equip("weapon_shotgun"));
            _economy.Add(ShotgunShards, 10);
            _weapons.TryUnlock("weapon_shotgun");
            Assert.IsTrue(_weapons.Equip("weapon_shotgun"));
            Assert.AreEqual("weapon_shotgun", _weapons.EquippedId);
            Assert.AreEqual("weapon_shotgun", _profile.equipped[EquipSlotIds.Weapon]);
        }

        [Test]
        public void EquippedId_InvalidInSave_FallsBackToPistol()
        {
            _profile.equipped[EquipSlotIds.Weapon] = "weapon_removed";
            Assert.AreEqual(WeaponService.DefaultWeaponId, _weapons.EquippedId);
            Assert.AreSame(_pistol, _weapons.EquippedData);
        }

        [Test]
        public void GetEffective_UsesLevelAndStars()
        {
            _economy.Add(CurrencyIds.Gold, 100000);
            _economy.Add(CurrencyIds.Ore, 1000);
            _weapons.TryUpgrade(WeaponService.DefaultWeaponId);
            Assert.AreEqual(WeaponRules.Effective(_pistol, 2, 1).damage, _weapons.GetEffective(WeaponService.DefaultWeaponId).damage, 1e-4);
        }

        [Test]
        public void Costs_ListExpectedCurrencies()
        {
            var cost = new List<CurrencyCost>();
            Assert.IsTrue(_weapons.GetUpgradeCost(WeaponService.DefaultWeaponId, cost));
            Assert.AreEqual(CurrencyIds.Gold, cost[0].id);
            Assert.AreEqual(CurrencyIds.Ore, cost[1].id);

            Assert.IsTrue(_weapons.GetStarCost("weapon_shotgun", cost), "chưa sở hữu: chi phí mở khóa (lên 1★)");
            Assert.AreEqual(ShotgunShards, cost[0].id);
            Assert.AreEqual(10, cost[0].amount);
        }
    }

    public class SaveMigrationV5Tests
    {
        [Test]
        public void V4File_GetsPistolOwnedAndEquipped()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ArenaSurvivalMig_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "save.json");
            try
            {
                // Đúng định dạng file của Phase 8B (save v4, có kho đồ, chưa có vũ khí).
                string payload = "{\"saveVersion\":4,\"accountXp\":40,\"currencies\":{\"gold\":200,\"ore\":7},\"stageStars\":{}," +
                                 "\"talents\":{},\"items\":[{\"uid\":\"i1\",\"id\":\"armor_leather\",\"rarity\":1,\"level\":3,\"ascension\":0}]," +
                                 "\"equipped\":{\"armor\":\"i1\"},\"nextItemUid\":2,\"tampered\":false}";
                string json = "{\"format\":2,\"payload\":" + Newtonsoft.Json.JsonConvert.ToString(payload) +
                              ",\"hmac\":\"" + SaveSecurity.Sign(payload) + "\"}";
                File.WriteAllText(path, json);

                SaveLoadResult r = new SaveService(path).Load();

                Assert.AreEqual(SaveLoadStatus.Migrated, r.Status);
                Assert.AreEqual(5, r.Profile.saveVersion);
                Assert.AreEqual(7, r.Profile.currencies["ore"], "dữ liệu cũ giữ nguyên");
                Assert.AreEqual("i1", r.Profile.equipped[EquipSlotIds.Armor]);
                Assert.AreEqual(1, r.Profile.weapons[WeaponService.DefaultWeaponId].stars);
                Assert.AreEqual(1, r.Profile.weapons[WeaponService.DefaultWeaponId].level);
                Assert.AreEqual(WeaponService.DefaultWeaponId, r.Profile.equipped[EquipSlotIds.Weapon]);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ProfileWithoutWeapons_IsGivenPistolOnLoad()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ArenaSurvivalWpn_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "save.json");
            try
            {
                var profile = new PlayerProfile();
                profile.weapons.Clear();
                var service = new SaveService(path);
                Assert.IsTrue(service.Save(profile));

                PlayerProfile loaded = service.Load().Profile;
                Assert.IsTrue(loaded.weapons.ContainsKey(WeaponService.DefaultWeaponId), "không bao giờ vào trận tay không");
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
