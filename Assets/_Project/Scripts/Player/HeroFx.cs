using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Hiệu ứng trang trí của nhân vật chính (10D): vòng sáng xoay dưới chân (giữ hướng riêng, không xoay theo thân),
    /// nảy nhẹ theo nhịp thở, và vệt sáng sau lưng chỉ hiện khi đang chạy. Chỉ hình ảnh — không ảnh hưởng lối chơi.
    /// </summary>
    public class HeroFx : MonoBehaviour
    {
        [SerializeField] private Transform _groundRing;
        [SerializeField] private float _ringSpin = 45f;
        [SerializeField] private TrailRenderer _runTrail;
        [SerializeField] private float _runSpeedThreshold = 0.5f;

        private Vector3 _ringScale;
        private float _ringAngle;
        private Vector3 _lastPosition;

        private void Awake()
        {
            if (_groundRing == null) _groundRing = transform.Find("GroundRing");
            if (_runTrail == null) _runTrail = GetComponentInChildren<TrailRenderer>(true);
            if (_groundRing != null) _ringScale = _groundRing.localScale;
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // tạm dừng/lên cấp: đứng yên đúng như hiện tại

            if (_groundRing != null)
            {
                // Đặt góc theo thế giới: thân xoay theo hướng chạy nhưng vòng dưới chân vẫn quay đều, không giật theo.
                _ringAngle += _ringSpin * dt;
                _groundRing.rotation = Quaternion.Euler(0f, 0f, _ringAngle);
                _groundRing.localScale = _ringScale * (1f + 0.05f * Mathf.Sin(Time.time * 3f));
            }

            if (_runTrail != null)
            {
                float speed = (transform.position - _lastPosition).magnitude / dt;
                _runTrail.emitting = speed > _runSpeedThreshold;
            }
            _lastPosition = transform.position;
        }
    }
}
