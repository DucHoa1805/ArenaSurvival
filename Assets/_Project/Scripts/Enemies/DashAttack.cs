using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Gồng (đứng rung tại chỗ, báo trước) → lao thẳng về chỗ Player đứng lúc hết gồng → nghỉ. Gây sát thương bằng va chạm như thường.
    /// Báo trước là bắt buộc: đòn lao nhanh mà không báo thì người chơi không có cách né, chỉ thấy "bị chết oan".
    /// Ô nhớ: Counter = giai đoạn (0 gồng, 1 lao, 2 nghỉ), Timer2 = thời gian còn lại của giai đoạn, Vector = hướng lao.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Boss Attack/Dash", fileName = "Attack_Dash")]
    public class DashAttack : BossAttack
    {
        [SerializeField] private float _windUp = 0.7f;
        [SerializeField] private float _shakeAmount = 0.07f;
        [SerializeField] private float _dashSpeed = 11f;
        [SerializeField] private float _dashDuration = 0.55f;
        [SerializeField] private float _recover = 0.5f;

        public override void Begin(EnemyContext ctx)
        {
            base.Begin(ctx);
            ctx.Timer2 = _windUp;
        }

        public override bool Tick(EnemyContext ctx, float dt)
        {
            ctx.Timer2 -= dt;
            switch (ctx.Counter)
            {
                case 0: // gồng: quay mặt theo Player, rung hình (chỉ object con Visual, không đụng vật lý)
                    ctx.Face(ctx.DirectionToPlayer, dt);
                    if (ctx.Visual != null) ctx.Visual.localPosition = Random.insideUnitCircle * _shakeAmount;
                    if (ctx.Timer2 > 0f) return false;
                    ctx.ResetVisual();
                    ctx.Vector = ctx.DirectionToPlayer;
                    ctx.Counter = 1;
                    ctx.Timer2 = _dashDuration;
                    return false;

                case 1: // lao
                    ctx.Move(ctx.Vector, _dashSpeed, dt);
                    ctx.Face(ctx.Vector, dt);
                    if (ctx.Timer2 > 0f) return false;
                    ctx.Counter = 2;
                    ctx.Timer2 = _recover;
                    return false;

                default: // nghỉ: khoảng hở để người chơi phản công
                    return ctx.Timer2 <= 0f;
            }
        }
    }
}
