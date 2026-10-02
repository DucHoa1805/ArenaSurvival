using System;
using System.Collections.Generic;
using System.IO;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;
using ArenaSurvival.Services;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    /// <summary>Kho mây giả: giữ 1 bản trong bộ nhớ, trả kết quả ngay (đồng bộ) để test đọc được tuần tự.</summary>
    public class MemoryCloudStorage : ICloudStorage
    {
        public CloudSnapshot Stored;
        public bool FailLoad;
        public int Saves;
        public bool IsReady => true;
        public string AccountId => "player1";
        public void WhenReady(Action onReady) => onReady();
        public void Load(Action<bool, CloudSnapshot> onDone) => onDone(!FailLoad, FailLoad ? null : Stored);
        public void Save(CloudSnapshot snapshot, Action<bool> onDone)
        {
            Stored = snapshot;
            Saves++;
            onDone(true);
        }
    }

    public class CloudSaveTests
    {
        private string _path;
        private ProfileService _profiles;
        private MemoryCloudStorage _storage;
        private FakeTime _time;
        private readonly Dictionary<string, string> _syncIds = new Dictionary<string, string>();

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), "arena-cloud-test-" + Guid.NewGuid().ToString("N") + ".json");
            _profiles = new ProfileService(new SaveService(_path));
            _profiles.Load();
            _storage = new MemoryCloudStorage();
            _time = new FakeTime();
            _syncIds.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (string p in new[] { _path, _path + ".bak", _path + ".tmp" }) if (File.Exists(p)) File.Delete(p);
        }

        private CloudSyncService NewSync() => new CloudSyncService(_profiles, _storage, _time,
            id => _syncIds.TryGetValue(id, out string s) ? s : null, (id, s) => _syncIds[id] = s);

        private static CloudSnapshot CloudWith(long xp, int stages, string syncId = "other")
        {
            var p = new PlayerProfile { accountXp = xp };
            for (int i = 0; i < stages; i++) p.stageStars["s" + i] = 3;
            p.currencies["gold"] = 777;
            return new CloudSnapshot
            {
                envelope = SaveService.ToEnvelope(p),
                summary = new CloudSummary { accountXp = xp, stagesCleared = stages, syncId = syncId },
            };
        }

        // ---------- Luật ----------

        [Test]
        public void Decide_AllBranches()
        {
            var empty = new CloudSummary();
            var progress = new CloudSummary { accountXp = 500, stagesCleared = 3 };
            var cloud = new CloudSummary { accountXp = 900, stagesCleared = 5, syncId = "abc" };
            Assert.AreEqual(CloudDecision.Upload, CloudSaveRules.Decide(progress, null, null), "chưa có bản mây");
            Assert.AreEqual(CloudDecision.Upload, CloudSaveRules.Decide(progress, cloud, "abc"), "bản mây do máy này tải lên");
            Assert.AreEqual(CloudDecision.AdoptCloud, CloudSaveRules.Decide(empty, cloud, null), "máy mới cài");
            Assert.AreEqual(CloudDecision.Upload, CloudSaveRules.Decide(progress, new CloudSummary { syncId = "x" }, null), "mây rỗng");
            Assert.AreEqual(CloudDecision.AskPlayer, CloudSaveRules.Decide(progress, cloud, "khac"), "cả hai có tiến độ");
        }

        [Test]
        public void MergePurchases_KeepsPaidEntitlements()
        {
            var chosen = new PlayerProfile();
            chosen.purchaseCounts["starter"] = 0;
            chosen.processedTransactions.Add("t1");
            var other = new PlayerProfile { removeAds = true };
            other.purchaseCounts["starter"] = 1;
            other.processedTransactions.Add("t1");
            other.processedTransactions.Add("t2");
            CloudSaveRules.MergePurchases(chosen, other);
            Assert.IsTrue(chosen.removeAds);
            Assert.AreEqual(1, chosen.purchaseCounts["starter"]);
            CollectionAssert.AreEquivalent(new[] { "t1", "t2" }, chosen.processedTransactions);
        }

        [Test]
        public void ShouldUpload_ThrottlesSixtySeconds()
        {
            Assert.IsFalse(CloudSaveRules.ShouldUpload(true, 30f, 0f));
            Assert.IsTrue(CloudSaveRules.ShouldUpload(true, 60f, 0f));
            Assert.IsFalse(CloudSaveRules.ShouldUpload(false, 999f, 0f));
        }

        // ---------- Save / hồ sơ ----------

        [Test]
        public void Envelope_RoundTripAndDetectsTampering()
        {
            var p = new PlayerProfile { accountXp = 42 };
            p.currencies["gem"] = 5;
            string env = SaveService.ToEnvelope(p);
            Assert.IsTrue(SaveService.TryParseEnvelope(env, out PlayerProfile back, out bool tampered));
            Assert.IsFalse(tampered);
            Assert.AreEqual(42, back.accountXp);
            Assert.AreEqual(5, back.currencies["gem"]);

            string hacked = env.Replace("\\\"gem\\\":5", "\\\"gem\\\":99999");
            Assert.AreNotEqual(env, hacked, "chuỗi thử phải thực sự bị sửa");
            Assert.IsTrue(SaveService.TryParseEnvelope(hacked, out _, out bool tampered2));
            Assert.IsTrue(tampered2);
            Assert.IsFalse(SaveService.TryParseEnvelope("rác", out _, out _));
        }

        [Test]
        public void ReplaceWith_KeepsSameObjectAndReplacesCollections()
        {
            PlayerProfile live = _profiles.Profile;
            live.currencies["gold"] = 10;
            live.stageStars["old"] = 1;
            var other = new PlayerProfile { accountXp = 300 };
            other.currencies["gem"] = 7;
            _profiles.ReplaceWith(other);
            Assert.AreSame(live, _profiles.Profile);
            Assert.AreEqual(300, live.accountXp);
            Assert.IsFalse(live.currencies.ContainsKey("gold"), "không gộp tiền của bản cũ");
            Assert.AreEqual(7, live.currencies["gem"]);
            Assert.IsFalse(live.stageStars.ContainsKey("old"));
        }

        // ---------- Đồng bộ ----------

        [Test]
        public void Sync_NoCloud_UploadsAfterTickAndRemembersSyncId()
        {
            var sync = NewSync();
            sync.Start();
            Assert.AreEqual(CloudSyncService.SyncState.Synced, sync.State);
            sync.Tick(100f);
            Assert.AreEqual(1, _storage.Saves);
            Assert.AreEqual(_storage.Stored.summary.syncId, _syncIds["player1"]);
            // Không có lưu mới → không tải lại; lưu mới nhưng chưa đủ 60 s → chưa tải.
            sync.Tick(200f);
            Assert.AreEqual(1, _storage.Saves);
            _profiles.Save();
            sync.Tick(130f);
            Assert.AreEqual(1, _storage.Saves);
            sync.Tick(161f);
            Assert.AreEqual(2, _storage.Saves);
        }

        [Test]
        public void Sync_FreshInstall_AdoptsCloudWithoutAsking()
        {
            _storage.Stored = CloudWith(900, 5);
            var sync = NewSync();
            sync.Start();
            Assert.AreEqual(CloudSyncService.SyncState.PendingAdopt, sync.State);
            Assert.IsTrue(sync.AdoptCloud(0f));
            Assert.AreEqual(900, _profiles.Profile.accountXp);
            Assert.AreEqual(777, _profiles.Profile.currencies["gold"]);
            Assert.AreEqual("other", _syncIds["player1"]);
            Assert.AreEqual(CloudSyncService.SyncState.Synced, sync.State);
        }

        [Test]
        public void Sync_BothHaveProgress_AsksAndNeverUploadsBeforeChoice()
        {
            _profiles.Profile.accountXp = 100;
            _profiles.Profile.stageStars["a"] = 1;
            _storage.Stored = CloudWith(900, 5);
            var sync = NewSync();
            sync.Start();
            Assert.AreEqual(CloudSyncService.SyncState.NeedsChoice, sync.State);
            sync.Tick(1000f);
            sync.Flush(1000f);
            Assert.AreEqual(0, _storage.Saves, "không đè bản mây khi người chơi chưa chọn");

            sync.KeepLocal(1000f);
            Assert.AreEqual(1, _storage.Saves);
            Assert.AreEqual(100, _storage.Stored.summary.accountXp);
        }

        [Test]
        public void Sync_AdoptKeepsLocalPurchases()
        {
            _profiles.Profile.accountXp = 100;
            _profiles.Profile.stageStars["a"] = 1;
            _profiles.Profile.removeAds = true;
            _storage.Stored = CloudWith(900, 5);
            var sync = NewSync();
            sync.Start();
            sync.AdoptCloud(0f);
            Assert.AreEqual(900, _profiles.Profile.accountXp);
            Assert.IsTrue(_profiles.Profile.removeAds, "Xóa quảng cáo đã mua trên máy không mất khi lấy bản mây");
        }

        [Test]
        public void Sync_LoadErrorThenRetry()
        {
            _storage.FailLoad = true;
            var sync = NewSync();
            sync.Start();
            Assert.AreEqual(CloudSyncService.SyncState.Error, sync.State);
            sync.Tick(500f);
            Assert.AreEqual(0, _storage.Saves);
            _storage.FailLoad = false;
            sync.RetryIfFailed();
            Assert.AreEqual(CloudSyncService.SyncState.Synced, sync.State);
        }
    }
}
