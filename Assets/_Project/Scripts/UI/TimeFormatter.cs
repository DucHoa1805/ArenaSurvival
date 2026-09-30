namespace ArenaSurvival.UI
{
    public static class TimeFormatter
    {
        /// <summary>Đổi số giây thành chuỗi "mm:ss" (75 giây → "01:15").</summary>
        public static string ToClock(float seconds)
        {
            int total = (int)seconds;
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
