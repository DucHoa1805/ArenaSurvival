using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Đòn boss: đứng yên bắn liên tục vài "cánh" đạn, góc xoay dần thành hình xoắn ốc. Khác vòng đạn (bắn từng đợt) ở chỗ có khe hở
    /// di chuyển liên tục — người chơi phải đi theo khe chứ không đứng chờ hết đợt.
    /// Ô nhớ EnemyContext (theo quy ước BossAttack): Timer2 (đếm tới lượt bắn), Vector.x (thời gian đã bắn).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Boss Attack/Spiral", fileName = "Attack_Spiral")]
    public class SpiralAttack : BossAttack
    {
        [SerializeField] private float _duration = 3f;
        [SerializeField] private float _interval = 0.09f;
        [Min(1)] [SerializeField] private int _arms = 3;
        [SerializeField] private float _rotateSpeed = 110f;
        [SerializeField] private float _bulletSpeed = 4f;

        public override bool Tick(EnemyContext ctx, float dt)
        {
            float elapsed = ctx.Vector.x + dt;
            ctx.Vector = new Vector2(elapsed, 0f);

            ctx.Timer2 -= dt;
            if (ctx.Timer2 <= 0f && ctx.Projectiles != null)
            {
                float angle = EnemyRules.SpiralAngle(elapsed, _rotateSpeed);
                for (int i = 0; i < _arms; i++)
                    ctx.Projectiles.Fire(ctx.Position, EnemyRules.RadialDirection(i, _arms, angle), _bulletSpeed, ctx.ProjectileDamage);
                ctx.Timer2 = _interval;
            }
            return elapsed >= _duration;
        }
    }
}
