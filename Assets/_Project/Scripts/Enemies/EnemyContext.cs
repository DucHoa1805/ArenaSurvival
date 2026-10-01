using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Trạng thái riêng của 1 con quái, dùng chung cho mọi hành vi. Asset hành vi (ScriptableObject) được nhiều con dùng chung
    /// nên KHÔNG được giữ trạng thái; mọi bộ đếm/giai đoạn nằm ở đây. Mỗi Enemy tạo object này đúng 1 lần trong Awake,
    /// lấy từ pool ra chỉ reset lại, nên không sinh rác khi quái đông.
    /// </summary>
    public class EnemyContext
    {
        public Enemy Enemy;
        public Rigidbody2D Body;
        public Transform Visual;
        /// <summary>Tia báo trước (LineRenderer con, tắt sẵn) — Sniper ngắm. Null nếu prefab không có.</summary>
        public LineRenderer Telegraph;          // object con "Visual": rung khi boss gồng đòn mà không đụng vật lý
        public Transform Player;
        public EnemyData Data;
        public EnemyModifiers Modifiers;
        public EnemyProjectiles Projectiles;
        public IEnemyWorld World;
        public float RotationSpeed;
        public float SpriteAngleOffset;

        // Ô nhớ chung cho hành vi/đòn đánh (ý nghĩa do từng asset tự quy ước, ghi ở comment của asset đó).
        public float Timer;
        public float Timer2;
        public int State;
        public int Index;
        public int Counter;
        public int Phase;
        public Vector2 Vector;

        public Vector2 Position => Body.position;
        public Vector2 ToPlayer => Player != null ? (Vector2)Player.position - Body.position : Vector2.zero;
        public float DistanceToPlayer => ToPlayer.magnitude;

        public Vector2 DirectionToPlayer
        {
            get
            {
                Vector2 v = ToPlayer;
                return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.zero;
            }
        }

        public float MoveSpeed => Data != null ? Data.moveSpeed : 0f;
        public float ProjectileDamage => Data != null ? Data.projectileDamage * Modifiers.damage : 0f;
        public float ContactDamage => Data != null ? Data.contactDamage * Modifiers.damage : 0f;
        public float HealthFraction => Enemy.HealthFraction;

        /// <summary>Di chuyển qua Rigidbody (đồng bộ bước vật lý), giữ trong sân: quái không va chạm tường nên tự kẹp vị trí.</summary>
        public void Move(Vector2 direction, float speed, float dt)
        {
            Vector2 target = Body.position + direction * (speed * dt);
            if (World != null) target = World.ClampToArena(target);
            Body.MovePosition(target);
        }

        /// <summary>Xoay mặt dần về hướng direction (qua Rigidbody, như MovePosition).</summary>
        public void Face(Vector2 direction, float dt)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            float target = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + SpriteAngleOffset;
            Body.MoveRotation(Mathf.MoveTowardsAngle(Body.rotation, target, RotationSpeed * dt));
        }

        public void ResetState()
        {
            Timer = Timer2 = 0f;
            State = Index = Counter = Phase = 0;
            Vector = Vector2.zero;
            ResetVisual();
        }

        public void ResetVisual()
        {
            if (Visual != null) Visual.localPosition = Vector3.zero;
            // Chết/trả về pool giữa lúc ngắm: không để tia đỏ treo lại trên sân.
            if (Telegraph != null) Telegraph.enabled = false;
        }
    }
}
