using System;
using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Progression
{
    /// <summary>
    /// Viên XP rơi ra khi quái chết. Nằm trên layer Pickup nên theo Layer Collision Matrix
    /// chỉ Player (và Default) chạm được nó — đạn và quái đi xuyên qua mà không tốn callback nào.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class XpOrb : MonoBehaviour
    {
        [Header("Magnet")]
        [SerializeField] private float _magnetRadius = 2.5f; // Player vào trong khoảng này thì orb bắt đầu bị hút
        [SerializeField] private float _magnetSpeed = 8f;    // phải lớn hơn tốc độ chạy của Player để orb luôn bắt kịp

        private Rigidbody2D _rb;
        private Transform _playerTransform;
        private int _xpAmount;
        private bool _isAttracted;

        // Giống Projectile/Enemy: orb không biết gì về ObjectPool<T>, chỉ gọi callback khi xong việc.
        public Action<XpOrb> OnReturnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>Gọi ngay sau khi lấy orb ra khỏi pool để gán lượng XP nó mang.</summary>
        public void Initialize(int xpAmount)
        {
            _xpAmount = xpAmount;
        }

        private void OnEnable()
        {
            if (_playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }

            // Reset mỗi lần lấy ra từ pool — orb cũ có thể đang ở trạng thái "đang bị hút".
            _isAttracted = false;
        }

        private void FixedUpdate()
        {
            if (_playerTransform == null) return;

            Vector2 playerPosition = _playerTransform.position;

            if (!_isAttracted)
            {
                if ((playerPosition - _rb.position).sqrMagnitude > _magnetRadius * _magnetRadius) return;

                // Đã bị hút thì bám theo tới cùng, kể cả khi Player chạy ra khỏi bán kính —
                // tránh cảnh orb đuổi nửa chừng rồi đứng lại.
                _isAttracted = true;
            }

            _rb.MovePosition(Vector2.MoveTowards(_rb.position, playerPosition, _magnetSpeed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Layer Pickup vẫn chạm layer Default (tường), nên phải lọc đúng Player.
            if (!other.CompareTag("Player")) return;

            GameEvents.XpGained(_xpAmount);
            OnReturnToPool?.Invoke(this);
        }
    }
}
