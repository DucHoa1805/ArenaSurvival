using System.Collections.Generic;
using ArenaSurvival.Core;
using ArenaSurvival.Enemies;
using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Sinh quái theo lệnh của StageSession: đặt đợt hiện tại (sinh đều theo nhịp của đợt), ngừng sinh, hoặc sinh từng
    /// con cụ thể (quái tinh anh). Không tự biết màn có mấy đợt hay khi nào kết thúc — việc đó là của StageSession,
    /// nên cùng một spawner dùng được cho màn thường, màn boss (6B) hay chế độ khác sau này.
    /// </summary>
    public class WaveSpawner : MonoBehaviour, IEnemyWorld
    {
        [Header("Prefab")]
        [Tooltip("Mọi loại quái dùng chung 1 prefab và 1 pool — khác biệt nằm hoàn toàn trong EnemyData + EnemyModifiers.")]
        [SerializeField] private Enemy _enemyPrefab;

        [Header("Spawn Area")]
        [SerializeField] private Camera _targetCamera;
        [Tooltip("Vùng được phép sinh quái (tọa độ thế giới): phần sân cát bên trong viền đá, có chừa lề. " +
                 "Quái luôn xuất hiện trong sân nên không bao giờ bị kẹt ngoài bãi cỏ.")]
        [SerializeField] private Rect _arenaArea = new Rect(-9.1f, -9.1f, 18.2f, 18.2f);
        [Tooltip("Quái chỉ xuất hiện cách mép màn hình ít nhất bấy nhiêu, để không hiện ra ngay trước mặt người chơi.")]
        [SerializeField] private float _offscreenMargin = 0.5f;

        [Header("Pooling")]
        [SerializeField] private int _poolPrewarmCount = 15;

        [Header("Services")]
        [Tooltip("Pool đạn chung cho quái bắn xa và boss.")]
        [SerializeField] private EnemyProjectiles _projectiles;

        // Số lần thử bốc ngẫu nhiên 1 điểm trong sân nằm ngoài màn hình trước khi dùng phương án dự phòng.
        private const int MaxSpawnAttempts = 20;

        private ObjectPool<Enemy> _enemyPool;
        // Gán sẵn 1 lần: gán thẳng tên hàm vào OnReturnToPool mỗi lần lấy từ pool là mỗi lần tạo 1 delegate mới
        // (rác GC) — Profiler bắt được đúng chỗ này khi quái đông.
        private System.Action<Enemy> _releaseEnemy;
        private readonly Vector2[] _corners = new Vector2[4];
        // Danh sách quái đang sống, tạo sẵn đủ chỗ: để "nổ theo boss" và đếm quái con mà không phải tìm kiếm trong scene.
        private readonly List<Enemy> _alive = new List<Enemy>(512);

        private WaveData _wave;
        private EnemyModifiers _waveModifiers = EnemyModifiers.Default;
        private float _spawnTimer;

        /// <summary>Quái chết vì trúng đòn (LootDropper nghe để rơi đồ). Không phát khi DespawnAll.</summary>
        public event System.Action<Enemy> OnEnemyKilled;

        /// <summary>Số quái đang sống trên sân.</summary>
        public int ActiveCount => _alive.Count;
        public Rect ArenaArea => _arenaArea;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            _enemyPool = new ObjectPool<Enemy>(_enemyPrefab, _poolPrewarmCount, transform);
            _releaseEnemy = ReturnEnemyToPool;
        }

        /// <summary>Bắt đầu sinh đều quái của đợt này. Hệ số (độ khó theo màn) áp cho mọi con sinh ra trong đợt.</summary>
        public void SetWave(WaveData wave, EnemyModifiers modifiers)
        {
            _wave = wave;
            _waveModifiers = modifiers;
            _spawnTimer = 0f;
        }

        /// <summary>Ngừng sinh theo nhịp (đợt cuối). Quái đang sống vẫn còn trên sân.</summary>
        public void StopSpawning() => _wave = null;

        /// <summary>Sinh ngay 1 con cụ thể, ví dụ quái tinh anh của đợt cuối.</summary>
        public Enemy Spawn(EnemyData data, EnemyModifiers modifiers) => SpawnAt(data, modifiers, GetSpawnPosition());

        /// <summary>Triệu hồi tại vị trí cho trước (boss gọi quái con quanh mình).</summary>
        public Enemy Summon(EnemyData data, EnemyModifiers modifiers, Vector2 position) => SpawnAt(data, modifiers, position);

        private Enemy SpawnAt(EnemyData data, EnemyModifiers modifiers, Vector2 position)
        {
            Enemy enemy = _enemyPool.Get(position, Quaternion.identity);
            enemy.SetWorld(this, _projectiles);
            enemy.Initialize(data, modifiers);
            enemy.OnReturnToPool = _releaseEnemy;
            _alive.Add(enemy);
            return enemy;
        }

        public int CountAlive(EnemyData data)
        {
            int count = 0;
            for (int i = 0; i < _alive.Count; i++)
            {
                if (_alive[i].Data == data) count++;
            }
            return count;
        }

        /// <summary>Quái không va chạm tường (tường chỉ giữ Player) nên tự kẹp trong vùng sân.</summary>
        public Vector2 ClampToArena(Vector2 position) => new Vector2(
            Mathf.Clamp(position.x, _arenaArea.xMin, _arenaArea.xMax),
            Mathf.Clamp(position.y, _arenaArea.yMin, _arenaArea.yMax));

        public void NotifyKilled(Enemy enemy) => OnEnemyKilled?.Invoke(enemy);

        /// <summary>Mọi quái còn sống nổ ngay, không rơi XP (khi boss chết).</summary>
        public void DespawnAll()
        {
            // Duyệt ngược: mỗi con trả về pool tự xóa mình khỏi danh sách.
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (i < _alive.Count) _alive[i].Despawn(false);
            }
        }

        /// <summary>Như DespawnAll nhưng giữ lại những con keep trả về true (tinh anh khi boss xuất hiện).</summary>
        public void DespawnAllExcept(System.Predicate<Enemy> keep)
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (i < _alive.Count && !keep(_alive[i])) _alive[i].Despawn(false);
            }
        }

        /// <summary>
        /// Sinh ngay thêm quái (loại của đợt hiện tại), không theo nhịp. Dùng cho StressTest;
        /// vẫn đi qua pool như quái thường nên đo được đúng chi phí của game thật.
        /// </summary>
        public void SpawnImmediately(int count)
        {
            for (int i = 0; i < count; i++) SpawnFromWave();
        }

        private void Update()
        {
            if (_wave == null) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _wave.spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnFromWave();
            }
        }

        private void SpawnFromWave()
        {
            if (_wave == null) return;

            EnemyData[] enemyTypes = _wave.enemyTypes;
            if (enemyTypes == null || enemyTypes.Length == 0) return;

            Spawn(enemyTypes[Random.Range(0, enemyTypes.Length)], _waveModifiers);
        }

        private void ReturnEnemyToPool(Enemy enemy)
        {
            // Release bỏ qua object đã tắt (trả 2 lần); chỉ xóa khỏi danh sách khi lần trả này thật sự có hiệu lực.
            if (enemy.gameObject.activeSelf)
            {
                // Xóa kiểu "đổi chỗ với phần tử cuối": không phải dời cả nửa danh sách; thứ tự không quan trọng.
                int index = _alive.IndexOf(enemy);
                if (index >= 0)
                {
                    int last = _alive.Count - 1;
                    _alive[index] = _alive[last];
                    _alive.RemoveAt(last);
                }
            }
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
            // Mảng tạo sẵn: đợt cuối có thể rơi vào nhánh này nhiều lần liên tiếp, không cấp phát mỗi lần.
            _corners[0] = new Vector2(_arenaArea.xMin, _arenaArea.yMin);
            _corners[1] = new Vector2(_arenaArea.xMax, _arenaArea.yMin);
            _corners[2] = new Vector2(_arenaArea.xMin, _arenaArea.yMax);
            _corners[3] = new Vector2(_arenaArea.xMax, _arenaArea.yMax);

            Vector2 best = _corners[0];
            foreach (Vector2 corner in _corners)
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
