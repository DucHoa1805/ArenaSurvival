using System;
using UnityEngine;

namespace ArenaSurvival.Audio
{
    /// <summary>
    /// 1 loại âm thanh trong game (tiếng bắn, tiếng trúng...). Có thể gồm nhiều clip: mỗi lần phát
    /// chọn ngẫu nhiên 1 clip để âm thanh lặp lại nhiều lần không bị đơn điệu.
    /// </summary>
    [Serializable]
    public class SoundEffect
    {
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Khoảng cách tối thiểu (giây) giữa 2 lần phát. Nhiều quái chết cùng lúc sẽ không " +
                 "cộng dồn thành 1 tiếng nổ chói tai.")]
        public float minInterval = 0.05f;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }
}
