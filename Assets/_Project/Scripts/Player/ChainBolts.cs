using System.Collections.Generic;
using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Tia điện nảy giữa các quái (10E D): súng Tesla và chiêu Lưới điện dùng chung. Tia là LineRenderer dựng sẵn (pool cố định) để
    /// lúc chơi không Instantiate; mỗi tia tự mờ dần trong vài phần giây.
    /// </summary>
    public class ChainBolts : MonoBehaviour
    {
        private const int MaxBolts = 40;
        private const int Segments = 7;

        [SerializeField] private Material _material;
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private float _boltLife = 0.16f;
        [SerializeField] private float _width = 0.14f;

        private LineRenderer[] _bolts;
        private float[] _life;
        private Color[] _colors;
        private int _next;
        private Transform _container;
        private ContactFilter2D _filter;
        private readonly List<Collider2D> _buffer = new List<Collider2D>(128);
        private readonly Transform[] _hit = new Transform[32];

        private void Awake()
        {
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = _enemyLayer, useTriggers = true };
            // Container ở gốc scene: tia vẽ theo toạ độ thế giới, không được xoay/di chuyển theo Player.
            _container = new GameObject("ChainBolts").transform;
            _bolts = new LineRenderer[MaxBolts];
            _life = new float[MaxBolts];
            _colors = new Color[MaxBolts];
            for (int i = 0; i < MaxBolts; i++)
            {
                var go = new GameObject("Bolt");
                go.transform.SetParent(_container, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = Segments;
                lr.sharedMaterial = _material;
                lr.widthMultiplier = _width;
                lr.numCapVertices = 2;
                lr.sortingOrder = 40;
                lr.enabled = false;
                _bolts[i] = lr;
            }
        }

        private void OnDestroy()
        {
            if (_container != null) Destroy(_container.gameObject);
        }

        private void Update()
        {
            for (int i = 0; i < MaxBolts; i++)
            {
                if (!_bolts[i].enabled) continue;
                _life[i] -= Time.deltaTime;
                if (_life[i] <= 0f) { _bolts[i].enabled = false; continue; }
                float a = _life[i] / _boltLife;
                Color c = _colors[i]; c.a = a;
                // Lõi sáng nhưng vẫn mang màu tia (trắng tinh làm tia nhìn xám trên nền cát).
                Color core = Color.Lerp(Color.white, _colors[i], 0.45f); core.a = a;
                _bolts[i].startColor = core;
                _bolts[i].endColor = c;
            }
        }

        /// <summary>
        /// Đánh first rồi nảy sang quái gần nhất chưa bị đánh (trong jumpRange), tối đa jumps lần. Trả số quái trúng.
        /// </summary>
        public int Strike(Vector2 from, Transform first, int jumps, float jumpRange, float damage, Color color, float widthScale = 1f)
        {
            jumps = Mathf.Min(jumps, _hit.Length);
            Transform target = first;
            int count = 0;
            for (; count < jumps && target != null; count++)
            {
                _hit[count] = target;
                Vector2 to = target.position;
                Draw(from, to, color, widthScale);
                if (target.TryGetComponent(out IHitReceiver receiver)) receiver.TakeHit(damage, (to - from).normalized);
                else if (target.TryGetComponent(out Health health)) health.TakeDamage(damage);
                from = to;
                target = Nearest(to, jumpRange, count + 1);
            }
            if (count > 0) Core.GameEvents.ProjectileHit(first.position);
            return count;
        }

        /// <summary>Quái gần nhất trong bán kính, bỏ qua hitCount con đầu của lượt nảy hiện tại.</summary>
        public Transform Nearest(Vector2 center, float radius, int hitCount = 0)
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

        private void Draw(Vector2 from, Vector2 to, Color color, float widthScale)
        {
            int i = _next;
            _next = (_next + 1) % MaxBolts;
            LineRenderer lr = _bolts[i];
            Vector2 dir = to - from;
            Vector2 normal = new Vector2(-dir.y, dir.x).normalized;
            float jag = Mathf.Min(0.35f, dir.magnitude * 0.12f);
            for (int s = 0; s < Segments; s++)
            {
                float t = s / (float)(Segments - 1);
                // Hai đầu nằm đúng điểm, đoạn giữa lệch ngẫu nhiên → hình tia sét gãy khúc, mỗi lần mỗi khác.
                float off = s == 0 || s == Segments - 1 ? 0f : Random.Range(-jag, jag);
                lr.SetPosition(s, from + dir * t + normal * off);
            }
            lr.widthMultiplier = _width * widthScale;
            _colors[i] = color;
            _life[i] = _boltLife;
            lr.startColor = Color.Lerp(Color.white, color, 0.45f);
            lr.endColor = color;
            lr.enabled = true;
        }
    }
}
