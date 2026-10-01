using System;
using UnityEngine;

namespace ArenaSurvival.Loot
{
    /// <summary>
    /// Đồ rơi trên sân (nguyên liệu hoặc trang bị). Hút về Player như XP orb; chạm Player thì báo LootDropper ghi lại.
    /// Nằm trên layer Pickup nên chỉ Player chạm được. Không biết gì về pool hay hồ sơ người chơi.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class LootPickup : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("Vòng sáng phía sau icon, tô màu theo độ hiếm (nguyên liệu: màu trắng mờ).")]
        [SerializeField] private SpriteRenderer _ring;

        [Header("Magnet")]
        [SerializeField] private float _magnetRadius = 2.5f;
        [SerializeField] private float _magnetSpeed = 8f;

        [Header("Hiệu ứng")]
        [Tooltip("Nhún lên xuống cho dễ thấy giữa đám quái.")]
        [SerializeField] private float _bobHeight = 0.08f;
        [SerializeField] private float _bobSpeed = 4f;

        private Rigidbody2D _rb;
        private Transform _player;
        private bool _isAttracted;
        private float _phase;

        public LootDrop Drop { get; private set; }

        // Gán bởi LootDropper (delegate cache sẵn ở đó): nhặt được, và trả về pool.
        public Action<LootPickup> OnCollected;
        public Action<LootPickup> OnReturnToPool;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        public void Setup(LootDrop drop, Sprite icon, Color ringColor)
        {
            Drop = drop;
            _icon.sprite = icon;
            _ring.color = ringColor;
        }

        private void OnEnable()
        {
            if (_player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) _player = playerObj.transform;
            }

            _isAttracted = false;
            // Lệch pha ngẫu nhiên: nhiều món rơi cùng chỗ không nhún đồng loạt như 1 khối.
            _phase = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            // Nhún phần hình (con) thay vì gốc, để không đụng vị trí vật lý.
            _phase += Time.deltaTime * _bobSpeed;
            _icon.transform.localPosition = new Vector3(0f, Mathf.Sin(_phase) * _bobHeight, 0f);
        }

        private void FixedUpdate()
        {
            if (_player == null) return;

            Vector2 target = _player.position;
            if (!_isAttracted)
            {
                if ((target - _rb.position).sqrMagnitude > _magnetRadius * _magnetRadius) return;
                _isAttracted = true; // đã hút thì bám tới cùng, như XP orb
            }

            _rb.MovePosition(Vector2.MoveTowards(_rb.position, target, _magnetSpeed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            OnCollected?.Invoke(this);
        }
    }
}
