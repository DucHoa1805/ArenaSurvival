using UnityEngine;

namespace ArenaSurvival.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0.15f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            // LateUpdate đảm bảo camera cập nhật SAU khi Player đã di chuyển xong trong frame này,
            // tránh hiện tượng camera bị "giật" hoặc đi trước nhân vật.
            if (_target == null) return;

            Vector3 targetPosition = new Vector3(_target.position.x, _target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
        }
    }
}