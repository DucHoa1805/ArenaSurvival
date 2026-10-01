using System.Collections.Generic;
using System.Text;
using ArenaSurvival.Economy;
using ArenaSurvival.Items;
using ArenaSurvival.Stats;

namespace ArenaSurvival.UI
{
    /// <summary>Chuỗi hiển thị cho trang bị: danh sách chỉ số, chi phí. Gom 1 chỗ để kho đồ và popup nói cùng một kiểu.</summary>
    public static class ItemTextFormatter
    {
        private static readonly StatKind[] Kinds =
            { StatKind.MaxHealth, StatKind.Armor, StatKind.DamagePercent, StatKind.MoveSpeedPercent, StatKind.HealthRegen };

        private static readonly StringBuilder Builder = new StringBuilder(128);

        /// <summary>Mỗi chỉ số khác 0 một dòng. Có next thì "hiện tại → cấp sau", kiểu chữ giống bảng Talent.</summary>
        public static string Stats(StatBlock now, StatBlock? next)
        {
            Builder.Clear();
            foreach (StatKind kind in Kinds)
            {
                float a = StatRules.Get(now, kind);
                float b = next.HasValue ? StatRules.Get(next.Value, kind) : a;
                if (a == 0f && b == 0f) continue;

                if (Builder.Length > 0) Builder.Append('\n');
                if (next.HasValue && b != a)
                    Builder.Append(TalentRow.FormatValue(kind, a)).Append(" → ").Append(TalentRow.FormatEffect(kind, b));
                else
                    Builder.Append(TalentRow.FormatEffect(kind, a));
            }
            return Builder.ToString();
        }

        /// <summary>"262 vàng · 3 Da". Tên nguyên liệu lấy từ database (không gõ cứng), tiền tệ thì dùng tên cố định.</summary>
        public static string Cost(IReadOnlyList<CurrencyCost> costs, ItemDatabase database)
        {
            Builder.Clear();
            for (int i = 0; i < costs.Count; i++)
            {
                if (i > 0) Builder.Append(" · ");
                Builder.Append(costs[i].amount).Append(' ').Append(CurrencyName(costs[i].id, database));
            }
            return Builder.ToString();
        }

        public static string CurrencyName(string id, ItemDatabase database)
        {
            if (id == CurrencyIds.Gold) return "vàng";
            if (id == CurrencyIds.Gem) return "kim cương";
            MaterialData material = database != null ? database.GetMaterial(id) : null;
            return material != null ? material.displayName : id;
        }
    }
}
