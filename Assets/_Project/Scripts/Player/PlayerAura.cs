using ArenaSurvival.Combat;
using ArenaSurvival.RunUpgrades;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Kỹ năng Vòng điện: mỗi nhịp gây sát thương mọi quái trong bán kính quanh Player (nhân hệ số sát thương trong màn).
    /// Không có hướng → Lính Khiên không chặn được. Tắt sẵn, UpgradeManager bật. Hình vòng là con "AuraVisual" (scale theo bán kính).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerAura : MonoBehaviour
    {
        [SerializeField] private float _tickInterval = 0.5f;
        [SerializeField] private Transform _visual;

        private const int MaxTargets = 64;
        // Dùng lại 1 buffer: quét mỗi nhịp mà không cấp phát.
        private readonly Collider2D[] _buffer = new Collider2D[MaxTargets];
        private ContactFilter2D _filter;
        private PlayerStats _stats;
        private int _level;
        private float _timer;
        private float _pulse;
        private Vector3 _baseScale;

        public int Level => _level;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };
            if (_visual == null) _visual = transform.Find("AuraVisual");
            if (_visual != null) _visual.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_visual != null) _visual.gameObject.SetActive(_level > 0);
        }

        private void OnDisable()
        {
            if (_visual != null) _visual.gameObject.SetActive(false);
        }

        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            enabled = true;
            if (_visual != null)
            {
                _visual.gameObject.SetActive(true);
                // Sprite vòng chuẩn hóa đường kính 1 ở scale 1; bù scale của Player để vòng đúng bán kính thế giới.
                float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
                _visual.localScale = Vector3.one * (SkillRules.AuraRadius(_level) * 2f / parentScale);
                _baseScale = _visual.localScale;
                // Cấp tối đa đổi sang vàng điện: nhìn là biết đã nâng hết.
                var sr = _visual.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = _level >= 5 ? new Color(1f, 0.85f, 0.35f, 0.55f) : new Color(0.45f, 0.75f, 1f, 0.45f);
            }
        }

        private void Update()
        {
            if (_level <= 0) return;
            if (_visual != null)
            {
                _visual.Rotate(0f, 0f, 90f * Time.deltaTime);
                // Nảy to lên mỗi nhịp gây sát thương rồi co lại: người chơi "thấy" vòng đang đánh.
                _pulse = Mathf.Max(0f, _pulse - Time.deltaTime * 4f);
                _visual.localScale = _baseScale * (1f + 0.12f * _pulse);
            }
            _timer += Time.deltaTime;
            if (_timer < _tickInterval) return;
            _timer = 0f;
            _pulse = 1f;

            float damage = SkillRules.AuraDamage(_level) * _stats.DamageMultiplier * _stats.StageScale;
            int count = Physics2D.OverlapCircle(transform.position, SkillRules.AuraRadius(_level), _filter, _buffer);
            for (int i = 0; i < count; i++)
            {
                if (_buffer[i].TryGetComponent(out Health health)) health.TakeDamage(damage);
            }
        }
    }
}
