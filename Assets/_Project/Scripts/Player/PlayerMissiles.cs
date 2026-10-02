using System;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.RunUpgrades;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Kỹ năng Tên lửa: định kỳ bắn vài quả nổ vào các quái gần nhất (mỗi quả 1 mục tiêu khác nhau nếu đủ quái).
    /// Dùng lại Projectile có nổ vùng (prefab đạn lựu) trong pool riêng; container ở gốc scene như đạn súng chính.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerMissiles : MonoBehaviour
    {
        [SerializeField] private Projectile _prefab;
        [SerializeField] private float _interval = 2.5f;
        [SerializeField] private float _range = 8f;
        [SerializeField] private float _speed = 9f;
        [SerializeField] private float _explosionRadius = 1.2f;
        [SerializeField] private int _prewarm = 12;

        private const int MaxTargets = 64;
        private readonly Collider2D[] _buffer = new Collider2D[MaxTargets];
        private readonly Transform[] _targets = new Transform[8];
        private ContactFilter2D _filter;
        private PlayerStats _stats;
        private ObjectPool<Projectile> _pool;
        private Action<Projectile> _release;
        private Transform _container;
        private int _level;
        private float _timer;

        public int Level => _level;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };
            if (_prefab != null)
            {
                // Không làm con của Player: con của Rigidbody2D bị kéo theo cha.
                _container = new GameObject("Missiles").transform;
                _pool = new ObjectPool<Projectile>(_prefab, _prewarm, _container);
                _release = _pool.Release;
            }
        }

        private void OnDestroy()
        {
            if (_container != null) Destroy(_container.gameObject);
        }

        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            enabled = true;
        }

        private void Update()
        {
            if (_level <= 0 || _pool == null) return;
            _timer += Time.deltaTime;
            if (_timer < _interval) return;

            int found = FindNearest(SkillRules.MissileCount(_level));
            if (found == 0) return; // chưa có quái trong tầm: giữ nhịp đã nạp, bắn ngay khi có
            _timer = 0f;

            float damage = SkillRules.MissileDamage(_level) * _stats.DamageMultiplier * _stats.StageScale;
            Vector2 origin = transform.position;
            for (int i = 0; i < SkillRules.MissileCount(_level); i++)
            {
                // Ít quái hơn số tên lửa thì quay vòng mục tiêu.
                Vector2 direction = ((Vector2)_targets[i % found].position - origin).normalized;
                Projectile p = _pool.Get(origin, Quaternion.identity);
                p.OnReturnToPool = _release;
                p.Launch(direction, damage, _speed, _range, 0, _explosionRadius, 0.6f, 0f);
            }
            GameEvents.PlayerFired();
        }

        /// <summary>Chọn n quái gần nhất trong tầm (chèn có thứ tự vào mảng nhỏ, không cấp phát).</summary>
        private int FindNearest(int n)
        {
            n = Mathf.Min(n, _targets.Length);
            int count = Physics2D.OverlapCircle(transform.position, _range, _filter, _buffer);
            int found = 0;
            Vector2 self = transform.position;
            for (int i = 0; i < count; i++)
            {
                Transform t = _buffer[i].transform;
                float d = ((Vector2)t.position - self).sqrMagnitude;
                int slot = found;
                while (slot > 0 && ((Vector2)_targets[slot - 1].position - self).sqrMagnitude > d)
                {
                    if (slot < n) _targets[slot] = _targets[slot - 1];
                    slot--;
                }
                if (slot < n)
                {
                    _targets[slot] = t;
                    if (found < n) found++;
                }
            }
            return found;
        }
    }
}
