using System;
using ArenaSurvival.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Missions
{
    /// <summary>1 dòng nhiệm vụ ngày: mô tả, thanh tiến độ, điểm, nút Nhận.</summary>
    public class MissionRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _description;
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _progress;
        [SerializeField] private TMP_Text _points;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _buttonLabel;
        [SerializeField] private CanvasGroup _group;

        public MissionData Mission { get; private set; }
        public event Action<MissionRowView> OnClaimClicked;

        private void Awake() => _button.onClick.AddListener(() => OnClaimClicked?.Invoke(this));

        public void Bind(MissionData mission, long progress, bool claimed)
        {
            Mission = mission;
            bool complete = progress >= mission.target;
            _description.text = mission.Describe();
            _fill.fillAmount = Mathf.Clamp01(progress / (float)mission.target);
            _progress.text = $"{progress:N0}/{mission.target:N0}";
            _points.text = $"+{mission.points}";
            _button.interactable = complete && !claimed;
            _buttonLabel.text = claimed ? "Đã nhận" : complete ? "Nhận" : "Chưa xong";
            // Đã nhận thì mờ đi để mắt dồn vào việc còn lại.
            _group.alpha = claimed ? 0.5f : 1f;
        }
    }
}
