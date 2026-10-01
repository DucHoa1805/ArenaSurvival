using System.Collections.Generic;
using System.IO;
using System.Text;
using ArenaSurvival.Meta;
using ArenaSurvival.Stages;
using UnityEditor;
using UnityEngine;

namespace ArenaSurvival.EditorTools
{
    /// <summary>Chạy ProgressionSim trên danh sách màn hiện có, in bảng và ghi Docs/Balance/sim-report.md.</summary>
    public static class ProgressionSimMenu
    {
        [MenuItem("ArenaSurvival/Mô phỏng tiến trình")]
        private static void RunMenu() => Debug.Log(Run());

        public static string Run(SimSettings settings = null)
        {
            settings ??= new SimSettings();
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(BalanceAssets.CatalogPath);
            var stages = new List<SimStage>();
            foreach (StageData s in catalog.stages)
            {
                if (s == null) continue;
                stages.Add(new SimStage
                {
                    name = s.displayName,
                    stageNumber = s.stageNumber,
                    recommendedPower = s.recommendedPower,
                    baseGold = s.baseGold,
                    // Đợt thường + ~40 giây cho đợt cuối (tinh anh/boss).
                    minutes = (s.PlannedDuration + 40f) / 60f,
                });
            }

            List<SimStageResult> results = ProgressionSim.Run(stages, settings);
            var sb = new StringBuilder();
            sb.AppendLine("# Mô phỏng tiến trình (người chơi không nạp)");
            sb.AppendLine();
            sb.AppendLine($"Giả định: lực chiến đầu {settings.startPower}, +{settings.powerPer100Gold} lực chiến/100 vàng (giảm dần theo {settings.diminishingGold} vàng đã tiêu), " +
                          $"{settings.runs} lần chạy, tối đa {settings.maxPlays} lượt. Thắng: 0% dưới 75% đề xuất → 100% từ 125%.");
            sb.AppendLine();
            sb.AppendLine("| Màn | Đề xuất | Lượt để qua (TB) | Phút cộng dồn | Lực chiến lúc qua | Kẹt |");
            sb.AppendLine("|---|---|---|---|---|---|");
            for (int i = 0; i < results.Count; i++)
            {
                SimStageResult r = results[i];
                sb.AppendLine($"| {r.name} | {stages[i].recommendedPower} | {r.averagePlays:0.0} | {r.averageMinutes:0} | {r.averagePowerAtClear:0} | {r.stuckRate:P0} |");
            }

            string path = BalanceAssets.FullBalancePath("sim-report.md");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return sb.ToString();
        }
    }
}
