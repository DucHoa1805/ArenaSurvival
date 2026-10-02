using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.Services
{
    /// <summary>
    /// Quảng cáo giả lập (11A): lớp phủ toàn màn hình đếm ngược rồi trao thưởng; bấm "Đóng" trước khi hết giờ = không thưởng,
    /// giống quảng cáo thật. Chạy bằng thời gian thực vì quảng cáo hồi sinh hiện lúc game đang dừng. Tự dựng UI bằng code để
    /// không phải thêm prefab vào mọi scene; 11B thay bằng LevelPlay.
    /// </summary>
    public class FakeAdProvider : MonoBehaviour, IAdProvider
    {
        /// <summary>Thời lượng quảng cáo giả (giây thực). Bot tự chơi đặt 0.</summary>
        public static float Duration = 3f;

        private GameObject _overlay;
        private TMP_Text _label;
        private Button _close;
        private Action<bool> _onComplete;
        private float _remaining;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Duration = 3f;

        public bool IsReady => true;

        public static FakeAdProvider Create()
        {
            var go = new GameObject("FakeAdProvider");
            DontDestroyOnLoad(go);
            return go.AddComponent<FakeAdProvider>();
        }

        public void SetConsent(bool personalized) => Debug.Log($"[FakeAd] consent personalized = {personalized}");

        public void Show(string placement, Action<bool> onComplete)
        {
            if (Duration <= 0f) { onComplete?.Invoke(true); return; }
            if (_overlay == null) BuildOverlay();
            _onComplete = onComplete;
            _remaining = Duration;
            _overlay.SetActive(true);
            Refresh(placement);
        }

        private void Update()
        {
            if (_overlay == null || !_overlay.activeSelf) return;
            _remaining -= Time.unscaledDeltaTime;
            Refresh(null);
            if (_remaining <= 0f) Finish(true);
        }

        private void Refresh(string placement)
        {
            _label.text = $"QUẢNG CÁO (giả lập)\n<size=60%>Nhận thưởng sau {Mathf.CeilToInt(Mathf.Max(0f, _remaining))} giây</size>";
        }

        private void Finish(bool rewarded)
        {
            _overlay.SetActive(false);
            Action<bool> cb = _onComplete;
            _onComplete = null;
            cb?.Invoke(rewarded);
        }

        private void BuildOverlay()
        {
            _overlay = new GameObject("FakeAdOverlay");
            _overlay.transform.SetParent(transform, false);
            var canvas = _overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = _overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            _overlay.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Bg").AddComponent<Image>();
            bg.transform.SetParent(_overlay.transform, false);
            bg.color = new Color(0.05f, 0.06f, 0.1f, 0.96f);
            Stretch(bg.rectTransform);

            _label = new GameObject("Label").AddComponent<TextMeshProUGUI>();
            _label.transform.SetParent(_overlay.transform, false);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 56;
            _label.color = Color.white;
            Stretch(_label.rectTransform);

            _close = new GameObject("Close").AddComponent<Button>();
            _close.transform.SetParent(_overlay.transform, false);
            var img = _close.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);
            _close.targetGraphic = img;
            var rt = _close.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -24f);
            rt.sizeDelta = new Vector2(150f, 60f);
            var closeLabel = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            closeLabel.transform.SetParent(_close.transform, false);
            closeLabel.text = "Đóng";
            closeLabel.fontSize = 30;
            closeLabel.alignment = TextAlignmentOptions.Center;
            Stretch(closeLabel.rectTransform);
            _close.onClick.AddListener(() => Finish(false));
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Bản phát hành khi chưa gắn SDK quảng cáo (trước 11B): không bao giờ sẵn sàng.</summary>
    public class UnavailableAdProvider : IAdProvider
    {
        public bool IsReady => false;
        public void Show(string placement, Action<bool> onComplete) => onComplete?.Invoke(false);
        public void SetConsent(bool personalized) { }
    }
}
