using System;
using System.Collections.Generic;
using System.Text;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Shop
{
    /// <summary>Bảng tỷ lệ rơi của rương (bắt buộc công bố trên Google Play/App Store). Số lấy thẳng từ LootTable.</summary>
    public class ShopOddsPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private Button _closeButton;
        private readonly List<LootOdds> _odds = new List<LootOdds>();

        private void Awake() => _closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        public void Show(string title, LootTable table)
        {
            _title.text = title;
            _odds.Clear();
            LootOddsRules.Odds(table, _odds);
            ItemDatabase db = GameServices.Inventory != null ? GameServices.Inventory.Database : null;
            var sb = new StringBuilder();
            float[] rarity = null;
            foreach (LootOdds o in _odds)
            {
                string name = o.kind == LootKind.Item
                    ? (db?.GetItem(o.id)?.displayName ?? o.id)
                    : ItemTextFormatter.CurrencyName(o.id, db);
                string amount = o.min == o.max ? $"{o.min}" : $"{o.min}–{o.max}";
                string qty = o.kind == LootKind.Item ? "" : $" ×{amount}";
                sb.AppendLine(o.chance >= 0.9999f ? $"• {name}{qty}: chắc chắn" : $"• {name}{qty}: {o.chance * 100f:0.##}%");
                if (o.rarityChances != null) rarity = o.rarityChances;
            }
            if (rarity != null)
            {
                sb.AppendLine();
                sb.AppendLine("<b>Độ hiếm trang bị</b>");
                for (int i = 0; i < rarity.Length; i++)
                    if (rarity[i] > 0f)
                        sb.AppendLine($"• <color=#{ColorUtility.ToHtmlStringRGB(RarityColors.Get((Rarity)i))}>{RarityColors.Name((Rarity)i)}</color>: {rarity[i] * 100f:0.##}%");
            }
            _body.text = sb.ToString();
            gameObject.SetActive(true);
        }
    }
}
