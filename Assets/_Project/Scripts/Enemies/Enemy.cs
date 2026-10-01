using System;
using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Phần lõi chung của mọi quái và boss: máu, sát thương khi chạm, nằm trong pool, áp hệ số lúc sinh.
    /// Cách di chuyển/tấn công KHÔNG nằm ở đây mà ở EnemyBehaviour gán trong EnemyData, nên thêm kiểu quái mới
    /// không phải sửa class này.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private float _damageCooldown = 1f; // tránh gây damage mỗi frame khi đứng chạm liên tục

        [Header("Rotation")]
        [Tooltip("Góc lệch nếu sprite gốc không quay mặt sang phải (1,0). Sprite Kenney mặc định quay sang phải nên để 0.")]
        [SerializeField] private float _spriteAngleOffset = 0f;
        [SerializeField] private float _rotationSpeed = 540f; // độ/giây — xoay mượt, không giật khi Player đổi hướng liên tục

        private Rigidbody2D _rb;
        private Health _health;
        private SpriteRenderer _spriteRenderer;
        private EnemyContext _ctx;
        private float _lastDamageTime = -999f;
        private float _contactDamage;

        // Dùng khi EnemyData chưa gán hành vi (asset cũ): tạo 1 lần cho cả game, không tạo mỗi con.
        private static ChaseBehaviour _fallbackBehaviour;

        private EnemyData _data;
        public EnemyData Data => _data;
        public EnemyModifiers Modifiers => _ctx.Modifiers;
        public Health Health => _health;
        public float HealthFraction => _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 0f;

        // Giống Projectile: Enemy không biết gì về ObjectPool<T>, chỉ gọi callback khi xong việc.
        public Action<Enemy> OnReturnToPool;

        /// <summary>Boss đổi pha (chỉ số pha mới). Chỉ boss phát; HUD và thanh máu boss nghe trong lúc boss còn sống.</summary>
        public event Action<int> OnBossPhaseChanged;

        private EnemyBehaviour Behaviour
        {
            get
            {
                if (_data != null && _data.behaviour != null) return _data.behaviour;
                if (_fallbackBehaviour == null) _fallbackBehaviour = ScriptableObject.CreateInstance<ChaseBehaviour>();
                return _fallbackBehaviour;
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            // Sprite nằm ở object con "Visual" (để SpriteBodyAnimator làm hoạt ảnh mà không đụng vào
            // transform gốc đang giữ vật lý, hướng mặt và scale theo loại quái).
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Tạo 1 lần cho cả vòng đời object (kể cả các lần lấy ra từ pool).
            _ctx = new EnemyContext
            {
                Enemy = this,
                Body = _rb,
                Visual = _spriteRenderer.transform,
                RotationSpeed = _rotationSpeed,
                SpriteAngleOffset = _spriteAngleOffset,
            };

            // Đăng ký 1 lần ở Awake chứ không ở OnEnable/OnDisable: object này nằm trong pool, bật/tắt mỗi lần quái sinh ra
            // hay chết, và mỗi lần += / -= là tạo delegate mới (rác GC). Health ở cùng object nên sống chết cùng nhau;
            // object đang tắt thì cũng không bị trúng đòn, nên không cần gỡ lúc tắt.
            _health.OnDied += HandleDied;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDied -= HandleDied;
        }

        /// <summary>Spawner gán mỗi lần sinh (chỉ gán tham chiếu, không cấp phát): pool đạn chung và "thế giới" để triệu hồi/giữ trong sân.</summary>
        public void SetWorld(IEnemyWorld world, EnemyProjectiles projectiles)
        {
            _ctx.World = world;
            _ctx.Projectiles = projectiles;
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
            // Đặt lại màu mỗi lần lấy từ pool: con trước có thể là quái tinh anh hoặc boss có màu riêng.
            _spriteRenderer.color = data.tint * modifiers.tint;

            // Gán lại mỗi lần lấy ra từ pool: object này lần trước có thể là 1 loại quái khác kích thước.
            // Scale transform thì collider cũng to theo, không cần chỉnh riêng.
            transform.localScale = Vector3.one * (data.scale * modifiers.scale);

            _ctx.Data = data;
            _ctx.Modifiers = modifiers;
            Behaviour.OnSpawn(_ctx);

            // Quay mặt về phía Player ngay lúc xuất hiện, thay vì giữ hướng của lần dùng trước rồi mới xoay dần.
            Vector2 toPlayer = _ctx.ToPlayer;
            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                _rb.rotation = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg + _spriteAngleOffset;
            }
        }

        private void OnEnable()
        {
            // Tìm Player 1 lần khi kích hoạt (kể cả khi được lấy ra từ pool) —
            // không dùng GameObject.Find trong Update vì rất tốn kém.
            if (_ctx.Player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _ctx.Player = playerObj.transform;
                }
            }

            _lastDamageTime = -999f;
        }

        private void FixedUpdate()
        {
            if (_ctx.Player == null || _data == null) return;
            Behaviour.Tick(_ctx, Time.fixedDeltaTime);
        }

        /// <summary>Chỉ BossBehaviour gọi.</summary>
        public void RaiseBossPhaseChanged(int phase) => OnBossPhaseChanged?.Invoke(phase);

        /// <summary>
        /// Biến mất ngay (nổ theo khi boss chết). Vẫn phát EnemyKilled để có hiệu ứng nổ và âm thanh,
        /// nhưng reward = false thì XP = 0 → không rơi orb.
        /// </summary>
        public void Despawn(bool reward)
        {
            if (!gameObject.activeSelf) return;

            _rb.linearVelocity = Vector2.zero;
            if (_data != null) Core.GameEvents.EnemyKilled(reward ? _data.xpReward : 0, transform.position);
            OnReturnToPool?.Invoke(this);
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

            if (other.TryGetComponent(out Health playerHealth))
            {
                playerHealth.TakeDamage(_contactDamage);
                _lastDamageTime = Time.time;
            }
        }

        private void HandleDied()
        {
            _rb.linearVelocity = Vector2.zero;
            _ctx.ResetVisual();

            // Phát sự kiện toàn cục để hệ thống XP (Phase 4) lắng nghe mà Enemy không cần biết
            // ProgressionSystem tồn tại — đây là ví dụ thực tế của giảm coupling qua event.
            if (_data != null)
            {
                Core.GameEvents.EnemyKilled(_data.xpReward, transform.position);
                // Trước khi trả về pool: lúc này vị trí, dữ liệu và hệ số của con quái vẫn còn nguyên.
                _ctx.World?.NotifyKilled(this);
            }

            OnReturnToPool?.Invoke(this);
        }
    }
}
