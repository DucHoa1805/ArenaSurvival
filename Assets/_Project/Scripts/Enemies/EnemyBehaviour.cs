using UnityEngine;

namespace ArenaSurvival.Enemies
{
    /// <summary>
    /// Cách 1 loại quái di chuyển và tấn công, dạng asset gán vào EnemyData. Thêm kiểu quái mới = viết 1 class con
    /// (hoặc tạo asset mới với số khác) mà không sửa Enemy. Asset dùng chung giữa nhiều con nên không giữ trạng thái:
    /// mọi trạng thái nằm trong EnemyContext.
    /// </summary>
    public abstract class EnemyBehaviour : ScriptableObject
    {
        /// <summary>Gọi mỗi lần quái được lấy ra từ pool, sau khi đã gán EnemyData và hệ số.</summary>
        public virtual void OnSpawn(EnemyContext ctx) => ctx.ResetState();

        /// <summary>Gọi trong FixedUpdate.</summary>
        public abstract void Tick(EnemyContext ctx, float dt);

        /// <summary>
        /// Gọi khi quái chết vì trúng đòn (trước khi trả về pool, vị trí/hệ số còn nguyên). KHÔNG gọi khi nổ theo boss (Despawn):
        /// quái phân thân nổ theo boss mà còn đẻ con thì màn không bao giờ dọn sạch.
        /// </summary>
        public virtual void OnDeath(EnemyContext ctx) { }

        /// <summary>
        /// Sát thương thực nhận khi trúng đòn có hướng (đạn). Mặc định nhận đủ; Lính Khiên giảm khi trúng phía trước.
        /// hitDirection = zero với đòn không hướng.
        /// </summary>
        public virtual float ModifyIncomingDamage(EnemyContext ctx, float damage, Vector2 hitDirection) => damage;
    }
}
