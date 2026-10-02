using System;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Kênh DUY NHẤT để kết quả màn đi ra khỏi scene Game. Tầng meta (hồ sơ, tiền tệ) nghe và tự áp — Stages không biết
    /// tầng meta tồn tại, đúng nguyên tắc "scene Game không ghi vào save".
    /// </summary>
    public static class StageEvents
    {
        public static event Action<StageResult> OnStageCompleted;
        /// <summary>Màn bắt đầu (analytics ghi lượt chơi) — chỉ báo, không ai được ghi save từ đây.</summary>
        public static event Action<StageData> OnStageStarted;

        public static void RaiseStageCompleted(StageResult result) => OnStageCompleted?.Invoke(result);
        public static void RaiseStageStarted(StageData stage) => OnStageStarted?.Invoke(stage);
    }
}
