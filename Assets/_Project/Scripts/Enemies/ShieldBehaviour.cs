using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Lính Khiên: tiến theo hướng đang quay mặt và xoay về phía Player CHẬM, khiên chặn phần lớn đạn bắn vào mặt.
    /// Vì súng tự ngắm con gần nhất nên đứng yên bắn sẽ trúng khiên; người chơi phải chạy vòng (khiên xoay không kịp)
    /// hoặc dùng nổ/lưỡi cưa (không có hướng → không bị chặn). Không dùng ô nhớ nào của EnemyContext.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Shield", fileName = "Behaviour_Shield")]
    public class ShieldBehaviour : EnemyBehaviour
    {
        [Tooltip("Độ/giây. Thấp hơn hẳn quái thường (540) để chạy vòng ra sau được.")]
        [SerializeField] private float _turnSpeed = 90f;
        [Tooltip("Cung khiên phía trước (độ).")]
        [SerializeField] private float _arc = 120f;
        [Range(0f, 1f)] [SerializeField] private float _blockFraction = 0.85f;

        public override void OnSpawn(EnemyContext ctx)
        {
            base.OnSpawn(ctx);
            if (ctx.Shield != null) ctx.Shield.SetActive(true);
        }

        public override void Tick(EnemyContext ctx, float dt)
        {
            Vector2 toPlayer = ctx.DirectionToPlayer;
            if (toPlayer == Vector2.zero) return;

            ctx.Face(toPlayer, dt, _turnSpeed);
            // Đi theo hướng mặt chứ không thẳng tới Player: nếu đi thẳng thì khiên vẫn lệch nhưng thân lại bám sát,
            // người chơi vòng ra sau xong cũng bị húc ngay — mất ý nghĩa của việc xoay chậm.
            ctx.Move(ctx.Facing, ctx.MoveSpeed, dt);
        }

        public override float ModifyIncomingDamage(EnemyContext ctx, float damage, Vector2 hitDirection) =>
            EnemyRules.ShieldBlocks(ctx.Facing, hitDirection, _arc) ? damage * (1f - _blockFraction) : damage;
    }
}
