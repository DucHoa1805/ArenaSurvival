using System;
using ArenaSurvival.Items;

namespace ArenaSurvival.Loot
{
    public enum LootKind
    {
        Material,
        Item,
    }

    /// <summary>
    /// 1 thứ rơi ra: dữ liệu thuần (chỉ id chuỗi), để đi qua StageResult sang tầng meta mà không kéo theo tham chiếu asset.
    /// Struct để quay bảng trong màn không cấp phát.
    /// </summary>
    [Serializable]
    public struct LootDrop
    {
        public LootKind kind;
        public string id;
        public int amount;
        /// <summary>Chỉ có nghĩa khi kind = Item.</summary>
        public Rarity rarity;

        public static LootDrop Material(string id, int amount) => new LootDrop { kind = LootKind.Material, id = id, amount = amount };
        public static LootDrop Item(string id, Rarity rarity) => new LootDrop { kind = LootKind.Item, id = id, amount = 1, rarity = rarity };
    }
}
