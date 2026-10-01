using System.IO;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class ProfileServiceTests
    {
        private string _directory;
        private ProfileService _profiles;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ArenaSurvivalProfile_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _profiles = new ProfileService(new SaveService(Path.Combine(_directory, "save.json")));
            _profiles.Load();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void ResetToNew_ClearsDataInPlaceAndPersists()
        {
            PlayerProfile same = _profiles.Profile;
            var economy = new EconomyService(same, _profiles.MarkDirty);
            economy.Add(CurrencyIds.Gold, 500);
            same.stageStars["c1_s01"] = 3;
            same.accountXp = 99;
            same.tampered = true;
            _profiles.Save();

            _profiles.ResetToNew();

            // Cùng object: các dịch vụ đang giữ tham chiếu (EconomyService...) thấy ngay hồ sơ mới.
            Assert.AreSame(same, _profiles.Profile);
            Assert.AreEqual(0, economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(0, same.stageStars.Count);
            Assert.AreEqual(0, same.accountXp);
            Assert.IsFalse(same.tampered);

            // Đã ghi xuống đĩa: mở lại vẫn là hồ sơ trống.
            var reopened = new ProfileService(new SaveService(Path.Combine(_directory, "save.json")));
            reopened.Load();
            Assert.AreEqual(0, reopened.Profile.currencies.Count);
            Assert.AreEqual(0, reopened.Profile.stageStars.Count);
        }

        [Test]
        public void SaveIfDirty_OnlyWritesWhenChanged()
        {
            string path = Path.Combine(_directory, "save.json");
            _profiles.SaveIfDirty();
            Assert.IsFalse(File.Exists(path), "chưa đổi gì thì không ghi");

            _profiles.MarkDirty();
            _profiles.SaveIfDirty();
            Assert.IsTrue(File.Exists(path));
        }
    }
}
