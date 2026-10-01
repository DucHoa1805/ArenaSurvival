using ArenaSurvival.Core;
using ArenaSurvival.Player;
using UnityEngine;
using ArenaSurvival.Combat;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Bộ lưỡi cưa: 1 Rigidbody2D kinematic đi theo vị trí Player và tự quay, các lưỡi cưa là collider con
    /// đặt đều trên vòng tròn. Quay cả bộ bằng MoveRotation thì mọi lưỡi quay theo mà không cần tính vị trí từng cái.
    ///
    /// Nằm trên layer PlayerProjectile nên theo Layer Collision Matrix chỉ chạm quái (và tường, vốn không có Health).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SawBladeRig : MonoBehaviour
    {
        private OrbitWeaponData _data;
        private Transform _owner;
        private PlayerStats _stats;
        private Rigidbody2D _rb;
        private Transform[] _visuals;
        private float _orbitAngle;

        /// <summary>Dựng bộ lưỡi cưa bằng code từ dữ liệu, nên đổi số lưỡi/bán kính chỉ cần sửa asset.</summary>
        public static SawBladeRig Create(OrbitWeaponData data, Transform owner, PlayerStats stats)
        {
            var root = new GameObject($"SawBlades ({data.name})");
            root.layer = LayerMask.NameToLayer("PlayerProjectile");
            root.transform.position = owner.position;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // quay nhanh nên cần nội suy cho mượt

            var rig = root.AddComponent<SawBladeRig>();
            rig._data = data;
            rig._owner = owner;
            rig._stats = stats;
            rig._rb = rb;
            rig._visuals = new Transform[data.bladeCount];

            for (int i = 0; i < data.bladeCount; i++)
            {
                float angle = i * Mathf.PI * 2f / data.bladeCount;

                var blade = new GameObject($"Blade{i + 1}");
                blade.layer = root.layer;
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * data.orbitRadius;

                var circle = blade.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = data.bladeSize * 0.5f;

                // Hình nằm ở object cháu để tự xoay cho đẹp mà không đụng tới collider.
                var visual = new GameObject("Visual");
                visual.transform.SetParent(blade.transform, false);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = data.bladeSprite;
                sr.sortingOrder = 1; // trên nhân vật và quái (0), dưới hiệu ứng (5)
                if (data.bladeSprite != null)
                {
                    float spriteSize = data.bladeSprite.bounds.size.x;
                    visual.transform.localScale = Vector3.one * (data.bladeSize / spriteSize);
                }

                rig._visuals[i] = visual.transform;
            }

            return rig;
        }

        private void OnEnable()
        {
            // Bật lên (lúc mở khóa) thì dịch ngay tới chỗ Player, không trượt từ vị trí cũ tới.
            if (_owner != null)
            {
                _rb.position = _owner.position;
                transform.position = _owner.position;
            }
        }

        private void FixedUpdate()
        {
            if (_owner == null) return;

            _orbitAngle += _data.orbitSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(_owner.position);
            _rb.MoveRotation(_orbitAngle);
        }

        private void Update()
        {
            float spin = _data.bladeSpinSpeed * Time.deltaTime;
            foreach (Transform visual in _visuals)
            {
                visual.Rotate(0f, 0f, spin);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Mỗi lần 1 lưỡi quét vào quái là 1 lần trúng; quái đứng yên sẽ bị các lưỡi lần lượt quét qua.
            if (!other.TryGetComponent(out Health targetHealth)) return;

            targetHealth.TakeDamage(_data.damage * _stats.DamageMultiplier);
            GameEvents.ProjectileHit(other.transform.position);
        }
    }
}
