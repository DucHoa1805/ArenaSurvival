using System;
using ArenaSurvival.Meta;
using ArenaSurvival.Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>
    /// Xử lý kết quả so sánh Cloud Save ở Hub (12B): máy rỗng → tự lấy bản mây; cả hai có tiến độ → hiện 2 bản cho người chơi chọn.
    /// Chỉ làm ở Hub để không thay hồ sơ giữa màn. Object luôn bật (để nghe sự kiện), phần hiển thị là con _content.
    /// </summary>
    public class CloudConflictPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _content;
        [SerializeField] private TMP_Text _localText;
        [SerializeField] private TMP_Text _cloudText;
        [SerializeField] private Button _keepLocalButton;
        [SerializeField] private Button _useCloudButton;
        [SerializeField] private HubToast _toast;

        private Action _onStateChanged;

        private void Awake()
        {
            _onStateChanged = Handle;
            _keepLocalButton.onClick.AddListener(KeepLocal);
            _useCloudButton.onClick.AddListener(UseCloud);
            _content.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameServices.Cloud == null) return;
            GameServices.Cloud.OnStateChanged += _onStateChanged;
            Handle();
        }

        private void OnDisable()
        {
            if (GameServices.Cloud != null) GameServices.Cloud.OnStateChanged -= _onStateChanged;
        }

        private void Handle()
        {
            CloudSyncService cloud = GameServices.Cloud;
            if (cloud.State == CloudSyncService.SyncState.PendingAdopt)
            {
                if (cloud.AdoptCloud(Time.realtimeSinceStartup)) Reload("Đã tải tiến độ từ đám mây");
                return;
            }
            if (cloud.State != CloudSyncService.SyncState.NeedsChoice || _content.activeSelf) return;
            _localText.text = Describe(cloud.LocalSummary());
            _cloudText.text = Describe(cloud.Pending?.summary);
            _content.SetActive(true);
        }

        private void KeepLocal()
        {
            GameServices.Cloud.KeepLocal(Time.realtimeSinceStartup);
            _content.SetActive(false);
            _toast.Show("Đã giữ bản trên máy");
        }

        private void UseCloud()
        {
            _content.SetActive(false);
            if (GameServices.Cloud.AdoptCloud(Time.realtimeSinceStartup)) Reload("Đã dùng bản trên đám mây");
            else _toast.Show("Bản đám mây bị hỏng, giữ bản trên máy");
        }

        // Nạp lại scene để mọi bảng đọc lại hồ sơ mới (như "Xóa hồ sơ" ở DevMenu). Thông báo qua static vì Hub được tạo lại.
        private static string _pendingToast;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _pendingToast = null;

        private void Start()
        {
            if (_pendingToast == null) return;
            _toast.Show(_pendingToast);
            _pendingToast = null;
        }

        private static void Reload(string toast)
        {
            _pendingToast = toast;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static string Describe(CloudSummary s)
        {
            if (s == null) return "Không đọc được";
            int level = AccountRules.FromXp(s.accountXp).level;
            string when = s.savedAtUtcTicks > 0
                ? new DateTime(s.savedAtUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM HH:mm")
                : "?";
            return $"Cấp {level}\n{s.stagesCleared} màn đã qua\nLưu lúc {when}";
        }
    }
}
