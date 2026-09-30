using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Sinh quái theo từng wave định nghĩa bằng WaveData. Hết danh sách wave thì lặp lại wave cuối
    /// vô tận, mỗi vòng quái trâu hơn và ra dày hơn — nên game luôn khó dần mà không cần tạo
    /// thêm asset.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private Enemy _enemyPrefab;

        [Header("Waves")]
        [Tooltip("Chạy lần lượt từ trên xuống. Tất cả loại quái dùng chung 1 prefab và 1 pool — " +
                 "khác biệt giữa các loại nằm hoàn toàn trong EnemyData.")]
        [SerializeField] private WaveData[] _waves;

        [Header("Endless Scaling")]
        [Tooltip("Mỗi lần lặp lại wave cuối, máu quái tăng thêm bấy nhiêu lần máu gốc (0.2 = +20%).")]
        [SerializeField] private float _healthGrowthPerLoop = 0.2f;
        [Tooltip("Mỗi lần lặp lại wave cuối, khoảng cách giữa 2 lần spawn nhân với số này (0.9 = nhanh hơn 10%).")]
        [SerializeField] private float _intervalMultiplierPerLoop = 0.9f;
        [SerializeField] private float _minSpawnInterval = 0.3f; // sàn để số quái không tăng vô hạn

        [Header("Spawn Area")]
        [SerializeField] private Camera _targetCamera;
        [Tooltip("Vùng được phép sinh quái (tọa độ thế giới): phần sân cát bên trong viền đá, có chừa lề. " +
                 "Quái luôn xuất hiện trong sân nên không bao giờ bị kẹt ngoài bãi cỏ.")]
        [SerializeField] private Rect _arenaArea = new Rect(-9.1f, -9.1f, 18.2f, 18.2f);
        [Tooltip("Quái chỉ xuất hiện cách mép màn hình ít nhất bấy nhiêu, để không hiện ra ngay trước mặt người chơi.")]
        [SerializeField] private float _offscreenMargin = 0.5f;

        // Số lần thử bốc ngẫu nhiên 1 điểm trong sân nằm ngoài màn hình trước khi dùng phương án dự phòng.
        private const int MaxSpawnAttempts = 20;

        [Header("Pooling")]
        [SerializeField] private int _poolPrewarmCount = 15;

        private ObjectPool<Enemy> _enemyPool;

        private int _waveIndex;   // vị trí trong _waves
        private int _waveNumber;  // số thứ tự hiển thị cho người chơi, tăng mãi kể cả khi lặp wave cuối
        private float _waveTimer;
        private float _spawnTimer;
        private float _currentInterval;
        private float _healthMultiplier = 1f;
        private float _intervalScale = 1f;

        private WaveData CurrentWave => _waves[_waveIndex];

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            _enemyPool = new ObjectPool<Enemy>(_enemyPrefab, _poolPrewarmCount, transform);
        }

        private void Start()
        {
            if (_waves == null || _waves.Length == 0)
            {
                Debug.LogWarning($"{nameof(EnemySpawner)}: chưa gán wave nào, spawner sẽ không sinh quái.", this);
                enabled = false;
                return;
            }

            // Bắt đầu ở Start (không phải Awake) để các listener đăng ký trong OnEnable
            // kịp nhận sự kiện OnWaveStarted của wave đầu tiên.
            BeginWave();
        }

        private void Update()
        {
            _waveTimer += Time.deltaTime;
            if (_waveTimer >= CurrentWave.duration)
            {
                AdvanceWave();
            }

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _currentInterval)
            {
                _spawnTimer = 0f;
                SpawnEnemy();
            }
        }

        private void AdvanceWave()
        {
            if (_waveIndex < _waves.Length - 1)
            {
                _waveIndex++;
            }
            else
            {
                // Hết wave soạn sẵn: giữ nguyên wave cuối nhưng tăng độ khó thêm 1 nấc.
                _healthMultiplier += _healthGrowthPerLoop;
                _intervalScale *= _intervalMultiplierPerLoop;
            }

            BeginWave();
        }

        private void BeginWave()
        {
            _waveTimer = 0f;
            _waveNumber++;
            _currentInterval = Mathf.Max(_minSpawnInterval, CurrentWave.spawnInterval * _intervalScale);

            GameEvents.WaveStarted(_waveNumber);
        }

        private void SpawnEnemy()
        {
            EnemyData[] enemyTypes = CurrentWave.enemyTypes;
            if (enemyTypes == null || enemyTypes.Length == 0) return;

            Vector2 spawnPosition = GetSpawnPosition();
            EnemyData data = enemyTypes[Random.Range(0, enemyTypes.Length)];

            Enemy enemy = _enemyPool.Get(spawnPosition, Quaternion.identity);
            enemy.Initialize(data, _healthMultiplier);
            enemy.OnReturnToPool = ReturnEnemyToPool;
        }

        private void ReturnEnemyToPool(Enemy enemy)
        {
            _enemyPool.Release(enemy);
        }

        /// <summary>
        /// 1 điểm trong sân nhưng ngoài tầm nhìn camera. Sân (19.2 đơn vị) rộng hơn màn hình theo chiều dọc
        /// (10 đơn vị) nên luôn còn khoảng trống phía trên hoặc dưới; bốc ngẫu nhiên vài lần là gần như chắc chắn trúng.
        /// </summary>
        private Vector2 GetSpawnPosition()
        {
            Rect view = GetCameraView(_offscreenMargin);

            for (int i = 0; i < MaxSpawnAttempts; i++)
            {
                var candidate = new Vector2(
                    Random.Range(_arenaArea.xMin, _arenaArea.xMax),
                    Random.Range(_arenaArea.yMin, _arenaArea.yMax));

                if (!view.Contains(candidate)) return candidate;
            }

            // Dự phòng (rất hiếm): góc sân xa camera nhất, chấp nhận có thể lọt vào mép màn hình.
            return GetFarthestCorner(view.center);
        }

        private Rect GetCameraView(float margin)
        {
            float halfHeight = _targetCamera.orthographicSize + margin;
            float halfWidth = _targetCamera.orthographicSize * _targetCamera.aspect + margin;
            Vector2 center = _targetCamera.transform.position;
            return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }

        private Vector2 GetFarthestCorner(Vector2 from)
        {
            Vector2[] corners =
            {
                new Vector2(_arenaArea.xMin, _arenaArea.yMin), new Vector2(_arenaArea.xMax, _arenaArea.yMin),
                new Vector2(_arenaArea.xMin, _arenaArea.yMax), new Vector2(_arenaArea.xMax, _arenaArea.yMax),
            };

            Vector2 best = corners[0];
            foreach (Vector2 corner in corners)
            {
                if ((corner - from).sqrMagnitude > (best - from).sqrMagnitude) best = corner;
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ vùng sinh quái trong Scene view để chỉnh cho khớp với sân.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_arenaArea.center, _arenaArea.size);
        }
    }
}
