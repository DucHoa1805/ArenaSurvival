using System;
using UnityEngine;

namespace ArenaSurvival.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _damage = 5f;
        [SerializeField] private float _lifetime = 3f; // tự trả về pool sau X giây nếu không trúng gì

        private Rigidbody2D _rb;
        private float _spawnTime;

        // Người tạo ra pool sẽ gán hàm này vào — Projectile không cần biết pool là gì,
        // chỉ cần biết "khi xong việc thì gọi callback này". Điều này giữ Projectile
        // không phụ thuộc trực tiếp vào ObjectPool<T>, dễ tái sử dụng ở nơi khác.
        public Action<Projectile> OnReturnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        /// <summary>Gọi ngay sau khi lấy đạn ra (từ pool) để thiết lập hướng bắn.</summary>
        public void Launch(Vector2 direction, float damageOverride = -1f, float speedOverride = -1f)
        {
            if (damageOverride >= 0f)
            {
                _damage = damageOverride;
            }

            if (speedOverride >= 0f)
            {
                _speed = speedOverride;
            }

            _rb.linearVelocity = direction.normalized * _speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= _lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) return;

            if (other.TryGetComponent(out Health targetHealth))
            {
                targetHealth.TakeDamage(_damage);
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            _rb.linearVelocity = Vector2.zero;
            OnReturnToPool?.Invoke(this);
        }
    }
}