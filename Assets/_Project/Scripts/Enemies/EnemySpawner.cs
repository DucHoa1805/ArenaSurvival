using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private Enemy _enemyPrefab;
        [Tooltip("Các loại quái spawner này có thể sinh ra. Tất cả dùng chung 1 prefab và 1 pool — " +
                 "khác biệt giữa các loại nằm hoàn toàn trong EnemyData.")]
        [SerializeField] private EnemyData[] _enemyTypes;

        [Header("Spawn Timing")]
        [SerializeField] private float _spawnInterval = 2f;

        [Header("Spawn Area")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private float _spawnPadding = 2f; // khoảng cách ngoài rìa camera để quái xuất hiện

        [Header("Pooling")]
        [SerializeField] private int _poolPrewarmCount = 15;

        private float _spawnTimer;
        private ObjectPool<Enemy> _enemyPool;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            _enemyPool = new ObjectPool<Enemy>(_enemyPrefab, _poolPrewarmCount, transform);
        }

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            if (_enemyTypes == null || _enemyTypes.Length == 0) return;

            Vector2 spawnPosition = GetRandomPositionOutsideCamera();
            EnemyData data = _enemyTypes[Random.Range(0, _enemyTypes.Length)];

            Enemy enemy = _enemyPool.Get(spawnPosition, Quaternion.identity);
            enemy.Initialize(data);
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