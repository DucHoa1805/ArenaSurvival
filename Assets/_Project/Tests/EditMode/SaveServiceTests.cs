using System.IO;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArenaSurvival.Tests
{
    public class SaveServiceTests
    {
        private string _directory;
        private string _path;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            // Thư mục tạm riêng cho mỗi test: không bao giờ đụng vào save thật của người chơi.
            _directory = Path.Combine(Path.GetTempPath(), "ArenaSurvivalSave_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "save.json");
            _service = new SaveService(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private static PlayerProfile Sample(long gold)
        {
            var p = new PlayerProfile { accountXp = 77 };
            p.currencies["gold"] = gold;
            p.stageStars["c1_s01"] = 3;
            return p;
        }

        [Test]
        public void Load_NoFile_ReturnsNewProfile()
        {
            SaveLoadResult r = _service.Load();
            Assert.AreEqual(SaveLoadStatus.New, r.Status);
            Assert.AreEqual(PlayerProfile.CurrentVersion, r.Profile.saveVersion);
            Assert.AreEqual(0, r.Profile.currencies.Count);
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllData()
        {
            Assert.IsTrue(_service.Save(Sample(500)));

            SaveLoadResult r = _service.Load();
            Assert.AreEqual(SaveLoadStatus.Loaded, r.Status);
            Assert.AreEqual(500, r.Profile.currencies["gold"]);
            Assert.AreEqual(3, r.Profile.stageStars["c1_s01"]);
            Assert.AreEqual(77, r.Profile.accountXp);
            Assert.IsFalse(r.Profile.tampered);
        }

        [Test]
        public void SecondSave_KeepsPreviousAsBackup()
        {
            _service.Save(Sample(100));
            _service.Save(Sample(200));

            Assert.IsTrue(File.Exists(_path + ".bak"));
            File.Delete(_path); // mất file chính: bản sao lưu phải là lần lưu trước
            File.Copy(_path + ".bak", _path);
            Assert.AreEqual(100, _service.Load().Profile.currencies["gold"]);
        }

        [Test]
        public void CorruptMain_WithGoodBackup_RestoresBackup()
        {
            _service.Save(Sample(100));
            _service.Save(Sample(200)); // .bak = 100
            File.WriteAllText(_path, "{ không phải json");

            LogAssert.ignoreFailingMessages = true;
            SaveLoadResult r = _service.Load();
            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, r.Status);
            Assert.AreEqual(100, r.Profile.currencies["gold"]);
        }

        [Test]
        public void CorruptMain_NoBackup_NewProfileAndKeepsCorruptCopy()
        {
            File.WriteAllText(_path, "\0\0rác");

            LogAssert.ignoreFailingMessages = true;
            SaveLoadResult r = _service.Load();
            Assert.AreEqual(SaveLoadStatus.Corrupt, r.Status);
            Assert.AreEqual(0, r.Profile.currencies.Count);
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "giữ file hỏng để hỗ trợ người chơi khôi phục");
        }

        [Test]
        public void TamperedMain_WithGoodBackup_UsesBackup()
        {
            _service.Save(Sample(100));
            _service.Save(Sample(500));
            File.WriteAllText(_path, File.ReadAllText(_path).Replace("500", "999999"));

            LogAssert.ignoreFailingMessages = true;
            SaveLoadResult r = _service.Load();
            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, r.Status);
            Assert.AreEqual(100, r.Profile.currencies["gold"]);
        }

        [Test]
        public void TamperedMain_NoBackup_KeepsDataButFlagsTampered()
        {
            _service.Save(Sample(500));
            File.WriteAllText(_path, File.ReadAllText(_path).Replace("500", "999999"));

            LogAssert.ignoreFailingMessages = true;
            SaveLoadResult r = _service.Load();
            Assert.AreEqual(SaveLoadStatus.Tampered, r.Status);
            // Không xóa tiến độ (có thể là lỗi ghi file của chính game), chỉ đánh dấu.
            Assert.AreEqual(999999, r.Profile.currencies["gold"]);
            Assert.IsTrue(r.Profile.tampered);
        }

        [Test]
        public void LegacyV1File_IsMigrated()
        {
            // Đúng định dạng save.json của bản cũ (SaveData: kỷ lục chế độ vô tận).
            File.WriteAllText(_path, "{\n  \"version\": 1,\n  \"bestSurvivalTime\": 255.5,\n  \"bestKillCount\": 245\n}");

            SaveLoadResult r = _service.Load();
            Assert.AreEqual(SaveLoadStatus.Migrated, r.Status);
            Assert.AreEqual(PlayerProfile.CurrentVersion, r.Profile.saveVersion);
            Assert.AreEqual(0, r.Profile.currencies.Count);
            Assert.IsFalse(r.Profile.tampered);
        }

        [Test]
        public void LeftoverTempFile_DoesNotAffectLoadOrSave()
        {
            _service.Save(Sample(300));
            File.WriteAllText(_path + ".tmp", "nửa chừng do tắt máy");

            Assert.AreEqual(300, _service.Load().Profile.currencies["gold"]);
            Assert.IsTrue(_service.Save(Sample(400)));
            Assert.AreEqual(400, _service.Load().Profile.currencies["gold"]);
        }

        [Test]
        public void SavedFile_ContainsSignature()
        {
            _service.Save(Sample(1));
            string text = File.ReadAllText(_path);
            StringAssert.Contains("\"hmac\"", text);
            StringAssert.Contains("\"payload\"", text);
        }
    }
}
