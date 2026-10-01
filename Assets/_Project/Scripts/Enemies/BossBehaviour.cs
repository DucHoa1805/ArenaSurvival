using System;
using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Boss = state machine theo pha. Mỗi pha có ngưỡng máu và danh sách đòn (BossAttack) dùng xoay vòng; giữa các đòn boss
    /// đi chậm về phía Player. Boss mới = ghép các đòn có sẵn thành asset mới; kiểu đòn mới = 1 class BossAttack mới.
    /// Ô nhớ: Phase = pha hiện tại, Index = số đòn đã dùng trong pha, State = 0 nghỉ / 1 đang ra đòn, Timer = thời gian nghỉ còn lại.
    /// Timer2, Counter, Vector để dành cho đòn đang chạy.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Enemy Behaviour/Boss", fileName = "BossBehaviour_")]
    public class BossBehaviour : EnemyBehaviour
    {
        [Tooltip("Theo thứ tự; pha đầu nên có ngưỡng 1 (đầy máu).")]
        [SerializeField] private BossPhase[] _phases;
        [Tooltip("Tốc độ đi giữa các đòn so với moveSpeed của EnemyData.")]
        [SerializeField] private float _chaseSpeedMultiplier = 0.6f;

        private float[] _thresholds;

        public int PhaseCount => _phases != null ? _phases.Length : 0;

        /// <summary>Ngưỡng máu của từng pha (để thanh máu boss vẽ vạch). Tạo 1 lần rồi dùng lại.</summary>
        public float[] Thresholds
        {
            get
            {
                if (_thresholds == null || _thresholds.Length != PhaseCount)
                {
                    _thresholds = new float[PhaseCount];
                    for (int i = 0; i < PhaseCount; i++) _thresholds[i] = _phases[i].healthThreshold;
                }
                return _thresholds;
            }
        }

        // Asset có thể bị sửa trong Inspector lúc đang chạy: tạo lại mảng ngưỡng ở lần đọc sau.
        private void OnValidate() => _thresholds = null;

        public override void OnSpawn(EnemyContext ctx)
        {
            base.OnSpawn(ctx);
            ctx.Timer = PhaseCount > 0 ? _phases[0].pauseBetweenAttacks : 1f;
        }

        public override void Tick(EnemyContext ctx, float dt)
        {
            if (PhaseCount == 0) return;

            int phase = EnemyRules.PhaseForHealth(Thresholds, ctx.HealthFraction, ctx.Phase);
            if (phase != ctx.Phase) EnterPhase(ctx, phase);

            BossPhase current = _phases[ctx.Phase];
            BossAttack[] attacks = current.attacks;
            bool hasAttacks = attacks != null && attacks.Length > 0;

            if (ctx.State == 1 && hasAttacks)
            {
                if (attacks[ctx.Index % attacks.Length].Tick(ctx, dt))
                {
                    ctx.State = 0;
                    ctx.Index++;
                    ctx.Timer = current.pauseBetweenAttacks;
                }
                return;
            }

            Vector2 direction = ctx.DirectionToPlayer;
            ctx.Move(direction, ctx.MoveSpeed * _chaseSpeedMultiplier, dt);
            ctx.Face(direction, dt);

            ctx.Timer -= dt;
            if (ctx.Timer > 0f || !hasAttacks) return;

            ctx.State = 1;
            attacks[ctx.Index % attacks.Length].Begin(ctx);
        }

        private void EnterPhase(EnemyContext ctx, int phase)
        {
            // Đổi pha bỏ ngang đòn đang ra: pha mới thường mạnh hơn, cho người chơi thấy ngay boss "nổi giận".
            ctx.Phase = phase;
            ctx.State = 0;
            ctx.Index = 0;
            ctx.Timer = _phases[phase].pauseBetweenAttacks;
            ctx.ResetVisual();
            ctx.Enemy.RaiseBossPhaseChanged(phase);
        }
    }

    [Serializable]
    public class BossPhase
    {
        [Tooltip("Vào pha này khi máu ≤ ngưỡng (0..1).")]
        [Range(0f, 1f)] public float healthThreshold = 1f;
        public BossAttack[] attacks;
        [Min(0f)] public float pauseBetweenAttacks = 1.2f;
    }
}
