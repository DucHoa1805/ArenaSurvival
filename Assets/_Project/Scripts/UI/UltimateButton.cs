using ArenaSurvival.Player;
using ArenaSurvival.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Nút chiêu trên HUD (10E C): vòng mana đầy dần quanh icon chiêu, sáng + nhịp phồng khi đầy, đếm ngược khi chiêu kéo dài.
    /// Chỉ hiển thị và chuyển lượt bấm cho PlayerUltimate. Chữ chỉ đổi khi giá trị hiển thị đổi (không tạo chuỗi mỗi frame).
    /// </summary>
    public class UltimateButton : MonoBehaviour
    {
        [SerializeField] private PlayerUltimate _ultimate;
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _button;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _readyGlow;

        private WeaponUltimate _shown;
        private int _lastValue = -1;
        private int _lastMode = -1;

        private void Awake() => _button.onClick.AddListener(() => _ultimate.TryActivate());

        private void Update()
        {
            WeaponUltimate ult = _ultimate != null ? _ultimate.Ultimate : null;
            if (_root.activeSelf != (ult != null)) _root.SetActive(ult != null);
            if (ult == null) return;

            if (ult != _shown)
            {
                _shown = ult;
                _icon.sprite = ult.icon;
                _icon.enabled = ult.icon != null;
                _fill.color = ult.color;
            }

            bool full = _ultimate.IsFull;
            bool running = _ultimate.IsRunning;
            _fill.fillAmount = running ? _ultimate.ActiveTimeLeft / Mathf.Max(0.01f, ult.Duration) : _ultimate.ManaFraction;
            _readyGlow.SetActive(full && !running);
            _button.interactable = full && !running;
            // Nhịp phồng nhẹ khi sẵn sàng để mắt người chơi tìm ra nút giữa trận đông.
            float s = full && !running ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            transform.localScale = new Vector3(s, s, 1f);

            int mode = running ? 2 : full ? 1 : 0;
            int value = running ? Mathf.CeilToInt(_ultimate.ActiveTimeLeft) : Mathf.FloorToInt(_ultimate.ManaFraction * 100f);
            if (mode == _lastMode && value == _lastValue) return;
            _lastMode = mode;
            _lastValue = value;
            _label.text = mode == 2 ? (ult.Duration > 0f ? $"{value}s" : "") : mode == 1 ? "SPACE" : $"{value}%";
        }
    }
}
