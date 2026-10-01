using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Bảng nâng Talent. Prefab độc lập (không phụ thuộc menu chính) để Phase 9 đặt nguyên vào tab Talent của Hub.
    /// Tự cập nhật khi vàng hoặc cấp Talent đổi (kể cả đổi từ nơi khác, ví dụ menu dev cộng tiền).
    /// </summary>
    public class TalentPanel : MonoBehaviour
    {
        [SerializeField] private TalentCatalog _catalog;
        [SerializeField] private HeroBaseStats _heroBase;
        [SerializeField] private TalentRow _rowPrefab;
        [SerializeField] private Transform _rowContainer;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _powerText;
        [SerializeField] private Button _closeButton;

        private readonly List<TalentRow> _rows = new List<TalentRow>();
        private Action<string, long> _onCurrencyChanged;
        private Action<string, int> _onTalentChanged;
        private Action<TalentData> _onUpgradeClicked;

        private void Awake()
        {
            _onCurrencyChanged = (_, __) => RefreshAll();
            _onTalentChanged = (_, __) => RefreshAll();
            _onUpgradeClicked = HandleUpgradeClicked;
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
        }

        private void OnEnable()
        {
            BuildRowsOnce();
            // Dịch vụ sống lâu hơn bảng: đăng ký khi hiện, gỡ khi ẩn.
            GameServices.Economy.OnCurrencyChanged += _onCurrencyChanged;
            GameServices.Talents.OnTalentChanged += _onTalentChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (GameServices.Economy != null) GameServices.Economy.OnCurrencyChanged -= _onCurrencyChanged;
            if (GameServices.Talents != null) GameServices.Talents.OnTalentChanged -= _onTalentChanged;
        }

        public void Close() => gameObject.SetActive(false);

        private void BuildRowsOnce()
        {
            if (_rows.Count > 0 || _catalog == null) return;
            foreach (TalentData talent in _catalog.talents)
            {
                if (talent == null) continue;
                TalentRow row = Instantiate(_rowPrefab, _rowContainer);
                row.Bind(talent, GameServices.Talents);
                row.OnUpgradeClicked += _onUpgradeClicked;
                _rows.Add(row);
            }
        }

        private void HandleUpgradeClicked(TalentData talent)
        {
            // Sự kiện OnTalentChanged / OnCurrencyChanged sẽ tự gọi RefreshAll.
            GameServices.Talents.TryUpgrade(talent);
        }

        private void RefreshAll()
        {
            foreach (TalentRow row in _rows) row.Refresh();
            if (_goldText != null) _goldText.SetText("{0}", GameServices.Economy.GetBalance(CurrencyIds.Gold));
            if (_powerText != null)
            {
                StatBlock heroBase = _heroBase != null ? _heroBase.baseStats : new StatBlock { maxHealth = 100f };
                StatBlock stats = StatCalculator.Compute(heroBase, _catalog, GameServices.Talents.GetLevel);
                _powerText.SetText("Lực chiến: {0}", StatCalculator.Power(stats));
            }
        }
    }
}
