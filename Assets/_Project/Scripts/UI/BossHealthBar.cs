using System;
using System.Collections.Generic;
using ArenaSurvival.Enemies;
using ArenaSurvival.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Thanh máu boss ở đáy màn hình: tên, lượng máu, vạch đánh dấu ngưỡng đổi pha (để người chơi biết sắp có đòn mới).
    /// Chỉ hiện khi boss còn sống; nghe Health của đúng con boss đó và gỡ ra khi boss chết (boss là object trong pool).
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private StageSession _session;
        [SerializeField] private GameObject _content;
        [SerializeField] private TMP_Text _nameText;
        [Tooltip("Phần fill (kéo neo phải theo tỉ lệ máu).")]
        [SerializeField] private RectTransform _fill;
        [Tooltip("Vạch ngưỡng pha tạo làm con của object này.")]
        [SerializeField] private RectTransform _markerRoot;
        [SerializeField] private Color _markerColor = new Color(1f, 1f, 1f, 0.85f);

        private Enemy _boss;
        private readonly List<GameObject> _markers = new List<GameObject>();
        private Action<float, float> _onHealthChanged;

        private void Awake()
        {
            _content.SetActive(false);
            _onHealthChanged = HandleHealthChanged;
        }

        private void OnEnable()
        {
            _session.OnBossSpawned += HandleBossSpawned;
            _session.OnBossDefeated += Hide;
        }

        private void OnDisable()
        {
            _session.OnBossSpawned -= HandleBossSpawned;
            _session.OnBossDefeated -= Hide;
            Detach();
        }

        private void HandleBossSpawned(Enemy boss)
        {
            Detach();
            _boss = boss;
            _boss.Health.OnHealthChanged += _onHealthChanged;
            _nameText.text = boss.Data.displayName;
            BuildMarkers(boss.Data.behaviour as BossBehaviour);
            HandleHealthChanged(boss.Health.CurrentHealth, boss.Health.MaxHealth);
            _content.SetActive(true);
        }

        private void BuildMarkers(BossBehaviour behaviour)
        {
            foreach (GameObject marker in _markers) Destroy(marker);
            _markers.Clear();
            if (behaviour == null) return;

            // Bỏ pha đầu (ngưỡng 1 = đầy máu); mỗi ngưỡng còn lại là 1 vạch dọc. Chỉ tạo 1 lần khi boss xuất hiện.
            float[] thresholds = behaviour.Thresholds;
            for (int i = 1; i < thresholds.Length; i++)
            {
                var go = new GameObject($"Phase{i + 1}", typeof(RectTransform), typeof(Image));
                go.layer = gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(_markerRoot, false);
                rt.anchorMin = new Vector2(thresholds[i], 0f);
                rt.anchorMax = new Vector2(thresholds[i], 1f);
                rt.sizeDelta = new Vector2(4f, 6f);
                var img = go.GetComponent<Image>();
                img.color = _markerColor;
                img.raycastTarget = false;
                _markers.Add(go);
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            _fill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
        }

        private void Hide()
        {
            Detach();
            _content.SetActive(false);
        }

        private void Detach()
        {
            if (_boss != null) _boss.Health.OnHealthChanged -= _onHealthChanged;
            _boss = null;
        }
    }
}
