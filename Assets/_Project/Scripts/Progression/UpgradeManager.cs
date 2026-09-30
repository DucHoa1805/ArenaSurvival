using System;
using System.Collections.Generic;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.Player;
using UnityEngine;

namespace ArenaSurvival.Progression
{
    /// <summary>
    /// Khi lên cấp: dừng game, đưa ra vài nâng cấp ngẫu nhiên, áp dụng cái được chọn rồi cho game chạy tiếp.
    /// Không biết gì về UI — chỉ phát OnOptionsOffered và chờ ai đó gọi Select().
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private UpgradeData[] _allUpgrades;
        [SerializeField] private int _choicesPerLevelUp = 3;

        [Header("Targets")]
        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private Health _playerHealth;

        /// <summary>Phát mỗi khi có 1 lượt chọn mới. UI nghe event này để hiển thị.</summary>
        public event Action<IReadOnlyList<UpgradeData>> OnOptionsOffered;

        private readonly List<UpgradeData> _currentOptions = new List<UpgradeData>();
        private readonly List<UpgradeData> _shuffleBuffer = new List<UpgradeData>();

        // Lên nhiều cấp cùng lúc (nhặt 1 lượng XP lớn) thì xếp hàng: chọn xong lượt này mới tới lượt kế.
        private int _pendingLevelUps;

        private void OnEnable()
        {
            GameEvents.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp(int newLevel)
        {
            _pendingLevelUps++;

            // Chỉ mở lượt chọn nếu chưa có lượt nào đang mở.
            if (_pendingLevelUps == 1)
            {
                OfferNextChoice();
            }
        }

        /// <summary>UI gọi hàm này khi người chơi bấm chọn 1 nâng cấp.</summary>
        public void Select(UpgradeData upgrade)
        {
            // Chỉ nhận đúng thứ đang được đưa ra — chặn bấm 2 lần hoặc bấm nhầm lượt cũ.
            if (!_currentOptions.Contains(upgrade)) return;

            Apply(upgrade);
            _currentOptions.Clear();
            _pendingLevelUps--;

            if (_pendingLevelUps > 0)
            {
                OfferNextChoice();
            }
            else
            {
                GameManager.Instance.EndLevelUp();
            }
        }

        private void OfferNextChoice()
        {
            GameManager.Instance.BeginLevelUp();

            // Không vào được trạng thái LevelUp (ví dụ vừa Game Over) hoặc chưa cấu hình nâng cấp nào
            // thì bỏ qua, tránh để game đứng hình mà không có gì để bấm.
            if (GameManager.Instance.CurrentState != GameState.LevelUp || _allUpgrades == null || _allUpgrades.Length == 0)
            {
                _pendingLevelUps = 0;
                GameManager.Instance.EndLevelUp();
                return;
            }

            PickRandomOptions();
            OnOptionsOffered?.Invoke(_currentOptions);
        }

        private void PickRandomOptions()
        {
            _currentOptions.Clear();
            _shuffleBuffer.Clear();
            _shuffleBuffer.AddRange(_allUpgrades);

            // Fisher–Yates chỉ trộn đủ số phần tử cần lấy: đảm bảo các lựa chọn không trùng nhau.
            int count = Mathf.Min(_choicesPerLevelUp, _shuffleBuffer.Count);
            for (int i = 0; i < count; i++)
            {
                int j = UnityEngine.Random.Range(i, _shuffleBuffer.Count);
                (_shuffleBuffer[i], _shuffleBuffer[j]) = (_shuffleBuffer[j], _shuffleBuffer[i]);
                _currentOptions.Add(_shuffleBuffer[i]);
            }
        }

        private void Apply(UpgradeData upgrade)
        {
            switch (upgrade.type)
            {
                case UpgradeType.Damage:
                    _playerStats.AddDamageBonus(upgrade.value);
                    break;
                case UpgradeType.FireRate:
                    _playerStats.AddFireRateBonus(upgrade.value);
                    break;
                case UpgradeType.MoveSpeed:
                    _playerStats.AddMoveSpeedBonus(upgrade.value);
                    break;
                case UpgradeType.MaxHealth:
                    // Tăng trần máu rồi hồi đúng phần vừa tăng, để người chơi thấy máu nhiều lên ngay.
                    _playerHealth.SetMaxHealth(_playerHealth.MaxHealth + upgrade.value);
                    _playerHealth.Heal(upgrade.value);
                    break;
                case UpgradeType.Heal:
                    _playerHealth.Heal(upgrade.value);
                    break;
            }
        }
    }
}
