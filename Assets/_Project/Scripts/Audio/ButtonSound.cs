using UnityEngine;

namespace ArenaSurvival.Audio
{
    /// <summary>Gắn vào nút bất kỳ để phát tiếng click khi bấm, không cần sửa script của nút đó.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class ButtonSound : MonoBehaviour
    {
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
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }
        }
    }
}
