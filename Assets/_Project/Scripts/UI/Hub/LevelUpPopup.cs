using System;
using System.Collections.Generic;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>Popup "LÊN CẤP n!" với phần thưởng (đã cộng sẵn lúc lên cấp) và tính năng vừa mở. Nút Nhận đóng popup.</summary>
    public class LevelUpPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private ItemSlotView _slotPrefab;
        [SerializeField] private Transform _rewardContainer;
        [SerializeField] private TMP_Text _unlockText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Sprite _goldIcon;
        [SerializeField] private Sprite _gemIcon;

        private readonly List<ItemSlotView> _slots = new List<ItemSlotView>();
        private Action _onClosed;

        private void Awake() => _claimButton.onClick.AddListener(Close);
        private void OnDestroy() => _claimButton.onClick.RemoveListener(Close);

        public void Show(LevelUpInfo info, Action onClosed)
        {
            _onClosed = onClosed;
            _title.SetText("LÊN CẤP {0}!", info.level);

            int used = 0;
            foreach (CurrencyCost reward in info.rewards)
            {
                if (used >= _slots.Count) _slots.Add(Instantiate(_slotPrefab, _rewardContainer));
                ItemSlotView slot = _slots[used++];
                slot.gameObject.SetActive(true);
                slot.ShowCurrency(reward.id == CurrencyIds.Gem ? _gemIcon : _goldIcon, reward.amount);
            }
            for (int i = used; i < _slots.Count; i++) _slots[i].gameObject.SetActive(false);

            bool unlocked = info.unlockedFeatures.Count > 0;
            _unlockText.gameObject.SetActive(unlocked);
            if (unlocked) _unlockText.text = "Mở khóa: " + string.Join(", ", info.unlockedFeatures);
            gameObject.SetActive(true);
        }

        private void Close()
        {
            gameObject.SetActive(false);
            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }
    }
}
