using System.Collections;
using ArenaSurvival.Core;
using ArenaSurvival.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Mana + chiêu của vũ khí chính (10E C). Mana hồi theo thời gian (đứng yên khi pause/lên cấp vì dùng deltaTime) và theo số quái hạ;
    /// đầy thì dùng chiêu của WeaponData.ultimate bằng nút trên HUD, phím Space, hoặc tự dùng (Cài đặt). HUD đọc trạng thái mỗi frame.
    /// </summary>
    public class PlayerUltimate : MonoBehaviour
    {
        public const string AutoUsePrefKey = "auto_ultimate";

        [SerializeField] private PlayerWeapon _weapon;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private LayerMask _enemyLayer;
        [Tooltip("Vật liệu tia laser (dùng chung Vfx_Particle). Trống = không có tia.")]
        [SerializeField] private Material _beamMaterial;

        /// <summary>Bot tự chơi luôn tự dùng chiêu, bất kể cài đặt.</summary>
        public static bool ForceAuto;

        private UltimateContext _ctx;
        private bool _running;
        private float _activeUntil;
        private System.Action<int, Vector3> _onKilled;

        public float Mana { get; private set; }
        public float ManaFraction => Mana / UltimateRules.MaxMana;
        public bool IsFull => UltimateRules.IsFull(Mana);
        public bool IsRunning => _running;
        public WeaponUltimate Ultimate => _weapon != null && _weapon.Data != null ? _weapon.Data.ultimate : null;
        /// <summary>Thời gian hiệu lực còn lại của chiêu kéo dài (nút chiêu hiện đếm ngược).</summary>
        public float ActiveTimeLeft => _running ? Mathf.Max(0f, _activeUntil - Time.time) : 0f;
        public static bool AutoUse => ForceAuto || PlayerPrefs.GetInt(AutoUsePrefKey, 0) == 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ForceAuto = false;

        private void Awake()
        {
            if (_weapon == null) _weapon = GetComponent<PlayerWeapon>();
            if (_stats == null) _stats = GetComponent<PlayerStats>();
            _onKilled = (xp, pos) => Mana = UltimateRules.AddKills(Mana, 1);
            _ctx = new UltimateContext { Player = transform, Weapon = _weapon, Stats = _stats, EnemyLayer = _enemyLayer, Beam = CreateBeam() };
        }

        private void OnEnable() => GameEvents.OnEnemyKilled += _onKilled;

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= _onKilled;
            // Bị tắt giữa chiêu (chết, rời màn): coroutine dừng nên phải tự gỡ cường hóa và tia.
            if (_running)
            {
                _running = false;
                if (_weapon != null) _weapon.ClearBuff();
                if (_ctx.Beam != null) _ctx.Beam.enabled = false;
            }
        }

        private void Update()
        {
            if (Ultimate == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!_running) Mana = UltimateRules.Regen(Mana, Time.deltaTime);

            bool pressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (pressed || (AutoUse && IsFull)) TryActivate();
        }

        /// <summary>DevMenu: nạp đầy mana để thử chiêu ngay.</summary>
        public void DebugFillMana() => Mana = UltimateRules.MaxMana;

        /// <summary>Nút chiêu trên HUD gọi. false nếu chưa đầy, đang dùng, hoặc không ở trạng thái chơi.</summary>
        public bool TryActivate()
        {
            WeaponUltimate ult = Ultimate;
            if (ult == null || _running || !IsFull) return false;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return false;
            Mana = 0f;
            _activeUntil = Time.time + ult.Duration;
            GameEvents.UltimateUsed(ult.color, ult.displayName);
            StartCoroutine(Run(ult));
            return true;
        }

        private IEnumerator Run(WeaponUltimate ult)
        {
            _running = true;
            yield return ult.Run(_ctx);
            _running = false;
        }

        private LineRenderer CreateBeam()
        {
            if (_beamMaterial == null) return null;
            // Tạo sẵn 1 lần, tắt khi không dùng: chiêu không Instantiate gì lúc chơi.
            var go = new GameObject("UltimateBeam");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.sharedMaterial = _beamMaterial;
            lr.numCapVertices = 4;
            lr.sortingOrder = 50;
            lr.positionCount = 2;
            lr.enabled = false;
            return lr;
        }
    }
}
