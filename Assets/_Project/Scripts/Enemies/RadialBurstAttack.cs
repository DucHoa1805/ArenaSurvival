using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Bắn nhiều vòng đạn tỏa tròn, mỗi vòng xoay lệch một góc để khe hở đổi chỗ — người chơi phải di chuyển để né.
    /// Ô nhớ: Counter = số vòng đã bắn, Timer2 = thời gian tới vòng kế.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Boss Attack/Radial Burst", fileName = "Attack_Radial")]
    public class RadialBurstAttack : BossAttack
    {
        [Min(1)] [SerializeField] private int _bulletsPerRing = 12;
        [Min(1)] [SerializeField] private int _rings = 3;
        [SerializeField] private float _ringInterval = 0.45f;
        [SerializeField] private float _rotatePerRing = 15f;
        [SerializeField] private float _bulletSpeed = 4.5f;

        public override bool Tick(EnemyContext ctx, float dt)
        {
            // Đứng yên khi bắn: đạn tỏa ra từ một tâm cố định dễ đọc hướng hơn.
            ctx.Timer2 -= dt;
            if (ctx.Timer2 > 0f) return false;

            if (ctx.Projectiles != null)
            {
                float offset = ctx.Counter * _rotatePerRing;
                for (int i = 0; i < _bulletsPerRing; i++)
                {
                    Vector2 direction = EnemyRules.RadialDirection(i, _bulletsPerRing, offset);
                    ctx.Projectiles.Fire(ctx.Position, direction, _bulletSpeed, ctx.ProjectileDamage);
                }
            }

            ctx.Counter++;
            ctx.Timer2 = _ringInterval;
            return ctx.Counter >= _rings;
        }
    }
}
