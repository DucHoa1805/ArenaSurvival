using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Cầu nối giữa Health (component dùng chung, không biết gì về game state)
    /// và GameEvents (hệ thống toàn cục). Tách riêng thành 1 script nhỏ thay vì
    /// nhét thẳng vào Health, để Health giữ được tính "dùng chung" cho cả Enemy.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;
        }

        private void HandleDamaged(float damage, float currentHealth)
        {
            GameEvents.PlayerDamaged(damage);
        }

        private void HandleDied()
        {
            GameEvents.PlayerDied();
        }
    }
}