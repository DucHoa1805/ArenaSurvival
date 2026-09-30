using UnityEngine;

namespace ArenaSurvival.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(ArenaSurvival.Combat.Health))]
    public class Enemy : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 2f;

        [Header("Combat")]
        [SerializeField] private float _contactDamage = 5f;
        [SerializeField] private float _damageCooldown = 1f; // tránh gây damage mỗi frame khi đứng chạm liên tục

        private Rigidbody2D _rb;
        private Transform _playerTransform;
        private float _lastDamageTime = -999f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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
            if (_playerTransform == null) return;

            Vector2 direction = (_playerTransform.position - transform.position).normalized;
            _rb.MovePosition(_rb.position + direction * (_moveSpeed * Time.fixedDeltaTime));
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
            if (Time.time - _lastDamageTime < _damageCooldown) return;
            if (!other.CompareTag("Player")) return;

            if (other.TryGetComponent(out ArenaSurvival.Combat.Health playerHealth))
            {
                playerHealth.TakeDamage(_contactDamage);
                _lastDamageTime = Time.time;
            }
        }
    }
}