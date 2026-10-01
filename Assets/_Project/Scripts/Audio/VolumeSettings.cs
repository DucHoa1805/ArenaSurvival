using UnityEngine;

namespace ArenaSurvival.Audio
{
    public enum VolumeChannel
    {
        Master,
        Music,
        Sfx,
    }

    /// <summary>
    /// Đọc/ghi âm lượng người chơi chọn và đổi sang dB cho AudioMixer.
    ///
    /// Lưu bằng PlayerPrefs chứ không vào file save tiến độ: cài đặt thuộc về thiết bị, không thuộc hồ sơ người chơi
    /// (cloud save ở Phase 12 không nên kéo âm lượng từ máy khác về), và file save tiến độ có vòng đời ghi riêng.
    /// Vài số lẻ kiểu cài đặt cũng đúng là việc PlayerPrefs sinh ra để làm.
    /// </summary>
    public static class VolumeSettings
    {
        // Mặc định 1 = 0 dB: lần đầu chơi nghe y như trước khi có mixer, cân bằng gốc nằm ở SoundBank.
        public const float DefaultVolume = 1f;
        public const float MinDecibels = -80f; // mức thấp nhất AudioMixer hỗ trợ, coi như tắt tiếng

        private const float MinLinear = 0.0001f; // 20 * log10(0.0001) = -80 dB

        public static float Load(VolumeChannel channel) => PlayerPrefs.GetFloat(Key(channel), DefaultVolume);

        /// <summary>Chỉ ghi vào bộ nhớ; gọi Flush để ghi xuống đĩa.</summary>
        public static void Store(VolumeChannel channel, float volume) =>
            PlayerPrefs.SetFloat(Key(channel), Mathf.Clamp01(volume));

        // Tách khỏi Store để không ghi đĩa mỗi frame lúc đang kéo slider; chỉ ghi khi đóng bảng cài đặt.
        public static void Flush() => PlayerPrefs.Save();

        /// <summary>Tên tham số exposed trong MainMixer, phải khớp chính xác với tên đặt trong Audio Mixer window.</summary>
        public static string ParameterName(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Music: return "MusicVolume";
                case VolumeChannel.Sfx: return "SFXVolume";
                default: return "MasterVolume";
            }
        }

        /// <summary>
        /// Slider 0..1 sang dB. Tai người nghe theo thang log: đặt thẳng giá trị tuyến tính thì nửa trên slider
        /// gần như không đổi gì còn đoạn cuối tụt hẳn. Qua log10 thì 0.5 ≈ -6 dB, nghe như "nhỏ đi một nửa".
        /// </summary>
        public static float ToDecibels(float linear)
        {
            linear = Mathf.Clamp01(linear);
            return linear <= MinLinear ? MinDecibels : Mathf.Log10(linear) * 20f;
        }

        private static string Key(VolumeChannel channel) => "volume." + channel;
    }
}
