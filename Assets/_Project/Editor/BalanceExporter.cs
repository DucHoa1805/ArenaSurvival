using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ArenaSurvival.Enemies;
using ArenaSurvival.Stages;
using ArenaSurvival.Weapons;
using UnityEditor;
using UnityEngine;

namespace ArenaSurvival.EditorTools
{
    /// <summary>
    /// Xuất asset hiện có ra CSV (Docs/Balance) — bản gốc để sửa bằng Google Sheets/Excel rồi nhập lại. Nhập ngay sau khi xuất
    /// phải báo "không đổi" (kiểm tra 2 chiều khớp nhau).
    /// </summary>
    public static class BalanceExporter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [MenuItem("ArenaSurvival/Xuất bảng cân bằng")]
        private static void ExportMenu() => Debug.Log(Export());

        public static string Export()
        {
            Directory.CreateDirectory(BalanceAssets.FullBalancePath(""));
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(BalanceAssets.CatalogPath);
            List<StageData> stages = catalog.stages.Where(s => s != null).ToList();

            // Chỉ xuất quái/đợt thực sự được dùng trong các màn (+ quái do boss/phân thân gọi ra), theo thứ tự xuất hiện.
            var waves = new List<WaveData>();
            var enemies = new List<EnemyData>();
            void AddEnemy(EnemyData e) { if (e != null && !enemies.Contains(e)) enemies.Add(e); }
            foreach (StageData s in stages)
            {
                foreach (WaveData w in s.waves ?? new WaveData[0])
                {
                    if (w == null) continue;
                    if (!waves.Contains(w)) waves.Add(w);
                    foreach (EnemyData e in w.enemyTypes ?? new EnemyData[0]) AddEnemy(e);
                }
                if (s.finalWave != null) foreach (EliteGroup g in s.finalWave) AddEnemy(g?.enemy);
                AddEnemy(s.boss);
            }
            foreach (EnemyData e in BalanceAssets.ByName<EnemyData>().Values.OrderBy(e => e.name)) AddEnemy(e);

            var sb = new StringBuilder("id,displayName,sprite,behaviour,maxHealth,moveSpeed,contactDamage,projectileDamage,xpReward,scale,tint,isBoss,loot,lootChance\n");
            foreach (EnemyData e in enemies)
            {
                Row(sb, e.name, e.displayName, BalanceAssets.SpritePath(e.sprite), e.behaviour != null ? e.behaviour.name : "",
                    F(e.maxHealth), F(e.moveSpeed), F(e.contactDamage), F(e.projectileDamage), e.xpReward.ToString(Inv), F(e.scale),
                    "#" + ColorUtility.ToHtmlStringRGBA(e.tint), e.isBoss ? "1" : "0", e.loot != null ? e.loot.name : "", F(e.lootChance));
            }
            Write("enemies.csv", sb);

            sb = new StringBuilder("id,duration,spawnInterval,enemies\n");
            foreach (WaveData w in waves)
                Row(sb, w.name, F(w.duration), F(w.spawnInterval), string.Join(";", (w.enemyTypes ?? new EnemyData[0]).Where(e => e != null).Select(e => e.name)));
            Write("waves.csv", sb);

            sb = new StringBuilder("chapter,chapterName,number,id,displayName,waves,eliteEnemy,eliteCount,eliteHealth,eliteLoot,boss,bossDelay," +
                                   "targetTime,baseGold,baseXp,recommendedPower,bossLoot,victoryLoot,firstClearShard,firstClearAmount\n");
            foreach (StageData s in stages)
            {
                ChapterData chapter = catalog.ChapterOf(s);
                EliteGroup g = s.finalWave != null && s.finalWave.Length > 0 ? s.finalWave[0] : null;
                string shardWeapon = "", shardAmount = "";
                if (s.firstClearReward != null && s.firstClearReward.Length > 0 && s.firstClearReward[0]?.material != null)
                {
                    shardWeapon = s.firstClearReward[0].material.id.StartsWith(WeaponRules.ShardPrefix)
                        ? s.firstClearReward[0].material.id.Substring(WeaponRules.ShardPrefix.Length) : "";
                    shardAmount = s.firstClearReward[0].min.ToString(Inv);
                }
                Row(sb, chapter != null ? chapter.id : "c1", chapter != null ? chapter.displayName : "Sa mạc", s.stageNumber.ToString(Inv), s.id, s.displayName,
                    string.Join(";", (s.waves ?? new WaveData[0]).Where(w => w != null).Select(w => w.name)),
                    g?.enemy != null ? g.enemy.name : "", g != null ? g.count.ToString(Inv) : "", g != null ? F(g.healthMultiplier) : "", g?.loot != null ? g.loot.name : "",
                    s.boss != null ? s.boss.name : "", F(s.bossDelay), F(s.targetTime), s.baseGold.ToString(Inv), s.baseAccountXp.ToString(Inv),
                    s.recommendedPower.ToString(Inv), s.bossLoot != null ? s.bossLoot.name : "", s.victoryLoot != null ? s.victoryLoot.name : "",
                    shardWeapon, shardAmount);
            }
            Write("stages.csv", sb);
            return $"[Bảng cân bằng] Đã xuất {enemies.Count} quái, {waves.Count} đợt, {stages.Count} màn vào {BalanceAssets.BalanceDir}/.";
        }

        private static string F(float v) => v.ToString("0.###", Inv);

        private static void Row(StringBuilder sb, params string[] cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0) sb.Append(',');
                string c = cells[i] ?? "";
                // Ô có dấu phẩy/ngoặc kép/xuống dòng phải bọc ngoặc kép để Sheets và CsvReader đọc đúng.
                if (c.IndexOfAny(new[] { ',', '"', '\n' }) >= 0) sb.Append('"').Append(c.Replace("\"", "\"\"")).Append('"');
                else sb.Append(c);
            }
            sb.Append('\n');
        }

        private static void Write(string file, StringBuilder sb) =>
            File.WriteAllText(BalanceAssets.FullBalancePath(file), sb.ToString(), new UTF8Encoding(true)); // BOM: Excel mới hiểu tiếng Việt
    }
}
