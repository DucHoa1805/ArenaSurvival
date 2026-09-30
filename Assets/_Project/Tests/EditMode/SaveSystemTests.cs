using System.IO;
using System.Text.RegularExpressions;
using ArenaSurvival.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArenaSurvival.Tests
{
    /// <summary>
    /// Ghi vào 1 thư mục tạm riêng cho mỗi test — không bao giờ đụng tới file save thật của người chơi.
    /// </summary>
    public class SaveSystemTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ArenaSurvivalTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void Load_MissingFile_ReturnsEmptyData()
        {
            SaveData data = SaveSystem.Load(Path.Combine(_directory, "missing.json"));

            Assert.AreEqual(0f, data.bestSurvivalTime);
            Assert.AreEqual(0, data.bestKillCount);
        }

        [Test]
        public void SaveThenLoad_ReturnsSameValues()
        {
            string path = Path.Combine(_directory, "save.json");
            var original = new SaveData { bestSurvivalTime = 123.5f, bestKillCount = 42 };

            bool saved = SaveSystem.Save(original, path);
            SaveData loaded = SaveSystem.Load(path);

            Assert.IsTrue(saved);
            Assert.AreEqual(original.bestSurvivalTime, loaded.bestSurvivalTime);
            Assert.AreEqual(original.bestKillCount, loaded.bestKillCount);
            Assert.AreEqual(original.version, loaded.version);
        }

        [Test]
        public void Save_OverwritesPreviousFile()
        {
            string path = Path.Combine(_directory, "save.json");
            SaveSystem.Save(new SaveData { bestSurvivalTime = 10f, bestKillCount = 1 }, path);
            SaveSystem.Save(new SaveData { bestSurvivalTime = 99f, bestKillCount = 7 }, path);

            SaveData loaded = SaveSystem.Load(path);

            Assert.AreEqual(99f, loaded.bestSurvivalTime);
            Assert.AreEqual(7, loaded.bestKillCount);
        }

        [Test]
        public void Load_CorruptFile_ReturnsEmptyDataAndWarns()
        {
            string path = Path.Combine(_directory, "corrupt.json");
            File.WriteAllText(path, "{ this is not json ");

            LogAssert.Expect(LogType.Warning, new Regex("Không đọc được file save"));
            SaveData data = SaveSystem.Load(path);

            Assert.AreEqual(0f, data.bestSurvivalTime);
            Assert.AreEqual(0, data.bestKillCount);
        }

        [Test]
        public void Save_IntoMissingDirectory_ReturnsFalseAndWarns()
        {
            string path = Path.Combine(_directory, "no_such_folder", "save.json");

            LogAssert.Expect(LogType.Warning, new Regex("Không ghi được file save"));
            bool saved = SaveSystem.Save(new SaveData(), path);

            Assert.IsFalse(saved);
        }
    }
}
