using ArenaSurvival.Combat;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Quái nổ: chạy nhanh tới Player, tới gần thì dừng chớp (rung) rồi nổ vùng và tự biến mất. Nổ tự hủy KHÔNG cho XP/đồ
    /// (Despawn(false)) — muốn có thưởng thì phải bắn hạ trước khi nó tới gần: thưởng cho người chơi giữ khoảng cách.
    /// Ô nhớ EnemyContext: State (0 chạy, 1 sắp nổ), Timer.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Kamikaze", fileName = "Behaviour_Kamikaze")]
    public class KamikazeBehaviour : EnemyBehaviour
    {
        [SerializeField] private float _fuseRange = 1.3f;
        [SerializeField] private float _fuseTime = 0.6f;
        [SerializeField] private float _radius = 1.8f;
        [Tooltip("Sát thương nổ = sát thương chạm (đã nhân độ khó) × hệ số này.")]
        [SerializeField] private float _damageMultiplier = 2f;
        [SerializeField] private float _shakeAmount = 0.09f;

        public override void Tick(EnemyContext ctx, float dt)
        {
            if (ctx.State == 0)
            {
                Vector2 toPlayer = ctx.DirectionToPlayer;
                if (toPlayer == Vector2.zero) return;
                ctx.Move(toPlayer, ctx.MoveSpeed, dt);
                ctx.Face(toPlayer, dt);
                if (ctx.DistanceToPlayer > _fuseRange) return;
                ctx.State = 1;
                ctx.Timer = _fuseTime;
                return;
            }

            if (ctx.Visual != null) ctx.Visual.localPosition = Random.insideUnitCircle * _shakeAmount;
            ctx.Timer -= dt;
            if (ctx.Timer > 0f) return;
            Explode(ctx);
        }

        private void Explode(EnemyContext ctx)
        {
            ctx.ResetVisual();
            if (ctx.Player != null && EnemyRules.KamikazeHit(ctx.DistanceToPlayer, _radius) && ctx.Player.TryGetComponent(out Health health))
            {
                health.TakeDamage(ctx.ContactDamage * _damageMultiplier);
            }
            Core.GameEvents.Explosion(ctx.Position, _radius);
            ctx.Enemy.Despawn(false);
        }
    }
}
