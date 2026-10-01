namespace ArenaSurvival.Economy
{
    /// <summary>
    /// ID tiền tệ khai báo tập trung: gõ chuỗi rải rác thì 1 lỗi chính tả ("glod") là tạo ra một loại tiền mới trong save.
    /// Giá trị KHÔNG được đổi sau khi phát hành (save lưu theo chuỗi này).
    /// </summary>
    public static class CurrencyIds
    {
        public const string Gold = "gold";
        public const string Gem = "gem";
    }
}
