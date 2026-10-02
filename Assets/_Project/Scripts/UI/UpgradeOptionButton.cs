using System;
using ArenaSurvival.RunUpgrades;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>1 ô lựa chọn trong bảng lên cấp: hiển thị 1 UpgradeData và báo lại khi được bấm.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class UpgradeOptionButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [Tooltip("Không bắt buộc. Nâng cấp không có icon thì ô icon được ẩn đi.")]
        [SerializeField] private UnityEngine.UI.Image _iconImage;

        private UnityEngine.UI.Button _button;
        private UpgradeData _upgrade;
        private Action<UpgradeData> _onClicked;

        private void Awake()
        {
            _button = GetComponent<UnityEngine.UI.Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        /// <param name="currentLevel">Cấp kỹ năng đang có trong màn (0 = chưa có).</param>
        public void Bind(UpgradeData upgrade, int currentLevel, Action<UpgradeData> onClicked)
        {
            _upgrade = upgrade;
            _onClicked = onClicked;
            int next = currentLevel + 1;
            // Ghi rõ đang nâng tới đâu để người chơi biết nên dồn vào kỹ năng nào; Hồi máu (không cấp) giữ tên trơn.
            if (upgrade.maxLevel <= 0) _nameText.text = upgrade.displayName;
            else if (currentLevel <= 0) _nameText.text = $"{upgrade.displayName}  <size=70%><color=#7CFF7C>MỚI</color></size>";
            else _nameText.text = $"{upgrade.displayName}  <size=70%>Cấp {currentLevel} → {next}</size>";
            _descriptionText.text = upgrade.DescriptionForLevel(next);

            if (_iconImage != null)
            {
                _iconImage.sprite = upgrade.icon;
                _iconImage.enabled = upgrade.icon != null;
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_upgrade);
        }
    }
}
