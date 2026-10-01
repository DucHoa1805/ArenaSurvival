namespace ArenaSurvival.Items
{
    /// <summary>
    /// Độ hiếm của 1 món đồ. Giá trị số được LƯU VÀO SAVE nên không đổi thứ tự / không chèn giữa; bậc mới thì thêm vào cuối.
    /// </summary>
    public enum Rarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3,
    }

    /// <summary>Ô trang bị. 8C thêm Weapon.</summary>
    public enum EquipSlot
    {
        Armor,
        Accessory,
    }

    /// <summary>
    /// Khóa của ô trong save. Không dùng EquipSlot.ToString(): đổi tên enum khi refactor sẽ làm mất đồ đang mặc của người chơi.
    /// </summary>
    public static class EquipSlotIds
    {
        public const string Armor = "armor";
        public const string Accessory = "accessory";

        public static string Key(EquipSlot slot) => slot == EquipSlot.Armor ? Armor : Accessory;
    }
}
