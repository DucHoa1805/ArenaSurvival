using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Quái hỗ trợ: giữ khoảng cách như Xạ thủ nhưng không bắn; định kỳ hồi máu cho quái xung quanh (không tính chính nó) kèm vòng sáng
    /// xanh để người chơi nhận ra và ưu tiên hạ trước. Không hồi cho Healer khác. Ô nhớ EnemyContext: Timer (đếm tới lần hồi kế).
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Healer", fileName = "Behaviour_Healer")]
    public class HealerBehaviour : EnemyBehaviour
    {
        // 5 chứ không 6: đứng đúng mép tầm Súng lục (6) thì hầu như không bị bắn tới (bot chơi thử 10C).
        [SerializeField] private float _preferredRange = 5f;
        [SerializeField] private float _tolerance = 1f;
        [SerializeField] private float _interval = 4f;
        [SerializeField] private float _radius = 3.5f;
        [Range(0f, 1f)] [SerializeField] private float _healFraction = 0.2f;

        // Buffer + bộ lọc dùng chung cho mọi Healer: hành vi chạy trên main thread nên không bao giờ quét chồng nhau.
        private static readonly Collider2D[] Buffer = new Collider2D[64];
        private static ContactFilter2D _filter;
        private static bool _filterReady;

        public override void OnSpawn(EnemyContext ctx)
        {
            base.OnSpawn(ctx);
            ctx.Timer = _interval * Random.Range(0.4f, 0.8f);
        }

        public override void Tick(EnemyContext ctx, float dt)
        {
            Vector2 direction = ctx.DirectionToPlayer;
            if (direction != Vector2.zero)
            {
                int intent = EnemyRules.RangeIntent(ctx.DistanceToPlayer, _preferredRange, _tolerance);
                if (intent != 0) ctx.Move(direction * intent, ctx.MoveSpeed, dt);
                ctx.Face(direction, dt);
            }

            ctx.Timer -= dt;
            if (ctx.Timer > 0f) return;
            ctx.Timer = _interval;
            HealAround(ctx);
        }

        private void HealAround(EnemyContext ctx)
        {
            if (!_filterReady)
            {
                _filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Enemy"), useTriggers = true };
                _filterReady = true;
            }

            int count = Physics2D.OverlapCircle(ctx.Position, _radius, _filter, Buffer);
            for (int i = 0; i < count; i++)
            {
                if (!Buffer[i].TryGetComponent(out Enemy other) || other == ctx.Enemy) continue;
                // Không hồi cho Healer khác: 2 Healer tinh anh đứng cạnh nhau hồi qua lại 20%/4 s gần như không hạ được
                // (bot chơi thử kẹt ở 1-9, nhiều lượt hết giờ).
                if (other.Data != null && other.Data.behaviour is HealerBehaviour) continue;
                var health = other.Health;
                health.Heal(EnemyRules.HealAmount(health.MaxHealth, health.CurrentHealth, _healFraction));
            }
            Core.GameEvents.EnemyHealPulse(ctx.Position, _radius);
        }
    }
}
