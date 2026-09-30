using System;
using UnityEngine;

namespace ArenaSurvival.Vfx
{
    /// <summary>
    /// 1 hiệu ứng particle chạy 1 lần rồi tự trả về pool. Không dùng timer: ParticleSystem được đặt
    /// Stop Action = Callback nên Unity tự gọi OnParticleSystemStopped khi hạt cuối cùng biến mất.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledVfx : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        // Giống Projectile/Enemy/XpOrb: hiệu ứng không biết gì về ObjectPool<T>, chỉ gọi callback khi xong việc.
        public Action<PooledVfx> OnReturnToPool;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();

            // Đặt bằng code để không phụ thuộc vào việc prefab có được cấu hình đúng hay không.
            ParticleSystem.MainModule main = _particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        private void OnEnable()
        {
            // Object lấy lại từ pool có thể còn hạt của lần trước: xóa sạch rồi phát lại từ đầu.
            _particleSystem.Clear(true);
            _particleSystem.Play(true);
        }

        private void OnParticleSystemStopped()
        {
            OnReturnToPool?.Invoke(this);
        }
    }
}
