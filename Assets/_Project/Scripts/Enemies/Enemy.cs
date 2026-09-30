using System;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(ArenaSurvival.Combat.Health))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Enemy : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private float _damageCooldown = 1f; // tránh gây damage mỗi frame khi đứng chạm liên tục

        private Rigidbody2D _rb;
        private ArenaSurvival.Combat.Health _health;
        private SpriteRenderer _spriteRenderer;
        private Transform _playerTransform;
        private float _lastDamageTime = -999f;

        private EnemyData _data;
        public EnemyData Data => _data;

        // Giống Projectile: Enemy không biết gì về ObjectPool<T>, chỉ gọi callback khi xong việc.
        public Action<Enemy> OnReturnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<ArenaSurvival.Combat.Health>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Gọi ngay sau khi lấy Enemy ra khỏi pool (lúc này object đã active và OnEnable đã chạy),
        /// để cấu hình đúng loại quái (máu, tốc độ, sprite...) theo EnemyData được truyền vào.
        /// healthMultiplier cho phép spawner làm quái trâu hơn ở các wave sau mà không sửa asset gốc.
        /// </summary>
        public void Initialize(EnemyData data, float healthMultiplier = 1f)
        {
            _data = data;
            _health.SetMaxHealth(data.maxHealth * healthMultiplier, healToFull: true);
            _spriteRenderer.sprite = data.sprite;

            // Gán lại mỗi lần lấy ra từ pool: object này lần trước có thể là 1 loại quái khác kích thước.
            // Scale transform thì collider cũng to theo, không cần chỉnh riêng.
            transform.localScale = Vector3.one * data.scale;
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
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDied;
        }

        private void FixedUpdate()
        {
            if (_playerTransform == null || _data == null) return;

            Vector2 direction = (_playerTransform.position - transform.position).normalized;
            _rb.MovePosition(_rb.position + direction * (_data.moveSpeed * Time.fixedDeltaTime));
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
                playerHealth.TakeDamage(_data.contactDamage);
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