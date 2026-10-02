using System;
using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Pool đạn chung cho mọi quái và boss. Quái nằm trong pool, bật/tắt liên tục, nên không tự giữ pool đạn riêng được
    /// (đạn đang bay sẽ mất chủ khi quái chết); một pool trong scene thì đạn sống độc lập với kẻ bắn.
    /// </summary>
    public class EnemyProjectiles : MonoBehaviour
    {
        [Tooltip("Prefab đạn trên layer EnemyProjectile (chỉ va chạm Player).")]
        [SerializeField] private Projectile _prefab;
        [Tooltip("Boss bắn vòng 16 viên × 4 lớp cùng lúc với vài xạ thủ: tạo sẵn đủ để lúc chơi không phải Instantiate.")]
        [SerializeField] private int _prewarmCount = 80;

        [Tooltip("Quả Mưa Pháo (vòng báo trước rồi nổ). Không bắt buộc: trống thì đòn Mưa Pháo không làm gì.")]
        [SerializeField] private ArtilleryShell _shellPrefab;
        [Tooltip("Boss gọi tối đa ~8 quả/đòn, 2 đòn có thể chồng nhau khi đổi pha.")]
        [SerializeField] private int _shellPrewarmCount = 24;

        private ObjectPool<Projectile> _pool;
        // Gán sẵn 1 lần: gán thẳng tên hàm vào OnReturnToPool mỗi lần bắn là mỗi lần tạo 1 delegate mới (rác GC).
        private Action<Projectile> _release;
        private ObjectPool<ArtilleryShell> _shellPool;
        private Action<ArtilleryShell> _releaseShell;

        private void Awake()
        {
            _pool = new ObjectPool<Projectile>(_prefab, _prewarmCount, transform);
            _release = _pool.Release;
            if (_shellPrefab != null)
            {
                _shellPool = new ObjectPool<ArtilleryShell>(_shellPrefab, _shellPrewarmCount, transform);
                _releaseShell = _shellPool.Release;
            }
        }

        /// <summary>Đặt 1 quả pháo nổ sau delay giây tại position; trúng target nếu còn trong bán kính.</summary>
        public void PlaceShell(Vector2 position, float delay, float radius, float damage, Transform target)
        {
            if (_shellPool == null) return;
            ArtilleryShell shell = _shellPool.Get(position, Quaternion.identity);
            shell.OnReturnToPool = _releaseShell;
            shell.Arm(position, delay, radius, damage, target);
        }

        public void Fire(Vector2 position, Vector2 direction, float speed, float damage)
        {
            Projectile projectile = _pool.Get(position, Quaternion.identity);
            projectile.OnReturnToPool = _release;
            projectile.Launch(direction, damage, speed);
        }
    }
}
