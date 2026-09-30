using UnityEngine;

namespace ArenaSurvival.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject _enemyPrefab;

        [Header("Spawn Timing")]
        [SerializeField] private float _spawnInterval = 2f;

        [Header("Spawn Area")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private float _spawnPadding = 2f; // khoảng cách ngoài rìa camera để quái xuất hiện

        private float _spawnTimer;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
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
            Vector2 spawnPosition = GetRandomPositionOutsideCamera();

            // TODO (Phase 2 cuối): thay Instantiate bằng object pool để tránh cấp phát GC liên tục.
            Instantiate(_enemyPrefab, spawnPosition, Quaternion.identity);
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