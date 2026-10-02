using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Mục tiêu muốn biết đòn đến từ hướng nào (Lính Khiên chặn đạn phía trước). Đạn gọi qua interface này thay vì
    /// tham chiếu thẳng Enemy, để thư mục Combat không phụ thuộc ngược vào Enemies.
    /// </summary>
    public interface IHitReceiver
    {
        /// <param name="direction">Hướng bay của đòn; zero = không có hướng (nổ lan, lưỡi cưa).</param>
        void TakeHit(float damage, Vector2 direction);
    }
}
