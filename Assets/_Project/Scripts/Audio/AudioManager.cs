using System.Collections.Generic;
using ArenaSurvival.Core;
using UnityEngine;

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
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        // Lần phát gần nhất của mỗi loại âm thanh, để chặn phát dồn dập (xem SoundEffect.minInterval).
        private readonly Dictionary<SoundEffect, float> _lastPlayTime = new Dictionary<SoundEffect, float>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            PlayMusic();
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
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        /// <summary>Dùng cho âm thanh không đến từ GameEvents, ví dụ tiếng bấm nút UI.</summary>
        public void PlayUiClick() => Play(_bank.uiClick);

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
            _sfxSource.PlayOneShot(clip, effect.volume);
        }

        private void HandlePlayerFired() => Play(_bank.shoot);
        private void HandleProjectileHit(Vector3 position) => Play(_bank.projectileHit);
        private void HandleEnemyKilled(int xpReward, Vector3 position) => Play(_bank.enemyDeath);
        private void HandlePlayerDamaged(float amount) => Play(_bank.playerHurt);
        private void HandleXpGained(int amount) => Play(_bank.xpPickup);
        private void HandleLevelUp(int newLevel) => Play(_bank.levelUp);

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                Play(_bank.gameOver);
            }
        }
    }
}
