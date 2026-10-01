using ArenaSurvival.Core;
using ArenaSurvival.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Bảng "Hồi sinh?" khi Player chết mà còn lượt. Chỉ chuyển lựa chọn cho StageSession; ở Phase 11 nút hồi sinh
    /// sẽ đổi thành tốn kim cương / xem quảng cáo mà StageSession không cần biết.
    /// </summary>
    public class RevivePanel : MonoBehaviour
    {
        [SerializeField] private StageSession _session;
        [SerializeField] private GameObject _content;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _giveUpButton;

        private void Awake()
        {
            _content.SetActive(false);
            _reviveButton.onClick.AddListener(HandleReviveClicked);
            _giveUpButton.onClick.AddListener(HandleGiveUpClicked);
        }

        private void OnDestroy()
        {
            _reviveButton.onClick.RemoveListener(HandleReviveClicked);
            _giveUpButton.onClick.RemoveListener(HandleGiveUpClicked);
        }

        private void OnEnable() => GameEvents.OnGameStateChanged += HandleGameStateChanged;

        private void OnDisable() => GameEvents.OnGameStateChanged -= HandleGameStateChanged;

        private void HandleGameStateChanged(GameState state) => _content.SetActive(state == GameState.RevivePrompt);

        private void HandleReviveClicked() => _session.Revive();

        private void HandleGiveUpClicked() => _session.GiveUp();
    }
}
