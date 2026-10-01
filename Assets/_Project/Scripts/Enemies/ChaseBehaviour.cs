using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>Đuổi thẳng tới Player và gây sát thương khi chạm (hành vi gốc của mọi quái trước Phase 6B).</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Chase", fileName = "Behaviour_Chase")]
    public class ChaseBehaviour : EnemyBehaviour
    {
        public override void Tick(EnemyContext ctx, float dt)
        {
            Vector2 direction = ctx.DirectionToPlayer;
            // Đã chồng lên Player: không có hướng rõ ràng, giữ nguyên để không bị xoay loạn.
            if (direction == Vector2.zero) return;

            ctx.Move(direction, ctx.MoveSpeed, dt);
            ctx.Face(direction, dt);
        }
    }
}
