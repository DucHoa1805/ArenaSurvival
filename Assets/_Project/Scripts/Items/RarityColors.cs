using UnityEngine;

namespace ArenaSurvival.Items
{
    /// <summary>Màu và tên hiển thị của độ hiếm — dùng chung cho pickup trên sân, kho đồ và màn kết quả để luôn khớp nhau.</summary>
    public static class RarityColors
    {
        private static readonly Color[] Colors =
        {
            new Color(0.72f, 0.75f, 0.80f), // Thường: xám
            new Color(0.32f, 0.62f, 1.00f), // Hiếm: xanh dương
            new Color(0.70f, 0.42f, 1.00f), // Sử thi: tím
            new Color(1.00f, 0.62f, 0.20f), // Huyền thoại: cam
        };

        private static readonly string[] Names = { "Thường", "Hiếm", "Sử thi", "Huyền thoại" };

        public static Color Get(Rarity rarity) => Colors[Index(rarity)];
        public static string Name(Rarity rarity) => Names[Index(rarity)];

        private static int Index(Rarity rarity) => Mathf.Clamp((int)rarity, 0, Colors.Length - 1);
    }
}
