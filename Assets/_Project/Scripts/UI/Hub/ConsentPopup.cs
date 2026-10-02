using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>
    /// Xin đồng ý quảng cáo cá nhân hóa (Phase 11A): tự hiện lần đầu vào Hub khi chưa hỏi; mở lại được từ bảng Cài đặt.
    /// Lựa chọn lưu trong hồ sơ và truyền vào nhà cung cấp quảng cáo (11B: LevelPlay) trước quảng cáo đầu tiên.
    /// </summary>
    public class ConsentPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _agreeButton;
        [SerializeField] private Button _declineButton;

        private void Awake()
        {
            _agreeButton.onClick.AddListener(() => Choose(true));
            _declineButton.onClick.AddListener(() => Choose(false));
        }

        private void Start()
        {
            bool ask = GameServices.IsInitialized && GameServices.Profile.Profile.consent < 0;
            gameObject.SetActive(ask);
            if (ask) Refresh();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            if (_status == null || !GameServices.IsInitialized) return;
            int c = GameServices.Profile.Profile.consent;
            _status.text = c < 0 ? "" : c == 1 ? "Hiện tại: đã đồng ý" : "Hiện tại: không đồng ý";
        }

        private void Choose(bool personalized)
        {
            if (GameServices.IsInitialized) GameServices.Ads.SetConsent(personalized);
            gameObject.SetActive(false);
        }
    }
}
