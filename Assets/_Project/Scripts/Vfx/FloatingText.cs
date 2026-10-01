using System;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.Vfx
{
    /// <summary>
    /// Dòng chữ nổi trong thế giới game ("CẤP 5!" trên đầu Player, số sát thương trên đầu quái):
    /// bật to ra, bay lên rồi mờ dần. Không tự tạo/hủy: hoặc dùng lại 1 object, hoặc lấy từ pool.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private float _duration = 1.2f;
        [SerializeField] private float _riseDistance = 1.3f;
        [SerializeField] private float _popScale = 1.35f;

        private TextMeshPro _text;
        private Color _baseColor;
        private Vector3 _start;
        private float _elapsed;

        // Giống PooledVfx: không biết gì về pool, chỉ gọi callback khi xong. Để trống thì tự tắt
        // (trường hợp chữ "CẤP n!" chỉ có 1 object dùng lại).
        public Action<FloatingText> OnReturnToPool;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _baseColor = _text.color;
        }

        public void Show(string message, Vector3 position)
        {
            _text.text = message;
            _start = position;
            _elapsed = 0f;
            gameObject.SetActive(true);
            Apply(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float p = _elapsed / _duration;
            if (p >= 1f)
            {
                if (OnReturnToPool != null) OnReturnToPool(this);
                else gameObject.SetActive(false);
                return;
            }

            Apply(p);
        }

        private void Apply(float p)
        {
            // Bay lên nhanh lúc đầu rồi chậm lại.
            transform.position = _start + Vector3.up * (_riseDistance * (1f - Mathf.Pow(1f - p, 3f)));

            // 15% đầu: phóng từ nhỏ lên to hơn cỡ thật; 20% kế: thu về cỡ thật.
            float scale = p < 0.15f
                ? Mathf.Lerp(0.3f, _popScale, p / 0.15f)
                : Mathf.Lerp(_popScale, 1f, Mathf.Clamp01((p - 0.15f) / 0.2f));
            transform.localScale = Vector3.one * scale;

            // 40% cuối: mờ dần.
            Color c = _baseColor;
            c.a = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
            _text.color = c;
        }
    }
}
