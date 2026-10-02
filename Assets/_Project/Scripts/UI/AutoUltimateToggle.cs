using ArenaSurvival.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>Công tắc "Tự dùng chiêu" (10E C) — cài đặt của thiết bị như âm lượng nên lưu PlayerPrefs, không vào hồ sơ.</summary>
    public class AutoUltimateToggle : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;

        private void OnEnable()
        {
            _toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PlayerUltimate.AutoUsePrefKey, 0) == 1);
            _toggle.onValueChanged.AddListener(Store);
        }

        private void OnDisable() => _toggle.onValueChanged.RemoveListener(Store);

        private static void Store(bool on)
        {
            PlayerPrefs.SetInt(PlayerUltimate.AutoUsePrefKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
