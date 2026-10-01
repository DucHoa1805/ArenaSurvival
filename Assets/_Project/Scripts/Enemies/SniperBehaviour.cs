using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Xạ thủ bắn tỉa: đứng xa, định kỳ ngắm (tia đỏ bám theo Player), khoảng cuối thì chốt hướng rồi bắn 1 viên rất nhanh.
    /// Tia chốt trước khi bắn để người chơi luôn né được nếu để ý — sát thương cao nhưng công bằng.
    /// Ô nhớ EnemyContext: State (0 di chuyển, 1 ngắm), Timer, Vector (hướng ngắm/chốt).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Sniper", fileName = "Behaviour_Sniper")]
    public class SniperBehaviour : EnemyBehaviour
    {
        [SerializeField] private float _preferredRange = 8f;
        [SerializeField] private float _tolerance = 1.5f;
        [SerializeField] private float _fireRange = 11f;
        [SerializeField] private float _interval = 3.5f;
        [SerializeField] private float _aimTime = 1f;
        [SerializeField] private float _lockWindow = 0.25f;
        [SerializeField] private float _bulletSpeed = 16f;
        [SerializeField] private float _beamLength = 14f;
        [SerializeField] private float _muzzleOffset = 0.5f;

        public override void OnSpawn(EnemyContext ctx)
        {
            base.OnSpawn(ctx);
            ctx.Timer = _interval * Random.Range(0.5f, 1f);
        }

        public override void Tick(EnemyContext ctx, float dt)
        {
            Vector2 toPlayer = ctx.DirectionToPlayer;
            if (ctx.State == 0)
            {
                if (toPlayer == Vector2.zero) return;
                int intent = EnemyRules.RangeIntent(ctx.DistanceToPlayer, _preferredRange, _tolerance);
                if (intent != 0) ctx.Move(toPlayer * intent, ctx.MoveSpeed, dt);
                ctx.Face(toPlayer, dt);

                ctx.Timer -= dt;
                if (ctx.Timer > 0f || ctx.DistanceToPlayer > _fireRange) return;
                ctx.State = 1;
                ctx.Timer = _aimTime;
                ctx.Vector = toPlayer;
                if (ctx.Telegraph != null) ctx.Telegraph.enabled = true;
                return;
            }

            // Ngắm: tia bám theo Player tới khi chốt.
            ctx.Timer -= dt;
            if (!EnemyRules.AimLocked(ctx.Timer, _lockWindow) && toPlayer != Vector2.zero) ctx.Vector = toPlayer;
            ctx.Face(ctx.Vector, dt);
            if (ctx.Telegraph != null)
            {
                Vector2 from = ctx.Position + ctx.Vector * _muzzleOffset;
                ctx.Telegraph.SetPosition(0, from);
                ctx.Telegraph.SetPosition(1, from + ctx.Vector * _beamLength);
            }
            if (ctx.Timer > 0f) return;

            if (ctx.Telegraph != null) ctx.Telegraph.enabled = false;
            if (ctx.Projectiles != null)
                ctx.Projectiles.Fire(ctx.Position + ctx.Vector * _muzzleOffset, ctx.Vector, _bulletSpeed, ctx.ProjectileDamage);
            ctx.State = 0;
            ctx.Timer = _interval;
        }
    }
}
