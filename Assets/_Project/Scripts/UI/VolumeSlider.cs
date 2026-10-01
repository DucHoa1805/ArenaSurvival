using ArenaSurvival.Audio;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Một hàng chỉnh âm lượng cho 1 kênh. Tự đọc giá trị đã lưu mỗi lần hiện ra, nên menu nào cần thì
    /// chỉ việc đặt prefab vào, không phải nối gì thêm.
    /// </summary>
    public class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private VolumeChannel _channel;
        [SerializeField] private UnityEngine.UI.Slider _slider;
        [SerializeField] private TMP_Text _valueText;

        private void Awake()
        {
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDestroy() => _slider.onValueChanged.RemoveListener(HandleValueChanged);

        private void OnEnable()
        {
            // Đọc lại mỗi lần hiện: cùng 1 kênh có slider ở cả menu chính lẫn menu ESC.
            // SetValueWithoutNotify để việc hiển thị không bị coi là người chơi vừa kéo.
            float volume = VolumeSettings.Load(_channel);
            _slider.SetValueWithoutNotify(volume);
            UpdateText(volume);
        }

        private void OnDisable()
        {
            // Bảng cài đặt đóng (hoặc rời scene) mới ghi xuống đĩa, không ghi mỗi frame lúc kéo.
            VolumeSettings.Flush();
        }

        private void HandleValueChanged(float volume)
        {
            // Không có AudioManager (scene test) thì vẫn lưu, lần sau có AudioManager sẽ áp dụng.
            if (AudioManager.Instance != null) AudioManager.Instance.SetVolume(_channel, volume);
            else VolumeSettings.Store(_channel, volume);

            UpdateText(volume);
        }

        private void UpdateText(float volume)
        {
            if (_valueText != null) _valueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }
    }
}
