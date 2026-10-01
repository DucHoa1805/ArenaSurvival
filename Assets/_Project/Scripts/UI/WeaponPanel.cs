using System;
using System.Collections.Generic;
using System.Text;
using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using ArenaSurvival.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI
{
    /// <summary>
    /// Bảng Vũ khí (8C): danh sách 5 khẩu + chi tiết khẩu đang chọn; Mở khóa / Dùng, Nâng cấp (vàng + quặng), Lên sao (mảnh).
    /// Prefab độc lập (phủ lên bảng Trang bị) để Phase 9 đặt vào Hub. Mọi thao tác qua WeaponService; bảng tự làm mới theo sự kiện.
    /// </summary>
    public class WeaponPanel : MonoBehaviour
    {
        [Header("Danh sách")]
        [SerializeField] private WeaponRowView _rowPrefab;
        [SerializeField] private Transform _rowContainer;

        [Header("Chi tiết")]
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private Image[] _detailStars = new Image[WeaponRules.MaxStars];
        [SerializeField] private TMP_Text _detailLevel;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _detailStats;
        [SerializeField] private TMP_Text _bonus3;
        [SerializeField] private TMP_Text _bonus5;

        [Header("Nút")]
        [SerializeField] private Button _equipButton;
        [SerializeField] private TMP_Text _equipLabel;
        [SerializeField] private TMP_Text _equipCost;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TMP_Text _upgradeCost;
        [SerializeField] private Button _starButton;
        [SerializeField] private TMP_Text _starLabel;
        [SerializeField] private TMP_Text _starCost;
        [SerializeField] private Button _closeButton;

        private static readonly Color StarOn = Color.white;
        private static readonly Color StarOff = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color BonusOn = new Color(0.96f, 0.95f, 0.92f);
        private static readonly Color BonusOff = new Color(0.66f, 0.71f, 0.77f, 0.55f);
        private static readonly Color[] StarFrames =
        {
            new Color(0.72f, 0.75f, 0.80f), new Color(0.40f, 0.78f, 0.45f), new Color(0.32f, 0.62f, 1f),
            new Color(0.70f, 0.42f, 1f), new Color(1f, 0.70f, 0.25f),
        };

        private readonly List<WeaponRowView> _rows = new List<WeaponRowView>();
        private readonly List<CurrencyCost> _cost = new List<CurrencyCost>(2);
        private readonly StringBuilder _text = new StringBuilder(160);
        private WeaponData _selected;
        private Action<string> _onWeaponChanged;
        private Action<string, long> _onCurrencyChanged;
        private Action<WeaponRowView> _onRowClicked;

        private static WeaponService Weapons => GameServices.Weapons;

        /// <summary>Màu viền theo số sao (ô vũ khí ở bảng Trang bị dùng chung).</summary>
        public static Color StarFrame(int stars) => StarFrames[Mathf.Clamp(stars, 1, StarFrames.Length) - 1];

        public static void ShowStars(Image[] images, int stars)
        {
            for (int i = 0; i < images.Length; i++) if (images[i] != null) images[i].color = i < stars ? StarOn : StarOff;
        }

        private void Awake()
        {
            _onWeaponChanged = _ => Refresh();
            _onCurrencyChanged = (_, __) => Refresh();
            _onRowClicked = row => Select(row.Data);
            _equipButton.onClick.AddListener(HandleEquip);
            _upgradeButton.onClick.AddListener(HandleUpgrade);
            _starButton.onClick.AddListener(HandleStar);
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            _equipButton.onClick.RemoveListener(HandleEquip);
            _upgradeButton.onClick.RemoveListener(HandleUpgrade);
            _starButton.onClick.RemoveListener(HandleStar);
            _closeButton.onClick.RemoveListener(Close);
        }

        private void OnEnable()
        {
            BuildRowsOnce();
            // Mở bảng thì chọn sẵn khẩu đang dùng.
            _selected = Weapons.EquippedData;
            Weapons.OnWeaponChanged += _onWeaponChanged;
            GameServices.Economy.OnCurrencyChanged += _onCurrencyChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (Weapons != null) Weapons.OnWeaponChanged -= _onWeaponChanged;
            if (GameServices.Economy != null) GameServices.Economy.OnCurrencyChanged -= _onCurrencyChanged;
        }

        public void Open() => gameObject.SetActive(true);
        public void Close() => gameObject.SetActive(false);

        private void BuildRowsOnce()
        {
            if (_rows.Count > 0 || Weapons.Database == null) return;
            foreach (WeaponData data in Weapons.Database.weapons)
            {
                if (data == null) continue;
                WeaponRowView row = Instantiate(_rowPrefab, _rowContainer);
                row.Bind(data);
                row.OnClicked += _onRowClicked;
                _rows.Add(row);
            }
        }

        private void Select(WeaponData data)
        {
            _selected = data;
            Refresh();
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled) return;
            foreach (WeaponRowView row in _rows) row.Refresh(Weapons, row.Data == _selected);
            if (_selected == null) return;

            string id = _selected.id;
            bool owned = Weapons.IsOwned(id);
            int stars = Weapons.GetStars(id);
            int level = Mathf.Max(1, Weapons.GetLevel(id));

            _detailIcon.sprite = _selected.icon;
            _detailName.text = _selected.displayName;
            ShowStars(_detailStars, stars);
            _detailLevel.text = owned ? $"Cấp {level}/{WeaponRules.LevelCap(stars)}" : "Chưa mở khóa";
            _detailDescription.text = _selected.description;

            WeaponStats now = Weapons.GetEffective(id);
            bool canUpgrade = Weapons.GetUpgradeCost(id, _cost);
            WeaponStats? next = canUpgrade ? WeaponRules.Effective(_selected, level + 1, stars) : (WeaponStats?)null;
            _detailStats.text = Describe(now, next);
            _upgradeButton.interactable = canUpgrade && GameServices.Economy.CanAfford(_cost);
            _upgradeButton.gameObject.SetActive(owned);
            _upgradeCost.text = canUpgrade ? ItemTextFormatter.Cost(_cost, Weapons.Database)
                : owned ? "Tới trần cấp — lên sao để nâng tiếp" : "";

            ShowBonus(_bonus3, 3, _selected.starBonus3, stars);
            ShowBonus(_bonus5, 5, _selected.starBonus5, stars);

            // Nút trái: Mở khóa (chưa có) / Dùng / Đang dùng.
            bool canStar = Weapons.GetStarCost(id, _cost);
            bool affordStar = canStar && GameServices.Economy.CanAfford(_cost);
            if (!owned)
            {
                _equipLabel.text = "Mở khóa";
                _equipButton.interactable = affordStar;
                _equipCost.text = ItemTextFormatter.Cost(_cost, Weapons.Database);
            }
            else
            {
                bool equipped = Weapons.EquippedId == id;
                _equipLabel.text = equipped ? "Đang dùng" : "Dùng";
                _equipButton.interactable = !equipped;
                _equipCost.text = "";
            }

            _starButton.gameObject.SetActive(owned);
            _starButton.interactable = owned && affordStar;
            _starLabel.text = canStar ? $"Lên {stars + 1} sao" : "Tối đa sao";
            _starCost.text = owned && canStar ? ItemTextFormatter.Cost(_cost, Weapons.Database) : "";
        }

        private void ShowBonus(TMP_Text text, int starsNeeded, WeaponStarBonus bonus, int stars)
        {
            text.text = $"{starsNeeded} sao: {bonus.text}";
            text.color = stars >= starsNeeded ? BonusOn : BonusOff;
        }

        /// <summary>Mỗi chỉ số 1 dòng; có next thì "hiện tại → cấp sau" như bảng Talent/Trang bị.</summary>
        private string Describe(WeaponStats now, WeaponStats? next)
        {
            _text.Clear();
            _text.Append("Sát thương: ").Append(now.damage.ToString("0.#"));
            if (next.HasValue) _text.Append(" → ").Append(next.Value.damage.ToString("0.#"));
            _text.Append('\n').Append("Tốc bắn: ").Append(now.fireRate.ToString("0.##")).Append(" phát/giây");
            if (now.projectileCount > 1) _text.Append('\n').Append("Số viên: ").Append(now.projectileCount);
            if (now.pierce > 0) _text.Append('\n').Append("Xuyên: ").Append(now.pierce).Append(" quái");
            if (now.explosionRadius > 0f)
                _text.Append('\n').Append("Nổ: bán kính ").Append(now.explosionRadius.ToString("0.#"))
                    .Append(" (").Append(Mathf.RoundToInt(now.explosionDamageFraction * 100f)).Append("% sát thương)");
            if (now.knockback > 0f) _text.Append('\n').Append("Đẩy lùi: ").Append(now.knockback.ToString("0.##"));
            _text.Append('\n').Append("Tầm: ").Append(now.range.ToString("0.#"));
            return _text.ToString();
        }

        private void HandleEquip()
        {
            if (_selected == null) return;
            if (Weapons.IsOwned(_selected.id)) Weapons.Equip(_selected.id);
            else Weapons.TryUnlock(_selected.id);
        }

        private void HandleUpgrade()
        {
            if (_selected != null) Weapons.TryUpgrade(_selected.id);
        }

        private void HandleStar()
        {
            if (_selected != null) Weapons.TryStarUp(_selected.id);
        }
    }
}
