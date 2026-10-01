using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Quái phân thân: đuổi thẳng; chết thì tách thành vài con nhỏ (EnemyData riêng, không tách tiếp). Tăng áp lực cho súng bắn
    /// đơn, và là lý do để mở khóa Shotgun/Phóng lựu. Con nhỏ giữ độ khó của màn nhưng không giữ hệ số máu tinh anh.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Splitter", fileName = "Behaviour_Splitter")]
    public class SplitterBehaviour : EnemyBehaviour
    {
        [SerializeField] private EnemyData _child;
        [Min(1)] [SerializeField] private int _count = 2;
        [SerializeField] private float _spread = 0.4f;

        public override void Tick(EnemyContext ctx, float dt)
        {
            Vector2 toPlayer = ctx.DirectionToPlayer;
            if (toPlayer == Vector2.zero) return;
            ctx.Move(toPlayer, ctx.MoveSpeed, dt);
            ctx.Face(toPlayer, dt);
        }

        public override void OnDeath(EnemyContext ctx)
        {
            if (_child == null || ctx.World == null) return;
            // Hệ số sát thương của màn = độ khó (tinh anh chỉ nhân máu), nên dùng nó làm độ khó cho con.
            EnemyModifiers mods = EnemyModifiers.ForDifficulty(ctx.Modifiers.damage);
            Vector2 side = Vector2.Perpendicular(ctx.DirectionToPlayer == Vector2.zero ? Vector2.right : ctx.DirectionToPlayer);
            for (int i = 0; i < _count; i++)
            {
                float t = _count == 1 ? 0f : i / (_count - 1f) * 2f - 1f;
                ctx.World.Summon(_child, mods, ctx.Position + side * (t * _spread));
            }
        }
    }
}
