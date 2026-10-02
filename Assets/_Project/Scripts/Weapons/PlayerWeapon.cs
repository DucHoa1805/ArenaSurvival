using System.Collections.Generic;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.Player;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Súng chính tự bắn quái gần nhất. Khẩu nào, cấp/sao bao nhiêu do PlayerLoadout gán lúc vào màn (Equip) theo vũ khí meta
    /// người chơi đang mặc (8C); _weaponData trên prefab chỉ là khẩu dự phòng khi chưa có dịch vụ (scene test cũ).
    /// Bắn theo WeaponStats (đã cộng cấp, sao) × hệ số nâng cấp trong màn (PlayerStats).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerWeapon : MonoBehaviour, IWeapon
    {
        [Header("Data")]
        [Tooltip("Khẩu dự phòng (Súng lục). Lúc chơi thật PlayerLoadout gán khẩu người chơi đang mặc.")]
        [SerializeField] private WeaponData _weaponData;

        public WeaponData Data => _weaponData;
        public WeaponDefinition Definition => _weaponData;
        public WeaponStats Stats => _stats;

        [Header("References")]
        [Tooltip("Đạn mặc định khi WeaponData không có prefab đạn riêng.")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Transform _firePoint;
        [Tooltip("Sprite lóe nòng (con của FirePoint, tắt sẵn). Không bắt buộc.")]
        [SerializeField] private SpriteRenderer _muzzleFlash;
        [SerializeField] private float _muzzleFlashTime = 0.05f;
        [SerializeField] private LayerMask _enemyLayer; // chỉ tìm mục tiêu trên layer Enemy

        [Header("Pooling")]
        [SerializeField] private int _poolPrewarmCount = 20;

        // Đủ lớn so với mật độ quái thực tế (300 quái trên sân, tầm 8): thiếu chỗ thì có thể sót con gần nhất.
        private const int MaxTargetsPerScan = 128;

        private float _fireCooldown;
        private WeaponStats _stats;
        private bool _equipped;
        private ObjectPool<Projectile> _projectilePool;
        private Projectile _poolPrefab;
        private Transform _projectileRoot;
        private PlayerStats _playerStats;
        private ContactFilter2D _enemyFilter;
        // Gán sẵn 1 lần: gán thẳng tên hàm vào OnReturnToPool mỗi lần lấy từ pool là mỗi lần tạo 1 delegate mới (rác GC) — Profiler bắt được đúng chỗ này khi quái đông.
        private System.Action<Projectile> _releaseProjectile;

        // List tạo sẵn đủ sức chứa rồi dùng lại mỗi frame: bản OverlapCircle nhận List không cấp phát khi còn đủ chỗ
        // (bản OverlapCircleNonAlloc cũ bị Unity 6 đánh dấu obsolete).
        private readonly List<Collider2D> _hits = new List<Collider2D>(MaxTargetsPerScan);

        private float _flashTimer;

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            if (_muzzleFlash == null && _firePoint != null)
            {
                Transform t = _firePoint.Find("MuzzleFlash");
                if (t != null) _muzzleFlash = t.GetComponent<SpriteRenderer>();
            }
            if (_muzzleFlash != null) _muzzleFlash.enabled = false;
            _releaseProjectile = ReturnProjectileToPool;
            _enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = _enemyLayer, useTriggers = true };
        }

        private void Start()
        {
            // Không ai gán (scene test không có dịch vụ): dùng khẩu dự phòng ở cấp 1, 1★.
            if (!_equipped && _weaponData != null) Equip(_weaponData, WeaponRules.Effective(_weaponData, 1, 1));
        }

        /// <summary>Gán khẩu súng + chỉ số đã tính. Gọi lại được (benchmark đổi súng) — pool đạn chỉ dựng lại khi đổi loại đạn.</summary>
        public void Equip(WeaponData data, WeaponStats stats)
        {
            _weaponData = data;
            _stats = stats;
            _equipped = true;
            _fireCooldown = 0f;

            Projectile prefab = data.projectilePrefab != null ? data.projectilePrefab : _projectilePrefab;
            if (_projectilePool != null && prefab == _poolPrefab) return;

            // Đạn nằm trong 1 container riêng ở gốc scene, KHÔNG làm con của Player: object con luôn
            // di chuyển/xoay theo cha, nên đạn đang bay sẽ bị quét lệch quỹ đạo mỗi khi Player xoay người.
            if (_projectileRoot != null) Destroy(_projectileRoot.gameObject);
            _projectileRoot = new GameObject($"Projectiles ({data.name})").transform;
            _projectilePool = new ObjectPool<Projectile>(prefab, _poolPrewarmCount, _projectileRoot);
            _poolPrefab = prefab;
        }

        private void OnDestroy()
        {
            if (_projectileRoot != null) Destroy(_projectileRoot.gameObject);
        }

        private void Update()
        {
            if (!_equipped) return;
            _fireCooldown -= Time.deltaTime;
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && _muzzleFlash != null) _muzzleFlash.enabled = false;
            }

            Transform target = FindNearestEnemy();
            if (target == null) return;

            // Xoay điểm bắn hướng về mục tiêu — súng ngắm độc lập với hướng thân Player.
            Vector2 direction = (target.position - _firePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _firePoint.rotation = Quaternion.Euler(0f, 0f, angle);

            if (_fireCooldown <= 0f)
            {
                Fire(direction);
                _fireCooldown = 1f / Mathf.Max(0.01f, _stats.fireRate * _playerStats.FireRateMultiplier);
            }
        }

        private Transform FindNearestEnemy()
        {
            int count = Physics2D.OverlapCircle(transform.position, _stats.range, _enemyFilter, _hits);

            Transform nearest = null;
            float nearestDistSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                float distSqr = (_hits[i].transform.position - transform.position).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = _hits[i].transform;
                }
            }

            return nearest;
        }

        private void Fire(Vector2 direction)
        {
            float damage = _stats.damage * _playerStats.DamageMultiplier;
            int extra = _playerStats.ExtraProjectiles;
            int count = Mathf.Max(1, _stats.projectileCount) + extra;
            float spread = RunUpgrades.SkillRules.SpreadWithExtra(_stats.spreadAngle, extra);
            int pierce = _stats.pierce + _playerStats.ExtraPierce;

            for (int i = 0; i < count; i++)
            {
                // Xoay hướng ngắm quanh trục Z để các viên tỏa đều thành hình quạt quanh mục tiêu; súng máy lệch thêm ngẫu nhiên.
                float offset = GetSpreadOffset(i, count, spread);
                if (_stats.aimJitter > 0f) offset += Random.Range(-_stats.aimJitter, _stats.aimJitter);
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, offset) * direction;

                Projectile projectile = _projectilePool.Get(_firePoint.position, Quaternion.identity);
                projectile.OnReturnToPool = _releaseProjectile;
                projectile.Launch(shotDirection, damage, _stats.projectileSpeed, _stats.range, pierce,
                    _stats.explosionRadius, _stats.explosionDamageFraction, _stats.knockback);
            }

            if (_muzzleFlash != null)
            {
                _muzzleFlash.enabled = true;
                // Xoay ngẫu nhiên quanh trục nòng để các lần lóe không giống hệt nhau.
                _muzzleFlash.transform.localRotation = Quaternion.Euler(Random.Range(0f, 360f), 0f, 0f);
                _flashTimer = _muzzleFlashTime;
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
            Gizmos.DrawWireSphere(transform.position, _equipped ? _stats.range : _weaponData.range);
        }
    }
}
