using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Đòn boss Mưa Pháo: gồng (rung) rồi gọi từng quả pháo vào chỗ Player đang đứng (lệch ngẫu nhiên). Mỗi quả có vòng đỏ báo trước
    /// và tự nổ sau delay (ArtilleryShell) — buộc người chơi liên tục đổi chỗ thay vì đứng bắn.
    /// Ô nhớ EnemyContext (theo quy ước BossAttack): Vector.x (thời gian đã chạy, gồm cả gồng), Timer2 (đếm tới quả kế), Counter (số quả đã gọi).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Boss Attack/Artillery", fileName = "Attack_Artillery")]
    public class ArtilleryAttack : BossAttack
    {
        [SerializeField] private float _windUp = 0.6f;
        [SerializeField] private float _shakeAmount = 0.05f;
        [Min(1)] [SerializeField] private int _shells = 5;
        [SerializeField] private float _interval = 0.35f;
        [Tooltip("Thời gian từ lúc hiện vòng tới lúc nổ — đủ để chạy ra khỏi bán kính.")]
        [SerializeField] private float _delay = 1.2f;
        [SerializeField] private float _radius = 1.5f;
        [Tooltip("Quả đầu nhắm đúng chỗ Player; các quả sau lệch ngẫu nhiên trong bán kính này để phủ cả hướng đang chạy.")]
        [SerializeField] private float _scatter = 1.2f;
        [SerializeField] private float _damageMultiplier = 1.5f;

        public override bool Tick(EnemyContext ctx, float dt)
        {
            float elapsed = ctx.Vector.x + dt;
            ctx.Vector = new Vector2(elapsed, 0f);

            if (elapsed < _windUp)
            {
                if (ctx.Visual != null) ctx.Visual.localPosition = Random.insideUnitCircle * _shakeAmount;
                return false;
            }
            if (ctx.Visual != null) ctx.Visual.localPosition = Vector3.zero;

            ctx.Timer2 -= dt;
            if (ctx.Timer2 > 0f) return false;

            if (ctx.Projectiles != null && ctx.Player != null)
            {
                Vector2 target = (Vector2)ctx.Player.position;
                if (ctx.Counter > 0) target += Random.insideUnitCircle * _scatter;
                if (ctx.World != null) target = ctx.World.ClampToArena(target);
                ctx.Projectiles.PlaceShell(target, _delay, _radius, ctx.ProjectileDamage * _damageMultiplier, ctx.Player);
            }
            ctx.Counter++;
            ctx.Timer2 = _interval;
            return ctx.Counter >= _shells;
        }
    }
}
