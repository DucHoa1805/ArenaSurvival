using System.Collections.Generic;
using ArenaSurvival.Progression;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Bảng chọn nâng cấp khi lên cấp. Chỉ làm việc hiển thị: nghe UpgradeManager đưa ra lựa chọn,
    /// vẽ lên các nút, và chuyển lựa chọn của người chơi về lại UpgradeManager. Không chứa logic game.
    ///
    /// Script nằm trên object luôn bật; phần nhìn thấy (_content) là object con được bật/tắt —
    /// nếu tắt chính object chứa script thì nó sẽ không còn nghe được event để tự bật lại.
    /// </summary>
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private UpgradeManager _upgradeManager;
        [SerializeField] private GameObject _content;
        [SerializeField] private UpgradeOptionButton[] _optionButtons;
        [Tooltip("Không bắt buộc: hoạt ảnh lúc bảng hiện ra.")]
        [SerializeField] private LevelUpPanelAnimator _animator;

        private void Awake()
        {
            _content.SetActive(false);
        }

        private void OnEnable()
        {
            _upgradeManager.OnOptionsOffered += Show;
        }

        private void OnDisable()
        {
            _upgradeManager.OnOptionsOffered -= Show;
        }

        private void Show(IReadOnlyList<UpgradeData> options)
        {
            // Bật trước rồi mới Bind: nút đang tắt thì Awake của nó chưa chạy.
            _content.SetActive(true);

            for (int i = 0; i < _optionButtons.Length; i++)
            {
                bool hasOption = i < options.Count;
                _optionButtons[i].gameObject.SetActive(hasOption);

                if (hasOption)
                {
                    _optionButtons[i].Bind(options[i], HandleOptionClicked);
                }
            }

            if (_animator != null) _animator.Play();
        }

        private void HandleOptionClicked(UpgradeData upgrade)
        {
            // Ẩn trước rồi mới báo: nếu còn lượt lên cấp đang chờ, UpgradeManager sẽ gọi Show() lại ngay.
            if (_animator != null) _animator.Stop();
            _content.SetActive(false);
            _upgradeManager.Select(upgrade);
        }
    }
}
