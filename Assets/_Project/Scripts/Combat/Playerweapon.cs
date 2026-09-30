using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Combat
{
    public class PlayerWeapon : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WeaponData _weaponData;

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

        // Cấp phát 1 lần duy nhất rồi dùng lại mỗi frame — đây mới là thứ làm cho "NonAlloc" có ý nghĩa.
        private readonly Collider2D[] _hitBuffer = new Collider2D[MaxTargetsPerScan];

        private void Awake()
        {
            _projectilePool = new ObjectPool<Projectile>(_projectilePrefab, _poolPrewarmCount, transform);
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
                _fireCooldown = 1f / _weaponData.fireRate;
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
            Projectile projectile = _projectilePool.Get(_firePoint.position, Quaternion.identity);
            projectile.OnReturnToPool = ReturnProjectileToPool;
            projectile.Launch(direction, _weaponData.damage, _weaponData.projectileSpeed);
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