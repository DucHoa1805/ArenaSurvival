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
        [Tooltip("Không bắt buộc: chữ \"CẤP n!\" bay lên trên đầu Player khi chọn xong nâng cấp.")]
        [SerializeField] private FloatingText _levelUpTextPrefab;
        [Tooltip("Không bắt buộc: số sát thương bay lên trên đầu quái.")]
        [SerializeField] private FloatingText _damageNumberPrefab;
        [Tooltip("Lệch ngang ngẫu nhiên tối đa, để số của nhiều quái đứng sát nhau không đè lên nhau.")]
        [SerializeField] private float _damageNumberJitter = 0.25f;

        [Header("References")]
        [SerializeField] private Transform _player;

        [Header("Pooling")]
        [SerializeField] private int _hitPrewarmCount = 20;
        [SerializeField] private int _deathPrewarmCount = 15;
        [SerializeField] private int _damageNumberPrewarmCount = 20;

        private ObjectPool<PooledVfx> _hitPool;
        private ObjectPool<PooledVfx> _deathPool;
        private ObjectPool<PooledVfx> _levelUpPool;
        private ObjectPool<FloatingText> _damageNumberPool;
        private GameState _previousState;
        private FloatingText _levelUpText;
        private int _currentLevel = 1;

        private void Awake()
        {
            _hitPool = new ObjectPool<PooledVfx>(_hitPrefab, _hitPrewarmCount, transform);
            _deathPool = new ObjectPool<PooledVfx>(_enemyDeathPrefab, _deathPrewarmCount, transform);
            _levelUpPool = new ObjectPool<PooledVfx>(_levelUpPrefab, 1, transform);

            if (_levelUpTextPrefab != null)
            {
                _levelUpText = Instantiate(_levelUpTextPrefab, transform);
                _levelUpText.gameObject.SetActive(false);
            }

            if (_damageNumberPrefab != null)
            {
                _damageNumberPool = new ObjectPool<FloatingText>(_damageNumberPrefab, _damageNumberPrewarmCount, transform);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnProjectileHit += HandleProjectileHit;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnEnemyDamaged += HandleEnemyDamaged;
            GameEvents.OnLevelUp += HandleLevelUp;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnProjectileHit -= HandleProjectileHit;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnEnemyDamaged -= HandleEnemyDamaged;
            GameEvents.OnLevelUp -= HandleLevelUp;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleLevelUp(int newLevel) => _currentLevel = newLevel;

        private void HandleProjectileHit(Vector3 position) => Play(_hitPool, position);

        private void HandleEnemyKilled(int xpReward, Vector3 position) => Play(_deathPool, position);

        private void HandleEnemyDamaged(float amount, Vector3 position)
        {
            if (_damageNumberPool == null) return;

            position.x += Random.Range(-_damageNumberJitter, _damageNumberJitter);
            FloatingText text = _damageNumberPool.Get(position, Quaternion.identity);
            text.OnReturnToPool = _damageNumberPool.Release;
            // Làm tròn và tối thiểu 1: sát thương lẻ (hệ số nâng cấp x1.2) hiện "12.4" trông rối mắt,
            // còn hiện "0" thì người chơi tưởng đòn không trúng.
            text.Show(Mathf.Max(1, Mathf.RoundToInt(amount)).ToString(), position);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            // Hiệu ứng lên cấp nổ ra lúc người chơi chọn xong nâng cấp và game chạy tiếp,
            // chứ không phải lúc vừa lên cấp — khi đó game đang dừng và bảng chọn đang che màn hình.
            if (_previousState == GameState.LevelUp && newState == GameState.Playing)
            {
                Play(_levelUpPool, _player.position);
                if (_levelUpText != null)
                {
                    _levelUpText.Show($"CẤP {_currentLevel}!", _player.position + Vector3.up * 0.7f);
                }
            }

            _previousState = newState;
        }

        private static void Play(ObjectPool<PooledVfx> pool, Vector3 position)
        {
            PooledVfx vfx = pool.Get(position, Quaternion.identity);
            vfx.OnReturnToPool = pool.Release;
        }
    }
}
