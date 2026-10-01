using System;
using ArenaSurvival.Economy;
using ArenaSurvival.Stages;
using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>
    /// Áp kết quả màn vào hồ sơ: thưởng (qua EconomyService, không tự sửa số vàng), XP tài khoản, sao cao nhất, rồi lưu ngay.
    /// Scene Game không gọi class này — nó chỉ phát StageEvents.OnStageCompleted; GameServices nối sự kiện vào Apply.
    /// </summary>
    public class StageProgressService
    {
        private readonly PlayerProfile _profile;
        private readonly EconomyService _economy;
        private readonly Action _save;

        /// <summary>Lần áp gần nhất — màn kết quả đọc để hiện số thực nhận.</summary>
        public StageGrant LastGrant { get; private set; }

        public StageProgressService(PlayerProfile profile, EconomyService economy, Action save)
        {
            _profile = profile;
            _economy = economy;
            _save = save;
        }

        public StageGrant Apply(StageResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.stageId)) return null;

            bool alreadyCleared = IsCleared(result.stageId);
            float multiplier = StageProgressRules.RewardMultiplier(result.victory, alreadyCleared);
            int starsBefore = GetStars(result.stageId);

            var grant = new StageGrant
            {
                stageId = result.stageId,
                gold = StageProgressRules.ApplyMultiplier(result.gold, multiplier),
                accountXp = StageProgressRules.ApplyMultiplier(result.accountXp, multiplier),
                firstClear = result.victory && !alreadyCleared,
                multiplier = multiplier,
                starsBefore = starsBefore,
            };

            if (grant.gold > 0) _economy.Add(CurrencyIds.Gold, grant.gold);
            _profile.accountXp += grant.accountXp;

            // Chỉ màn thắng mới vào danh sách đã qua; sao chỉ tăng — chơi lại kém hơn không làm mất thành tích cũ.
            if (result.victory) _profile.stageStars[result.stageId] = Mathf.Max(starsBefore, result.starCount);
            grant.starsAfter = GetStars(result.stageId);

            LastGrant = grant;
            _save?.Invoke();
            return grant;
        }

        public bool IsCleared(string stageId) => _profile.stageStars.ContainsKey(stageId);

        public int GetStars(string stageId) => _profile.stageStars.TryGetValue(stageId, out int stars) ? stars : 0;

        /// <summary>Mở khóa tuần tự: màn đầu luôn mở, các màn sau mở khi màn ngay trước đã thắng.</summary>
        public bool IsUnlocked(StageCatalog catalog, StageData stage)
        {
            int index = catalog.IndexOf(stage);
            if (index < 0) return false;
            return index == 0 || IsCleared(catalog.stages[index - 1].id);
        }

        /// <summary>Màn nên chơi tiếp: màn chưa thắng đầu tiên; thắng hết thì màn cuối.</summary>
        public StageData NextStage(StageCatalog catalog)
        {
            if (catalog.stages == null || catalog.stages.Length == 0) return null;
            foreach (StageData stage in catalog.stages)
            {
                if (stage != null && !IsCleared(stage.id)) return stage;
            }
            return catalog.stages[catalog.stages.Length - 1];
        }
    }
}
