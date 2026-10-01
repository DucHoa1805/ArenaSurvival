using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>Thông báo ngắn ("Mở ở cấp 2", "Sắp ra mắt") hiện vài giây rồi mờ dần. Không chặn bấm.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HubToast : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _visibleSeconds = 1.6f;
        [SerializeField] private float _fadeSeconds = 0.35f;

        private CanvasGroup _group;
        private float _timer = -1f;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        public void Show(string message)
        {
            _text.text = message;
            _timer = 0f;
            _group.alpha = 1f;
        }

        private void Update()
        {
            if (_timer < 0f) return;
            _timer += Time.unscaledDeltaTime;
            if (_timer < _visibleSeconds) return;

            float t = (_timer - _visibleSeconds) / _fadeSeconds;
            _group.alpha = 1f - Mathf.Clamp01(t);
            if (t >= 1f) _timer = -1f;
        }
    }
}
