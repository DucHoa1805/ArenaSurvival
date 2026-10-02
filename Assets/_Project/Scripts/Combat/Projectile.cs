using System;
using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Đạn dùng chung cho mọi súng (và đạn của quái). Kiểu bắn khác nhau chỉ là bộ số khác nhau truyền vào Launch (phương án A, 8C):
    /// tầm (đạn tự hủy theo quãng đường), xuyên, nổ vùng, đẩy lùi. Đạn quái gọi Launch ngắn nên không có các hiệu ứng này.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _damage = 5f;
        [SerializeField] private float _lifetime = 3f; // tự trả về pool sau X giây nếu không trúng gì
        [Tooltip("Phát GameEvents.ProjectileHit (tia lửa + tiếng \"trúng quái\"). Đạn của quái tắt: Player bị trúng đã có tiếng/hiệu ứng riêng.")]
        [SerializeField] private bool _raiseHitEvent = true;
        [Tooltip("Layer bị sát thương nổ lan (Enemy). Chỉ đạn có nổ mới dùng.")]
        [SerializeField] private LayerMask _explosionMask;

        // Buffer dùng chung cho mọi viên đạn nổ: đạn chạy trên main thread nên không bao giờ quét chồng nhau.
        private const int MaxExplosionTargets = 64;
        private static readonly Collider2D[] ExplosionBuffer = new Collider2D[MaxExplosionTargets];

        private Rigidbody2D _rb;
        private float _spawnTime;
        private float _life;
        private Vector2 _direction;
        private int _pierceLeft;
        private float _explosionRadius;
        private float _explosionFraction;
        private float _knockback;
        private ContactFilter2D _explosionFilter;

        // Người tạo ra pool sẽ gán hàm này vào — Projectile không cần biết pool là gì,
        // chỉ cần biết "khi xong việc thì gọi callback này". Điều này giữ Projectile
        // không phụ thuộc trực tiếp vào ObjectPool<T>, dễ tái sử dụng ở nơi khác.
        public Action<Projectile> OnReturnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_explosionMask.value == 0) _explosionMask = LayerMask.GetMask("Enemy");
            _explosionFilter = new ContactFilter2D { useLayerMask = true, layerMask = _explosionMask, useTriggers = true };
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        /// <summary>Đạn thường (đạn quái, hoặc súng không có hiệu ứng): bay theo thời gian sống mặc định của prefab.</summary>
        public void Launch(Vector2 direction, float damageOverride = -1f, float speedOverride = -1f)
        {
            Launch(direction, damageOverride >= 0f ? damageOverride : _damage, speedOverride >= 0f ? speedOverride : _speed,
                range: 0f, pierce: 0, explosionRadius: 0f, explosionFraction: 0f, knockback: 0f);
        }

        /// <summary>
        /// Đạn của súng chính. range &gt; 0 thì đạn tự hủy sau đúng quãng đường đó (Shotgun tầm ngắn thật, không chỉ "ngắm gần").
        /// </summary>
        public void Launch(Vector2 direction, float damage, float speed, float range, int pierce,
            float explosionRadius, float explosionFraction, float knockback)
        {
            _damage = damage;
            _speed = speed;
            _life = range > 0f && speed > 0f ? range / speed : _lifetime;
            _pierceLeft = Mathf.Max(0, pierce);
            _explosionRadius = explosionRadius;
            _explosionFraction = explosionFraction;
            _knockback = knockback;
            _direction = direction.normalized;

            _rb.linearVelocity = _direction * _speed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (Time.time - _spawnTime < _life) return;

            // Lựu đạn bay hết tầm mà không trúng ai thì vẫn nổ tại chỗ (giống lựu đạn thật, và không phí phát bắn).
            if (_explosionRadius > 0f) Explode(null);
            ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!gameObject.activeSelf) return;

            // Không cần tự lọc mục tiêu ở đây: Layer Collision Matrix quyết định đạn chạm ai
            // (PlayerProjectile chỉ chạm Enemy, EnemyProjectile chỉ chạm Player).
            if (!other.TryGetComponent(out Health targetHealth)) return;

            // Quái nhận đòn kèm hướng bay (khiên chặn phía trước); Player không cần nên đi thẳng vào Health.
            if (other.TryGetComponent(out IHitReceiver receiver)) receiver.TakeHit(_damage, _direction);
            else targetHealth.TakeDamage(_damage);
            if (_raiseHitEvent) Core.GameEvents.ProjectileHit(transform.position);

            if (_knockback > 0f && other.attachedRigidbody != null)
            {
                // Dời thẳng vị trí (giống đẩy quái lúc hồi sinh): hành vi quái đặt lại vận tốc mỗi FixedUpdate nên lực đẩy sẽ bị ghi đè.
                other.attachedRigidbody.position += _direction * _knockback;
            }

            if (_explosionRadius > 0f)
            {
                Explode(other);
                ReturnToPool();
                return;
            }

            // Xuyên: còn lượt thì bay tiếp, hết lượt mới trả về pool.
            if (_pierceLeft-- > 0) return;
            ReturnToPool();
        }

        /// <summary>Sát thương lan cho mọi quái khác trong bán kính (con bị trúng trực tiếp đã nhận đủ sát thương).</summary>
        private void Explode(Collider2D directHit)
        {
            Vector2 center = transform.position;
            int count = Physics2D.OverlapCircle(center, _explosionRadius, _explosionFilter, ExplosionBuffer);
            float splash = _damage * _explosionFraction;
            for (int i = 0; i < count; i++)
            {
                Collider2D target = ExplosionBuffer[i];
                if (target == directHit) continue;
                if (target.TryGetComponent(out Health health)) health.TakeDamage(splash);
            }
            Core.GameEvents.Explosion(center, _explosionRadius);
        }

        private void ReturnToPool()
        {
            _rb.linearVelocity = Vector2.zero;
            OnReturnToPool?.Invoke(this);
        }
    }
}
