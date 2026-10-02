using System.Collections.Generic;
using System.IO;
using ArenaSurvival.RunUpgrades;

namespace ArenaSurvival.DevTools
{
    /// <summary>1 lựa chọn tiêu tiền của bot ở Hub: tăng bao nhiêu lực chiến, tốn bao nhiêu vàng, đủ tiền/nguyên liệu chưa.</summary>
    public struct AutoplayOption
    {
        public float powerGain;
        public long goldCost;
        public bool affordable;

        public AutoplayOption(float powerGain, long goldCost, bool affordable)
        {
            this.powerGain = powerGain;
            this.goldCost = goldCost;
            this.affordable = affordable;
        }
    }

    public enum StageChoice { Target, Farm, Stuck }

    /// <summary>Quyết định của bot tự chơi viết dạng hàm thuần để test được mà không chạy game.</summary>
    public static class AutoplayRules
    {
        public const string SaveFileName = "autoplay-save.json";
        public const string LogFileName = "autoplay-log.csv";
        public const string SummaryFileName = "autoplay-summary.md";
        public const int StuckLimit = 15;
        public const int FarmEvery = 3;

        /// <summary>
        /// Save riêng của bot, cùng thư mục với save thật nhưng khác tên — bot không bao giờ được chạm vào tiến độ của người chơi.
        /// </summary>
        public static string SavePath(string dataDir) => Path.Combine(dataDir, SaveFileName);

        /// <summary>
        /// Chọn thứ "tăng lực chiến nhiều nhất trên mỗi đồng vàng" — gần cách người chơi hợp lý tiêu tiền. Thứ không tốn vàng
        /// (mặc đồ tốt hơn, mở khóa bằng mảnh) mà có lợi thì làm trước. Trả -1 khi không còn gì đáng mua.
        /// </summary>
        public static int PickBest(IReadOnlyList<AutoplayOption> options)
        {
            int best = -1;
            bool bestFree = false;
            float bestScore = 0f;
            for (int i = 0; i < options.Count; i++)
            {
                AutoplayOption o = options[i];
                if (!o.affordable || o.powerGain <= 0f) continue;

                bool free = o.goldCost <= 0;
                float score = free ? o.powerGain : o.powerGain / o.goldCost;
                // Thứ miễn phí luôn thắng thứ tốn vàng; cùng loại thì so điểm.
                if (best < 0 || (free && !bestFree) || (free == bestFree && score > bestScore))
                {
                    best = i;
                    bestFree = free;
                    bestScore = score;
                }
            }
            return best;
        }

        /// <summary>
        /// Thua <see cref="FarmEvery"/> lượt liền ở màn đang đẩy thì chơi lại 1 màn đã thắng để kiếm vàng (như người thật);
        /// thua quá <see cref="StuckLimit"/> lượt ở 1 màn thì coi là kẹt — số liệu cân bằng quan trọng nhất cần báo.
        /// </summary>
        public static StageChoice ChooseStage(int lossesAtTarget, int lossesSinceFarm, bool canFarm)
        {
            if (lossesAtTarget >= StuckLimit) return StageChoice.Stuck;
            if (canFarm && lossesSinceFarm >= FarmEvery) return StageChoice.Farm;
            return StageChoice.Target;
        }

        /// <summary>
        /// Thứ hạng nâng cấp trong màn (nhỏ = chọn trước): kỹ năng (Đa nòng, Lưỡi cưa, Vòng điện, Tên lửa, Lá chắn) → chỉ số → Hồi máu;
        /// máu dưới 50% thì Hồi máu lên đầu.
        /// </summary>
        public static int UpgradeRank(UpgradeType type, float hpFraction)
        {
            if (type == UpgradeType.Heal && hpFraction < 0.5f) return -1;
            switch (type)
            {
                // Kỹ năng gây sát thương/phòng thủ trước, chỉ số sau — giống người chơi hợp lý (spec run skills 10C).
                case UpgradeType.Multishot: return 0;
                case UpgradeType.AddWeapon: return 1;
                case UpgradeType.Lightning: return 2;
                case UpgradeType.Drone: return 2;
                case UpgradeType.Aura: return 2;
                case UpgradeType.Missile: return 3;
                case UpgradeType.Shield: return 4;
                case UpgradeType.Damage: return 5;
                case UpgradeType.FireRate: return 6;
                case UpgradeType.Pierce: return 7;
                case UpgradeType.MaxHealth: return 8;
                case UpgradeType.MoveSpeed: return 9;
                case UpgradeType.Magnet: return 10;
                default: return 11;
            }
        }
    }
}
