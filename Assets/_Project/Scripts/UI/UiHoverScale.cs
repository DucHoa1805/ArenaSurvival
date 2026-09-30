using UnityEngine;
using UnityEngine.EventSystems;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Phóng to nhẹ 1 phần tử UI khi rê chuột vào (hoặc chọn bằng bàn phím/tay cầm), thu lại khi rời ra.
    /// Dùng unscaledDeltaTime vì menu Pause, bảng lên cấp và Game Over đều hiện lúc timeScale = 0.
    /// </summary>
    public class UiHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private float _hoverScale = 1.06f;
        [SerializeField] private float _speed = 14f;

        private Vector3 _baseScale = Vector3.one;
        private float _target = 1f;
        private float _current = 1f;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            // Bảng bị ẩn lúc đang phóng to thì lần hiện sau phải về kích thước gốc.
            _target = _current = 1f;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;

            // _speed = số lần đi hết quãng phóng to mỗi giây (14 => ~0.07 giây).
            _current = Mathf.MoveTowards(_current, _target, Mathf.Abs(_hoverScale - 1f) * _speed * Time.unscaledDeltaTime);
            transform.localScale = _baseScale * _current;
        }

        public void OnPointerEnter(PointerEventData eventData) => _target = _hoverScale;
        public void OnPointerExit(PointerEventData eventData) => _target = 1f;
        public void OnSelect(BaseEventData eventData) => _target = _hoverScale;
        public void OnDeselect(BaseEventData eventData) => _target = 1f;
    }
}
