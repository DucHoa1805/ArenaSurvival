using System.Collections.Generic;
using ArenaSurvival.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace ArenaSurvival.Audio
{
    /// <summary>
    /// Phát nhạc nền và hiệu ứng âm thanh. Singleton sống xuyên scene (DontDestroyOnLoad) để nhạc
    /// không bị ngắt khi chuyển MainMenu ↔ Game; prefab được đặt ở cả 2 scene để vào thẳng scene nào
    /// cũng có âm thanh, bản thừa sẽ tự hủy.
    ///
    /// Âm thanh gameplay được kích hoạt qua GameEvents — gameplay không gọi thẳng vào AudioManager,
    /// giống cách HUD và hiệu ứng particle đang làm.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SoundBank _bank;
        [Tooltip("Cần có tham số exposed MasterVolume, MusicVolume, SFXVolume (xem VolumeSettings).")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Tooltip("Số tiếng hiệu ứng được phát chồng lên nhau cùng lúc. Mỗi tiếng cần 1 AudioSource riêng " +
                 "để đổi cao độ (pitch) mà không ảnh hưởng tiếng khác đang phát.")]
        [SerializeField] private int _sfxVoices = 10;

        // Lần phát gần nhất của mỗi loại âm thanh, để chặn phát dồn dập (xem SoundEffect.minInterval).
        private readonly Dictionary<SoundEffect, float> _lastPlayTime = new Dictionary<SoundEffect, float>();
        private AudioSource[] _voices;
        private int _nextVoice;
        private float _lastVolumePreviewTime = -1f;

        // Kéo slider bắn onValueChanged mỗi frame; giới hạn riêng của uiHover (0.04s) vẫn quá dày cho việc này.
        private const float VolumePreviewInterval = 0.15f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateVoices();
            PlayMusic();
        }

        private void CreateVoices()
        {
            // AudioSource gốc trên prefab là giọng đầu tiên; các giọng còn lại sao chép cấu hình của nó.
            _voices = new AudioSource[Mathf.Max(1, _sfxVoices)];
            _voices[0] = _sfxSource;
            for (int i = 1; i < _voices.Length; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = _sfxSource.spatialBlend;
                voice.outputAudioMixerGroup = _sfxSource.outputAudioMixerGroup;
                _voices[i] = voice;
            }
        }

        private void Start()
        {
            // Áp âm lượng đã lưu ở Start chứ không ở Awake: AudioMixer.SetFloat gọi trong Awake bị Unity bỏ qua.
            if (Instance != this) return;

            ApplyVolume(VolumeChannel.Master, VolumeSettings.Load(VolumeChannel.Master));
            ApplyVolume(VolumeChannel.Music, VolumeSettings.Load(VolumeChannel.Music));
            ApplyVolume(VolumeChannel.Sfx, VolumeSettings.Load(VolumeChannel.Sfx));
        }

        /// <summary>Gọi từ slider cài đặt: đổi âm lượng ngay và ghi nhớ (chưa ghi đĩa, xem VolumeSettings.Flush).</summary>
        public void SetVolume(VolumeChannel channel, float volume)
        {
            VolumeSettings.Store(channel, volume);
            ApplyVolume(channel, volume);

            // Cho người chơi nghe ngay mức SFX mới. Kênh Music thì nhạc nền đang phát sẵn, không cần.
            if (channel != VolumeChannel.Music && Time.unscaledTime - _lastVolumePreviewTime >= VolumePreviewInterval)
            {
                _lastVolumePreviewTime = Time.unscaledTime;
                Play(_bank.uiHover);
            }
        }

        private void ApplyVolume(VolumeChannel channel, float volume)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(VolumeSettings.ParameterName(channel), VolumeSettings.ToDecibels(volume));
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            // Bản thừa bị Destroy trong Awake vẫn kịp chạy OnEnable trước khi thật sự bị hủy
            // (Destroy chỉ có hiệu lực cuối frame) — không đăng ký để tránh phát âm thanh 2 lần.
            if (Instance != this) return;

            GameEvents.OnPlayerFired += HandlePlayerFired;
            GameEvents.OnProjectileHit += HandleProjectileHit;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnXpGained += HandleXpGained;
            GameEvents.OnLevelUp += HandleLevelUp;
            GameEvents.OnUpgradeChosen += HandleUpgradeChosen;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            // Hủy đăng ký luôn chạy (kể cả bản thừa): gỡ 1 handler chưa từng đăng ký là vô hại.
            GameEvents.OnPlayerFired -= HandlePlayerFired;
            GameEvents.OnProjectileHit -= HandleProjectileHit;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnXpGained -= HandleXpGained;
            GameEvents.OnLevelUp -= HandleLevelUp;
            GameEvents.OnUpgradeChosen -= HandleUpgradeChosen;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        /// <summary>Dùng cho âm thanh không đến từ GameEvents, ví dụ tiếng bấm nút UI.</summary>
        public void PlayUiClick() => Play(_bank.uiClick);
        public void PlayUiHover() => Play(_bank.uiHover);

        private void PlayMusic()
        {
            if (_bank.music == null) return;

            _musicSource.clip = _bank.music;
            _musicSource.volume = _bank.musicVolume;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        private void Play(SoundEffect effect)
        {
            if (effect == null) return;

            // Dùng unscaledTime: âm thanh UI vẫn phải phát được khi game đang dừng (timeScale = 0).
            float now = Time.unscaledTime;
            if (_lastPlayTime.TryGetValue(effect, out float last) && now - last < effect.minInterval) return;

            AudioClip clip = effect.PickClip();
            if (clip == null) return;

            _lastPlayTime[effect] = now;

            // Xoay vòng các giọng; lệch cao độ ngẫu nhiên một chút để tiếng lặp lại liên tục
            // (trúng đạn, quái chết) không bị nghe như máy.
            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = 1f + Random.Range(-effect.pitchVariation, effect.pitchVariation);
            voice.PlayOneShot(clip, effect.volume);
        }

        private void HandlePlayerFired() => Play(_bank.shoot);
        private void HandleProjectileHit(Vector3 position) => Play(_bank.projectileHit);
        private void HandleEnemyKilled(int xpReward, Vector3 position) => Play(_bank.enemyDeath);
        private void HandlePlayerDamaged(float amount) => Play(_bank.playerHurt);
        private void HandleXpGained(int amount) => Play(_bank.xpPickup);
        private void HandleLevelUp(int newLevel) => Play(_bank.levelUp);
        private void HandleUpgradeChosen() => Play(_bank.upgradeSelect);

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                Play(_bank.gameOver);
            }
        }
    }
}
