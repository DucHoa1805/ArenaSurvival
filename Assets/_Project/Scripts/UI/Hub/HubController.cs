using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>
    /// Giao diện chính (Phase 9, thay menu chính cũ): điều phối tab ở thanh dưới (khóa theo level tài khoản, chấm đỏ),
    /// hiệu ứng nhận thưởng khi vừa chơi xong 1 màn, popup lên cấp, bảng cài đặt và nút thoát.
    /// Nội dung từng tab là prefab riêng (BattleTab, EquipmentPanel, TalentPanel) — Hub chỉ bật/tắt.
    /// </summary>
    public class HubController : MonoBehaviour
    {
        [Header("Tab")]
        [SerializeField] private HubTab[] _tabs;
        [SerializeField] private HubTab _defaultTab;
        [SerializeField] private TalentCatalog _talents;

        [Header("Thành phần")]
        [SerializeField] private HubTopBar _topBar;
        [SerializeField] private LevelUpPopup _levelUpPopup;
        [SerializeField] private RewardFly _rewardFly;
        [SerializeField] private HubToast _toast;
        [SerializeField] private Sprite _goldIcon;
        [SerializeField] private Sprite _gemIcon;

        [Header("Cài đặt")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button _closeSettingsButton;
        [SerializeField] private Button _quitButton;

        // Màn vừa ăn mừng gần nhất: static để quay lại Hub lần 2 (không chơi màn mới) không bay tiền lại.
        private static StageGrant _celebratedGrant;

        private HubTab _current;
        private Action<HubTab> _onTabClicked;
        private Action _onChanged;
        private Action _onXpChanged;
        private Action<string, long> _onCurrencyChanged;
        private Action<string, int> _onTalentChanged;
        private Action<string> _onWeaponChanged;
        private readonly List<CurrencyCost> _rewardBuffer = new List<CurrencyCost>(2);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _celebratedGrant = null;

        private void Awake()
        {
            _onTabClicked = HandleTabClicked;
            _onChanged = RefreshTabs;
            _onXpChanged = HandleXpChanged;
            _onCurrencyChanged = (_, __) => RefreshTabs();
            _onTalentChanged = (_, __) => RefreshTabs();
            _onWeaponChanged = _ => RefreshTabs();
            foreach (HubTab tab in _tabs) tab.OnClicked += _onTabClicked;

            _settingsButton.onClick.AddListener(OpenSettings);
            _closeSettingsButton.onClick.AddListener(CloseSettings);
            if (_quitButton != null) _quitButton.onClick.AddListener(Quit);
            _settingsPanel.SetActive(false);
            _levelUpPopup.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _closeSettingsButton.onClick.RemoveListener(CloseSettings);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(Quit);
        }

        private void OnEnable()
        {
            GameServices.Economy.OnCurrencyChanged += _onCurrencyChanged;
            GameServices.Talents.OnTalentChanged += _onTalentChanged;
            GameServices.Inventory.OnInventoryChanged += _onChanged;
            GameServices.Inventory.OnEquipmentChanged += _onChanged;
            GameServices.Weapons.OnWeaponChanged += _onWeaponChanged;
            GameServices.Account.OnXpChanged += _onXpChanged;
            GameServices.Missions.OnChanged += _onChanged;
        }

        private void OnDisable()
        {
            // Dịch vụ sống lâu hơn scene: không gỡ thì chúng giữ tham chiếu tới Hub đã bị hủy.
            if (!GameServices.IsInitialized) return;
            GameServices.Economy.OnCurrencyChanged -= _onCurrencyChanged;
            GameServices.Talents.OnTalentChanged -= _onTalentChanged;
            GameServices.Inventory.OnInventoryChanged -= _onChanged;
            GameServices.Inventory.OnEquipmentChanged -= _onChanged;
            GameServices.Weapons.OnWeaponChanged -= _onWeaponChanged;
            GameServices.Account.OnXpChanged -= _onXpChanged;
            GameServices.Missions.OnChanged -= _onChanged;
        }

        private void Start()
        {
            _current = _defaultTab;
            RefreshTabs();
            float delay = CelebrateLastStage();
            if (delay > 0f) Invoke(nameof(ShowNextLevelUp), delay);
            else ShowNextLevelUp();
        }

        /// <summary>Vừa chơi xong 1 màn: tiền/kim cương bay vào thanh trên. Trả thời gian hiệu ứng (0 nếu không có gì).</summary>
        private float CelebrateLastStage()
        {
            StageGrant grant = GameServices.StageProgress.LastGrant;
            if (grant == null || grant == _celebratedGrant) return 0f;
            _celebratedGrant = grant;

            long gold = grant.gold, gem = 0;
            for (int level = grant.levelBefore + 1; level <= grant.levelAfter && grant.levelBefore > 0; level++)
            {
                AccountRules.RewardFor(level, _rewardBuffer);
                foreach (CurrencyCost r in _rewardBuffer)
                {
                    if (r.id == CurrencyIds.Gold) gold += r.amount;
                    else if (r.id == CurrencyIds.Gem) gem += r.amount;
                }
            }
            if (gold <= 0 && gem <= 0) return 0f;

            int coins = Mathf.Clamp((int)(gold / 25), 3, 10);
            float duration = _rewardFly.DurationFor(coins);
            _topBar.HoldCounters(GameServices.Economy.GetBalance(CurrencyIds.Gold) - gold,
                GameServices.Economy.GetBalance(CurrencyIds.Gem) - gem, duration);
            if (gold > 0) _rewardFly.Play(_goldIcon, coins, _topBar.GoldIcon);
            if (gem > 0) _rewardFly.Play(_gemIcon, 4, _topBar.GemIcon);
            return duration + 0.2f;
        }

        /// <summary>Lên cấp ngay khi đang ở Hub (sau này: quà, nhiệm vụ): hiện popup luôn nếu chưa có popup nào đang mở.</summary>
        private void HandleXpChanged()
        {
            RefreshTabs();
            if (!_levelUpPopup.gameObject.activeSelf && !IsInvoking(nameof(ShowNextLevelUp))) ShowNextLevelUp();
        }

        private void ShowNextLevelUp()
        {
            if (!GameServices.Account.TakePendingLevelUp(out LevelUpInfo info)) return;
            RefreshTabs();
            _levelUpPopup.Show(info, ShowNextLevelUp);
        }

        private void HandleTabClicked(HubTab tab)
        {
            if (tab.ComingSoon)
            {
                _toast.Show($"{tab.Label}: sắp ra mắt");
                return;
            }
            if (!IsUnlocked(tab))
            {
                _toast.Show($"{tab.Label} mở ở cấp {GameServices.Account.UnlockLevel(tab.FeatureId)}");
                return;
            }
            _current = tab;
            RefreshTabs();
        }

        private static bool IsUnlocked(HubTab tab) =>
            !tab.ComingSoon && (string.IsNullOrEmpty(tab.FeatureId) || GameServices.Account.IsUnlocked(tab.FeatureId));

        private void RefreshTabs()
        {
            foreach (HubTab tab in _tabs)
            {
                bool locked = !IsUnlocked(tab);
                string lockText = tab.ComingSoon ? "Sắp ra mắt" : $"Cấp {GameServices.Account.UnlockLevel(tab.FeatureId)}";
                tab.SetState(tab == _current, locked, lockText, HasRedDot(tab));
            }
        }

        private bool HasRedDot(HubTab tab)
        {
            switch (tab.FeatureId)
            {
                case FeatureIds.Talents: return RedDotRules.TalentAvailable(_talents, GameServices.Talents);
                case FeatureIds.Equipment:
                    return RedDotRules.EquipmentAvailable(GameServices.Inventory, GameServices.Weapons, GameServices.Economy);
                case FeatureIds.Missions: return GameServices.Missions.HasClaimable;
                default: return false;
            }
        }

        // Tắt bảng là đủ để lưu âm lượng: VolumeSlider tự ghi xuống đĩa trong OnDisable.
        private void OpenSettings() => _settingsPanel.SetActive(true);
        private void CloseSettings() => _settingsPanel.SetActive(false);

        private void Quit()
        {
            // Application.Quit() không có tác dụng trong Editor, nên trong Editor thoát Play mode để nút vẫn thử được.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
