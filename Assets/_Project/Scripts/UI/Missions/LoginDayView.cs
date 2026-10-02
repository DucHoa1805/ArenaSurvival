using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Missions
{
    /// <summary>1 ô quà đăng nhập: đã nhận (mờ + dấu ✓), hôm nay (viền sáng, bấm để nhận), chưa tới.</summary>
    public class LoginDayView : MonoBehaviour
    {
        public enum State { Claimed, Today, Upcoming }

        [SerializeField] private TMP_Text _day;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private GameObject _claimedMark;
        [SerializeField] private GameObject _highlight;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _button;

        public event Action OnClicked;

        private void Awake() => _button.onClick.AddListener(() => OnClicked?.Invoke());

        public void Bind(int day, Sprite icon, string amount, State state)
        {
            _day.text = $"Ngày {day}";
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _amount.text = amount;
            _claimedMark.SetActive(state == State.Claimed);
            _highlight.SetActive(state == State.Today);
            _group.alpha = state == State.Claimed ? 0.5f : 1f;
            _button.interactable = state == State.Today;
        }
    }
}
