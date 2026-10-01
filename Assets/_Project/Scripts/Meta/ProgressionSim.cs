using System;
using System.Collections.Generic;
using ArenaSurvival.Stages;

namespace ArenaSurvival.Meta
{
    /// <summary>1 màn trong mô phỏng (lấy từ StageData).</summary>
    public struct SimStage
    {
        public string name;
        public int stageNumber;
        public int recommendedPower;
        public int baseGold;
        /// <summary>Thời lượng 1 lượt thắng (phút). Thua tính bằng một nửa.</summary>
        public float minutes;
    }

    /// <summary>Giả định về người chơi không nạp. Số khởi điểm thô — chỉnh khi có dữ liệu thật (Phase 12).</summary>
    public class SimSettings
    {
        public int seed = 1;
        public int runs = 100;
        public int maxPlays = 400;
        /// <summary>Lực chiến người mới (Súng lục, 100 máu ≈ 130).</summary>
        public float startPower = 130f;
        /// <summary>Lực chiến nhận được mỗi 100 vàng tiêu ở thời điểm đầu.</summary>
        public float powerPer100Gold = 15f;
        /// <summary>Giá nâng tăng dần: hiệu quả mỗi đồng giảm theo tổng đã tiêu (càng lớn càng giảm chậm).</summary>
        public float diminishingGold = 4000f;
    }

    public struct SimStageResult
    {
        public string name;
        /// <summary>Số lượt chơi CHÍNH màn này (gồm cả lượt thua) cho tới khi qua, trung bình.</summary>
        public float averagePlays;
        /// <summary>Phút chơi cộng dồn từ đầu game tới lúc qua màn này, trung bình (lượt bị kẹt tính tới lúc dừng).</summary>
        public float averageMinutes;
        /// <summary>Tỉ lệ lượt mô phỏng hết maxPlays mà vẫn chưa qua (tường quá cao).</summary>
        public float stuckRate;
        public float averagePowerAtClear;
    }

    /// <summary>
    /// Mô phỏng tiến trình của người chơi KHÔNG NẠP (hàm thuần, có seed, unit test được): mỗi lượt chơi màn chưa qua đầu tiên,
    /// thắng theo tỉ lệ lực chiến/đề xuất, nhận thưởng theo đúng công thức của game (StageRules, StageProgressRules), tiêu hết vàng vào
    /// lực chiến với hiệu quả giảm dần. Không đoán chính xác từng trận — dùng để so màn với nhau và tìm "tường" (plan Phase 10).
    /// </summary>
    public static class ProgressionSim
    {
        /// <summary>Dưới 75% đề xuất thua chắc; 75% → 15%; từ 125% gần như chắc thắng.</summary>
        public static float WinChance(float power, float recommended)
        {
            if (recommended <= 0f) return 1f;
            float ratio = power / recommended;
            if (ratio < 0.75f) return 0f;
            return Math.Min(1f, Math.Max(0f, 0.15f + 0.85f * (ratio - 0.75f) / 0.5f));
        }

        public static List<SimStageResult> Run(IReadOnlyList<SimStage> stages, SimSettings settings)
        {
            int n = stages.Count;
            var plays = new double[n];
            var minutes = new double[n];
            var power = new double[n];
            var stuck = new int[n];
            var rng = new Random(settings.seed);

            for (int run = 0; run < settings.runs; run++)
            {
                double powerNow = settings.startPower, spent = 0, time = 0;
                int current = 0, totalPlays = 0, playsHere = 0;

                while (current < n && totalPlays < settings.maxPlays)
                {
                    SimStage stage = stages[current];
                    totalPlays++;
                    playsHere++;
                    bool win = rng.NextDouble() < WinChance((float)powerNow, stage.recommendedPower);

                    // Thua: trung bình trụ được ~60% màn (không có dữ liệu thật thì lấy giữa).
                    (int gold, _) = StageRules.CalculateRewards(stage.baseGold, 0, stage.stageNumber, win, win ? 1f : 0.6f);
                    gold = StageProgressRules.ApplyMultiplier(gold, StageProgressRules.RewardMultiplier(win, false));
                    time += win ? stage.minutes : stage.minutes * 0.5f;
                    powerNow += PowerFromGold(gold, ref spent, settings);

                    if (!win) continue;
                    plays[current] += playsHere;
                    minutes[current] += time;
                    power[current] += powerNow;
                    playsHere = 0;
                    current++;
                }

                // Kẹt: các màn chưa qua tính số lượt đã dồn vào màn đang kẹt, thời gian tới lúc dừng.
                for (int i = current; i < n; i++)
                {
                    stuck[i]++;
                    plays[i] += i == current ? playsHere : 0;
                    minutes[i] += time;
                    power[i] += powerNow;
                }
            }

            var result = new List<SimStageResult>(n);
            for (int i = 0; i < n; i++)
            {
                result.Add(new SimStageResult
                {
                    name = stages[i].name,
                    averagePlays = (float)(plays[i] / settings.runs),
                    averageMinutes = (float)(minutes[i] / settings.runs),
                    stuckRate = stuck[i] / (float)settings.runs,
                    averagePowerAtClear = (float)(power[i] / settings.runs),
                });
            }
            return result;
        }

        /// <summary>Đổi vàng ra lực chiến theo từng 100 vàng, hiệu quả giảm dần theo tổng đã tiêu (giá nâng cấp tăng theo cấp).</summary>
        private static double PowerFromGold(int gold, ref double spent, SimSettings s)
        {
            double gained = 0;
            double remaining = gold;
            while (remaining > 0)
            {
                double chunk = Math.Min(100, remaining);
                gained += chunk / 100.0 * s.powerPer100Gold / (1.0 + spent / s.diminishingGold);
                spent += chunk;
                remaining -= chunk;
            }
            return gained;
        }
    }
}
