namespace ArenaSurvival.Meta
{
    /// <summary>
    /// 1 vũ khí chính người chơi đã sở hữu (lưu trong save, khóa là id vũ khí). Khác trang bị 8B: mỗi khẩu chỉ có 1 bản,
    /// mảnh thừa dùng để lên sao chứ không thành món mới — nên không cần uid.
    /// </summary>
    public class OwnedWeapon
    {
        public int level = 1;
        /// <summary>1..5. Có trong từ điển = đã mở khóa (ít nhất 1★).</summary>
        public int stars = 1;
    }
}
