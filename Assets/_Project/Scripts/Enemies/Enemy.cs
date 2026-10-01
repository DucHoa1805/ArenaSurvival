using System;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(ArenaSurvival.Combat.Health))]
    public class Enemy : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private float _damageCooldown = 1f; // tránh gây damage mỗi frame khi đứng chạm liên tục

        [Header("Rotation")]
        [Tooltip("Góc lệch nếu sprite gốc không quay mặt sang phải (1,0). Sprite Kenney mặc định quay sang phải nên để 0.")]
        [SerializeField] private float _spriteAngleOffset = 0f;
        [SerializeField] private float _rotationSpeed = 540f; // độ/giây — xoay mượt, không giật khi Player đổi hướng liên tục

        private Rigidbody2D _rb;
        private ArenaSurvival.Combat.Health _health;
        private SpriteRenderer _spriteRenderer;
        private Transform _playerTransform;
        private float _lastDamageTime = -999f;
        private float _contactDamage;

        private EnemyData _data;
        public EnemyData Data => _data;

        // Giống Projectile: Enemy không biết gì về ObjectPool<T>, chỉ gọi callback khi xong việc.
        public Action<Enemy> OnReturnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<ArenaSurvival.Combat.Health>();
            // Sprite nằm ở object con "Visual" (để SpriteBodyAnimator làm hoạt ảnh mà không đụng vào
            // transform gốc đang giữ vật lý, hướng mặt và scale theo loại quái).
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Đăng ký 1 lần ở Awake chứ không ở OnEnable/OnDisable: object này nằm trong pool, bật/tắt mỗi lần quái sinh ra
            // hay chết, và mỗi lần += / -= là tạo delegate mới (rác GC). Health ở cùng object nên sống chết cùng nhau;
            // object đang tắt thì cũng không bị trúng đòn, nên không cần gỡ lúc tắt.
            _health.OnDied += HandleDied;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDied -= HandleDied;
        }

        /// <summary>
        /// Gọi ngay sau khi lấy Enemy ra khỏi pool (lúc này object đã active và OnEnable đã chạy),
        /// để cấu hình đúng loại quái (máu, tốc độ, sprite...) theo EnemyData được truyền vào.
        /// modifiers làm quái trâu hơn theo màn hoặc thành quái tinh anh mà không sửa asset gốc.
        /// </summary>
        public void Initialize(EnemyData data, EnemyModifiers modifiers)
        {
            _data = data;
            _health.SetMaxHealth(data.maxHealth * modifiers.health, healToFull: true);
            _contactDamage = data.contactDamage * modifiers.damage;
            _spriteRenderer.sprite = data.sprite;
            // Đặt lại màu mỗi lần lấy từ pool: con trước có thể là quái tinh anh có màu riêng.
            _spriteRenderer.color = modifiers.tint;

            // Gán lại mỗi lần lấy ra từ pool: object này lần trước có thể là 1 loại quái khác kích thước.
            // Scale transform thì collider cũng to theo, không cần chỉnh riêng.
            transform.localScale = Vector3.one * (data.scale * modifiers.scale);

            // Quay mặt về phía Player ngay lúc xuất hiện, thay vì giữ hướng của lần dùng trước rồi mới xoay dần.
            if (_playerTransform != null)
            {
                Vector2 toPlayer = (Vector2)_playerTransform.position - (Vector2)transform.position;
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    _rb.rotation = GetFacingAngle(toPlayer);
                }
            }
        }

        private void OnEnable()
        {
            // Tìm Player 1 lần khi kích hoạt (kể cả khi được lấy ra từ pool) — 
            // không dùng GameObject.Find trong Update vì rất tốn kém.
            if (_playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }

            _lastDamageTime = -999f;
        }

        private void FixedUpdate()
        {
            if (_playerTransform == null || _data == null) return;

            Vector2 toPlayer = (Vector2)_playerTransform.position - _rb.position;

            // Đã chồng lên Player: không có hướng rõ ràng, giữ nguyên để không bị xoay loạn.
            if (toPlayer.sqrMagnitude < 0.0001f) return;

            Vector2 direction = toPlayer.normalized;
            _rb.MovePosition(_rb.position + direction * (_data.moveSpeed * Time.fixedDeltaTime));

            // Xoay qua Rigidbody2D (không set transform.rotation) để đồng bộ với bước vật lý như MovePosition.
            float targetAngle = GetFacingAngle(direction);
            _rb.MoveRotation(Mathf.MoveTowardsAngle(_rb.rotation, targetAngle, _rotationSpeed * Time.fixedDeltaTime));
        }

        private float GetFacingAngle(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _spriteAngleOffset;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Hỗ trợ cả trường hợp collider của Enemy hoặc Player là Trigger.
            TryDamagePlayer(other);
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (_data == null) return;
            if (Time.time - _lastDamageTime < _damageCooldown) return;
            if (!other.CompareTag("Player")) return;

            if (other.TryGetComponent(out ArenaSurvival.Combat.Health playerHealth))
            {
                playerHealth.TakeDamage(_contactDamage);
                _lastDamageTime = Time.time;
            }
        }

        private void HandleDied()
        {
            _rb.linearVelocity = Vector2.zero;

            // Phát sự kiện toàn cục để hệ thống XP (Phase 4) lắng nghe mà Enemy không cần biết
            // ProgressionSystem tồn tại — đây là ví dụ thực tế của giảm coupling qua event.
            if (_data != null)
            {
                Core.GameEvents.EnemyKilled(_data.xpReward, transform.position);
            }

            OnReturnToPool?.Invoke(this);
        }
    }
}