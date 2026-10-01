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

        // Nguyên liệu (8B) dùng chung sổ cái với tiền tệ: cùng luật không âm, cùng sự kiện, trừ chung được bằng TrySpendMany.
        public const string Ore = "ore";
        public const string Leather = "leather";
        public const string BossCore = "boss_core";
    }
}
