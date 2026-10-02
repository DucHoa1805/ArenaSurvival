using System;
using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// 1 quả của Mưa Pháo: vòng đỏ báo trước tại chỗ, phần tô bên trong lớn dần, hết giờ thì nổ và trúng Player nếu còn đứng trong vòng.
    /// Tự đếm giờ thay vì để đòn của boss đếm: boss có thể đổi pha/chết giữa chừng mà quả đã gọi vẫn phải rơi đúng như đã báo.
    /// Sprite vòng và phần tô có đường kính 1 đơn vị ở scale 1, nên scale gốc = 2 × bán kính.
    /// </summary>
    public class ArtilleryShell : MonoBehaviour
    {
        [Tooltip("Phần tô lớn dần từ tâm — cho người chơi thấy còn bao lâu nữa nổ.")]
        [SerializeField] private Transform _fill;

        private float _delay;
        private float _elapsed;
        private float _radius;
        private float _damage;
        private Transform _target;

        public Action<ArtilleryShell> OnReturnToPool;

        public void Arm(Vector2 position, float delay, float radius, float damage, Transform target)
        {
            transform.position = position;
            transform.localScale = Vector3.one * (radius * 2f);
            _delay = Mathf.Max(0.05f, delay);
            _radius = radius;
            _damage = damage;
            _target = target;
            _elapsed = 0f;
            if (_fill != null) _fill.localScale = Vector3.zero;
        }

        private void Update()
        {
            // deltaTime: lên cấp/tạm dừng thì quả cũng đứng yên, không nổ lúc người chơi không điều khiển được.
            _elapsed += Time.deltaTime;
            if (_fill != null) _fill.localScale = Vector3.one * Mathf.Clamp01(_elapsed / _delay);
            if (_elapsed < _delay) return;

            Vector2 center = transform.position;
            if (_target != null && EnemyRules.InBlast(center, _radius, _target.position) && _target.TryGetComponent(out Health health))
            {
                health.TakeDamage(_damage);
            }
            Core.GameEvents.Explosion(center, _radius);
            OnReturnToPool?.Invoke(this);
        }
    }
}
