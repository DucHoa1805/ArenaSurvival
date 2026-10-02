using UnityEngine;

namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Hàm thuần cho nền vô tận (10D): mọi lựa chọn tile chỉ phụ thuộc tọa độ ô + seed, nên chunk bị xóa rồi tô lại vẫn y hệt
    /// (người chơi quay lại chỗ cũ không thấy nền "nhảy"). Không dùng UnityEngine.Random vì nó có trạng thái toàn cục.
    /// </summary>
    public static class GroundRules
    {
        /// <summary>Băm số nguyên 2D → 32 bit, phân bố đều (biến thể của hash PCG/xxHash rút gọn).</summary>
        public static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA77u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xC2B2AE3Du;
                h *= 0x27D4EB2Fu;
                h ^= h >> 15;
                h *= 0x165667B1u;
                h ^= h >> 13;
                return h;
            }
        }

        /// <summary>Số thực 0..1 từ hash.</summary>
        public static float Hash01(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFFFF) / 16777216f;

        /// <summary>
        /// Nhiễu giá trị (value noise) mượt 0..1: nội suy giữa các điểm lưới cách nhau 1/scale ô. Dùng để tạo mảng lớn liền khối
        /// (bãi cỏ, vệt đường đất, mảng bê tông) thay vì từng ô lốm đốm.
        /// </summary>
        public static float Noise(int x, int y, float scale, int seed)
        {
            float fx = x * scale, fy = y * scale;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = Smooth(fx - x0), ty = Smooth(fy - y0);
            float a = Hash01(x0, y0, seed), b = Hash01(x0 + 1, y0, seed);
            float c = Hash01(x0, y0 + 1, seed), d = Hash01(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        /// <summary>Chunk chứa ô (chia lấy phần nguyên xuống, đúng cả với tọa độ âm).</summary>
        public static Vector2Int ChunkOf(int cellX, int cellY, int chunkSize) =>
            new Vector2Int(FloorDiv(cellX, chunkSize), FloorDiv(cellY, chunkSize));

        public static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

        /// <summary>Chọn 1 phần tử trong mảng độ dài count theo hash (ổn định theo ô).</summary>
        public static int Pick(int x, int y, int seed, int count) => count <= 0 ? -1 : (int)(Hash(x, y, seed) % (uint)count);

        /// <summary>Ô có vật trang trí không (mật độ 0..1).</summary>
        public static bool Scatter(int x, int y, int seed, float density) => density > 0f && Hash01(x, y, seed) < density;

        /// <summary>
        /// Mảng dạng khối: chia lưới khối blockSize ô, mỗi khối được phủ với xác suất density (theo hash khối), trừ lề margin ô
        /// ở mép khối. Cạnh thẳng nên trông như ô phố/sân bê tông có chủ đích — tile Kenney không có mảnh chuyển tiếp cho mép cong.
        /// </summary>
        public static bool InBlockPatch(int x, int y, int blockSize, int margin, float density, int seed)
        {
            int bx = FloorDiv(x, blockSize), by = FloorDiv(y, blockSize);
            if (Hash01(bx, by, seed) >= density) return false;
            int lx = x - bx * blockSize, ly = y - by * blockSize;
            return lx >= margin && ly >= margin && lx < blockSize - margin && ly < blockSize - margin;
        }

        /// <summary>Chunk có nằm trong bán kính (ô vuông) quanh chunk trung tâm không.</summary>
        public static bool InRange(Vector2Int chunk, Vector2Int center, int radius) =>
            Mathf.Abs(chunk.x - center.x) <= radius && Mathf.Abs(chunk.y - center.y) <= radius;
    }
}
