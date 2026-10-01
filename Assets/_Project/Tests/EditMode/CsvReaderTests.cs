using System.Collections.Generic;
using ArenaSurvival.Data;
using NUnit.Framework;
using UnityEngine;

namespace ArenaSurvival.Tests
{
    public class CsvReaderTests
    {
        [Test]
        public void Parse_HeaderAndRows_ToDictionaries()
        {
            List<Dictionary<string, string>> rows = CsvReader.Parse("id,hp\nbasic,20\nfast,10\n");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("basic", rows[0]["id"]);
            Assert.AreEqual("10", rows[1]["hp"]);
        }

        [Test]
        public void Parse_QuotedCellWithCommaAndQuote()
        {
            var rows = CsvReader.Parse("id,name\nboss,\"Thủ Lĩnh, Sa Mạc \"\"X\"\"\"\n");
            Assert.AreEqual("Thủ Lĩnh, Sa Mạc \"X\"", rows[0]["name"]);
        }

        [Test]
        public void Parse_SkipsBlankLines_TrimsCells_HandlesBomAndCrlf()
        {
            var rows = CsvReader.Parse("﻿id, hp \r\n\r\n basic , 20 \r\n   \r\n");
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("basic", rows[0]["id"]);
            Assert.AreEqual("20", rows[0]["hp"]);
        }

        [Test]
        public void Parse_MissingCells_BecomeEmpty()
        {
            var rows = CsvReader.Parse("a,b,c\n1,2\n");
            Assert.AreEqual("", rows[0]["c"]);
        }

        [Test]
        public void Parse_TooManyCells_ThrowsWithLineNumber()
        {
            var e = Assert.Throws<CsvException>(() => CsvReader.Parse("a,b\n1,2\n1,2,3\n"));
            StringAssert.Contains("dòng 3", e.Message);
        }

        [Test]
        public void Parse_LineNumberColumn_IsAdded()
        {
            var rows = CsvReader.Parse("a\n\nx\n");
            Assert.AreEqual("3", rows[0][CsvReader.LineKey]);
        }
    }

    public class BalanceRowsTests
    {
        private static Dictionary<string, string> Row(params string[] kv)
        {
            var d = new Dictionary<string, string> { [CsvReader.LineKey] = "7" };
            for (int i = 0; i < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return d;
        }

        [Test]
        public void SplitList_TrimsAndDropsEmpty()
        {
            CollectionAssert.AreEqual(new[] { "basic", "fast" }, BalanceRows.SplitList(" basic ; ;fast;"));
            Assert.AreEqual(0, BalanceRows.SplitList("").Length);
        }

        [Test]
        public void ParseColor_HexOrDefault()
        {
            Assert.AreEqual(new Color(1f, 0f, 0f, 1f), BalanceRows.ParseColor("#FF0000", Color.white));
            Assert.AreEqual(Color.white, BalanceRows.ParseColor("", Color.white));
        }

        [Test]
        public void EnemyRow_ParsesNumbers()
        {
            var errors = new List<string>();
            EnemyRow e = EnemyRow.Parse(Row("id", "charger", "displayName", "Kẻ lao", "maxHealth", "30", "moveSpeed", "2.5",
                "contactDamage", "8", "xpReward", "2", "lootChance", "0.03"), errors);
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual("charger", e.id);
            Assert.AreEqual(30f, e.maxHealth);
            Assert.AreEqual(2.5f, e.moveSpeed);
            Assert.AreEqual(1f, e.scale, "cột trống → mặc định");
        }

        [Test]
        public void EnemyRow_BadNumber_ReportsLineAndColumn()
        {
            var errors = new List<string>();
            EnemyRow.Parse(Row("id", "x", "maxHealth", "abc"), errors);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("dòng 7", errors[0]);
            StringAssert.Contains("maxHealth", errors[0]);
        }

        [Test]
        public void Row_MissingId_IsError()
        {
            var errors = new List<string>();
            WaveRow.Parse(Row("duration", "60"), errors);
            Assert.AreEqual(1, errors.Count);
        }

        [Test]
        public void StageRow_ParsesListsAndElite()
        {
            var errors = new List<string>();
            StageRow s = StageRow.Parse(Row("chapter", "c1", "chapterName", "Sa mạc", "number", "3", "id", "c1_s03", "displayName", "1-3",
                "waves", "c1_w1;c1_w2", "eliteEnemy", "tank", "eliteCount", "2", "eliteHealth", "4", "targetTime", "220",
                "baseGold", "100", "baseXp", "20", "recommendedPower", "150", "firstClearShard", "weapon_shotgun", "firstClearAmount", "10"), errors);
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
            CollectionAssert.AreEqual(new[] { "c1_w1", "c1_w2" }, s.waves);
            Assert.AreEqual(2, s.eliteCount);
            Assert.AreEqual(4f, s.eliteHealth);
            Assert.AreEqual(10, s.firstClearAmount);
            Assert.AreEqual("", s.boss);
        }
    }
}
