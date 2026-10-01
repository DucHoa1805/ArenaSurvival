using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Quái bắn xa: giữ khoảng cách với Player và bắn từng viên. Buộc người chơi phải di chuyển chủ động
    /// thay vì đứng yên cho súng tự bắn quái đang lao tới.
    /// Ô nhớ: Timer = thời gian còn lại tới phát bắn kế.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Keep Distance Shooter", fileName = "Behaviour_Shooter")]
    public class KeepDistanceShooterBehaviour : EnemyBehaviour
    {
        [SerializeField] private float _preferredRange = 5.5f;
        [SerializeField] private float _tolerance = 1f;
        [Tooltip("Chỉ bắn khi Player trong tầm này (tránh bắn từ ngoài màn hình).")]
        [SerializeField] private float _fireRange = 8f;
        [SerializeField] private float _fireInterval = 2f;
        [SerializeField] private float _bulletSpeed = 5f;
        [SerializeField] private float _muzzleOffset = 0.5f;

        public override void OnSpawn(EnemyContext ctx)
        {
            base.OnSpawn(ctx);
            // Lệch nhịp ngẫu nhiên: cả nhóm xạ thủ sinh cùng lúc mà bắn cùng một nhịp thì trông như máy.
            ctx.Timer = _fireInterval * Random.Range(0.5f, 1f);
        }

        public override void Tick(EnemyContext ctx, float dt)
        {
            Vector2 direction = ctx.DirectionToPlayer;
            if (direction == Vector2.zero) return;

            float distance = ctx.DistanceToPlayer;
            int intent = EnemyRules.RangeIntent(distance, _preferredRange, _tolerance);
            if (intent != 0) ctx.Move(direction * intent, ctx.MoveSpeed, dt);
            ctx.Face(direction, dt); // lùi vẫn quay mặt về Player để bắn

            ctx.Timer -= dt;
            if (ctx.Timer > 0f || distance > _fireRange || ctx.Projectiles == null) return;

            ctx.Projectiles.Fire(ctx.Position + direction * _muzzleOffset, direction, _bulletSpeed, ctx.ProjectileDamage);
            ctx.Timer = _fireInterval;
        }
    }
}
