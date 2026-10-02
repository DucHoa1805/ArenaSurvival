using System.Collections.Generic;
using System.Globalization;

namespace ArenaSurvival.Core
{
    /// <summary>
    /// Số liệu cân bằng chỉnh được từ xa (Remote Config, Phase 12): giá trị mặc định = số đang chạy trong game, RemoteConfigService
    /// ghi đè bằng Apply. Gom về đây để logic (StageRules, AdPlacements...) chỉ đọc 1 chỗ và vẫn là hàm thuần test được
    /// (test gọi Reset để về mặc định). Khóa là chuỗi ổn định — đã khai báo trên dashboard thì không đổi tên.
    /// </summary>
    public static class Tuning
    {
        public const string KeyDifficultyGrowth = "difficulty_growth";
        public const string KeyRewardGrowth = "reward_growth";
        public const string KeyReviveGemCost = "revive_gem_cost";
        public const string KeyAdLimitDouble = "ad_limit_double_reward";
        public const string KeyAdLimitRevive = "ad_limit_revive";
        public const string KeyAdLimitFreeChest = "ad_limit_free_chest";
        public const string KeyStageGoldMultiplier = "stage_gold_multiplier";
        public const string KeyMissionRewardMultiplier = "mission_reward_multiplier";
        public const string KeySpawnDensity = "spawn_density";
        public const string KeySpawnEarlyBoost = "spawn_early_boost";
        public const string KeyOpeningPack = "opening_pack";
        public const string KeyMaxAlive = "max_alive";
        public const string KeyUpgradeGrowth = "upgrade_growth";

        // 0.12 → 0.09 (10C, bot kẹt boss 1-10) → 0.075 (10D, người dùng muốn game dễ hơn nữa).
        public const float DefaultDifficultyGrowth = 0.075f;
        public const float DefaultRewardGrowth = 0.15f;
        public const int DefaultReviveGemCost = 20;
        public const int DefaultAdLimitDouble = 5;
        public const int DefaultAdLimitRevive = 3;
        public const int DefaultAdLimitFreeChest = 2;
        // 10E: người dùng thấy đầu màn quá vắng (1-1 cứ 2 s mới 1 quái) → chương đầu sinh dày hơn, giảm dần tới chương 3.
        // Thử đứng yên ở 1-1 (Súng lục cấp 1): ×2 + đàn 10 chết sau 7 s; ×2 + đàn 6 chết sau 15 s; ×1.8 + đàn 5 mất 15 máu,
        // hạ 40 quái/46 s (bản cũ 19) — đông, có áp lực mà người mới không chết ngay.
        public const float DefaultSpawnEarlyBoost = 0.8f;
        public const int DefaultOpeningPack = 5;
        // Benchmark 300 quái vẫn ~2 ms/frame; chừa chỗ cho tinh anh, boss, quái triệu hồi.
        public const int DefaultMaxAlive = 220;
        public const float DefaultUpgradeGrowth = 0.03f;

        public static float DifficultyGrowth { get; private set; }
        public static float RewardGrowth { get; private set; }
        public static int ReviveGemCost { get; private set; }
        public static int AdLimitDouble { get; private set; }
        public static int AdLimitRevive { get; private set; }
        public static int AdLimitFreeChest { get; private set; }
        public static float StageGoldMultiplier { get; private set; }
        public static float MissionRewardMultiplier { get; private set; }
        public static float SpawnDensity { get; private set; }
        public static float SpawnEarlyBoost { get; private set; }
        public static int OpeningPack { get; private set; }
        public static int MaxAlive { get; private set; }
        public static float UpgradeGrowth { get; private set; }

        static Tuning() => Reset();

        public static void Reset()
        {
            DifficultyGrowth = DefaultDifficultyGrowth;
            RewardGrowth = DefaultRewardGrowth;
            ReviveGemCost = DefaultReviveGemCost;
            AdLimitDouble = DefaultAdLimitDouble;
            AdLimitRevive = DefaultAdLimitRevive;
            AdLimitFreeChest = DefaultAdLimitFreeChest;
            StageGoldMultiplier = 1f;
            MissionRewardMultiplier = 1f;
            SpawnDensity = 1f;
            SpawnEarlyBoost = DefaultSpawnEarlyBoost;
            OpeningPack = DefaultOpeningPack;
            MaxAlive = DefaultMaxAlive;
            UpgradeGrowth = DefaultUpgradeGrowth;
        }

        /// <summary>
        /// Ghi đè từ bộ khóa/giá trị (chuỗi, số theo InvariantCulture). Khóa lạ hoặc giá trị sai kiểu/ngoài khoảng hợp lý bị bỏ qua
        /// và giữ mặc định: 1 lỗi gõ trên dashboard không được làm hỏng game của mọi người chơi. Trả số khóa đã áp.
        /// </summary>
        public static int Apply(IReadOnlyDictionary<string, string> values)
        {
            Reset();
            if (values == null) return 0;
            int applied = 0;
            foreach (KeyValuePair<string, string> kv in values)
            {
                switch (kv.Key)
                {
                    case KeyDifficultyGrowth: if (TryFloat(kv.Value, 0f, 1f, out float dg)) { DifficultyGrowth = dg; applied++; } break;
                    case KeyRewardGrowth: if (TryFloat(kv.Value, 0f, 2f, out float rg)) { RewardGrowth = rg; applied++; } break;
                    case KeyReviveGemCost: if (TryInt(kv.Value, 1, 1000, out int rc)) { ReviveGemCost = rc; applied++; } break;
                    case KeyAdLimitDouble: if (TryInt(kv.Value, 0, 50, out int ad)) { AdLimitDouble = ad; applied++; } break;
                    case KeyAdLimitRevive: if (TryInt(kv.Value, 0, 50, out int ar)) { AdLimitRevive = ar; applied++; } break;
                    case KeyAdLimitFreeChest: if (TryInt(kv.Value, 0, 50, out int ac)) { AdLimitFreeChest = ac; applied++; } break;
                    case KeyStageGoldMultiplier: if (TryFloat(kv.Value, 0.1f, 10f, out float sg)) { StageGoldMultiplier = sg; applied++; } break;
                    case KeyMissionRewardMultiplier: if (TryFloat(kv.Value, 0.1f, 10f, out float mr)) { MissionRewardMultiplier = mr; applied++; } break;
                    case KeySpawnDensity: if (TryFloat(kv.Value, 0.2f, 5f, out float sd)) { SpawnDensity = sd; applied++; } break;
                    case KeySpawnEarlyBoost: if (TryFloat(kv.Value, 0f, 5f, out float eb)) { SpawnEarlyBoost = eb; applied++; } break;
                    case KeyOpeningPack: if (TryInt(kv.Value, 0, 60, out int op)) { OpeningPack = op; applied++; } break;
                    case KeyMaxAlive: if (TryInt(kv.Value, 30, 400, out int ma)) { MaxAlive = ma; applied++; } break;
                    case KeyUpgradeGrowth: if (TryFloat(kv.Value, 0f, 0.2f, out float ug)) { UpgradeGrowth = ug; applied++; } break;
                }
            }
            return applied;
        }

        private static bool TryFloat(string s, float min, float max, out float value) =>
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= min && value <= max;

        private static bool TryInt(string s, int min, int max, out int value) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= min && value <= max;
    }
}
