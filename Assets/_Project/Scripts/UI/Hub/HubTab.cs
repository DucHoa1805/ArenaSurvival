using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>1 nút ở thanh dưới của Hub: chọn tab, hiện khóa ("Cấp 5" / "Sắp ra mắt") và chấm đỏ. Chỉ hiển thị + báo bấm.</summary>
    public class HubTab : MonoBehaviour
    {
        [Tooltip("Tính năng cần mở theo level (FeatureIds). Trống = luôn mở.")]
        [SerializeField] private string _featureId;
        [Tooltip("Chưa có nội dung (Cửa hàng, Sự kiện) — luôn khóa với chữ \"Sắp ra mắt\".")]
        [SerializeField] private bool _comingSoon;
        [Tooltip("Nội dung của tab (bật khi chọn). Trống với tab Sắp ra mắt.")]
        [SerializeField] private GameObject _content;

        [Header("Hiển thị")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _redDot;
        [SerializeField] private GameObject _lock;
        [SerializeField] private TMP_Text _lockText;

        private static readonly Color Normal = new Color(0.10f, 0.12f, 0.17f, 0.95f);
        private static readonly Color Selected = new Color(0.95f, 0.71f, 0.27f, 1f);
        private static readonly Color LabelNormal = new Color(0.66f, 0.71f, 0.77f);
        private static readonly Color LabelSelected = new Color(0.09f, 0.11f, 0.15f);

        public string FeatureId => _featureId;
        public bool ComingSoon => _comingSoon;
        public GameObject Content => _content;
        public string Label => _label != null ? _label.text : name;

        public event Action<HubTab> OnClicked;

        private void Awake() => _button.onClick.AddListener(HandleClick);
        private void OnDestroy() => _button.onClick.RemoveListener(HandleClick);

        public void SetState(bool selected, bool locked, string lockText, bool redDot)
        {
            _background.color = selected ? Selected : Normal;
            if (_label != null) _label.color = selected ? LabelSelected : LabelNormal;
            _icon.color = locked ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            if (_lock != null) _lock.SetActive(locked);
            if (_lockText != null)
            {
                _lockText.gameObject.SetActive(locked);
                _lockText.text = lockText;
            }
            if (_redDot != null) _redDot.SetActive(redDot && !locked);
            if (_content != null) _content.SetActive(selected && !locked);
        }

        private void HandleClick() => OnClicked?.Invoke(this);
    }
}
