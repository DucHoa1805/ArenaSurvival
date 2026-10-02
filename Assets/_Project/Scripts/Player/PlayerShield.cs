using ArenaSurvival.Combat;
using ArenaSurvival.RunUpgrades;
using UnityEngine;

namespace ArenaSurvival.Player
{
    /// <summary>
    /// Kỹ năng Lá chắn: giữ Health.ShieldLayers đầy tới mức của cấp; mất lớp thì đếm hồi rồi trả lại từng lớp.
    /// Tắt sẵn trên prefab Player, UpgradeManager bật khi chọn kỹ năng. Vòng sáng (con "ShieldVisual") hiện khi còn lớp.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerShield : MonoBehaviour
    {
        [SerializeField] private GameObject _visual;

        private Health _health;
        private int _level;
        private float _timer;
        private float _flash;
        private SpriteRenderer _visualSprite;
        private Vector3 _visualScale = Vector3.one;
        private System.Action _onBlocked;

        public int Level => _level;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_visual == null)
            {
                Transform t = transform.Find("ShieldVisual");
                if (t != null) _visual = t.gameObject;
            }
            if (_visual != null)
            {
                _visual.SetActive(false);
                _visualSprite = _visual.GetComponentInChildren<SpriteRenderer>(true);
                _visualScale = _visual.transform.localScale;
            }
            // Đăng ký 1 lần ở Awake (Player không nằm trong pool, nhưng giữ cùng quy ước với các component khác).
            _onBlocked = () => _flash = 1f;
            _health.OnShieldBlocked += _onBlocked;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnShieldBlocked -= _onBlocked;
        }

        private void OnDisable()
        {
            if (_health != null) _health.ShieldLayers = 0;
            if (_visual != null) _visual.SetActive(false);
        }

        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            enabled = true;
            // Nhận/nâng kỹ năng thì lá chắn đầy ngay — người chơi thấy hiệu quả lập tức.
            _health.ShieldLayers = SkillRules.ShieldLayers(_level);
            _timer = 0f;
        }

        private void Update()
        {
            int max = SkillRules.ShieldLayers(_level);
            if (_health.ShieldLayers < max)
            {
                // deltaTime: lên cấp/tạm dừng thì không hồi.
                _timer += Time.deltaTime;
                if (_timer >= SkillRules.ShieldCooldown(_level))
                {
                    _timer = 0f;
                    _health.ShieldLayers++;
                }
            }
            else
            {
                _timer = 0f;
            }

            if (_visual != null)
            {
                bool on = _health.ShieldLayers > 0 || _flash > 0f;
                if (_visual.activeSelf != on) _visual.SetActive(on);
                if (on)
                {
                    _flash = Mathf.Max(0f, _flash - Time.deltaTime * 3f);
                    _visual.transform.Rotate(0f, 0f, -60f * Time.deltaTime);
                    // Lấp lánh nhẹ khi còn lớp; chặn đòn thì phồng to + sáng trắng rồi tắt dần.
                    float shimmer = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
                    _visual.transform.localScale = _visualScale * (1f + 0.35f * _flash);
                    if (_visualSprite != null)
                    {
                        Color c = Color.Lerp(new Color(0.4f, 0.85f, 1f, 0.9f * shimmer), Color.white, _flash);
                        if (_health.ShieldLayers <= 0) c.a = _flash;
                        _visualSprite.color = c;
                    }
                }
            }
        }
    }
}
