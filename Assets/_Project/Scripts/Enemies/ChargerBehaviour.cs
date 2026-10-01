using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Quái lao: đuổi theo, tới gần thì đứng gồng (rung hình, khóa hướng) rồi lao thẳng, nghỉ, lặp lại. Hướng lao chốt lúc gồng xong
    /// nên người chơi né ngang được — dạy kỹ năng né trước khi gặp boss chương (cũng có đòn lao).
    /// Ô nhớ EnemyContext: State (0 đuổi, 1 gồng, 2 lao, 3 nghỉ), Timer, Vector (hướng lao).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Charger", fileName = "Behaviour_Charger")]
    public class ChargerBehaviour : EnemyBehaviour
    {
        [SerializeField] private float _triggerRange = 5f;
        [SerializeField] private float _windUp = 0.6f;
        [SerializeField] private float _shakeAmount = 0.06f;
        [SerializeField] private float _dashSpeed = 12f;
        [SerializeField] private float _dashTime = 0.45f;
        [SerializeField] private float _recover = 0.8f;

        public override void Tick(EnemyContext ctx, float dt)
        {
            Vector2 toPlayer = ctx.DirectionToPlayer;
            switch (ctx.State)
            {
                case 0: // đuổi
                    if (toPlayer == Vector2.zero) return;
                    ctx.Move(toPlayer, ctx.MoveSpeed, dt);
                    ctx.Face(toPlayer, dt);
                    if (ctx.DistanceToPlayer <= _triggerRange)
                    {
                        ctx.State = 1;
                        ctx.Timer = _windUp;
                    }
                    return;

                case 1: // gồng: quay theo Player tới lúc chốt hướng
                    ctx.Face(toPlayer, dt);
                    if (ctx.Visual != null) ctx.Visual.localPosition = Random.insideUnitCircle * _shakeAmount;
                    ctx.Timer -= dt;
                    if (ctx.Timer > 0f) return;
                    ctx.ResetVisual();
                    ctx.Vector = toPlayer == Vector2.zero ? (Vector2)ctx.Enemy.transform.right : toPlayer;
                    ctx.State = 2;
                    ctx.Timer = _dashTime;
                    return;

                case 2: // lao
                    ctx.Move(ctx.Vector, _dashSpeed, dt);
                    ctx.Timer -= dt;
                    if (ctx.Timer > 0f) return;
                    ctx.State = 3;
                    ctx.Timer = _recover;
                    return;

                default: // nghỉ: khoảng hở để người chơi phản công
                    ctx.Timer -= dt;
                    if (ctx.Timer <= 0f) ctx.State = 0;
                    return;
            }
        }
    }
}
