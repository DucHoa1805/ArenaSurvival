using ArenaSurvival.Combat;
using ArenaSurvival.Core;
using UnityEngine;

namespace ArenaSurvival.Vfx
{
    /// <summary>
    /// Gộp sát thương quái nhận trong một khoảng ngắn rồi báo ra GameEvents để hiện số.
    /// Tách khỏi Enemy vì đây thuần là chuyện hiển thị; nghe Health nên bao luôn cả đạn lẫn lưỡi cưa
    /// mà không phải sửa từng loại vũ khí.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageNumberSource : MonoBehaviour
    {
        [Tooltip("Lưỡi cưa gây sát thương liên tục; không gộp thì số chồng lên nhau đọc không nổi.")]
        [SerializeField] private float _mergeWindow = 0.2f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.5f, 0f);

        private Health _health;
        private Enemies.Enemy _enemy;
        private bool _pendingBlocked;
        private float _pendingDamage;
        private float _windowEnd;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _enemy = GetComponent<Enemies.Enemy>();

            // Đăng ký 1 lần ở Awake chứ không ở OnEnable/OnDisable: object này nằm trong pool, bật/tắt mỗi lần quái sinh ra
            // hay chết, và mỗi lần += / -= là tạo delegate mới (rác GC). Health ở cùng object nên sống chết cùng nhau;
            // object đang tắt thì cũng không bị trúng đòn, nên không cần gỡ lúc tắt.
            _health.OnDamaged += HandleDamaged;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDamaged -= HandleDamaged;
        }

        private void OnDisable()
        {
            // Quái chết thì bị tắt ngay (vào pool) nên Update không còn chạy: báo nốt phần đang gộp,
            // nếu không đòn kết liễu sẽ không bao giờ hiện số. Còn bị tắt vì đổi scene thì bỏ, vì phát
            // event lúc đó có thể khiến pool tạo object mới giữa lúc scene đang bị hủy.
            // Không nghe Health.OnDied để làm việc này: Enemy cũng nghe OnDied và tắt object, thứ tự
            // giữa hai bên phụ thuộc thứ tự component nên dễ vỡ.
            if (_health.IsDead) Flush();
            _pendingDamage = 0f;
            _pendingBlocked = false;
        }

        private void HandleDamaged(float damage, float currentHealth)
        {
            if (_pendingDamage <= 0f) _windowEnd = Time.time + _mergeWindow;
            _pendingDamage += damage;
            if (_enemy != null && _enemy.ApplyingBlockedHit) _pendingBlocked = true;
        }

        private void Update()
        {
            if (_pendingDamage > 0f && Time.time >= _windowEnd) Flush();
        }

        private void Flush()
        {
            if (_pendingDamage <= 0f) return;

            GameEvents.EnemyDamaged(_pendingDamage, transform.position + _offset, _pendingBlocked);
            _pendingDamage = 0f;
            _pendingBlocked = false;
        }
    }
}
