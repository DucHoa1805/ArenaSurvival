namespace ArenaSurvival.Economy
{
    /// <summary>1 dòng chi phí (id tiền tệ/nguyên liệu + số lượng), để trừ nhiều loại cùng lúc bằng EconomyService.TrySpendMany.</summary>
    public readonly struct CurrencyCost
    {
        public readonly string id;
        public readonly long amount;

        public CurrencyCost(string id, long amount)
        {
            this.id = id;
            this.amount = amount;
        }
    }
}
