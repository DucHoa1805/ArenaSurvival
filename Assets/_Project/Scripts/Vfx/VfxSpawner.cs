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

        [Header("References")]
        [SerializeField] private Transform _player;

        [Header("Pooling")]
        [SerializeField] private int _hitPrewarmCount = 20;
        [SerializeField] private int _deathPrewarmCount = 15;

        private ObjectPool<PooledVfx> _hitPool;
        private ObjectPool<PooledVfx> _deathPool;
        private ObjectPool<PooledVfx> _levelUpPool;
        private GameState _previousState;

        private void Awake()
        {
            _hitPool = new ObjectPool<PooledVfx>(_hitPrefab, _hitPrewarmCount, transform);
            _deathPool = new ObjectPool<PooledVfx>(_enemyDeathPrefab, _deathPrewarmCount, transform);
            _levelUpPool = new ObjectPool<PooledVfx>(_levelUpPrefab, 1, transform);
        }

        private void OnEnable()
        {
            GameEvents.OnProjectileHit += HandleProjectileHit;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnProjectileHit -= HandleProjectileHit;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleProjectileHit(Vector3 position) => Play(_hitPool, position);

        private void HandleEnemyKilled(int xpReward, Vector3 position) => Play(_deathPool, position);

        private void HandleGameStateChanged(GameState newState)
        {
            // Hiệu ứng lên cấp nổ ra lúc người chơi chọn xong nâng cấp và game chạy tiếp,
            // chứ không phải lúc vừa lên cấp — khi đó game đang dừng và bảng chọn đang che màn hình.
            if (_previousState == GameState.LevelUp && newState == GameState.Playing)
            {
                Play(_levelUpPool, _player.position);
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
