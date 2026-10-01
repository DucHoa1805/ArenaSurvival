using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ArenaSurvival.Data
{
    /// <summary>
    /// Đọc từng dòng bảng cân bằng (đã tách bởi CsvReader) thành dữ liệu có kiểu, gom lỗi theo dòng/cột thay vì ném ngay —
    /// để người sửa bảng thấy MỌI chỗ sai trong 1 lần nhập. Hàm thuần: phần ghi asset nằm ở công cụ Editor.
    /// </summary>
    public static class BalanceRows
    {
        public static string[] SplitList(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
            var list = new List<string>();
            foreach (string part in value.Split(';'))
            {
                string p = part.Trim();
                if (p.Length > 0) list.Add(p);
            }
            return list.ToArray();
        }

        public static Color ParseColor(string value, Color fallback) =>
            !string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value.Trim(), out Color c) ? c : fallback;

        public static string Line(Dictionary<string, string> row) => row.TryGetValue(CsvReader.LineKey, out string l) ? l : "?";

        public static string Text(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out string v) ? v ?? "" : "";

        public static float Float(Dictionary<string, string> row, string column, float fallback, List<string> errors)
        {
            string v = Text(row, column);
            if (v.Length == 0) return fallback;
            // InvariantCulture: máy Việt Nam dùng dấu phẩy thập phân, còn bảng luôn ghi "2.5".
            if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
            errors.Add($"dòng {Line(row)}, cột {column}: \"{v}\" không phải số.");
            return fallback;
        }

        public static int Int(Dictionary<string, string> row, string column, int fallback, List<string> errors)
        {
            string v = Text(row, column);
            if (v.Length == 0) return fallback;
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return i;
            errors.Add($"dòng {Line(row)}, cột {column}: \"{v}\" không phải số nguyên.");
            return fallback;
        }

        public static bool Bool(Dictionary<string, string> row, string column)
        {
            string v = Text(row, column).ToLowerInvariant();
            return v == "1" || v == "true" || v == "x" || v == "yes" || v == "có";
        }

        public static string Id(Dictionary<string, string> row, List<string> errors)
        {
            string id = Text(row, "id");
            if (id.Length == 0) errors.Add($"dòng {Line(row)}: thiếu cột id.");
            return id;
        }
    }

    /// <summary>1 dòng enemies.csv.</summary>
    public class EnemyRow
    {
        public int line;
        public string id, displayName, sprite, behaviour, loot;
        public float maxHealth, moveSpeed, contactDamage, projectileDamage, scale, lootChance;
        public int xpReward;
        public Color tint;
        public bool isBoss;

        public static EnemyRow Parse(Dictionary<string, string> row, List<string> errors) => new EnemyRow
        {
            line = int.TryParse(BalanceRows.Line(row), out int l) ? l : 0,
            id = BalanceRows.Id(row, errors),
            displayName = BalanceRows.Text(row, "displayName"),
            sprite = BalanceRows.Text(row, "sprite"),
            behaviour = BalanceRows.Text(row, "behaviour"),
            maxHealth = BalanceRows.Float(row, "maxHealth", 20f, errors),
            moveSpeed = BalanceRows.Float(row, "moveSpeed", 2f, errors),
            contactDamage = BalanceRows.Float(row, "contactDamage", 5f, errors),
            projectileDamage = BalanceRows.Float(row, "projectileDamage", 6f, errors),
            xpReward = BalanceRows.Int(row, "xpReward", 1, errors),
            scale = BalanceRows.Float(row, "scale", 1f, errors),
            tint = BalanceRows.ParseColor(BalanceRows.Text(row, "tint"), Color.white),
            isBoss = BalanceRows.Bool(row, "isBoss"),
            loot = BalanceRows.Text(row, "loot"),
            lootChance = BalanceRows.Float(row, "lootChance", 0f, errors),
        };
    }

    /// <summary>1 dòng waves.csv.</summary>
    public class WaveRow
    {
        public int line;
        public string id;
        public float duration, spawnInterval;
        public string[] enemies;

        public static WaveRow Parse(Dictionary<string, string> row, List<string> errors) => new WaveRow
        {
            line = int.TryParse(BalanceRows.Line(row), out int l) ? l : 0,
            id = BalanceRows.Id(row, errors),
            duration = BalanceRows.Float(row, "duration", 60f, errors),
            spawnInterval = BalanceRows.Float(row, "spawnInterval", 1.5f, errors),
            enemies = BalanceRows.SplitList(BalanceRows.Text(row, "enemies")),
        };
    }

    /// <summary>1 dòng stages.csv.</summary>
    public class StageRow
    {
        public int line;
        public string chapter, chapterName, id, displayName, eliteEnemy, boss, bossLoot, victoryLoot, eliteLoot, firstClearShard;
        public int number, eliteCount, baseGold, baseXp, recommendedPower, firstClearAmount;
        public float eliteHealth, bossDelay, targetTime;
        public string[] waves;

        public static StageRow Parse(Dictionary<string, string> row, List<string> errors) => new StageRow
        {
            line = int.TryParse(BalanceRows.Line(row), out int l) ? l : 0,
            chapter = BalanceRows.Text(row, "chapter"),
            chapterName = BalanceRows.Text(row, "chapterName"),
            number = BalanceRows.Int(row, "number", 1, errors),
            id = BalanceRows.Id(row, errors),
            displayName = BalanceRows.Text(row, "displayName"),
            waves = BalanceRows.SplitList(BalanceRows.Text(row, "waves")),
            eliteEnemy = BalanceRows.Text(row, "eliteEnemy"),
            eliteCount = BalanceRows.Int(row, "eliteCount", 0, errors),
            eliteHealth = BalanceRows.Float(row, "eliteHealth", 5f, errors),
            eliteLoot = BalanceRows.Text(row, "eliteLoot"),
            boss = BalanceRows.Text(row, "boss"),
            bossDelay = BalanceRows.Float(row, "bossDelay", 1.5f, errors),
            targetTime = BalanceRows.Float(row, "targetTime", 210f, errors),
            baseGold = BalanceRows.Int(row, "baseGold", 100, errors),
            baseXp = BalanceRows.Int(row, "baseXp", 20, errors),
            recommendedPower = BalanceRows.Int(row, "recommendedPower", 100, errors),
            bossLoot = BalanceRows.Text(row, "bossLoot"),
            victoryLoot = BalanceRows.Text(row, "victoryLoot"),
            firstClearShard = BalanceRows.Text(row, "firstClearShard"),
            firstClearAmount = BalanceRows.Int(row, "firstClearAmount", 0, errors),
        };
    }
}
