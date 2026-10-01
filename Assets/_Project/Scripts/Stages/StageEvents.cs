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

        public static void RaiseStageCompleted(StageResult result) => OnStageCompleted?.Invoke(result);
    }
}
