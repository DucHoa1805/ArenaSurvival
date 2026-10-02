using System;
using System.Globalization;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Networking;

namespace ArenaSurvival.Services
{
    /// <summary>
    /// Khởi tạo Unity Gaming Services + đăng nhập ẩn danh + lấy giờ máy chủ (12B). Mọi adapter UGS chờ ở WhenReady.
    /// Lỗi/mất mạng không chặn game: dịch vụ cục bộ vẫn chạy, ServicesHost gọi RetryIfFailed định kỳ và khi app quay lại từ nền.
    /// </summary>
    public static class UgsBootstrap
    {
        public enum Status { Off, Initializing, Ready, Failed }

        // Header Date của máy chủ Unity: yêu cầu HEAD rất nhẹ, không cần dịch vụ riêng để biết giờ thật.
        private const string TimeUrl = "https://player-auth.services.api.unity.com/";
        private const float RetryInterval = 60f;

        public static Status State { get; private set; } = Status.Off;
        public static string Environment { get; private set; }
        public static string PlayerId { get; private set; }
        public static string LastError { get; private set; }
        /// <summary>Độ lệch giờ máy chủ − giờ máy (null nếu chưa lấy được).</summary>
        public static TimeSpan? ServerOffset { get; private set; }

        private static event Action Ready;
        private static float _lastAttempt = float.NegativeInfinity;
        private static GameClock _clock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            State = Status.Off;
            Environment = null;
            PlayerId = null;
            LastError = null;
            ServerOffset = null;
            Ready = null;
            _lastAttempt = float.NegativeInfinity;
            _clock = null;
        }

        public static void Start(string environment, GameClock clock)
        {
            if (State == Status.Initializing || State == Status.Ready) return;
            Environment = environment;
            _clock = clock;
            Run();
        }

        public static void WhenReady(Action action)
        {
            if (action == null) return;
            if (State == Status.Ready) action();
            else Ready += action;
        }

        /// <summary>Thử lại sau lỗi, tối đa 1 lần/60 s.</summary>
        public static void RetryIfFailed(float realtime)
        {
            if (State != Status.Failed || realtime - _lastAttempt < RetryInterval) return;
            Run();
        }

        private static async void Run()
        {
            State = Status.Initializing;
            _lastAttempt = Time.realtimeSinceStartup;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(Environment));
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                PlayerId = AuthenticationService.Instance.PlayerId;
                State = Status.Ready;
                LastError = null;
                Debug.Log($"[UGS] Sẵn sàng (môi trường {Environment}, người chơi {PlayerId}).");
                await SyncServerTime();
                Action callbacks = Ready;
                Ready = null;
                callbacks?.Invoke();
            }
            catch (Exception e)
            {
                // Mất mạng là chuyện thường trên điện thoại: chỉ cảnh báo, không làm phiền người chơi.
                State = Status.Failed;
                LastError = e.Message;
                Debug.LogWarning($"[UGS] Chưa kết nối được: {e.Message}");
            }
        }

        private static async Task SyncServerTime()
        {
            using (UnityWebRequest request = UnityWebRequest.Head(TimeUrl))
            {
                request.timeout = 10;
                var done = new TaskCompletionSource<bool>();
                request.SendWebRequest().completed += _ => done.TrySetResult(true);
                await done.Task;
                string header = request.GetResponseHeader("Date");
                if (string.IsNullOrEmpty(header) ||
                    !DateTime.TryParse(header, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime server))
                {
                    Debug.LogWarning("[UGS] Không lấy được giờ máy chủ; dùng giờ máy (vẫn chặn chỉnh lùi).");
                    return;
                }
                ServerOffset = server - DateTime.UtcNow;
                _clock?.SetServerOffset(ServerOffset.Value);
            }
        }
    }
}
