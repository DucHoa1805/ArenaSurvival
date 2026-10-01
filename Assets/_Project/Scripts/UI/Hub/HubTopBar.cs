using System;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>
    /// Thanh trên cùng: level tài khoản + thanh XP, vàng, kim cương. Số tiền chạy tăng dần tới số dư thật (cảm giác "nhận thưởng");
    /// HoldCounters giữ số cũ trong lúc tiền đang bay tới, để số chỉ nhảy khi đồng xu chạm thanh.
    /// </summary>
    public class HubTopBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _xpFill;
        [SerializeField] private TMP_Text _xpText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _gemText;
        [SerializeField] private RectTransform _goldIcon;
        [SerializeField] private RectTransform _gemIcon;
        [Tooltip("Thời gian (giây, thời gian thực) để số chạy tới số dư mới.")]
        [SerializeField] private float _countDuration = 0.7f;

        public RectTransform GoldIcon => _goldIcon;
        public RectTransform GemIcon => _gemIcon;

        private double _shownGold, _shownGem, _fromGold, _fromGem;
        private float _countTimer = -1f;
        private float _holdUntil;
        private Action<string, long> _onCurrencyChanged;
        private Action _onXpChanged;

        private void Awake()
        {
            _onCurrencyChanged = (_, __) => StartCount();
            _onXpChanged = RefreshAccount;
        }

        private void OnEnable()
        {
            GameServices.Economy.OnCurrencyChanged += _onCurrencyChanged;
            GameServices.Account.OnXpChanged += _onXpChanged;
            _shownGold = GameServices.Economy.GetBalance(CurrencyIds.Gold);
            _shownGem = GameServices.Economy.GetBalance(CurrencyIds.Gem);
            ShowCounts();
            RefreshAccount();
        }

        private void OnDisable()
        {
            if (GameServices.Economy != null) GameServices.Economy.OnCurrencyChanged -= _onCurrencyChanged;
            if (GameServices.Account != null) GameServices.Account.OnXpChanged -= _onXpChanged;
        }

        /// <summary>Giữ số đang hiện (vd tiền thưởng sau màn) tới khi hiệu ứng bay xong rồi mới chạy số.</summary>
        public void HoldCounters(double fromGold, double fromGem, float seconds)
        {
            _shownGold = fromGold;
            _shownGem = fromGem;
            _holdUntil = Time.unscaledTime + seconds;
            ShowCounts();
            StartCount();
        }

        public void RefreshAccount()
        {
            AccountService account = GameServices.Account;
            _levelText.SetText("{0}", account.Level);
            bool max = account.XpForLevel <= 0;
            _xpFill.fillAmount = max ? 1f : Mathf.Clamp01(account.XpInLevel / (float)account.XpForLevel);
            if (max) _xpText.text = "TỐI ĐA";
            else _xpText.SetText("{0}/{1}", account.XpInLevel, account.XpForLevel);
        }

        private void StartCount()
        {
            _fromGold = _shownGold;
            _fromGem = _shownGem;
            _countTimer = 0f;
        }

        private void Update()
        {
            if (_countTimer < 0f || Time.unscaledTime < _holdUntil) return;

            // Thời gian thực: Hub có thể mở khi timeScale = 0 (vừa thoát từ màn đang dừng).
            _countTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_countTimer / _countDuration);
            float eased = 1f - (1f - t) * (1f - t);
            _shownGold = Lerp(_fromGold, GameServices.Economy.GetBalance(CurrencyIds.Gold), eased);
            _shownGem = Lerp(_fromGem, GameServices.Economy.GetBalance(CurrencyIds.Gem), eased);
            ShowCounts();
            if (t >= 1f) _countTimer = -1f;
        }

        private static double Lerp(double a, double b, float t) => a + (b - a) * t;

        private void ShowCounts()
        {
            // SetText với số không tạo chuỗi mới (đổi mỗi frame lúc đang chạy số).
            _goldText.SetText("{0}", (float)Math.Round(_shownGold));
            _gemText.SetText("{0}", (float)Math.Round(_shownGem));
        }
    }
}
