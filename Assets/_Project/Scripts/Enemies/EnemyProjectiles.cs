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

        private ObjectPool<Projectile> _pool;
        // Gán sẵn 1 lần: gán thẳng tên hàm vào OnReturnToPool mỗi lần bắn là mỗi lần tạo 1 delegate mới (rác GC).
        private Action<Projectile> _release;

        private void Awake()
        {
            _pool = new ObjectPool<Projectile>(_prefab, _prewarmCount, transform);
            _release = _pool.Release;
        }

        public void Fire(Vector2 position, Vector2 direction, float speed, float damage)
        {
            Projectile projectile = _pool.Get(position, Quaternion.identity);
            projectile.OnReturnToPool = _release;
            projectile.Launch(direction, damage, speed);
        }
    }
}
