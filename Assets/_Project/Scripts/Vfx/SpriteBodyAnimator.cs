using UnityEngine;

namespace ArenaSurvival.Vfx
{
    /// <summary>
    /// Hoạt ảnh bằng code cho sprite top-down không có khung hình chạy: thở nhẹ khi đứng, lắc lư và co giãn
    /// theo bước khi di chuyển, phóng to lúc xuất hiện.
    ///
    /// Gắn trên object con "Visual" chứ không trên gốc: gốc đã có Rigidbody/collider, code xoay mặt và scale
    /// theo loại quái. Transform con nhân với transform cha nên hai bên không ghi đè nhau.
    /// Tốc độ đo từ độ dời của object cha, nên không phụ thuộc PlayerController/Enemy — dùng chung được.
    /// </summary>
    public class SpriteBodyAnimator : MonoBehaviour
    {
        [Header("Đứng yên")]
        [SerializeField] private float _breatheAmount = 0.02f;
        [SerializeField] private float _breatheFrequency = 0.8f; // lần/giây

        [Header("Di chuyển")]
        [Tooltip("Số chu kỳ lắc (trái + phải) cho mỗi đơn vị quãng đường. Tính theo quãng đường chứ không theo " +
                 "thời gian, nên quái nhanh tự lắc nhanh hơn quái chậm.")]
        [SerializeField] private float _cyclesPerUnit = 0.6f;
        [SerializeField] private float _wobbleAngle = 8f;     // độ
        [SerializeField] private float _stretchAmount = 0.06f;
        [Tooltip("Tốc độ (đơn vị/giây) từ đó trở lên thì hoạt ảnh đi bộ chạy đủ biên độ.")]
        [SerializeField] private float _fullMotionSpeed = 1f;
        [Tooltip("Thời gian chuyển giữa đứng yên và di chuyển, tránh giật khi vừa dừng/vừa chạy.")]
        [SerializeField] private float _blendTime = 0.12f;

        [Header("Xuất hiện")]
        [SerializeField] private bool _popOnEnable = true;
        [SerializeField] private float _popDuration = 0.2f;

        // Quá xa trong 1 frame nghĩa là bị dịch chuyển tức thời (lấy ra từ pool, hồi sinh), không phải đang chạy.
        private const float TeleportDistance = 2f;

        private Transform _body;
        private Vector3 _lastPosition;
        private float _smoothedSpeed;
        private float _stepPhase;
        private float _breathePhase;
        private float _popTimer;

        private void Awake()
        {
            _body = transform.parent != null ? transform.parent : transform;
        }

        private void OnEnable()
        {
            // ObjectPool đặt vị trí trước khi SetActive(true), nên ở đây đã là vị trí mới.
            _lastPosition = _body.position;
            _smoothedSpeed = 0f;

            // Lệch pha ngẫu nhiên: cả đàn quái sinh cùng lúc mà lắc cùng nhịp trông như robot.
            _stepPhase = Random.value * Mathf.PI * 2f;
            _breathePhase = Random.value * Mathf.PI * 2f;
            _popTimer = _popOnEnable ? 0f : _popDuration;

            Apply(0f);
        }

        private void OnDisable()
        {
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            // LateUpdate: đọc vị trí sau khi Player/quái đã di chuyển và xoay xong trong frame này.
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // Pause/LevelUp (timeScale = 0): đứng im đúng tư thế hiện tại

            Vector3 position = _body.position;
            float distance = Vector2.Distance(position, _lastPosition);
            _lastPosition = position;
            if (distance > TeleportDistance) distance = 0f;

            // Làm mượt tốc độ THÔ rồi mới kẹp về 0..1. Rigidbody không nội suy nên vị trí chỉ đổi ở bước vật lý
            // (50 lần/giây): ở fps cao, đa số frame có độ dời 0, thỉnh thoảng 1 frame vọt lên. Kẹp từng frame
            // trước sẽ cắt mất phần vọt và làm hoạt ảnh yếu hẳn; trung bình mũ thì giữ đúng tốc độ trung bình.
            float rawSpeed = distance / dt;
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, rawSpeed, 1f - Mathf.Exp(-dt / _blendTime));
            float moveBlend = Mathf.Clamp01(_smoothedSpeed / _fullMotionSpeed);

            _stepPhase += distance * _cyclesPerUnit * Mathf.PI * 2f;
            _breathePhase += dt * _breatheFrequency * Mathf.PI * 2f;
            _popTimer += dt;

            Apply(moveBlend);
        }

        private void Apply(float moveBlend)
        {
            float idle = 1f - moveBlend;

            // Mỗi chu kỳ lắc có 2 bước chân, nên co giãn chạy nhịp gấp đôi góc lắc.
            float wobble = Mathf.Sin(_stepPhase) * _wobbleAngle * moveBlend;
            float stretch = Mathf.Abs(Mathf.Cos(_stepPhase)) * _stretchAmount * moveBlend;
            float breathe = Mathf.Sin(_breathePhase) * _breatheAmount * idle;

            // Sprite Kenney quay mặt theo trục X cục bộ: dài ra theo hướng đi, hẹp lại hai bên.
            float pop = Pop(Mathf.Clamp01(_popTimer / _popDuration));
            transform.localScale = new Vector3(
                (1f + stretch + breathe) * pop,
                (1f - stretch * 0.6f + breathe) * pop,
                1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, wobble);
        }

        // Ease-out-back: vọt quá cỡ thật một chút rồi thu về, trông "bật" ra thay vì chỉ to dần.
        private static float Pop(float t)
        {
            if (t >= 1f) return 1f;
            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }
    }
}
