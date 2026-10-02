using ArenaSurvival.Combat;
using ArenaSurvival.RunUpgrades;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Kỹ năng Sét lan (10D): định kỳ đánh quái gần nhất rồi nhảy sang quái kế bên chưa bị đánh. Tia sét là vài LineRenderer
    /// dựng sẵn (không cấp phát), vẽ đường gãy khúc rồi mờ dần. Không có hướng → Lính Khiên không chặn được.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerLightning : MonoBehaviour
    {
        [SerializeField] private float _range = 7f;
        [SerializeField] private float _jumpRange = 3.5f;
        [SerializeField] private float _boltLife = 0.18f;
        [SerializeField] private Color _color = new Color(0.65f, 0.9f, 1f, 1f);
        [SerializeField] private Material _material;

        private const int MaxBolts = 8;
        private const int PointsPerBolt = 6;
        private const int MaxTargets = 64;
        private readonly Collider2D[] _buffer = new Collider2D[MaxTargets];
        private readonly Transform[] _hit = new Transform[MaxBolts];
        private LineRenderer[] _bolts;
        private float _boltTimer;
        private ContactFilter2D _filter;
        private PlayerStats _stats;
        private Transform _container;
        private int _level;
        private float _timer;

        public int Level => _level;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };
            if (_material == null) _material = new Material(Shader.Find("Sprites/Default"));

            // Container ở gốc scene: tia vẽ bằng tọa độ thế giới, không cần theo Player.
            _container = new GameObject("LightningBolts").transform;
            _bolts = new LineRenderer[MaxBolts];
            for (int i = 0; i < MaxBolts; i++)
            {
                var go = new GameObject("Bolt" + i);
                go.transform.SetParent(_container, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = PointsPerBolt;
                lr.sharedMaterial = _material;
                lr.widthMultiplier = 0.09f;
                lr.numCapVertices = 2;
                lr.sortingOrder = 6;
                lr.enabled = false;
                _bolts[i] = lr;
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
            if (_boltTimer > 0f)
            {
                _boltTimer -= Time.deltaTime;
                float a = Mathf.Clamp01(_boltTimer / _boltLife);
                Color c = _color; c.a = a;
                Color edge = Color.white; edge.a = a;
                for (int i = 0; i < MaxBolts; i++)
                {
                    if (!_bolts[i].enabled) continue;
                    _bolts[i].startColor = edge;
                    _bolts[i].endColor = c;
                    if (_boltTimer <= 0f) _bolts[i].enabled = false;
                }
            }

            if (_level <= 0) return;
            _timer += Time.deltaTime;
            if (_timer < SkillRules.LightningInterval(_level)) return;

            Transform first = Nearest(transform.position, _range, 0);
            if (first == null) return; // giữ nhịp đã nạp, đánh ngay khi có quái trong tầm
            _timer = 0f;
            Strike(first);
        }

        private void Strike(Transform first)
        {
            float damage = SkillRules.LightningDamage(_level) * _stats.DamageMultiplier * _stats.StageScale;
            int jumps = Mathf.Min(SkillRules.LightningJumps(_level), MaxBolts);
            Vector2 from = transform.position;
            Transform target = first;
            int count = 0;
            for (; count < jumps && target != null; count++)
            {
                _hit[count] = target;
                Vector2 to = target.position;
                DrawBolt(_bolts[count], from, to);
                if (target.TryGetComponent(out Health health)) health.TakeDamage(damage);
                from = to;
                target = Nearest(to, _jumpRange, count + 1);
            }
            for (int i = count; i < MaxBolts; i++) _bolts[i].enabled = false;
            _boltTimer = _boltLife;
            Core.GameEvents.ProjectileHit(first.position);
        }

        /// <summary>Quái gần nhất trong bán kính, bỏ qua những con đã bị đánh trong lượt này (hitCount phần tử đầu của _hit).</summary>
        private Transform Nearest(Vector2 center, float radius, int hitCount)
        {
            int n = Physics2D.OverlapCircle(center, radius, _filter, _buffer);
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Transform t = _buffer[i].transform;
                bool already = false;
                for (int h = 0; h < hitCount; h++) if (_hit[h] == t) { already = true; break; }
                if (already) continue;
                float d = ((Vector2)t.position - center).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = t; }
            }
            return best;
        }

        private static void DrawBolt(LineRenderer lr, Vector2 from, Vector2 to)
        {
            Vector2 dir = to - from;
            Vector2 normal = new Vector2(-dir.y, dir.x).normalized;
            float jitter = Mathf.Min(0.35f, dir.magnitude * 0.12f);
            for (int i = 0; i < PointsPerBolt; i++)
            {
                float t = (float)i / (PointsPerBolt - 1);
                // Điểm giữa lệch ngẫu nhiên ngang tia → nét gãy khúc như sét thật; 2 đầu nằm đúng điểm.
                float offset = i == 0 || i == PointsPerBolt - 1 ? 0f : Random.Range(-jitter, jitter);
                lr.SetPosition(i, from + dir * t + normal * offset);
            }
            lr.enabled = true;
        }
    }
}
