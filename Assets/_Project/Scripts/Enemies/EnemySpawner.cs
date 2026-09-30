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
        [SerializeField] private float _spawnPadding = 2f; // khoảng cách ngoài rìa camera để quái xuất hiện

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

            Vector2 spawnPosition = GetRandomPositionOutsideCamera();
            EnemyData data = enemyTypes[Random.Range(0, enemyTypes.Length)];

            Enemy enemy = _enemyPool.Get(spawnPosition, Quaternion.identity);
            enemy.Initialize(data, _healthMultiplier);
            enemy.OnReturnToPool = ReturnEnemyToPool;
        }

        private void ReturnEnemyToPool(Enemy enemy)
        {
            _enemyPool.Release(enemy);
        }

        private Vector2 GetRandomPositionOutsideCamera()
        {
            float camHeight = _targetCamera.orthographicSize;
            float camWidth = camHeight * _targetCamera.aspect;
            Vector2 camCenter = _targetCamera.transform.position;

            // Chọn ngẫu nhiên 1 trong 4 cạnh để spawn ngoài rìa màn hình.
            int side = Random.Range(0, 4);
            float x, y;

            switch (side)
            {
                case 0: // trên
                    x = Random.Range(-camWidth, camWidth);
                    y = camHeight + _spawnPadding;
                    break;
                case 1: // dưới
                    x = Random.Range(-camWidth, camWidth);
                    y = -camHeight - _spawnPadding;
                    break;
                case 2: // trái
                    x = -camWidth - _spawnPadding;
                    y = Random.Range(-camHeight, camHeight);
                    break;
                default: // phải
                    x = camWidth + _spawnPadding;
                    y = Random.Range(-camHeight, camHeight);
                    break;
            }

            return camCenter + new Vector2(x, y);
        }
    }
}
