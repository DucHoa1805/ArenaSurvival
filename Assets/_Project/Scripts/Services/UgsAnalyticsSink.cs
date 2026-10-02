using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UnityConsent;
using UgsAnalytics = Unity.Services.Analytics.AnalyticsService;

namespace ArenaSurvival.Services
{
    /// <summary>
    /// Gửi sự kiện lên UGS Analytics (12B). SDK bỏ im lặng mọi sự kiện ghi khi nó chưa được bật bởi trạng thái đồng ý "Granted", và việc bật
    /// diễn ra khi SDK nhận sự kiện EndUserConsent.consentStateChanged — có thể sau lời gọi SetConsentState. Vì vậy sự kiện đầu phiên
    /// (session_start) được xếp hàng tới khi chắc chắn SDK đã bật (tối đa 64). Sự kiện tùy chỉnh phải khai báo trên dashboard (Docs/ugs-setup.md).
    /// </summary>
    public class UgsAnalyticsSink : IAnalyticsSink
    {
        private const int MaxQueue = 64;
        private readonly Queue<KeyValuePair<string, Dictionary<string, object>>> _queue =
            new Queue<KeyValuePair<string, Dictionary<string, object>>>();
        private bool _ready;
        private bool _granted;
        // SDK đã nhận "Granted" (ghi được sự kiện).
        private bool _active;
        private bool _listening;

        public UgsAnalyticsSink(bool consentGranted)
        {
            _granted = consentGranted;
            UgsBootstrap.WhenReady(() =>
            {
                _ready = true;
                SetConsent(_granted);
            });
        }

        public void Send(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (_active)
            {
                Record(eventName, parameters);
                return;
            }
            if (_queue.Count >= MaxQueue) return;
            var copy = new Dictionary<string, object>();
            if (parameters != null) foreach (var kv in parameters) copy[kv.Key] = kv.Value;
            _queue.Enqueue(new KeyValuePair<string, Dictionary<string, object>>(eventName, copy));
        }

        /// <summary>Người chơi đổi ý trong Cài đặt. Gọi trước khi UGS sẵn sàng thì nhớ lại và áp khi sẵn sàng.</summary>
        public void SetConsent(bool granted)
        {
            _granted = granted;
            if (!_ready) return;
            try
            {
                ConsentState state = EndUserConsent.GetConsentState();
                ConsentStatus wanted = granted ? ConsentStatus.Granted : ConsentStatus.Denied;
                if (state.AnalyticsIntent == wanted)
                {
                    // Trạng thái đã lưu từ phiên trước: SDK tự bật/tắt theo nó lúc khởi tạo, không có sự kiện đổi nào nữa.
                    OnConsentApplied(wanted);
                    return;
                }
                if (!_listening)
                {
                    // Đăng ký SAU SDK (SDK đăng ký lúc UnityServices khởi tạo) nên khi tới lượt ta, SDK đã bật xong.
                    EndUserConsent.consentStateChanged += s => OnConsentApplied(s.AnalyticsIntent);
                    _listening = true;
                }
                state.AnalyticsIntent = wanted;
                EndUserConsent.SetConsentState(state);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGS] Không đặt được trạng thái đồng ý analytics: {e.Message}");
            }
        }

        private void OnConsentApplied(ConsentStatus status)
        {
            _active = status == ConsentStatus.Granted;
            if (!_active)
            {
                _queue.Clear();
                return;
            }
            if (_queue.Count > 0) Debug.Log($"[UGS] Analytics đã bật, gửi {_queue.Count} sự kiện đang chờ.");
            while (_queue.Count > 0)
            {
                var e = _queue.Dequeue();
                Record(e.Key, e.Value);
            }
        }

        private static void Record(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            try
            {
                var e = new Unity.Services.Analytics.CustomEvent(eventName);
                if (parameters != null) foreach (var kv in parameters) e.Add(kv.Key, kv.Value);
                UgsAnalytics.Instance.RecordEvent(e);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGS] Ghi sự kiện {eventName} lỗi: {ex.Message}");
            }
        }
    }
}
