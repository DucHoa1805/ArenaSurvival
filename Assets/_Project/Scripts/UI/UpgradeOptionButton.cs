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

        public void Bind(UpgradeData upgrade, Action<UpgradeData> onClicked)
        {
            _upgrade = upgrade;
            _onClicked = onClicked;
            _nameText.text = upgrade.displayName;
            _descriptionText.text = upgrade.description;

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
