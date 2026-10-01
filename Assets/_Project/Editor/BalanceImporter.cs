using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArenaSurvival.Data;
using ArenaSurvival.Enemies;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Stages;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArenaSurvival.EditorTools
{
    /// <summary>
    /// Nhập bảng cân bằng (Docs/Balance/*.csv) thành ScriptableObject. "Tất cả hoặc không gì": kiểm tra mọi dòng + mọi tham chiếu trước,
    /// có lỗi thì không ghi asset nào. Asset có sẵn được sửa tại chỗ (giữ GUID → save/scene không vỡ); ô trống = giữ giá trị đang có,
    /// nên asset có chỉnh tay riêng (vd Enemy_Tank) chỉ đổi đúng các ô bảng ghi. Không mở hộp thoại (CLI/eval không bị chặn).
    /// </summary>
    public static class BalanceImporter
    {
        [MenuItem("ArenaSurvival/Nhập bảng cân bằng")]
        private static void ImportMenu() => Debug.Log(Import());

        /// <summary>Trả báo cáo (cũng gọi được từ eval_file).</summary>
        public static string Import()
        {
            var errors = new List<string>();
            var report = new StringBuilder("[Bảng cân bằng] ");

            List<Dictionary<string, string>> enemyRaw = Read("enemies.csv", errors);
            List<Dictionary<string, string>> waveRaw = Read("waves.csv", errors);
            List<Dictionary<string, string>> stageRaw = Read("stages.csv", errors);
            if (errors.Count > 0) return report.Append("KHÔNG nhập gì:\n").Append(string.Join("\n", errors)).ToString();

            var enemies = enemyRaw.Select(r => (raw: r, row: EnemyRow.Parse(r, errors))).ToList();
            var waves = waveRaw.Select(r => (raw: r, row: WaveRow.Parse(r, errors))).ToList();
            var stages = stageRaw.Select(r => (raw: r, row: StageRow.Parse(r, errors))).ToList();
            Duplicates(enemies.Select(e => (e.row.id, e.row.line)), "enemies.csv", errors);
            Duplicates(waves.Select(w => (w.row.id, w.row.line)), "waves.csv", errors);
            Duplicates(stages.Select(s => (s.row.id, s.row.line)), "stages.csv", errors);

            // --- Kiểm tra tham chiếu (chưa ghi gì) ---
            Dictionary<string, EnemyData> enemyAssets = BalanceAssets.ByName<EnemyData>();
            Dictionary<string, WaveData> waveAssets = BalanceAssets.ByName<WaveData>();
            Dictionary<string, EnemyBehaviour> behaviours = BalanceAssets.Behaviours();
            Dictionary<string, LootTable> loots = BalanceAssets.LootTables();
            var enemyIds = new HashSet<string>(enemies.Select(e => e.row.id));
            enemyIds.UnionWith(enemyAssets.Keys);
            var waveIds = new HashSet<string>(waves.Select(w => w.row.id));
            waveIds.UnionWith(waveAssets.Keys);

            foreach (var (raw, e) in enemies)
            {
                if (Has(raw, "sprite") && BalanceAssets.LoadSprite(e.sprite) == null) errors.Add($"enemies.csv dòng {e.line}: không thấy sprite \"{e.sprite}\".");
                if (Has(raw, "behaviour") && !behaviours.ContainsKey(e.behaviour)) errors.Add($"enemies.csv dòng {e.line}: không thấy hành vi \"{e.behaviour}\".");
                if (Has(raw, "loot") && !loots.ContainsKey(e.loot)) errors.Add($"enemies.csv dòng {e.line}: không thấy bảng rơi \"{e.loot}\".");
                if (!enemyAssets.ContainsKey(e.id) && !Has(raw, "sprite")) errors.Add($"enemies.csv dòng {e.line}: quái mới \"{e.id}\" phải có sprite.");
            }
            foreach (var (_, w) in waves)
                foreach (string id in w.enemies)
                    if (!enemyIds.Contains(id)) errors.Add($"waves.csv dòng {w.line}: không thấy quái \"{id}\".");
            foreach (var (raw, s) in stages)
            {
                if (s.chapter.Length == 0) errors.Add($"stages.csv dòng {s.line}: thiếu cột chapter.");
                foreach (string id in s.waves)
                    if (!waveIds.Contains(id)) errors.Add($"stages.csv dòng {s.line}: không thấy đợt \"{id}\".");
                if (s.eliteEnemy.Length > 0 && !enemyIds.Contains(s.eliteEnemy)) errors.Add($"stages.csv dòng {s.line}: không thấy quái tinh anh \"{s.eliteEnemy}\".");
                if (s.boss.Length > 0 && !enemyIds.Contains(s.boss)) errors.Add($"stages.csv dòng {s.line}: không thấy boss \"{s.boss}\".");
                foreach (string col in new[] { "bossLoot", "victoryLoot", "eliteLoot" })
                    if (Has(raw, col) && !loots.ContainsKey(raw[col])) errors.Add($"stages.csv dòng {s.line}: không thấy bảng rơi \"{raw[col]}\" ({col}).");
                if (s.firstClearShard.Length > 0 && BalanceAssets.ShardOf(s.firstClearShard) == null)
                    errors.Add($"stages.csv dòng {s.line}: không thấy mảnh của vũ khí \"{s.firstClearShard}\".");
            }
            if (errors.Count > 0) return report.Append($"KHÔNG nhập gì — {errors.Count} lỗi:\n").Append(string.Join("\n", errors)).ToString();

            // --- Ghi ---
            var stats = new Stats();
            BalanceAssets.EnsureFolder(BalanceAssets.WaveDir);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (raw, e) in enemies) enemyAssets[e.id] = WriteEnemy(raw, e, enemyAssets, behaviours, loots, stats);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            // Đợt/màn tham chiếu quái vừa tạo: dừng batch để asset mới nạp được.
            foreach (var (raw, w) in waves) waveAssets[w.id] = WriteWave(raw, w, waveAssets, enemyAssets, stats);
            Dictionary<string, StageData> stageAssets = BalanceAssets.StagesById();
            foreach (var (raw, s) in stages) stageAssets[s.id] = WriteStage(raw, s, stageAssets, waveAssets, enemyAssets, loots, stats);
            WriteChapters(stages.Select(s => s.row).ToList(), stageAssets, stats);
            AssetDatabase.SaveAssets();

            report.Append($"OK: tạo {stats.created}, sửa {stats.changed}, không đổi {stats.unchanged}.");
            if (stats.changedNames.Count > 0) report.Append("\nĐã sửa: ").Append(string.Join(", ", stats.changedNames));
            if (stats.changedNames.Contains("Enemy_Tank")) report.Append("\nLƯU Ý: Enemy_Tank.asset bị đổi — kiểm tra diff trước khi commit (asset này hay có chỉnh tay).");
            return report.ToString();
        }

        private class Stats
        {
            public int created, changed, unchanged;
            public readonly List<string> changedNames = new List<string>();
        }

        private static List<Dictionary<string, string>> Read(string file, List<string> errors)
        {
            string path = BalanceAssets.FullBalancePath(file);
            if (!File.Exists(path)) { errors.Add($"thiếu file {BalanceAssets.BalanceDir}/{file}"); return new List<Dictionary<string, string>>(); }
            try { return CsvReader.Parse(File.ReadAllText(path, Encoding.UTF8)); }
            catch (CsvException e) { errors.Add(file + ": " + e.Message); return new List<Dictionary<string, string>>(); }
        }

        private static void Duplicates(IEnumerable<(string id, int line)> rows, string file, List<string> errors)
        {
            var seen = new Dictionary<string, int>();
            foreach (var (id, line) in rows)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (seen.TryGetValue(id, out int first)) errors.Add($"{file} dòng {line}: id \"{id}\" trùng với dòng {first}.");
                else seen[id] = line;
            }
        }

        private static bool Has(Dictionary<string, string> raw, string col) => raw.TryGetValue(col, out string v) && v.Length > 0;

        /// <summary>Tạo asset nếu chưa có; ghi lại nếu nội dung đổi (so JSON trước/sau) — chạy lại lần 2 không làm bẩn gì.</summary>
        private static T Upsert<T>(T existing, string newPath, Action<T> fill, Stats stats) where T : ScriptableObject
        {
            if (existing == null)
            {
                T created = ScriptableObject.CreateInstance<T>();
                fill(created);
                AssetDatabase.CreateAsset(created, newPath);
                stats.created++;
                return created;
            }

            string before = EditorJsonUtility.ToJson(existing);
            fill(existing);
            if (EditorJsonUtility.ToJson(existing) == before) { stats.unchanged++; return existing; }
            EditorUtility.SetDirty(existing);
            stats.changed++;
            stats.changedNames.Add(existing.name);
            return existing;
        }

        private static EnemyData WriteEnemy(Dictionary<string, string> raw, EnemyRow e, Dictionary<string, EnemyData> assets,
            Dictionary<string, EnemyBehaviour> behaviours, Dictionary<string, LootTable> loots, Stats stats)
        {
            assets.TryGetValue(e.id, out EnemyData existing);
            return Upsert(existing, $"{BalanceAssets.EnemyDir}/{e.id}.asset", d =>
            {
                if (Has(raw, "displayName")) d.displayName = e.displayName;
                if (Has(raw, "sprite")) d.sprite = BalanceAssets.LoadSprite(e.sprite);
                if (Has(raw, "behaviour")) d.behaviour = behaviours[e.behaviour];
                if (Has(raw, "maxHealth")) d.maxHealth = e.maxHealth;
                if (Has(raw, "moveSpeed")) d.moveSpeed = e.moveSpeed;
                if (Has(raw, "contactDamage")) d.contactDamage = e.contactDamage;
                if (Has(raw, "projectileDamage")) d.projectileDamage = e.projectileDamage;
                if (Has(raw, "xpReward")) d.xpReward = e.xpReward;
                if (Has(raw, "scale")) d.scale = e.scale;
                // So theo mã hex: CSV chỉ giữ 8 bit/kênh, ghi thẳng sẽ làm tròn màu chỉnh tay (0.82 → 0.8196) dù không ai sửa.
                if (Has(raw, "tint") && ColorUtility.ToHtmlStringRGBA(d.tint) != ColorUtility.ToHtmlStringRGBA(e.tint)) d.tint = e.tint;
                if (Has(raw, "isBoss")) d.isBoss = e.isBoss;
                if (Has(raw, "loot")) d.loot = loots[e.loot];
                if (Has(raw, "lootChance")) d.lootChance = e.lootChance;
            }, stats);
        }

        private static WaveData WriteWave(Dictionary<string, string> raw, WaveRow w, Dictionary<string, WaveData> assets,
            Dictionary<string, EnemyData> enemies, Stats stats)
        {
            assets.TryGetValue(w.id, out WaveData existing);
            return Upsert(existing, $"{BalanceAssets.WaveDir}/{w.id}.asset", d =>
            {
                if (Has(raw, "duration")) d.duration = w.duration;
                if (Has(raw, "spawnInterval")) d.spawnInterval = w.spawnInterval;
                if (Has(raw, "enemies")) d.enemyTypes = w.enemies.Select(id => enemies[id]).ToArray();
            }, stats);
        }

        private static StageData WriteStage(Dictionary<string, string> raw, StageRow s, Dictionary<string, StageData> assets,
            Dictionary<string, WaveData> waves, Dictionary<string, EnemyData> enemies, Dictionary<string, LootTable> loots, Stats stats)
        {
            assets.TryGetValue(s.id, out StageData existing);
            string fileName = "Stage_" + s.id.ToUpperInvariant().Replace("_S", "_");
            return Upsert(existing, $"{BalanceAssets.StageDir}/{fileName}.asset", d =>
            {
                d.id = s.id;
                if (Has(raw, "displayName")) d.displayName = s.displayName;
                if (Has(raw, "number")) d.stageNumber = s.number;
                if (Has(raw, "waves")) d.waves = s.waves.Select(id => waves[id]).ToArray();

                // Đợt cuối: 1 nhóm tinh anh. Sửa nhóm có sẵn (giữ màu/scale chỉnh tay) thay vì tạo lại.
                if (Has(raw, "eliteEnemy"))
                {
                    if (d.finalWave == null || d.finalWave.Length == 0) d.finalWave = new[] { new EliteGroup() };
                    EliteGroup g = d.finalWave[0];
                    g.enemy = enemies[s.eliteEnemy];
                    if (Has(raw, "eliteCount")) g.count = Mathf.Max(1, s.eliteCount);
                    if (Has(raw, "eliteHealth")) g.healthMultiplier = s.eliteHealth;
                    if (Has(raw, "eliteLoot")) g.loot = loots[s.eliteLoot];
                    else if (g.loot == null && loots.TryGetValue("Loot_Elite", out LootTable elite)) g.loot = elite;
                }

                if (raw.ContainsKey("boss")) d.boss = s.boss.Length > 0 ? enemies[s.boss] : null; // cột có mặt + ô trống = màn không boss
                if (Has(raw, "bossDelay")) d.bossDelay = s.bossDelay;
                if (Has(raw, "bossLoot")) d.bossLoot = loots[s.bossLoot];
                else if (raw.ContainsKey("bossLoot") && d.boss == null) d.bossLoot = null;
                if (Has(raw, "targetTime")) d.targetTime = s.targetTime;
                if (Has(raw, "baseGold")) d.baseGold = s.baseGold;
                if (Has(raw, "baseXp")) d.baseAccountXp = s.baseXp;
                if (Has(raw, "recommendedPower")) d.recommendedPower = s.recommendedPower;
                if (Has(raw, "victoryLoot")) d.victoryLoot = loots[s.victoryLoot];

                if (raw.ContainsKey("firstClearShard"))
                {
                    MaterialData shard = s.firstClearShard.Length > 0 ? BalanceAssets.ShardOf(s.firstClearShard) : null;
                    d.firstClearReward = shard == null ? new LootEntry[0] : new[]
                    {
                        new LootEntry { material = shard, min = Mathf.Max(1, s.firstClearAmount), max = Mathf.Max(1, s.firstClearAmount), weight = 1f },
                    };
                }
            }, stats);
        }

        private static void WriteChapters(List<StageRow> rows, Dictionary<string, StageData> stages, Stats stats)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(BalanceAssets.CatalogPath);
            Dictionary<string, ChapterData> existing = BalanceAssets.ByName<ChapterData>();
            var chapters = new List<ChapterData>();
            var flat = new List<StageData>();

            // Thứ tự chương = thứ tự xuất hiện trong bảng; màn trong chương xếp theo số màn.
            foreach (string chapterId in rows.Select(r => r.chapter).Distinct())
            {
                List<StageRow> members = rows.Where(r => r.chapter == chapterId).OrderBy(r => r.number).ToList();
                string name = members.Select(m => m.chapterName).FirstOrDefault(n => n.Length > 0) ?? chapterId;
                StageData[] chapterStages = members.Select(m => stages[m.id]).ToArray();
                string assetName = "Chapter_" + chapterId.ToUpperInvariant();
                existing.TryGetValue(assetName, out ChapterData chapter);
                int number = chapters.Count + 1;
                chapter = Upsert(chapter, $"{BalanceAssets.StageDir}/{assetName}.asset", c =>
                {
                    c.id = chapterId;
                    c.number = number;
                    c.displayName = name;
                    c.stages = chapterStages;
                }, stats);
                chapters.Add(chapter);
                flat.AddRange(chapterStages);
            }

            Upsert(catalog, BalanceAssets.CatalogPath, c =>
            {
                c.chapters = chapters.ToArray();
                c.stages = flat.ToArray();
            }, stats);
        }
    }
}
