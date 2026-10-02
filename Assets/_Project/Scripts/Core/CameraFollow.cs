using UnityEngine;

namespace ArenaSurvival.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0.15f;

        [Header("Rung khi Player bị đánh")]
        [Tooltip("Độ lệch tối đa (đơn vị thế giới) khi trauma = 1.")]
        [SerializeField] private float _maxShakeOffset = 0.35f;
        [Tooltip("Trauma cộng thêm cho mỗi 1 điểm sát thương Player nhận.")]
        [SerializeField] private float _traumaPerDamage = 0.05f;
        [Tooltip("Trauma tối thiểu mỗi lần bị đánh, để đòn nhẹ vẫn cảm nhận được.")]
        [SerializeField] private float _minTraumaPerHit = 0.3f;
        [SerializeField] private float _traumaDecay = 2.5f; // trauma giảm mỗi giây
        [SerializeField] private float _shakeFrequency = 25f;

        private Vector3 _velocity;
        // Vị trí bám theo tách khỏi transform: nếu cộng độ rung thẳng vào transform.position thì
        // lần SmoothDamp sau sẽ "đuổi theo" cả độ rung đó, làm camera trôi lệch khỏi Player.
        private Vector3 _followPosition;
        private float _trauma;
        private float _noiseSeed;

        private void Awake()
        {
            _followPosition = transform.position;
            _noiseSeed = Random.value * 100f;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnUltimateUsed += HandleUltimate;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnUltimateUsed -= HandleUltimate;
        }

        // Chiêu là khoảnh khắc "đã tay" nhất: rung mạnh 1 nhịp cho người chơi cảm được sức mạnh.
        private void HandleUltimate(Color color, string name) => _trauma = Mathf.Max(_trauma, 0.75f);

        private void HandlePlayerDamaged(float damage)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Max(_minTraumaPerHit, damage * _traumaPerDamage));
        }

        private void LateUpdate()
        {
            // LateUpdate đảm bảo camera cập nhật SAU khi Player đã di chuyển xong trong frame này,
            // tránh hiện tượng camera bị "giật" hoặc đi trước nhân vật.
            if (_target != null)
            {
                Vector3 targetPosition = new Vector3(_target.position.x, _target.position.y, _followPosition.z);
                _followPosition = Vector3.SmoothDamp(_followPosition, targetPosition, ref _velocity, _smoothTime);
            }

            transform.position = _followPosition + GetShakeOffset();
        }

        private Vector3 GetShakeOffset()
        {
            if (_trauma <= 0f) return Vector3.zero;

            // Dùng deltaTime (bị timeScale ảnh hưởng) để lúc Pause/LevelUp camera đứng yên chứ không rung tiếp.
            _trauma = Mathf.Max(0f, _trauma - _traumaDecay * Time.deltaTime);

            // Bình phương trauma: đòn nhẹ rung rất ít, đòn nặng/dồn dập mới rung mạnh, và tắt dần mượt.
            // Perlin noise thay cho Random để độ lệch liền mạch giữa các frame, không nhảy loạn.
            float strength = _trauma * _trauma * _maxShakeOffset;
            float t = Time.time * _shakeFrequency;
            float x = (Mathf.PerlinNoise(_noiseSeed, t) * 2f - 1f) * strength;
            float y = (Mathf.PerlinNoise(_noiseSeed + 1f, t) * 2f - 1f) * strength;
            return new Vector3(x, y, 0f);
        }
    }
}
