using ArenaSurvival.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>Lóe màn hình theo màu chiêu + tên chiêu phóng to rồi mờ dần (10E C). Dùng unscaledTime để vẫn chạy nếu game vừa pause.</summary>
    public class UltimateFlash : MonoBehaviour
    {
        [SerializeField] private Image _flash;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private float _flashTime = 0.35f;
        [SerializeField] private float _titleTime = 1.1f;

        private float _t = -1f;
        private Color _color;

        private void Awake()
        {
            _flash.enabled = false;
            _title.enabled = false;
        }

        private void OnEnable() => GameEvents.OnUltimateUsed += Play;
        private void OnDisable() => GameEvents.OnUltimateUsed -= Play;

        private void Play(Color color, string name)
        {
            _color = color;
            _title.text = name.ToUpperInvariant() + "!";
            _title.color = color;
            _t = 0f;
            _flash.enabled = true;
            _title.enabled = true;
        }

        private void Update()
        {
            if (_t < 0f) return;
            _t += Time.unscaledDeltaTime;

            float f = 1f - Mathf.Clamp01(_t / _flashTime);
            _flash.color = new Color(_color.r, _color.g, _color.b, 0.45f * f * f);
            if (_t >= _flashTime) _flash.enabled = false;

            float k = Mathf.Clamp01(_t / _titleTime);
            // Bật nhanh từ to về vừa, giữ, rồi mờ ở 40% cuối.
            float scale = Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(_t / 0.18f));
            _title.transform.localScale = new Vector3(scale, scale, 1f);
            Color c = _title.color;
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            _title.color = c;
            if (_t >= _titleTime)
            {
                _title.enabled = false;
                _t = -1f;
            }
        }
    }
}
