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

        public int Level => _level;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_visual == null)
            {
                Transform t = transform.Find("ShieldVisual");
                if (t != null) _visual = t.gameObject;
            }
            if (_visual != null) _visual.SetActive(false);
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
                bool on = _health.ShieldLayers > 0;
                if (_visual.activeSelf != on) _visual.SetActive(on);
            }
        }
    }
}
