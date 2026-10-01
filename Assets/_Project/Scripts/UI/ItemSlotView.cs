using System;
using ArenaSurvival.Items;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// 1 ô vuông hiện 1 món (trang bị hoặc nguyên liệu): icon, viền màu độ hiếm, chữ góc ("Lv 5", "x12", "Mất").
    /// Dùng chung cho lưới kho đồ, 2 ô đang mặc và hàng "Vật phẩm nhận được" ở màn kết quả. Chỉ hiển thị, không tự đổi dữ liệu.
    /// </summary>
    public class ItemSlotView : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [Tooltip("Dấu \"đang mặc\" (không bắt buộc).")]
        [SerializeField] private GameObject _equippedMark;
        [Tooltip("Chữ hiện khi ô trống, ví dụ \"Giáp\" (không bắt buộc).")]
        [SerializeField] private TMP_Text _placeholder;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _group;

        private static readonly Color EmptyFrame = new Color(0.42f, 0.46f, 0.55f, 0.6f);
        private static readonly Color MaterialFrame = new Color(0.85f, 0.80f, 0.68f);

        /// <summary>uid món đang hiện (null nếu là nguyên liệu / ô trống).</summary>
        public string Uid { get; private set; }

        public event Action<ItemSlotView> OnClicked;

        private void Awake()
        {
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        public void ShowItem(OwnedItem item, ItemData data, bool equipped)
        {
            Uid = item.uid;
            SetContent(data != null ? data.icon : null, RarityColors.Get(item.rarity), $"Lv {item.level}");
            SetEquipped(equipped);
            SetDimmed(false);
        }

        /// <summary>Trang bị chưa có trong kho (vd. đồ bị mất khi thua) — chỉ có id + độ hiếm.</summary>
        public void ShowItemPreview(ItemData data, Rarity rarity, string label)
        {
            Uid = null;
            SetContent(data != null ? data.icon : null, RarityColors.Get(rarity), label);
            SetEquipped(false);
        }

        /// <summary>Ô vũ khí chính ở bảng Trang bị: viền màu theo số sao, chữ "Lv n".</summary>
        public void ShowWeapon(Sprite icon, Color frame, int level)
        {
            Uid = null;
            SetContent(icon, frame, $"Lv {level}");
            SetEquipped(false);
            SetDimmed(false);
        }

        public void ShowMaterial(MaterialData data, long amount)
        {
            Uid = null;
            SetContent(data != null ? data.icon : null, MaterialFrame, $"x{amount}");
            SetEquipped(false);
            SetDimmed(false);
        }

        public void ShowEmpty()
        {
            Uid = null;
            _icon.enabled = false;
            _frame.color = EmptyFrame;
            if (_label != null) _label.text = "";
            if (_placeholder != null) _placeholder.gameObject.SetActive(true);
            SetEquipped(false);
            SetDimmed(false);
        }

        public void SetDimmed(bool dimmed)
        {
            if (_group != null) _group.alpha = dimmed ? 0.4f : 1f;
        }

        private void SetContent(Sprite icon, Color frame, string label)
        {
            _icon.enabled = icon != null;
            _icon.sprite = icon;
            _frame.color = frame;
            if (_label != null) _label.text = label;
            if (_placeholder != null) _placeholder.gameObject.SetActive(false);
        }

        private void SetEquipped(bool equipped)
        {
            if (_equippedMark != null) _equippedMark.SetActive(equipped);
        }

        private void HandleClick() => OnClicked?.Invoke(this);
    }
}
