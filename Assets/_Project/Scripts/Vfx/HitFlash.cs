using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.Vfx
{
    /// <summary>
    /// Nháy màu và phồng nhẹ khi trúng đòn. Tự nghe Health trên cùng object nên gameplay không phải gọi gì;
    /// gỡ component này ra thì game vẫn chạy y hệt, chỉ mất phần cảm giác.
    ///
    /// Nháy đỏ nhạt chứ không nháy trắng: SpriteRenderer.color được NHÂN với màu sprite, nên chỉ làm sprite
    /// ngả màu/tối đi, không thể làm trắng nếu không có shader riêng.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _flashColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private float _duration = 0.1f;
        [SerializeField] private float _punchScale = 1.15f;

        private Health _health;
        private Color _baseColor;
        private Vector3 _baseScale;
        private float _remaining;
        private bool _isFlashing;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable() => _health.OnDamaged += HandleDamaged;

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;

            // Quái chết giữa lúc đang nháy sẽ vào pool; nếu không trả lại ngay thì lần sau lấy ra
            // nó xuất hiện với màu đỏ và to hơn bình thường.
            Restore();
        }

        private void HandleDamaged(float damage, float currentHealth)
        {
            // Chỉ chụp màu/scale gốc khi chưa nháy: trúng đòn liên tục mà chụp lại thì sẽ chụp nhầm
            // trạng thái đang đỏ/đang phồng làm "gốc". Không chụp trong Awake/OnEnable vì Enemy.Initialize
            // đặt scale theo loại quái (Tank to x1.5) SAU khi OnEnable đã chạy.
            if (!_isFlashing)
            {
                _baseColor = _renderer.color;
                _baseScale = transform.localScale;
                _isFlashing = true;
            }

            _remaining = _duration;
            _renderer.color = _flashColor;
        }

        private void Update()
        {
            if (!_isFlashing) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Restore();
                return;
            }

            // Phồng lên ngay rồi xẹp dần về cỡ gốc cùng lúc màu hết nháy.
            float t = _remaining / _duration;
            transform.localScale = _baseScale * Mathf.Lerp(1f, _punchScale, t);
        }

        private void Restore()
        {
            if (!_isFlashing) return;

            _renderer.color = _baseColor;
            transform.localScale = _baseScale;
            _isFlashing = false;
        }
    }
}
