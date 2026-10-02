using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Missions
{
    /// <summary>1 mốc điểm trên thanh điểm ngày: icon phần thưởng, số điểm cần, bấm để nhận khi đủ.</summary>
    public class MilestoneView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _points;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private GameObject _claimedMark;
        [SerializeField] private GameObject _readyGlow;
        [SerializeField] private Button _button;

        public event Action OnClicked;

        private void Awake() => _button.onClick.AddListener(() => OnClicked?.Invoke());

        public void Bind(int points, Sprite icon, string amount, bool claimed, bool ready)
        {
            _points.text = points.ToString();
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _amount.text = amount;
            _claimedMark.SetActive(claimed);
            _readyGlow.SetActive(ready);
            _button.interactable = ready;
        }
    }
}
