using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Đứng niệm một lúc rồi gọi quái con quanh mình. Có trần số quái con còn sống, để pha cuối không thành bão quái vô hạn.
    /// Ô nhớ: Timer2 = thời gian niệm còn lại.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Boss Attack/Summon", fileName = "Attack_Summon")]
    public class SummonAttack : BossAttack
    {
        [SerializeField] private EnemyData _minion;
        [Min(1)] [SerializeField] private int _count = 4;
        [Tooltip("Không gọi thêm nếu số quái con loại này còn sống đã đạt mức này.")]
        [Min(1)] [SerializeField] private int _maxAlive = 8;
        [SerializeField] private float _radius = 2f;
        [SerializeField] private float _castTime = 0.6f;

        public override void Begin(EnemyContext ctx)
        {
            base.Begin(ctx);
            ctx.Timer2 = _castTime;
        }

        public override bool Tick(EnemyContext ctx, float dt)
        {
            ctx.Timer2 -= dt;
            if (ctx.Timer2 > 0f) return false;
            if (_minion == null || ctx.World == null) return true;

            int count = Mathf.Min(_count, _maxAlive - ctx.World.CountAlive(_minion));
            // Quái con mạnh theo độ khó của màn như quái thường, nhưng không mang màu/kích thước của boss.
            var mods = EnemyModifiers.ForDifficulty(ctx.Modifiers.damage);
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                Vector2 position = ctx.Position + EnemyRules.RadialDirection(i, count, start) * _radius;
                ctx.World.Summon(_minion, mods, ctx.World.ClampToArena(position));
            }
            return true;
        }
    }
}
