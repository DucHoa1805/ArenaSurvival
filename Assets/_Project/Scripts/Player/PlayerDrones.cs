using System;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using ArenaSurvival.RunUpgrades;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Kỹ năng Drone hộ vệ (10D): vài drone bay vòng quanh Player, mỗi chiếc tự bắn quái gần nhất trong tầm.
    /// Drone dựng sẵn tối đa (ẩn), chỉ bật số chiếc theo cấp; đạn dùng pool riêng như Tên lửa. Container ở gốc scene.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerDrones : MonoBehaviour
    {
        [SerializeField] private Projectile _bulletPrefab;
        [SerializeField] private Sprite _droneSprite;
        [SerializeField] private float _orbitRadius = 1.5f;
        [SerializeField] private float _orbitSpeed = 120f;
        [SerializeField] private float _range = 6.5f;
        [SerializeField] private float _bulletSpeed = 12f;
        [SerializeField] private float _droneSize = 0.55f;

        private const int MaxDrones = 3;
        private const int MaxTargets = 64;
        private readonly Collider2D[] _buffer = new Collider2D[MaxTargets];
        private readonly Transform[] _drones = new Transform[MaxDrones];
        private readonly float[] _cooldowns = new float[MaxDrones];
        private ContactFilter2D _filter;
        private PlayerStats _stats;
        private ObjectPool<Projectile> _pool;
        private Action<Projectile> _release;
        private Transform _container;
        private int _level;
        private float _angle;

        public int Level => _level;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };
            _container = new GameObject("Drones").transform;
            for (int i = 0; i < MaxDrones; i++)
            {
                var go = new GameObject("Drone" + i);
                go.transform.SetParent(_container, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _droneSprite;
                sr.sortingOrder = 3;
                if (_droneSprite != null) go.transform.localScale = Vector3.one * (_droneSize / _droneSprite.bounds.size.x);
                go.SetActive(false);
                _drones[i] = go.transform;
                _cooldowns[i] = 0.3f * i; // lệch nhịp để các drone không bắn cùng lúc
            }
            if (_bulletPrefab != null)
            {
                _pool = new ObjectPool<Projectile>(_bulletPrefab, 12, _container);
                _release = _pool.Release;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < MaxDrones; i++) if (_drones[i] != null) _drones[i].gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_container != null) Destroy(_container.gameObject);
        }

        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            enabled = true;
            int count = Mathf.Min(SkillRules.DroneCount(_level), MaxDrones);
            for (int i = 0; i < MaxDrones; i++) _drones[i].gameObject.SetActive(i < count);
        }

        private void Update()
        {
            if (_level <= 0) return;
            int count = Mathf.Min(SkillRules.DroneCount(_level), MaxDrones);
            _angle += _orbitSpeed * Time.deltaTime;
            Vector2 center = transform.position;
            float bob = Mathf.Sin(Time.time * 4f) * 0.08f;

            for (int i = 0; i < count; i++)
            {
                float a = (_angle + i * 360f / count) * Mathf.Deg2Rad;
                Vector2 pos = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (_orbitRadius + bob);
                Transform drone = _drones[i];
                drone.position = pos;

                _cooldowns[i] -= Time.deltaTime;
                if (_cooldowns[i] > 0f || _pool == null) continue;
                Transform target = Nearest(pos);
                if (target == null) continue;
                _cooldowns[i] = SkillRules.DroneFireInterval(_level);

                Vector2 dir = ((Vector2)target.position - pos).normalized;
                drone.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                Projectile p = _pool.Get(pos, Quaternion.identity);
                p.OnReturnToPool = _release;
                p.Launch(dir, SkillRules.DroneDamage(_level) * _stats.DamageMultiplier * _stats.StageScale, _bulletSpeed, _range, 0, 0f, 0f, 0f);
            }
        }

        private Transform Nearest(Vector2 center)
        {
            int n = Physics2D.OverlapCircle(center, _range, _filter, _buffer);
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                float d = ((Vector2)_buffer[i].transform.position - center).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = _buffer[i].transform; }
            }
            return best;
        }
    }
}
