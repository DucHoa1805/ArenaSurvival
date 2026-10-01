using System;
using UnityEngine;

namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Component quản lý máu, dùng chung cho Player và Enemy.
    /// Không tự xử lý logic chết (destroy/deactivate) — việc đó do script khác
    /// lắng nghe sự kiện OnDied và tự quyết định (Player thì Game Over, Enemy thì trả về pool).
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 10f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => _maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        /// <summary>Bỏ qua mọi sát thương. Dùng cho StressTest: giữ Player sống để cảnh đông quái đứng yên mà đo.</summary>
        public bool Invulnerable { get; set; }

        // Truyền kèm giá trị sát thương/máu hiện tại để UI (thanh máu) có thể dùng trực tiếp,
        // không cần tự query lại Health mỗi lần.
        public event Action<float, float> OnDamaged; // (damage, currentHealth)
        public event Action OnDied;

        // Phát mỗi khi máu hoặc máu tối đa đổi vì BẤT KỲ lý do gì (trúng đòn, hồi máu, nâng cấp),
        // để thanh máu chỉ cần nghe 1 event duy nhất.
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)

        private void OnEnable()
        {
            // Reset máu mỗi khi object được kích hoạt lại — quan trọng cho object lấy ra từ pool,
            // vì object cũ có thể đã chết ở lần dùng trước.
            CurrentHealth = _maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || Invulnerable || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnDamaged?.Invoke(amount, CurrentHealth);
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);

            if (IsDead)
            {
                OnDied?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }

        /// <summary>Dùng khi nâng cấp tăng máu tối đa (Phase 4).</summary>
        public void SetMaxHealth(float newMax, bool healToFull = false)
        {
            _maxHealth = newMax;
            CurrentHealth = healToFull ? _maxHealth : Mathf.Min(CurrentHealth, _maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }
    }
}