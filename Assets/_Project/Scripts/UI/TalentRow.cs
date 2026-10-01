using System;
using ArenaSurvival.Meta;
using ArenaSurvival.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>1 dòng Talent: icon, tên, cấp, hiệu quả hiện tại → cấp sau, nút giá. Chỉ hiển thị + chuyển lệnh, không tự trừ tiền.</summary>
    public class TalentRow : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _detail;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TMP_Text _costText;

        private TalentData _talent;
        private TalentService _service;

        public event Action<TalentData> OnUpgradeClicked;

        private void Awake() => _upgradeButton.onClick.AddListener(HandleClick);

        private void OnDestroy() => _upgradeButton.onClick.RemoveListener(HandleClick);

        public void Bind(TalentData talent, TalentService service)
        {
            _talent = talent;
            _service = service;
            _icon.sprite = talent.icon;
            _name.text = talent.displayName;
            Refresh();
        }

        public void Refresh()
        {
            if (_talent == null) return;

            int level = _service.GetLevel(_talent.id);
            int cost = _service.GetCost(_talent);
            string now = FormatEffect(_talent.stat, _talent.valuePerLevel * level);

            if (cost < 0)
            {
                _detail.text = $"Cấp {level}/{_talent.maxLevel} · {now} · TỐI ĐA";
                _costText.text = "MAX";
                _upgradeButton.interactable = false;
                return;
            }

            // Vế trước chỉ ghi số, đơn vị chỉ ghi 1 lần ở vế sau: dòng ngắn lại, không bị "…" với tên chỉ số dài.
            string nowValue = FormatValue(_talent.stat, _talent.valuePerLevel * level);
            string next = FormatEffect(_talent.stat, _talent.valuePerLevel * (level + 1));
            _detail.text = $"Cấp {level}/{_talent.maxLevel} · {nowValue} → {next}";
            _costText.SetText("{0}", cost);
            // Mờ nút khi không đủ vàng: người chơi thấy ngay còn thiếu, thay vì bấm rồi không có gì xảy ra.
            _upgradeButton.interactable = _service.CanUpgrade(_talent);
        }

        private void HandleClick() => OnUpgradeClicked?.Invoke(_talent);

        /// <summary>Hiển thị giá trị kèm đơn vị theo loại chỉ số (để Talent mới dùng lại được mà không viết thêm UI).</summary>
        public static string FormatEffect(StatKind kind, float value)
        {
            string number = FormatValue(kind, value);
            switch (kind)
            {
                case StatKind.MaxHealth: return number + " máu";
                case StatKind.Armor: return number + " giáp";
                case StatKind.DamagePercent: return number + " sát thương";
                case StatKind.MoveSpeedPercent: return number + " tốc chạy";
                case StatKind.HealthRegen: return number + " máu/giây";
                default: return number;
            }
        }

        /// <summary>Chỉ phần số (có "%" nếu là chỉ số phần trăm), không kèm tên chỉ số.</summary>
        public static string FormatValue(StatKind kind, float value)
        {
            switch (kind)
            {
                case StatKind.MaxHealth:
                case StatKind.Armor: return $"+{value:0}";
                case StatKind.DamagePercent:
                case StatKind.MoveSpeedPercent: return $"+{value:0}%";
                case StatKind.HealthRegen: return $"+{value:0.0}";
                default: return $"+{value}";
            }
        }
    }
}
