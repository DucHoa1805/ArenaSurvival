using ArenaSurvival.Core;
using ArenaSurvival.Player;
using UnityEngine;
using ArenaSurvival.Combat;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// 1 khẩu súng tự bắn quái gần nhất. Player có thể gắn nhiều component này (mỗi cái 1 WeaponData);
    /// khẩu chưa mở khóa để disabled và được UpgradeManager bật lên khi người chơi chọn nâng cấp tương ứng.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerWeapon : MonoBehaviour, IWeapon
    {
        [Header("Data")]
        [SerializeField] private WeaponData _weaponData;

        public WeaponData Data => _weaponData;
        public WeaponDefinition Definition => _weaponData;

        [Header("References")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private LayerMask _enemyLayer; // chỉ tìm mục tiêu trên layer Enemy

        [Header("Pooling")]
        [SerializeField] private int _poolPrewarmCount = 20;

        // Số collider tối đa đọc được trong 1 lần quét. Nếu số quái trong tầm vượt con số này,
        // phần dư bị bỏ qua và có thể sót con gần nhất — nên để dư so với mật độ quái thực tế.
        private const int MaxTargetsPerScan = 64;

        private float _fireCooldown;
        private ObjectPool<Projectile> _projectilePool;
        private PlayerStats _stats;
        // Gán sẵn 1 lần: gán thẳng tên hàm vào OnReturnToPool mỗi lần lấy từ pool là mỗi lần tạo 1 delegate mới (rác GC) — Profiler bắt được đúng chỗ này khi quái đông.
        private System.Action<Projectile> _releaseProjectile;

        // Cấp phát 1 lần duy nhất rồi dùng lại mỗi frame — đây mới là thứ làm cho "NonAlloc" có ý nghĩa.
        private readonly Collider2D[] _hitBuffer = new Collider2D[MaxTargetsPerScan];

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _releaseProjectile = ReturnProjectileToPool;

            // Đạn nằm trong 1 container riêng ở gốc scene, KHÔNG làm con của Player: object con luôn
            // di chuyển/xoay theo cha, nên đạn đang bay sẽ bị quét lệch quỹ đạo mỗi khi Player xoay người.
            // Awake chạy cả khi component đang disabled, nên súng chưa mở khóa cũng tạo sẵn pool từ đầu trận.
            Transform projectileRoot = new GameObject($"Projectiles ({_weaponData.name})").transform;
            _projectilePool = new ObjectPool<Projectile>(_projectilePrefab, _poolPrewarmCount, projectileRoot);
        }

        private void Update()
        {
            _fireCooldown -= Time.deltaTime;

            Transform target = FindNearestEnemy();
            if (target == null) return;

            // Xoay điểm bắn hướng về mục tiêu — súng ngắm độc lập với hướng thân Player.
            Vector2 direction = (target.position - _firePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _firePoint.rotation = Quaternion.Euler(0f, 0f, angle);

            if (_fireCooldown <= 0f)
            {
                Fire(direction);
                _fireCooldown = 1f / (_weaponData.fireRate * _stats.FireRateMultiplier);
            }
        }

        private Transform FindNearestEnemy()
        {
            // NonAlloc ghi kết quả vào buffer có sẵn thay vì trả về mảng mới mỗi frame — quan trọng khi có nhiều quái.
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _weaponData.range, _hitBuffer, _enemyLayer);

            Transform nearest = null;
            float nearestDistSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                float distSqr = (_hitBuffer[i].transform.position - transform.position).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = _hitBuffer[i].transform;
                }
            }

            return nearest;
        }

        private void Fire(Vector2 direction)
        {
            float damage = _weaponData.damage * _stats.DamageMultiplier;
            int count = Mathf.Max(1, _weaponData.projectileCount);

            for (int i = 0; i < count; i++)
            {
                // Xoay hướng ngắm quanh trục Z để các viên tỏa đều thành hình quạt quanh mục tiêu.
                float offset = GetSpreadOffset(i, count, _weaponData.spreadAngle);
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, offset) * direction;

                Projectile projectile = _projectilePool.Get(_firePoint.position, Quaternion.identity);
                projectile.OnReturnToPool = _releaseProjectile;
                projectile.Launch(shotDirection, damage, _weaponData.projectileSpeed);
            }

            // 1 tiếng cho cả loạt đạn, không phải mỗi viên 1 tiếng.
            GameEvents.PlayerFired();
        }

        /// <summary>
        /// Góc lệch (độ) của viên thứ <paramref name="index"/> trong 1 loạt <paramref name="count"/> viên,
        /// chia đều từ -spread/2 đến +spread/2. Viên giữa (nếu số viên lẻ) bay thẳng vào mục tiêu.
        /// </summary>
        public static float GetSpreadOffset(int index, int count, float spreadAngle)
        {
            if (count <= 1) return 0f;

            float t = (float)index / (count - 1);
            return Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);
        }

        private void ReturnProjectileToPool(Projectile projectile)
        {
            _projectilePool.Release(projectile);
        }

        private void OnDrawGizmosSelected()
        {
            if (_weaponData == null) return;

            // Vẽ vòng tròn tầm bắn trong Scene view để dễ chỉnh bằng mắt.
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _weaponData.range);
        }
    }
}