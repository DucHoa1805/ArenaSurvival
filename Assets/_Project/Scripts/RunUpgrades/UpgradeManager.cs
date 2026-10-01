using System;
using System.Collections.Generic;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.Player;
using UnityEngine;
using ArenaSurvival.Weapons;

namespace ArenaSurvival.RunUpgrades
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
        [Tooltip("Mọi component vũ khí (IWeapon) gắn trên Player, kể cả cái chưa mở khóa (đang disabled).")]
        [SerializeField] private MonoBehaviour[] _weapons;

        /// <summary>Phát mỗi khi có 1 lượt chọn mới. UI nghe event này để hiển thị.</summary>
        public event Action<IReadOnlyList<UpgradeData>> OnOptionsOffered;

        private readonly List<UpgradeData> _currentOptions = new List<UpgradeData>();
        private readonly List<UpgradeData> _availableUpgrades = new List<UpgradeData>();
        private readonly List<UpgradeData> _shuffleBuffer = new List<UpgradeData>();

        // Lên nhiều cấp cùng lúc (nhặt 1 lượng XP lớn) thì xếp hàng: chọn xong lượt này mới tới lượt kế.
        private int _pendingLevelUps;
        private int _currentLevel = 1;

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
            _currentLevel = newLevel;
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
            GameEvents.UpgradeChosen();
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

            // Chỉ rút từ những nâng cấp còn ý nghĩa (ví dụ không đưa lại súng đã có).
            _availableUpgrades.Clear();
            foreach (UpgradeData upgrade in _allUpgrades)
            {
                if (IsAvailable(upgrade)) _availableUpgrades.Add(upgrade);
            }

            RandomPicker.PickDistinct(_availableUpgrades, _choicesPerLevelUp, _currentOptions, _shuffleBuffer, UnityEngine.Random.Range);
            OnOptionsOffered?.Invoke(_currentOptions);
        }

        private bool IsAvailable(UpgradeData upgrade)
        {
            if (_currentLevel < upgrade.requiredLevel) return false;
            if (upgrade.type != UpgradeType.AddWeapon) return true;

            // Vũ khí mở khóa được khi Player có gắn nó và nó chưa được bật.
            MonoBehaviour weapon = FindWeapon(upgrade.weapon);
            return weapon != null && !weapon.enabled;
        }

        private MonoBehaviour FindWeapon(WeaponDefinition definition)
        {
            foreach (MonoBehaviour behaviour in _weapons)
            {
                if (behaviour is IWeapon weapon && weapon.Definition == definition) return behaviour;
            }

            return null;
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
                case UpgradeType.AddWeapon:
                    MonoBehaviour weapon = FindWeapon(upgrade.weapon);
                    if (weapon != null) weapon.enabled = true;
                    break;
            }
        }
    }
}
