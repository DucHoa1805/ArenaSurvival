using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Mọi phép tính của màn chơi (độ khó, sao, phần thưởng) dưới dạng hàm thuần: không đụng scene hay asset,
    /// nên unit test được và sau này đổi số bằng bảng cân bằng / Remote Config mà không sửa logic.
    /// Công thức lấy từ Docs/project-plan.md, Phần D1.
    /// </summary>
    public static class StageRules
    {
        // Gom hằng số một chỗ: Phase 10 (bảng cân bằng) và Phase 12 (Remote Config) chỉ cần thay ở đây.
        public const float DifficultyGrowthPerStage = 0.12f;
        public const float RewardGrowthPerStage = 0.15f;
        public const float StarHealthThreshold = 0.5f;
        public const float DefeatRewardShare = 0.5f;
        // Thua mà nhận như đã đi hết màn thì không còn lý do cố thắng.
        public const float DefeatProgressCap = 0.9f;

        public static float DifficultyMultiplier(int stageNumber) =>
            1f + DifficultyGrowthPerStage * (Mathf.Max(1, stageNumber) - 1);

        public static float RewardScale(int stageNumber) =>
            1f + RewardGrowthPerStage * (Mathf.Max(1, stageNumber) - 1);

        /// <summary>3 sao tính độc lập: [0] thắng màn, [1] còn trên 50% máu, [2] thắng trong thời gian mục tiêu.</summary>
        public static bool[] EvaluateStars(bool victory, float hpFraction, float clearTime, float targetTime)
        {
            if (!victory) return new[] { false, false, false };
            return new[] { true, hpFraction > StarHealthThreshold, clearTime <= targetTime };
        }

        public static (int gold, int xp) CalculateRewards(int baseGold, int baseXp, int stageNumber, bool victory, float progress)
        {
            float share = victory ? 1f : Mathf.Clamp(progress, 0f, DefeatProgressCap) * DefeatRewardShare;
            float scale = RewardScale(stageNumber) * share;
            // Làm tròn xuống và chặn âm: phần thưởng không bao giờ "trừ" của người chơi.
            return (Mathf.Max(0, Mathf.FloorToInt(baseGold * scale + 0.0001f)),
                    Mathf.Max(0, Mathf.FloorToInt(baseXp * scale + 0.0001f)));
        }

        public static float Progress(float survivalTime, float plannedDuration) =>
            plannedDuration <= 0f ? 0f : Mathf.Clamp01(survivalTime / plannedDuration);
    }
}
