using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// 1 đòn của boss. Asset dùng chung nên không giữ trạng thái: dùng Timer2 / Counter / Vector của EnemyContext
    /// (BossBehaviour không đụng tới 3 ô này trong lúc đòn đang chạy).
    /// </summary>
    public abstract class BossAttack : ScriptableObject
    {
        public virtual void Begin(EnemyContext ctx)
        {
            ctx.Timer2 = 0f;
            ctx.Counter = 0;
            ctx.Vector = Vector2.zero;
        }

        /// <summary>Trả về true khi đòn đã xong.</summary>
        public abstract bool Tick(EnemyContext ctx, float dt);
    }
}
