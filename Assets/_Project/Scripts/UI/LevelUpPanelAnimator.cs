using System.Collections;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Hoạt ảnh xuất hiện của bảng lên cấp: nền mờ dần hiện ra, tiêu đề nảy lên, hàng thẻ trượt lên
    /// và từng thẻ bật ra lần lượt; sau đó tiêu đề "thở" nhẹ trong lúc chờ người chơi chọn.
    ///
    /// Chạy bằng unscaledDeltaTime vì lúc này game đang dừng (timeScale = 0). Thẻ chỉ bấm được khi
    /// hoạt ảnh xong, tránh người chơi đang bấm liên tục (bắn/né) lỡ tay chọn nhầm.
    /// </summary>
    public class LevelUpPanelAnimator : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private CanvasGroup _content;
        [SerializeField] private RectTransform _title;
        [SerializeField] private RectTransform _cardRow;
        [SerializeField] private CanvasGroup[] _cards;

        [Header("Timing (giây)")]
        [SerializeField] private float _fadeDuration = 0.18f;
        [SerializeField] private float _titleDuration = 0.45f;
        [SerializeField] private float _cardDelay = 0.12f;
        [SerializeField] private float _cardStagger = 0.08f;
        [SerializeField] private float _cardDuration = 0.38f;
        [SerializeField] private float _rowSlideDistance = 90f;

        private Coroutine _running;
        private Vector2 _rowRestPosition;
        private bool _restCached;

        public void Play()
        {
            if (!_restCached)
            {
                _rowRestPosition = _cardRow.anchoredPosition;
                _restCached = true;
            }

            Stop();
            _running = StartCoroutine(Animate());
        }

        public void Stop()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
        }

        private IEnumerator Animate()
        {
            SetCardsInteractable(false);

            float end = _cardDelay + _cardStagger * (_cards.Length - 1) + _cardDuration;
            float t = 0f;
            while (t < end)
            {
                t += Time.unscaledDeltaTime;
                Apply(t);
                yield return null;
            }

            Apply(end);
            SetCardsInteractable(true);

            // Chờ chọn: tiêu đề phập phồng rất nhẹ cho bảng không bị "chết".
            while (true)
            {
                _title.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(Time.unscaledTime * 3.2f));
                yield return null;
            }
        }

        private void Apply(float t)
        {
            _content.alpha = Mathf.Clamp01(t / _fadeDuration);

            float titleT = Mathf.Clamp01(t / _titleDuration);
            _title.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, EaseOutBack(titleT));

            float rowT = Mathf.Clamp01((t - _cardDelay) / (_cardDuration + _cardStagger * (_cards.Length - 1)));
            _cardRow.anchoredPosition = _rowRestPosition + Vector2.down * (_rowSlideDistance * (1f - EaseOutCubic(rowT)));

            for (int i = 0; i < _cards.Length; i++)
            {
                float cardT = Mathf.Clamp01((t - _cardDelay - i * _cardStagger) / _cardDuration);
                _cards[i].alpha = EaseOutCubic(cardT);
                _cards[i].transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, EaseOutBack(cardT));
            }
        }

        private void SetCardsInteractable(bool value)
        {
            foreach (CanvasGroup card in _cards)
            {
                card.interactable = value;
                card.blocksRaycasts = value;
            }
        }

        // Vượt quá đích một chút rồi quay về: tạo cảm giác "bật" ra.
        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    }
}
