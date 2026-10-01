using System;
using ArenaSurvival.Meta;
using ArenaSurvival.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>1 dòng trong bảng Vũ khí: icon, tên, sao, thanh mảnh tới sao kế, trạng thái. Chỉ hiển thị + báo bấm.</summary>
    public class WeaponRowView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [Tooltip("5 icon sao (font không có ký tự ★ nên dùng sprite).")]
        [SerializeField] private Image[] _stars = new Image[WeaponRules.MaxStars];
        [SerializeField] private Image _barFill;
        [SerializeField] private TMP_Text _barText;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _button;

        private static readonly Color Normal = new Color(0.086f, 0.106f, 0.145f, 0.85f);
        private static readonly Color Selected = new Color(0.25f, 0.21f, 0.11f, 0.95f);
        private static readonly Color Locked = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color Equipped = new Color(0.25f, 0.64f, 0.36f);
        private static readonly Color Grey = new Color(0.66f, 0.71f, 0.77f);
        private static readonly Color Ready = new Color(0.95f, 0.71f, 0.27f);

        public WeaponData Data { get; private set; }
        public event Action<WeaponRowView> OnClicked;

        private void Awake() => _button.onClick.AddListener(HandleClick);
        private void OnDestroy() => _button.onClick.RemoveListener(HandleClick);

        public void Bind(WeaponData data)
        {
            Data = data;
            _icon.sprite = data.icon;
            _name.text = data.displayName;
        }

        public void Refresh(WeaponService weapons, bool selected)
        {
            string id = Data.id;
            bool owned = weapons.IsOwned(id);
            int stars = weapons.GetStars(id);

            _background.color = selected ? Selected : Normal;
            _icon.color = owned ? Color.white : Locked;
            WeaponPanel.ShowStars(_stars, stars);

            long shards = weapons.GetShards(id);
            if (stars >= WeaponRules.MaxStars)
            {
                _barFill.fillAmount = 1f;
                _barText.text = "TỐI ĐA";
            }
            else
            {
                int need = WeaponRules.ShardsFor(stars + 1);
                _barFill.fillAmount = Mathf.Clamp01(shards / (float)need);
                _barText.SetText("{0}/{1}", shards, need);
            }

            if (!owned)
            {
                // Đủ mảnh mà chưa mở: nhắc rõ, người chơi hay không để ý thanh mảnh đã đầy.
                bool ready = shards >= WeaponRules.ShardsFor(1);
                _status.text = ready ? "Có thể mở khóa!" : "Chưa mở khóa";
                _status.color = ready ? Ready : Grey;
            }
            else if (weapons.EquippedId == id)
            {
                _status.text = "Đang dùng";
                _status.color = Equipped;
            }
            else
            {
                _status.SetText("Cấp {0}", weapons.GetLevel(id));
                _status.color = Grey;
            }
        }

        private void HandleClick() => OnClicked?.Invoke(this);
    }
}
