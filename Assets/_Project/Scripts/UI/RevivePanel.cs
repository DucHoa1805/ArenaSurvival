using ArenaSurvival.Core;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Services;
using ArenaSurvival.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Bảng "Hồi sinh?" khi Player chết mà còn lượt (Phase 11A): hồi sinh bằng xem quảng cáo (giới hạn/ngày) hoặc 20 kim cương.
    /// StageSession không biết cách trả giá — bảng này lo phần đó rồi mới gọi Revive.
    /// </summary>
    public class RevivePanel : MonoBehaviour
    {
        public const long ReviveGemCost = 20;

        [SerializeField] private StageSession _session;
        [SerializeField] private GameObject _content;
        [Tooltip("Hồi sinh bằng quảng cáo.")]
        [SerializeField] private Button _reviveButton;
        [SerializeField] private TMP_Text _reviveLabel;
        [Tooltip("Hồi sinh bằng kim cương (không bắt buộc).")]
        [SerializeField] private Button _gemButton;
        [SerializeField] private TMP_Text _gemLabel;
        [SerializeField] private Button _giveUpButton;

        private bool _busy;

        private void Awake()
        {
            _content.SetActive(false);
            _reviveButton.onClick.AddListener(HandleAdClicked);
            if (_gemButton != null) _gemButton.onClick.AddListener(HandleGemClicked);
            _giveUpButton.onClick.AddListener(HandleGiveUpClicked);
        }

        private void OnDestroy()
        {
            _reviveButton.onClick.RemoveListener(HandleAdClicked);
            if (_gemButton != null) _gemButton.onClick.RemoveListener(HandleGemClicked);
            _giveUpButton.onClick.RemoveListener(HandleGiveUpClicked);
        }

        private void OnEnable() => GameEvents.OnGameStateChanged += HandleGameStateChanged;

        private void OnDisable() => GameEvents.OnGameStateChanged -= HandleGameStateChanged;

        private void HandleGameStateChanged(GameState state)
        {
            bool show = state == GameState.RevivePrompt;
            _content.SetActive(show);
            if (show) Refresh();
        }

        private void Refresh()
        {
            bool services = GameServices.IsInitialized;
            int adsLeft = services ? GameServices.Ads.Remaining(AdPlacements.Revive) : 0;
            _reviveButton.interactable = !_busy && services && GameServices.Ads.CanShow(AdPlacements.Revive);
            if (_reviveLabel != null)
                _reviveLabel.text = !services ? "Hồi sinh"
                    : GameServices.Ads.RemoveAds ? $"Hồi sinh ({adsLeft})"
                    : adsLeft > 0 ? $"Xem QC hồi sinh ({adsLeft})" : "Hết lượt quảng cáo";
            if (_gemButton != null)
            {
                _gemButton.interactable = !_busy && services && GameServices.Economy.GetBalance(CurrencyIds.Gem) >= ReviveGemCost;
                if (_gemLabel != null) _gemLabel.text = $"Hồi sinh · {ReviveGemCost} KC";
            }
        }

        private void HandleAdClicked()
        {
            if (_busy || !GameServices.IsInitialized) return;
            _busy = true;
            Refresh();
            GameServices.Ads.Show(AdPlacements.Revive, ok =>
            {
                _busy = false;
                if (ok) _session.Revive();
                else Refresh();
            });
        }

        private void HandleGemClicked()
        {
            if (_busy || !GameServices.IsInitialized) return;
            // EconomyService trả false khi không đủ: không bao giờ âm tiền, không hồi sinh.
            if (GameServices.Economy.TrySpend(CurrencyIds.Gem, ReviveGemCost))
            {
                GameServices.Profile.Save();
                _session.Revive();
            }
            else Refresh();
        }

        private void HandleGiveUpClicked() => _session.GiveUp();
    }
}
