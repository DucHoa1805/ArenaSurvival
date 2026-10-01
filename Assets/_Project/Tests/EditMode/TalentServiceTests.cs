using System.IO;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;
using ArenaSurvival.Stats;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class TalentServiceTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private TalentService _talents;
        private TalentData _hp;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile();
            _economy = new EconomyService(_profile, null);
            _saves = 0;
            _talents = new TalentService(_profile, _economy, () => _saves++);
            _hp = ScriptableObject.CreateInstance<TalentData>();
            _hp.id = "talent_hp"; _hp.stat = StatKind.MaxHealth; _hp.valuePerLevel = 10;
            _hp.maxLevel = 3; _hp.baseCost = 100; _hp.costGrowth = 1.12f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_hp);

        [Test]
        public void NewProfile_LevelZero_CostIsBase()
        {
            Assert.AreEqual(0, _talents.GetLevel("talent_hp"));
            Assert.AreEqual(100, _talents.GetCost(_hp));
        }

        [Test]
        public void TryUpgrade_WithEnoughGold_SpendsLevelsUpSaves()
        {
            _economy.Add(CurrencyIds.Gold, 250);
            string changedId = null; int changedLevel = -1;
            _talents.OnTalentChanged += (id, lv) => { changedId = id; changedLevel = lv; };

            Assert.IsTrue(_talents.TryUpgrade(_hp));
            Assert.AreEqual(1, _talents.GetLevel("talent_hp"));
            Assert.AreEqual(150, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(112, _talents.GetCost(_hp), "giá cấp kế tăng 1.12 lần");
            Assert.AreEqual(1, _saves);
            Assert.AreEqual("talent_hp", changedId);
            Assert.AreEqual(1, changedLevel);
        }

        [Test]
        public void TryUpgrade_NotEnoughGold_ChangesNothing()
        {
            _economy.Add(CurrencyIds.Gold, 99);
            Assert.IsFalse(_talents.CanUpgrade(_hp));
            Assert.IsFalse(_talents.TryUpgrade(_hp));
            Assert.AreEqual(0, _talents.GetLevel("talent_hp"));
            Assert.AreEqual(99, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(0, _saves);
        }

        [Test]
        public void TryUpgrade_AtMaxLevel_Refused()
        {
            _economy.Add(CurrencyIds.Gold, 10000);
            for (int i = 0; i < 3; i++) Assert.IsTrue(_talents.TryUpgrade(_hp));
            long goldAtMax = _economy.GetBalance(CurrencyIds.Gold);

            Assert.AreEqual(-1, _talents.GetCost(_hp));
            Assert.IsFalse(_talents.CanUpgrade(_hp));
            Assert.IsFalse(_talents.TryUpgrade(_hp));
            Assert.AreEqual(3, _talents.GetLevel("talent_hp"));
            Assert.AreEqual(goldAtMax, _economy.GetBalance(CurrencyIds.Gold));
        }

        [Test]
        public void TryUpgrade_NullTalent_Refused()
        {
            Assert.IsFalse(_talents.TryUpgrade(null));
        }
    }

    public class SaveMigrationV3Tests
    {
        [Test]
        public void V2File_IsMigratedToV3WithTalents()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ArenaSurvivalMig_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "save.json");
            try
            {
                // Đúng định dạng file của Phase 7 (save v2, có chữ ký hợp lệ, chưa có "talents").
                string payload = "{\"saveVersion\":2,\"accountXp\":40,\"currencies\":{\"gold\":200},\"stageStars\":{\"c1_s01\":3},\"tampered\":false}";
                string json = "{\"format\":2,\"payload\":" + Newtonsoft.Json.JsonConvert.ToString(payload) +
                              ",\"hmac\":\"" + SaveSecurity.Sign(payload) + "\"}";
                File.WriteAllText(path, json);

                SaveLoadResult r = new SaveService(path).Load();

                Assert.AreEqual(SaveLoadStatus.Migrated, r.Status);
                Assert.AreEqual(PlayerProfile.CurrentVersion, r.Profile.saveVersion);
                Assert.AreEqual(200, r.Profile.currencies["gold"], "dữ liệu cũ giữ nguyên");
                Assert.AreEqual(3, r.Profile.stageStars["c1_s01"]);
                Assert.IsNotNull(r.Profile.talents);
                Assert.AreEqual(0, r.Profile.talents.Count);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
