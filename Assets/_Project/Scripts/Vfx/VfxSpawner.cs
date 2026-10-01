using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Vfx
{
    /// <summary>
    /// Phát hiệu ứng particle theo sự kiện game. Gameplay không biết hiệu ứng tồn tại —
    /// bỏ object này khỏi scene thì game vẫn chạy y hệt, chỉ mất phần hình ảnh.
    /// Hiệu ứng sinh ra liên tục (mỗi phát trúng, mỗi con quái chết) nên đều đi qua pool.
    /// </summary>
    public class VfxSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PooledVfx _hitPrefab;
        [SerializeField] private PooledVfx _enemyDeathPrefab;
        [SerializeField] private PooledVfx _levelUpPrefab;
        [Tooltip("Không bắt buộc: hiệu ứng nổ của đạn Phóng lựu (phóng to theo bán kính nổ).")]
        [SerializeField] private PooledVfx _explosionPrefab;
        [Tooltip("Không bắt buộc: vòng sáng xanh khi Healer hồi máu cho quái (phóng to theo bán kính).")]
        [SerializeField] private PooledVfx _healPulsePrefab;
        [Tooltip("Bán kính nổ mà prefab hiệu ứng nổ trông vừa khớp ở scale 1.")]
        [SerializeField] private float _explosionPrefabRadius = 1f;
        [Tooltip("Không bắt buộc: chữ \"CẤP n!\" bay lên trên đầu Player khi chọn xong nâng cấp.")]
        [SerializeField] private FloatingText _levelUpTextPrefab;
        [Tooltip("Không bắt buộc: số sát thương bay lên trên đầu quái.")]
        [SerializeField] private FloatingText _damageNumberPrefab;
        [Tooltip("Lệch ngang ngẫu nhiên tối đa, để số của nhiều quái đứng sát nhau không đè lên nhau.")]
        [SerializeField] private float _damageNumberJitter = 0.25f;

        [Header("References")]
        [SerializeField] private Transform _player;

        [Header("Pooling")]
        // Số tối đa cùng lúc, và cũng là số tạo sẵn lúc vào trận: lúc chơi không bao giờ phải Instantiate thêm.
        // Đo ở 300 quái: bình thường chỉ ~30-65 cái cùng lúc, nhưng không giới hạn thì pool phình lên 180-250
        // theo từng đợt đỉnh, mỗi lần phình là 1 frame giật. Vượt giới hạn thì bỏ qua hiệu ứng đó.
        [Tooltip("Tia lửa khi đạn trúng quái.")]
        [SerializeField] private int _maxHitEffects = 48;
        [Tooltip("Hiệu ứng nổ khi quái chết.")]
        [SerializeField] private int _maxDeathEffects = 32;
        [Tooltip("Hơn số này thì màn hình toàn chữ số, đọc không nổi nữa.")]
        [SerializeField] private int _maxDamageNumbers = 60;
        [Tooltip("Phóng lựu bắn ~1 quả/giây; dư cho súng nâng tốc bắn trong màn.")]
        [SerializeField] private int _maxExplosions = 12;

        private ObjectPool<PooledVfx> _hitPool;
        private ObjectPool<PooledVfx> _deathPool;
        private ObjectPool<PooledVfx> _levelUpPool;
        private ObjectPool<FloatingText> _damageNumberPool;
        private ObjectPool<PooledVfx> _explosionPool;
        private ObjectPool<PooledVfx> _healPool;
        private System.Action<PooledVfx> _releaseHeal;
        private System.Action<PooledVfx> _releaseExplosion;
        // Gán sẵn 1 lần: gán thẳng tên hàm vào OnReturnToPool mỗi lần lấy từ pool là mỗi lần tạo 1 delegate mới (rác GC) — Profiler bắt được đúng chỗ này khi quái đông.
        private System.Action<PooledVfx> _releaseHit;
        private System.Action<PooledVfx> _releaseDeath;
        private System.Action<PooledVfx> _releaseLevelUp;
        private System.Action<FloatingText> _releaseDamageNumber;
        private GameState _previousState;
        private FloatingText _levelUpText;
        private int _currentLevel = 1;

        private void Awake()
        {
            _hitPool = new ObjectPool<PooledVfx>(_hitPrefab, _maxHitEffects, transform, _maxHitEffects);
            _deathPool = new ObjectPool<PooledVfx>(_enemyDeathPrefab, _maxDeathEffects, transform, _maxDeathEffects);
            _levelUpPool = new ObjectPool<PooledVfx>(_levelUpPrefab, 1, transform);
            _releaseHit = _hitPool.Release;
            _releaseDeath = _deathPool.Release;
            _releaseLevelUp = _levelUpPool.Release;

            if (_levelUpTextPrefab != null)
            {
                _levelUpText = Instantiate(_levelUpTextPrefab, transform);
                _levelUpText.gameObject.SetActive(false);
            }

            if (_explosionPrefab != null)
            {
                _explosionPool = new ObjectPool<PooledVfx>(_explosionPrefab, _maxExplosions, transform, _maxExplosions);
                _releaseExplosion = _explosionPool.Release;
            }

            if (_healPulsePrefab != null)
            {
                // Healer hồi mỗi 4 s; vài con cùng lúc là nhiều — vượt giới hạn thì bỏ qua hiệu ứng.
                _healPool = new ObjectPool<PooledVfx>(_healPulsePrefab, 6, transform, 6);
                _releaseHeal = _healPool.Release;
            }

            if (_damageNumberPrefab != null)
            {
                _damageNumberPool = new ObjectPool<FloatingText>(
                    _damageNumberPrefab, _maxDamageNumbers, transform, _maxDamageNumbers);
                _releaseDamageNumber = _damageNumberPool.Release;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnProjectileHit += HandleProjectileHit;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnEnemyDamaged += HandleEnemyDamaged;
            GameEvents.OnLevelUp += HandleLevelUp;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnExplosion += HandleExplosion;
            GameEvents.OnEnemyHealPulse += HandleHealPulse;
        }

        private void OnDisable()
        {
            GameEvents.OnProjectileHit -= HandleProjectileHit;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnEnemyDamaged -= HandleEnemyDamaged;
            GameEvents.OnLevelUp -= HandleLevelUp;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnExplosion -= HandleExplosion;
            GameEvents.OnEnemyHealPulse -= HandleHealPulse;
        }

        private void HandleLevelUp(int newLevel) => _currentLevel = newLevel;

        private void HandleProjectileHit(Vector3 position) => Play(_hitPool, _releaseHit, position);

        private void HandleEnemyKilled(int xpReward, Vector3 position) => Play(_deathPool, _releaseDeath, position);

        private void HandleExplosion(Vector3 position, float radius)
        {
            if (_explosionPool == null || !_explosionPool.TryGet(position, Quaternion.identity, out PooledVfx vfx)) return;
            vfx.OnReturnToPool = _releaseExplosion;
            // Đặt lại scale mỗi lần lấy từ pool: nâng sao Phóng lựu làm bán kính nổ to hơn.
            vfx.transform.localScale = Vector3.one * (radius / Mathf.Max(0.01f, _explosionPrefabRadius));
        }

        private void HandleHealPulse(Vector3 position, float radius)
        {
            if (_healPool == null || !_healPool.TryGet(position, Quaternion.identity, out PooledVfx vfx)) return;
            vfx.OnReturnToPool = _releaseHeal;
            vfx.transform.localScale = Vector3.one * (radius / Mathf.Max(0.01f, _explosionPrefabRadius));
        }

        private void HandleEnemyDamaged(float amount, Vector3 position)
        {
            if (_damageNumberPool == null) return;

            position.x += Random.Range(-_damageNumberJitter, _damageNumberJitter);
            if (!_damageNumberPool.TryGet(position, Quaternion.identity, out FloatingText text)) return;
            text.OnReturnToPool = _releaseDamageNumber;
            // Làm tròn và tối thiểu 1: sát thương lẻ (hệ số nâng cấp x1.2) hiện "12.4" trông rối mắt,
            // còn hiện "0" thì người chơi tưởng đòn không trúng.
            text.ShowNumber(Mathf.Max(1, Mathf.RoundToInt(amount)), position);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            // Hiệu ứng lên cấp nổ ra lúc người chơi chọn xong nâng cấp và game chạy tiếp,
            // chứ không phải lúc vừa lên cấp — khi đó game đang dừng và bảng chọn đang che màn hình.
            if (_previousState == GameState.LevelUp && newState == GameState.Playing)
            {
                Play(_levelUpPool, _releaseLevelUp, _player.position);
                if (_levelUpText != null)
                {
                    _levelUpText.Show($"CẤP {_currentLevel}!", _player.position + Vector3.up * 0.7f);
                }
            }

            _previousState = newState;
        }

        private static void Play(ObjectPool<PooledVfx> pool, System.Action<PooledVfx> release, Vector3 position)
        {
            if (!pool.TryGet(position, Quaternion.identity, out PooledVfx vfx)) return;
            vfx.OnReturnToPool = release;
        }
    }
}
