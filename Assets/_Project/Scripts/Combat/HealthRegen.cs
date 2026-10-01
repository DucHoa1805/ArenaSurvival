using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Hồi máu theo thời gian (Talent "Hồi máu"). Hồi theo nhịp 0.5 s chứ không mỗi frame: mỗi lần hồi là 1 lần phát
    /// OnHealthChanged (HUD vẽ lại thanh máu), không cần làm việc đó 60 lần/giây cho vài máu.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthRegen : MonoBehaviour
    {
        private const float TickInterval = 0.5f;

        public float RegenPerSecond { get; set; }

        private Health _health;
        private float _timer;

        private void Awake() => _health = GetComponent<Health>();

        private void Update()
        {
            if (RegenPerSecond <= 0f || _health.IsDead || _health.CurrentHealth >= _health.MaxHealth) return;

            // deltaTime: pause / lên cấp / chờ hồi sinh thì không hồi.
            _timer += Time.deltaTime;
            if (_timer < TickInterval) return;

            _health.Heal(RegenPerSecond * _timer);
            _timer = 0f;
        }
    }
}
