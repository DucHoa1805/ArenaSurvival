namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Thứ bị đốt cháy được (quái). Đạn Phun lửa và chiêu Bão lửa chỉ biết interface này — Combat không phụ thuộc Enemies.
    /// </summary>
    public interface IBurnable
    {
        /// <summary>Cháy dps sát thương/giây trong duration giây. Đốt chồng: giữ dps lớn hơn, làm mới thời gian.</summary>
        void ApplyBurn(float dps, float duration);
    }
}
