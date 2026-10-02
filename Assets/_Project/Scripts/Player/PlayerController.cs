using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvival.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Rotation")]
        [Tooltip("Góc lệch nếu sprite gốc không mặc định hướng lên trên (0,1). " +
                 "Ví dụ sprite mặc định hướng lên thì để 0; nếu hướng sang phải thì để -90.")]
        [SerializeField] private float _spriteAngleOffset = 0f;
        [SerializeField] private float _rotationSpeed = 720f; // độ/giây, xoay mượt thay vì snap tức thì

        [Header("Input")]
        [SerializeField] private InputActionReference _moveAction;

        private Rigidbody2D _rb;
        private PlayerStats _stats;
        private Vector2 _moveInput;

        /// <summary>Hướng đi do bot tự chơi đặt (chỉ build dev); null = đọc bàn phím/cần điều khiển như bình thường.</summary>
        public Vector2? MoveOverride { get; set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stats = GetComponent<PlayerStats>();
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
        }

        private void Update()
        {
            // Đọc input trong Update: input cần được lấy mỗi frame để không bị trễ.
            _moveInput = MoveOverride ?? _moveAction.action.ReadValue<Vector2>();

            // Chỉ xoay khi thực sự có input — tránh nhân vật "giật" về góc 0 khi đứng yên.
            if (_moveInput.sqrMagnitude > 0.0001f)
            {
                float targetAngle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg + _spriteAngleOffset;
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            // Di chuyển vật lý phải nằm trong FixedUpdate để đồng bộ với physics step.
            float speed = _moveSpeed * _stats.MoveSpeedMultiplier;
            Vector2 targetPosition = _rb.position + _moveInput.normalized * (speed * Time.fixedDeltaTime);
            _rb.MovePosition(targetPosition);
        }
    }
}