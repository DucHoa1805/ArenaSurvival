using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Quy tắc thưởng theo tiến độ (hàm thuần, test được). Lần thắng đầu ×2 để mở màn mới luôn đáng giá;
    /// chơi lại ×0.5 (plan D1: 40–60%) để farm vẫn có ích nhưng không nhanh hơn đi tiếp.
    /// Thua giữ nguyên (×1) vì phần thưởng khi thua đã bị giảm theo tiến độ ở StageRules.
    /// </summary>
    public static class StageProgressRules
    {
        public const float FirstClearMultiplier = 2f;
        public const float ReplayMultiplier = 0.5f;

        public static float RewardMultiplier(bool victory, bool alreadyCleared)
        {
            if (!victory) return 1f;
            return alreadyCleared ? ReplayMultiplier : FirstClearMultiplier;
        }

        public static int ApplyMultiplier(int amount, float multiplier) =>
            Mathf.Max(0, Mathf.FloorToInt(amount * multiplier + 0.0001f));
    }
}
