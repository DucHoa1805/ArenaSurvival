namespace ArenaSurvival.Economy
{
    /// <summary>
    /// Lý do thay đổi tiền tệ — analytics dùng để biết tiền đến từ đâu và tiêu vào đâu, nhiệm vụ ngày dùng để đếm "nâng cấp"/"mở rương".
    /// Hằng số tập trung như CurrencyIds: đổi chuỗi sau khi phát hành thì số liệu cũ và mới không ghép được.
    /// </summary>
    public static class EconomyReasons
    {
        public const string Unknown = "unknown";

        // Nguồn
        public const string StageReward = "stage_reward";
        public const string StageLoot = "stage_loot";
        public const string AccountLevel = "account_level";
        public const string Dismantle = "dismantle";
        public const string ShopIap = "shop_iap";
        public const string ShopChest = "shop_chest";
        public const string ShopGold = "shop_gold";
        public const string AdChest = "ad_chest";
        public const string AdDouble = "ad_double";
        public const string Mission = "mission";
        public const string Login = "login";
        public const string Dev = "dev";

        // Nơi tiêu
        public const string TalentUpgrade = "talent_upgrade";
        public const string ItemUpgrade = "item_upgrade";
        public const string ItemAscend = "item_ascend";
        public const string WeaponUnlock = "weapon_unlock";
        public const string WeaponStar = "weapon_star";
        public const string WeaponUpgrade = "weapon_upgrade";
        public const string ShopPurchase = "shop_purchase";
        public const string Revive = "revive";

        /// <summary>Lần tiêu này là 1 lần "nâng cấp" (nhiệm vụ ngày). Mở khóa vũ khí không tính: đó là nhận đồ mới, không phải làm mạnh.</summary>
        public static bool IsUpgrade(string reason) =>
            reason == TalentUpgrade || reason == ItemUpgrade || reason == ItemAscend || reason == WeaponStar || reason == WeaponUpgrade;
    }

    /// <summary>1 lần đổi số dư. delta âm = tiêu. Struct để phát sự kiện không cấp phát.</summary>
    public readonly struct CurrencyTransaction
    {
        public readonly string id;
        public readonly long delta;
        public readonly long balance;
        public readonly string reason;

        public CurrencyTransaction(string id, long delta, long balance, string reason)
        {
            this.id = id;
            this.delta = delta;
            this.balance = balance;
            this.reason = reason;
        }
    }
}
