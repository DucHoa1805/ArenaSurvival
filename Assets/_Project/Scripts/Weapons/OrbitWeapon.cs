using ArenaSurvival.Player;
using UnityEngine;

namespace ArenaSurvival.Weapons
{
    /// <summary>
    /// Vũ khí phụ: vài lưỡi cưa quay vòng quanh Player, quái chạm vào là mất máu.
    /// Component này nằm trên Player (giống PlayerWeapon) và chỉ bật/tắt bộ lưỡi cưa;
    /// phần chuyển động và gây sát thương do SawBladeRig đảm nhận.
    ///
    /// Bộ lưỡi cưa là 1 object riêng ở gốc scene, KHÔNG làm con của Player — cùng lý do với đạn:
    /// con của 1 Rigidbody2D sẽ bị kéo/xoay theo cha, còn collider con sẽ bị tính là collider của Player.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class OrbitWeapon : MonoBehaviour, IWeapon
    {
        [SerializeField] private OrbitWeaponData _data;

        public WeaponDefinition Definition => _data;

        private SawBladeRig _rig;

        private void Awake()
        {
            // Awake chạy cả khi component đang disabled: dựng sẵn bộ lưỡi cưa (ẩn) từ đầu trận.
            _rig = SawBladeRig.Create(_data, transform, GetComponent<PlayerStats>());
            _rig.gameObject.SetActive(enabled);
        }

        private void OnEnable()
        {
            if (_rig != null) _rig.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_rig != null) _rig.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_rig != null) Destroy(_rig.gameObject);
        }
    }
}
