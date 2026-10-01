using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>
    /// Hiệu ứng nhận thưởng: vài icon bay từ giữa màn hình theo đường cong vào thanh trên cùng. Icon tạo sẵn 1 lần (pool nhỏ),
    /// chạy theo thời gian thực vì Hub có thể đang ở timeScale = 0.
    /// </summary>
    public class RewardFly : MonoBehaviour
    {
        [SerializeField] private Image _iconTemplate;
        [SerializeField] private int _poolSize = 14;
        [SerializeField] private float _flyDuration = 0.65f;
        [SerializeField] private float _spawnSpread = 90f;
        [SerializeField] private float _stagger = 0.05f;

        private readonly List<Image> _pool = new List<Image>();

        /// <summary>Tổng thời gian 1 đợt bay (để thanh trên giữ số cũ tới lúc icon chạm vào).</summary>
        public float DurationFor(int count) => _flyDuration + _stagger * Mathf.Max(0, count - 1);

        private void Awake()
        {
            _iconTemplate.gameObject.SetActive(false);
            for (int i = 0; i < _poolSize; i++)
            {
                Image icon = Instantiate(_iconTemplate, transform);
                icon.gameObject.SetActive(false);
                _pool.Add(icon);
            }
        }

        public void Play(Sprite sprite, int count, RectTransform target)
        {
            if (target == null || sprite == null) return;
            StartCoroutine(Fly(sprite, Mathf.Clamp(count, 1, _poolSize), target));
        }

        private IEnumerator Fly(Sprite sprite, int count, RectTransform target)
        {
            var rect = (RectTransform)transform;
            int started = 0;
            for (int i = 0; i < _pool.Count && started < count; i++)
            {
                if (_pool[i].gameObject.activeSelf) continue;
                StartCoroutine(FlyOne(_pool[i], sprite, rect, target));
                started++;
                yield return new WaitForSecondsRealtime(_stagger);
            }
        }

        private IEnumerator FlyOne(Image icon, Sprite sprite, RectTransform layer, RectTransform target)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(true);
            var rt = (RectTransform)icon.transform;

            Vector2 start = Random.insideUnitCircle * _spawnSpread;
            Vector2 end = layer.InverseTransformPoint(target.position);
            // Điểm uốn lệch sang ngang để các icon bay thành chùm cong, không đi thẳng một hàng.
            Vector2 control = Vector2.Lerp(start, end, 0.3f) + new Vector2(Random.Range(-220f, 220f), -60f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / _flyDuration;
                float e = Mathf.Clamp01(t);
                float ease = e * e;
                Vector2 a = Vector2.Lerp(start, control, ease);
                Vector2 b = Vector2.Lerp(control, end, ease);
                rt.anchoredPosition = Vector2.Lerp(a, b, ease);
                rt.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.7f, ease);
                yield return null;
            }
            icon.gameObject.SetActive(false);
        }
    }
}
