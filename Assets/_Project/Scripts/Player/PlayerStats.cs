using UnityEngine;
using ArenaSurvival.Weapons;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Các hệ số cộng dồn từ nâng cấp trong 1 ván chơi. Chỉ số gốc vẫn nằm ở WeaponData /
    /// PlayerController; nơi dùng sẽ nhân chỉ số gốc với hệ số ở đây.
    ///
    /// Vì sao không sửa thẳng WeaponData: ScriptableObject là asset dùng chung — sửa lúc chơi thì
    /// thay đổi bị giữ lại trong Editor sau khi thoát Play mode, và mọi thứ dùng asset đó đều bị ảnh hưởng.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        public float DamageMultiplier { get; private set; } = 1f;
        public float FireRateMultiplier { get; private set; } = 1f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;

        /// <summary>
        /// Hệ số gốc từ chỉ số ngoài trận (Talent, sau này trang bị) — đặt 1 lần lúc vào màn, TRƯỚC mọi nâng cấp trong màn;
        /// nâng cấp trong màn vẫn cộng dồn lên trên như cũ.
        /// </summary>
        public void SetBaseMultipliers(float damage, float moveSpeed)
        {
            DamageMultiplier = damage;
            MoveSpeedMultiplier = moveSpeed;
        }

        // bonus là phần cộng thêm: 0.2 nghĩa là +20% so với chỉ số gốc.
        public void AddDamageBonus(float bonus) => DamageMultiplier += bonus;
        public void AddFireRateBonus(float bonus) => FireRateMultiplier += bonus;
        public void AddMoveSpeedBonus(float bonus) => MoveSpeedMultiplier += bonus;
    }
}
