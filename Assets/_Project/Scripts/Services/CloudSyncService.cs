using System;
using ArenaSurvival.Meta;
using ArenaSurvival.Save;

namespace ArenaSurvival.Services
{
    /// <summary>Bản save trên mây: chuỗi save có chữ ký + tóm tắt.</summary>
    public class CloudSnapshot
    {
        public string envelope;
        public CloudSummary summary;
    }

    /// <summary>Kho lưu trên mây (UGS Cloud Save ở 12B; kho giả trong test). Callback chạy trên main thread.</summary>
    public interface ICloudStorage
    {
        bool IsReady { get; }
        /// <summary>Định danh tài khoản (để nhớ syncId theo từng tài khoản).</summary>
        string AccountId { get; }
        /// <summary>Gọi onReady khi kho sẵn sàng (ngay nếu đã sẵn sàng).</summary>
        void WhenReady(Action onReady);
        /// <summary>onDone(true, snapshot hoặc null nếu chưa có bản nào) / onDone(false, null) khi lỗi mạng.</summary>
        void Load(Action<bool, CloudSnapshot> onDone);
        void Save(CloudSnapshot snapshot, Action<bool> onDone);
    }

    /// <summary>
    /// Đồng bộ hồ sơ với Cloud Save. Lúc khởi động so bản máy với bản mây (CloudSaveRules.Decide); chưa quyết định thì không bao giờ
    /// tải lên — tránh đè mất bản mây bằng bản máy vừa cài lại. Sau đó tải lên tối đa 1 lần/60 s khi có lưu mới, và ngay khi app chuyển nền.
    /// Việc lấy bản mây (thay hồ sơ đang chạy) do Hub làm (CloudConflictPopup) để không xảy ra giữa màn chơi.
    /// </summary>
    public class CloudSyncService
    {
        public enum SyncState { Offline, Checking, Synced, PendingAdopt, NeedsChoice, Error }

        private readonly ProfileService _profiles;
        private readonly ICloudStorage _storage;
        private readonly ITimeSource _clock;
        private readonly Func<string, string> _readLastSyncId;
        private readonly Action<string, string> _writeLastSyncId;
        private bool _dirty = true;
        private bool _uploading;
        private float _lastUpload = float.NegativeInfinity;

        public SyncState State { get; private set; } = SyncState.Offline;
        /// <summary>Bản mây đang chờ lấy/chờ người chơi chọn.</summary>
        public CloudSnapshot Pending { get; private set; }
        public DateTime? LastSyncUtc { get; private set; }
        public event Action OnStateChanged;

        /// <param name="readLastSyncId">(accountId) → syncId đã đồng bộ lần cuối. Lưu ngoài hồ sơ (PlayerPrefs): là trạng thái của lần cài.</param>
        public CloudSyncService(ProfileService profiles, ICloudStorage storage, ITimeSource clock,
            Func<string, string> readLastSyncId, Action<string, string> writeLastSyncId)
        {
            _profiles = profiles;
            _storage = storage;
            _clock = clock;
            _readLastSyncId = readLastSyncId;
            _writeLastSyncId = writeLastSyncId;
            _profiles.OnSaved += () => _dirty = true;
        }

        public void Start() => _storage.WhenReady(Check);

        /// <summary>Gọi mỗi frame (ServicesHost) với thời gian thực, không bị timeScale.</summary>
        public void Tick(float realtime)
        {
            if (State == SyncState.Synced && !_uploading && CloudSaveRules.ShouldUpload(_dirty, realtime, _lastUpload)) Upload(realtime);
        }

        /// <summary>App chuyển nền/thoát: tải lên luôn nếu có thay đổi (bỏ qua giới hạn 60 s).</summary>
        public void Flush(float realtime)
        {
            if (State == SyncState.Synced && _dirty && !_uploading) Upload(realtime);
        }

        /// <summary>Người chơi chọn giữ bản máy: tải lên đè bản mây.</summary>
        public void KeepLocal(float realtime)
        {
            if (State != SyncState.NeedsChoice && State != SyncState.PendingAdopt) return;
            Pending = null;
            SetState(SyncState.Synced);
            _dirty = true;
            Upload(realtime);
        }

        /// <summary>
        /// Lấy bản mây (tự động khi máy rỗng, hoặc người chơi chọn): thay hồ sơ đang chạy, giữ quyền lợi đã trả tiền của bản máy, lưu.
        /// Trả false nếu bản mây không đọc được (khi đó giữ bản máy và tải lên).
        /// </summary>
        public bool AdoptCloud(float realtime)
        {
            if (Pending == null) return false;
            if (!SaveService.TryParseEnvelope(Pending.envelope, out PlayerProfile cloud, out bool tampered))
            {
                KeepLocal(realtime);
                return false;
            }
            cloud.tampered |= tampered;
            CloudSaveRules.MergePurchases(cloud, _profiles.Profile);
            _profiles.ReplaceWith(cloud);
            _writeLastSyncId(_storage.AccountId, Pending.summary != null ? Pending.summary.syncId : null);
            Pending = null;
            LastSyncUtc = _clock.UtcNow;
            SetState(SyncState.Synced);
            _profiles.Save();
            // Lưu xong thì OnSaved đánh dấu cần tải lên: bản mây sẽ nhận lại bản đã gộp quyền lợi ở lượt Tick sau.
            return true;
        }

        public CloudSummary LocalSummary() => CloudSummary.Of(_profiles.Profile, _clock.UtcNow);

        private void Check()
        {
            SetState(SyncState.Checking);
            _storage.Load((ok, snapshot) =>
            {
                if (!ok)
                {
                    SetState(SyncState.Error);
                    return;
                }
                CloudDecision decision = CloudSaveRules.Decide(LocalSummary(), snapshot?.summary, _readLastSyncId(_storage.AccountId));
                switch (decision)
                {
                    case CloudDecision.Upload:
                        SetState(SyncState.Synced);
                        _dirty = true;
                        break;
                    case CloudDecision.AdoptCloud:
                        Pending = snapshot;
                        SetState(SyncState.PendingAdopt);
                        break;
                    default:
                        Pending = snapshot;
                        SetState(SyncState.NeedsChoice);
                        break;
                }
            });
        }

        /// <summary>Lỗi lúc kiểm tra: thử lại (ServicesHost gọi khi có mạng lại / quay về từ nền).</summary>
        public void RetryIfFailed()
        {
            if (State == SyncState.Error && _storage.IsReady) Check();
        }

        private void Upload(float realtime)
        {
            _uploading = true;
            _lastUpload = realtime;
            _dirty = false;
            string syncId = Guid.NewGuid().ToString("N");
            var snapshot = new CloudSnapshot
            {
                envelope = SaveService.ToEnvelope(_profiles.Profile),
                summary = CloudSummary.Of(_profiles.Profile, _clock.UtcNow, syncId),
            };
            _storage.Save(snapshot, ok =>
            {
                _uploading = false;
                if (ok)
                {
                    _writeLastSyncId(_storage.AccountId, syncId);
                    LastSyncUtc = _clock.UtcNow;
                    OnStateChanged?.Invoke();
                }
                else _dirty = true; // thử lại ở lượt sau (vẫn giữ giới hạn 60 s)
            });
        }

        private void SetState(SyncState state)
        {
            State = state;
            OnStateChanged?.Invoke();
        }
    }
}
