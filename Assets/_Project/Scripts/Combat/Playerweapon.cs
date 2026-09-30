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

        private float _fireCooldown;
        private ObjectPool<Projectile> _projectilePool;

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
            // NonAlloc để không cấp phát mảng mới mỗi frame — quan trọng khi có nhiều quái.
            Collider2D[] hits = new Collider2D[20];
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _weaponData.range, hits, _enemyLayer);

            Transform nearest = null;
            float nearestDistSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                float distSqr = (hits[i].transform.position - transform.position).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = hits[i].transform;
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