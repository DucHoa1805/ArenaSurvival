using UnityEngine;
using UnityEngine.EventSystems;

namespace ArenaSurvival.Audio
{
    /// <summary>
    /// Gắn vào nút bất kỳ để có tiếng khi rê chuột vào và khi bấm, không cần sửa script của nút đó.
    /// Thẻ nâng cấp tắt tiếng bấm vì đã có tiếng riêng phát qua GameEvents.OnUpgradeChosen.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class ButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private bool _playClick = true;
        [SerializeField] private bool _playHover = true;

        private UnityEngine.UI.Button _button;

        private void Awake()
        {
            _button = GetComponent<UnityEngine.UI.Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            // AudioManager có thể chưa tồn tại (ví dụ scene test không có nó) — khi đó chỉ bỏ qua tiếng.
            if (_playClick && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_playHover && _button.interactable && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiHover();
            }
        }
    }
}
